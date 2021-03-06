﻿using System;
using System.Collections.Generic;
using System.Threading;
using CSPspEmu.Core.Gpu.State;
using CSPspEmu.Core.Memory;
using CSPspEmu.Core.Threading.Synchronization;
using CSharpUtils.Extensions;

namespace CSPspEmu.Core.Gpu
{
    public unsafe class GpuProcessor : IInjectInitialize
    {
        /*
         *   - GU_SYNC_FINISH - 0 - Wait until the last sceGuFinish command is reached
         *   - GU_SYNC_SIGNAL - 1 - Wait until the last (?) signal is executed
         *   - GU_SYNC_DONE   - 2 - Wait until all commands currently in list are executed
         *   - GU_SYNC_LIST   - 3 - Wait for the currently executed display list (GU_DIRECT)
         *   - GU_SYNC_SEND   - 4 - Wait for the last send list
         *   
         *   int sceGuSync(int mode, SyncTypeEnum what)
         *	 {
         *		 switch (mode)
         *		 {
         *			 case GU_SYNC_FINISH: return sceGeDrawSync(what);
         *			 case GU_SYNC_LIST  : return sceGeListSync(ge_list_executed[0], what);
         *			 case GU_SYNC_SEND  : return sceGeListSync(ge_list_executed[1], what);
         *		 	 default: case GU_SYNC_SIGNAL: case GU_SYNC_DONE: return 0;
         *	 	 }
         *	 }
         */
        /// <summary>
        /// Wait conditions for sceGeListSync() and sceGeDrawSync()
        /// </summary>
        public enum SyncTypeEnum : uint
        {
            /// <summary>
            /// 
            /// </summary>
            ListDone = 0,

            /// <summary>
            /// 
            /// </summary>
            ListQueued = 1,

            /// <summary>
            /// 
            /// </summary>
            ListDrawingDone = 2,

            /// <summary>
            /// 
            /// </summary>
            ListStallReached = 3,

            /// <summary>
            /// 
            /// </summary>
            ListCancelDone = 4,
        }

        /// <summary>
        /// 
        /// </summary>
        public GlobalGpuState GlobalGpuState = new GlobalGpuState();

        /// <summary>
        /// 
        /// </summary>
        internal volatile LinkedList<GpuDisplayList> DisplayListQueue;

        /// <summary>
        /// 
        /// </summary>
        public volatile AutoResetEvent DisplayListQueueUpdated = new AutoResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        protected volatile Queue<GpuDisplayList> DisplayListFreeQueue;

        public const int DisplayListsCount = 64;

        /// <summary>
        /// All the supported Psp Display Lists (Available and not available).
        /// </summary>
        private readonly GpuDisplayList[] DisplayLists = new GpuDisplayList[DisplayListsCount];

        public GpuDisplayList GetDisplayList(int Index)
        {
            lock (DisplayLists) return DisplayLists[Index];
            //return DisplayLists[Index];
        }

        public enum Status2Enum
        {
            Completed = 0,
            HavePendingLists = 1,
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly WaitableStateMachine<Status2Enum> Status2 =
            new WaitableStateMachine<Status2Enum>(Status2Enum.Completed, Debug: false);

        /// <summary>
        /// 
        /// </summary>
        [Inject] public GpuImpl GpuImpl;

        /// <summary>
        /// 
        /// </summary>
        [Inject] public GpuConfig GpuConfig;

        /// <summary>
        /// 
        /// </summary>
        [Inject] public PspMemory Memory;

        /// <summary>
        /// 
        /// </summary>
        [Inject] public IGpuConnector Connector;

        private GpuProcessor()
        {
        }

        void IInjectInitialize.Initialize()
        {
            DisplayListQueue = new LinkedList<GpuDisplayList>();
            DisplayListFreeQueue = new Queue<GpuDisplayList>();
            for (int n = 0; n < DisplayListsCount; n++)
            {
                var DisplayList = new GpuDisplayList(Memory, this, n);
                DisplayLists[n] = DisplayList;
                //this.DisplayListFreeQueue.Enqueue(DisplayLists[n]);
                EnqueueFreeDisplayList(DisplayLists[n]);
            }
        }

