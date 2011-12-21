using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unplugged.IbmBits.Tests
{
    [TestClass]
    public class BinaryReaderExtensionMethodsTest
    {
        #region ReadStringEbcdic

        [TestMethod]
        public void ReadEbcdicShouldConsumeRequestedBytes()
        {
            var expected = 23;
            AssertBytesConsumed(r => r.ReadStringEbcdic(expected), expected);
        }

        [TestMethod]
        public void ShouldConvertCharacters()
        {
            VerifyValueFromByteStream("J", r => r.ReadStringEbcdic(1), new byte[] { 0xD1 });
        }

        #endregion

        #region ReadInt16BigEndian()

        [TestMethod]
        public void Int16ShouldConsume2Bytes()
        {
            AssertBytesConsumed(r => r.ReadInt16BigEndian(), 2);
        }

        [TestMethod]
        public void ShouldConvertToInt16()
        {
            VerifyValueFromByteStream(2, r => r.ReadInt16BigEndian(), new byte[] { 0, 2 });
        }

        #endregion

        #region ReadInt32BigEndian()

        [TestMethod]
        public void Int32ShouldConsume4Bytes()
        {
            AssertBytesConsumed(r => r.ReadInt32BigEndian(), 4);
        }

        [TestMethod]
        public void ShouldConvertToInt32()
        {
            VerifyValueFromByteStream(3, r => r.ReadInt32BigEndian(), new byte[] { 0, 0, 0, 3 });
        }

        #endregion

        #region ReadSingle()

        [TestMethod]
        public void SingleShouldConsume4Bytes()
        {
            AssertBytesConsumed(r => r.ReadSingleIbm(), 4);
        }

        [TestMethod]
        public void ShouldConvertToSingle()
        {
            VerifyValueFromByteStream(1f, r => r.ReadSingleIbm(), new byte[] { 65, 16, 0, 0 });
        }

        #endregion

        // TODO: System.IO.EndOfStreamException: Unable to read beyond the end of the stream.

        private static void VerifyValueFromByteStream<T>(T expected, Func<BinaryReader, T> act, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                T actual = act(reader);

                // Assert
                Assert.AreEqual(expected, actual);
            }
        }

        public static void AssertBytesConsumed(Action<BinaryReader> act, int expectedNumberOfBytes)
        {
            var bytes = new byte[2 * expectedNumberOfBytes];
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                act(reader);

                // Assert
                Assert.AreEqual(expectedNumberOfBytes, stream.Position, "Wrong number of bytes were consumed.");
            }
        }
    }
}
