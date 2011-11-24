﻿using CSPspEmu.Hle.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CSPspEmu.Hle.Vfs;
using CSPspEmu.Hle.Vfs.Local;

namespace CSPspEmu.Core.Tests
{
	[TestClass]
	public class HleIoManagerTest
	{
		[TestMethod]
		public void ParsePathTest()
		{
			var HleIoManager = new HleIoManager(new PspEmulatorContext(new PspConfig()));
			var DriverName = "ms:";
			var Driver = new HleIoDriverLocalFileSystem("C:/$INVALID$PATH$");
			HleIoManager.AddDriver(DriverName, Driver);

			var Parts = HleIoManager.ParsePath("ms3:/path/to/file.txt");

			Assert.AreEqual(Driver, Parts.HleIoDrvFileArg.HleIoDriver);
			Assert.AreEqual(3, Parts.HleIoDrvFileArg.FileSystemNumber);
			Assert.AreEqual("/path/to/file.txt", Parts.LocalPath);
		}
	}
}
