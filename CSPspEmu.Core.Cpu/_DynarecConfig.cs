﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSPspEmu.Core.Cpu
{
	public class _DynarecConfig
	{
		public const bool FunctionCallWithStaticReferences = true;
		//public const bool FunctionCallWithStaticReferences = false;

		public const bool AllowFastMemory = true;
		public const bool EMIT_CALL_TICK = true;
		public const bool ENABLE_TAIL_CALL = true;
		public const bool BRANCH_FLAG_AS_LOCAL = true;

	}
}