        AutoResetEvent DisplayListFreeEvent = new AutoResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GpuDisplayList DequeueFreeDisplayList()
        {
            lock (DisplayListFreeQueue)
            {
                var DisplayList = DisplayListFreeQueue.Dequeue();
                DisplayList.Available = false;
                return DisplayList;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="GpuDisplayList"></param>
        public void EnqueueFreeDisplayList(GpuDisplayList GpuDisplayList)
        {
            //Console.WriteLine("EnqueueFreeDisplayList: {0}", this.DisplayListFreeQueue.Count);
            lock (DisplayListFreeQueue)
            {
                DisplayListFreeQueue.Enqueue(GpuDisplayList);
                GpuDisplayList.SetFree();
            }
            DisplayListFreeEvent.Set();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DisplayList"></param>
        public void EnqueueDisplayListFirst(GpuDisplayList DisplayList)
        {
            //Console.WriteLine("EnqueueDisplayListFirst: {0}", this.DisplayListFreeQueue.Count);
            AddedDisplayList();
            lock (DisplayListQueue)
            {
                DisplayListQueue.AddFirst(DisplayList);
                DisplayList.SetQueued();
            }
            DisplayListQueueUpdated.Set();
            ListEnqueuedEvent.Set();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DisplayList"></param>
        public void EnqueueDisplayListLast(GpuDisplayList DisplayList)
        {
            //Console.WriteLine("EnqueueDisplayListLast: {0}", this.DisplayListFreeQueue.Count);
            AddedDisplayList();
            lock (DisplayListQueue)
            {
                DisplayListQueue.AddLast(DisplayList);
                DisplayList.SetQueued();
            }
            DisplayListQueueUpdated.Set();
            ListEnqueuedEvent.Set();
        }


        public void ProcessInit()
        {
        }

        public AutoResetEvent ListEnqueuedEvent = new AutoResetEvent(false);

        private volatile GpuDisplayList CurrentGpuDisplayList = null;
        private volatile GpuDisplayList LastProcessedGpuDisplayList = null;
        public bool UsingGe { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void ProcessStep()
        {
            CurrentGpuDisplayList = null;

            if (DisplayListQueue.GetCountLock() > 0)
            {
                UsingGe = true;
                while (DisplayListQueue.GetCountLock() > 0)
                {
                    CurrentGpuDisplayList = DisplayListQueue.RemoveFirstAndGet();
                    CurrentGpuDisplayList.SetDequeued();
                    LastProcessedGpuDisplayList = CurrentGpuDisplayList;
                    CurrentGpuDisplayList.Process();
                    EnqueueFreeDisplayList(CurrentGpuDisplayList);
                }
                CurrentGpuDisplayList = null;

                Status2.SetValue(Status2Enum.Completed);
            }
        }

        protected void AddedDisplayList()
        {
            //Console.WriteLine("Running");
            Status2.SetValue(Status2Enum.HavePendingLists);
            GpuImpl.AddedDisplayList();
        }

        internal bool Syncing = false;

        public void GeDrawSync(Action SyncCallback)
        {
            Syncing = true;
            Status2.CallbackOnStateOnce(Status2Enum.Completed, () =>
            {
                CapturingWaypoint();
                GpuImpl.Sync(LastProcessedGpuDisplayList.GpuStateStructPointer);
                SyncCallback();
                Syncing = false;
            });
        }

        private void CapturingWaypoint()
        {
            if (CapturingFrame)
            {
                CapturingFrame = false;
                Console.WriteLine("EndCapturingFrame!");
                GpuImpl.EndCapture();
            }

            if (StartCapturingFrame)
            {
                StartCapturingFrame = false;
                CapturingFrame = true;
                GpuImpl.StartCapture();
                Console.WriteLine("StartCapturingFrame!");
            }
        }

        internal void MarkDepthBufferLoad()
        {
            //throw new NotImplementedException();
        }

        public void SetCurrent()
        {
            GpuImpl.SetCurrent();
        }

        public void UnsetCurrent()
        {
            GpuImpl.UnsetCurrent();
        }

        bool StartCapturingFrame = false;
        bool CapturingFrame = false;

        public void CaptureFrame()
        {
            StartCapturingFrame = true;
            Console.WriteLine("Waiting StartCapturingFrame!");
        }

        public int GeContinue()
        {
            throw new NotImplementedException();
        }

        public GpuDisplayList GetCurrentGpuDisplayList()
        {
            return CurrentGpuDisplayList;
        }

        public bool IsBreak = false;

        public DisplayListStatusEnum PeekStatus()
        {
            var GpuDisplayList = CurrentGpuDisplayList;
            if (GpuDisplayList == null) return DisplayListStatusEnum.Completed;
            return GpuDisplayList.PeekStatus();
        }
    }

    public enum TextureLevelMode
    {
        Auto = 0,
        Const = 1,
        Slope = 2
    }
}