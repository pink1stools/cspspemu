﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CSharpUtils;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace CSPspEmu.Core.Audio.Imple.Openal
{
	unsafe public class PspAudioOpenalImpl : PspAudioImpl
	{
		static protected AudioContext AudioContext;
		//static protected XRamExtension XRam;
		static internal AudioStream AudioStream;

		public PspAudioOpenalImpl(PspEmulatorContext PspEmulatorContext) : base(PspEmulatorContext)
		{
			if (AudioContext == null)
			{
				//AudioContext = new AudioContext(AudioContext.DefaultDevice, 44100, 4410);
				AudioContext = new AudioContext();
				//XRam = new XRamExtension();

				var Position = new Vector3(0, 0, 0);
				var Velocity = new Vector3(0, 0, 0);
				//var Orientation = new float[] { 0, 0, 1 };
				AL.Listener(ALListener3f.Position, ref Position);
				AL.Listener(ALListener3f.Velocity, ref Velocity);
				//AL.Listener(ALListenerfv.Orientation, ref Orientation);

				AudioStream = new AudioStream();
			}
		}

		override public void Update(Func<int, short[]> ReadStream)
		{
			AudioContext.Process();
			AudioStream.Update(ReadStream);
		}

		override public void StopSynchronized()
		{
		}
	}
}