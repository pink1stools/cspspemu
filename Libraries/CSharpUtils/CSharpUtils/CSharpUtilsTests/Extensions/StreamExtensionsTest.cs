﻿using System.Linq;
using System.IO;
using CSharpUtils.Extensions;
using Xunit;


namespace CSharpUtilsTests.Extensions
{
    
    public class StreamExtensionsTest
    {
        struct Test
        {
#pragma warning disable 649
            public uint Uint;
            public string String;
#pragma warning restore 649
        }

        [Fact]
        public void TestReadManagedStruct()
        {
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write((uint) 0x12345678);
            binaryWriter.Write((byte) 'H');
            binaryWriter.Write((byte) 'e');
            binaryWriter.Write((byte) 'l');
            binaryWriter.Write((byte) 'l');
            binaryWriter.Write((byte) 'o');
            binaryWriter.Write((byte) 0);
            memoryStream.Position = 0;
            var test = memoryStream.ReadManagedStruct<Test>();
            Assert.Equal(0x12345678U, test.Uint);
            Assert.Equal("Hello", test.String);
        }

        struct TestShorts
        {
            public short A, B, C;

            public override string ToString()
            {
                return $"TestShorts(0x{A:X4}, 0x{B:X4}, 0x{C:X4})";
            }
        }

        [Fact]
        public void ReadStructVectorTest()
        {
            var data = new byte[]
            {
                0x01, 0x00, 0x02, 0x00, 0x03, 0x00,
                0x01, 0x01, 0x02, 0x01, 0x03, 0x01,
                0x01, 0x02, 0x02, 0x02, 0x03, 0x02,
            };
            var testShorts = (new MemoryStream(data)).ReadStructVector<TestShorts>(3);
            Assert.Equal("TestShorts(0x0001, 0x0002, 0x0003)", testShorts[0].ToString());
            Assert.Equal("TestShorts(0x0101, 0x0102, 0x0103)", testShorts[1].ToString());
            Assert.Equal("TestShorts(0x0201, 0x0202, 0x0203)", testShorts[2].ToString());
        }

        [Fact]
        public void ReadStructVectorEmptyTest()
        {
            var data = new byte[]
            {
                0x01, 0x00, 0x02, 0x00, 0x03, 0x00,
                0x01, 0x01, 0x02, 0x01, 0x03, 0x01,
                0x01, 0x02, 0x02, 0x02, 0x03, 0x02,
            };
            var testShorts = (new MemoryStream(data)).ReadStructVector<TestShorts>(0);
            Assert.Equal(0, testShorts.Length);
        }


        [Fact]
        public void WriteStructVectorTest()
        {
            var data = new byte[]
            {
                0x01, 0x00, 0x02, 0x00, 0x03, 0x00,
                0x01, 0x01, 0x02, 0x01, 0x03, 0x01,
                0x01, 0x02, 0x02, 0x02, 0x03, 0x02,
            };
            var shorts = new[]
            {
                new TestShorts() {A = 0x0001, B = 0x0002, C = 0x0003},
                new TestShorts() {A = 0x0101, B = 0x0102, C = 0x0103},
                new TestShorts() {A = 0x0201, B = 0x0202, C = 0x0203},
            };
            var memoryStream = new MemoryStream();
            memoryStream.WriteStructVector(shorts);
            Assert.Equal(
                data.ToHexString(),
                memoryStream.ToArray().ToHexString()
            );
        }

        [Fact]
        public void CountStringzBytesTest()
        {
            var stream1 = new MemoryStream(new[] {'H', 'e', 'l', 'l', 'o', (char) 0, 'W', 'o', 'r', 'l', 'd'}
                .Select(item => (byte) item).ToArray());
            var stream2 = new MemoryStream(new[] {'H', 'e', 'l', 'l', 'o', (char) 0, (char) 0, 'W', 'o', 'r', 'l', 'd'}
                .Select(item => (byte) item).ToArray());
            var stream3 =
                new MemoryStream(new[] {'H', 'e', 'l', 'l', 'o', (char) 0, (char) 0, (char) 0, 'W', 'o', 'r', 'l', 'd'}
                    .Select(item => (byte) item).ToArray());
            var stream4 = new MemoryStream(new[]
                    {'H', 'e', 'l', 'l', 'o', (char) 0, (char) 0, (char) 0, (char) 0, 'W', 'o', 'r', 'l', 'd'}
                .Select(item => (byte) item).ToArray());
            Assert.Equal(6, stream1.CountStringzBytes());
            Assert.Equal(6, stream2.CountStringzBytes());
            Assert.Equal(6, stream3.CountStringzBytes());
            Assert.Equal(6, stream4.CountStringzBytes());

            Assert.Equal(6, stream1.CountStringzBytes(alignTo4: true));
            Assert.Equal(7, stream2.CountStringzBytes(alignTo4: true));
            Assert.Equal(8, stream3.CountStringzBytes(alignTo4: true));

            // FIXME! Use Virtual Position to know when it is aligned.
            //Assert.Equal(8, stream4.CountStringzBytes(alignTo4: true));
        }
    }
}