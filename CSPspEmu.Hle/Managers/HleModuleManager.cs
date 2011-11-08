﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSPspEmu.Core.Cpu;
using CSPspEmu.Core.Cpu.Cpu.Emiter;

namespace CSPspEmu.Hle.Managers
{
	public class HleModuleManager
	{
		protected Dictionary<Type, HleModuleHost> HleModules = new Dictionary<Type, HleModuleHost>();
		public HleState HleState;
		public uint DelegateLastId = 0;
		public Dictionary<uint, Action<CpuThreadState>> DelegateTable = new Dictionary<uint, Action<CpuThreadState>>();

		static public IEnumerable<Type> GetAllHleModules()
		{
			var FindType = typeof(HleModuleHost);
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(Assembly => Assembly.GetTypes())
				.Where(Type => FindType.IsAssignableFrom(Type))
			;
		}

		static private Dictionary<String, Type> _HleModuleTypes;
		static public Dictionary<String, Type> HleModuleTypes
		{
			get {
				if (_HleModuleTypes == null)
				{
					_HleModuleTypes = GetAllHleModules().ToDictionary(Type => Type.Name);
				}
				return _HleModuleTypes;
			}
		}

		public HleModuleManager(HleState HleState)
		{
			this.HleState = HleState;
			HleState.Processor.RegisterNativeSyscall(FunctionGenerator.NativeCallSyscallCode, (Code, CpuThreadState) =>
			{
				uint Info = CpuThreadState.Processor.Memory.Read4(CpuThreadState.PC + 4);
				{
					Console.WriteLine("{0:X}", CpuThreadState.RA);
					DelegateTable[Info](CpuThreadState);
				}
				CpuThreadState.PC = CpuThreadState.RA;
			});
		}

		public TType GetModule<TType>() where TType : HleModuleHost
		{
			return (TType)GetModuleByType(typeof(TType));
		}

		public HleModuleHost GetModuleByType(Type Type)
		{
			if (!HleModules.ContainsKey(Type))
			{
				var HleModule = HleModules[Type] = (HleModuleHost)Activator.CreateInstance(Type);
				HleModule.Initialize(HleState);
			}

			return (HleModuleHost)HleModules[Type];
		}

		public HleModuleHost GetModuleByName(String ModuleNameToFind)
		{
			if (!HleModuleTypes.ContainsKey(ModuleNameToFind))
			{
				throw (new KeyNotFoundException("Can't find module '" + ModuleNameToFind + "'"));
			}
			return GetModuleByType(HleModuleTypes[ModuleNameToFind]);
		}

		public Action<CpuThreadState> GetModuleDelegate<TType>(String FunctionName) where TType : HleModuleHost
		{
			return GetModule<TType>().DelegatesByName[FunctionName];
		}

		public uint AllocDelegateSlot(Action<CpuThreadState> Action, string Info)
		{
			uint DelegateId = DelegateLastId++;
			if (Action == null)
			{
				Action = (CpuThreadState) =>
				{
					throw (new NotImplementedException("Not Implemented Syscall '" + Info + "'"));
				};
			}
			DelegateTable[DelegateId] = Action;
			return DelegateId;
		}
	}
}