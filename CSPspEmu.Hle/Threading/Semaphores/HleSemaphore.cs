﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSPspEmu.Hle.Threading.Semaphores;
using CSharpUtils;

namespace CSPspEmu.Hle.Threading.Semaphores
{
	unsafe public class HleSemaphore
	{
		public class WaitingSemaphoreThread
		{
			public HleThread HleThread;
			public int ExpectedMinimumCount;
			public WakeUpCallbackDelegate WakeUpCallback;
		}

		public SceKernelSemaInfo SceKernelSemaInfo;
		protected List<WaitingSemaphoreThread> WaitingSemaphoreThreadList = new List<WaitingSemaphoreThread>();
		//public SortedSet<>

		protected int CurrentCount { get { return SceKernelSemaInfo.CurrentCount; } set { SceKernelSemaInfo.CurrentCount = value; } }

		public HleSemaphore()
		{
			SceKernelSemaInfo.Size = sizeof(SceKernelSemaInfo);
		}

		public String Name
		{
			get
			{
				fixed (byte* NamePtr = SceKernelSemaInfo.Name) return PointerUtils.PtrToString(NamePtr, Encoding.ASCII);
			}
			set
			{
				fixed (byte* NamePtr = SceKernelSemaInfo.Name) PointerUtils.StoreStringOnPtr(value, Encoding.ASCII, NamePtr);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="IncrementCount"></param>
		/// <returns>Number of awaken threads</returns>
		public int IncrementCount(int IncrementCount)
		{
			CurrentCount += IncrementCount;
			CurrentCount = Math.Min(CurrentCount, SceKernelSemaInfo.MaximumCount);

			return UpdatedCurrentCount();
		}

		public int WaitThread(HleThread HleThread, WakeUpCallbackDelegate WakeUpCallback, int ExpectedMinimumCount)
		{
			WaitingSemaphoreThreadList.Add(
				new WaitingSemaphoreThread()
				{
					HleThread = HleThread,
					ExpectedMinimumCount = ExpectedMinimumCount,
					WakeUpCallback = WakeUpCallback,
				}
			);

			return UpdatedCurrentCount();
		}

		protected int UpdatedCurrentCount()
		{
			// Selects all the waiting semaphores, that fit the count condition, in a FIFO order.
			var WaitingSemaphoreThreadIterator = WaitingSemaphoreThreadList
				//.Reverse()
				//.Where(WaitingThread => (CurrentCount >= WaitingThread.ExpectedMinimumCount))
				.AsEnumerable()
				.Reverse()
			;

			// Reorders the waiting semaphores in a Thread priority order (descending).
			if (SceKernelSemaInfo.Attributes == SemaphoreAttribute.Priority)
			{
				WaitingSemaphoreThreadIterator = WaitingSemaphoreThreadIterator
					.OrderByDescending(WaitingThread => WaitingThread.HleThread.PriorityValue)
				;
			}

			int AwakenCount = 0;

			// Iterates all the waiting semaphores in order removing them from list.
			foreach (var WaitingSemaphoreThread in WaitingSemaphoreThreadIterator.ToArray())
			{
				//Console.WriteLine("if (CurrentCount >= WaitingSemaphoreThread.ExpectedMinimumCount) {0}, {1}", CurrentCount, WaitingSemaphoreThread.ExpectedMinimumCount);
				if (CurrentCount >= WaitingSemaphoreThread.ExpectedMinimumCount)
				{
					CurrentCount -= WaitingSemaphoreThread.ExpectedMinimumCount;
					WaitingSemaphoreThreadList.Remove(WaitingSemaphoreThread);
					WaitingSemaphoreThread.WakeUpCallback();
					AwakenCount++;
				}
			}

			// Updates the statistic about waiting threads.
			SceKernelSemaInfo.NumberOfWaitingThreads = WaitingSemaphoreThreadList.Count;

			return AwakenCount;
		}

		public void Release()
		{
		}
	}
}