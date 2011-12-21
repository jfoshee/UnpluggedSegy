using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unplugged.IbmBits.Tests
{
    [TestClass]
    public class BinaryReaderExtensionMethodsTest
    {
        #region ReadSingle()

        [TestMethod]
        public void ShouldConsume4Bytes()
        {
            AssertBytesConsumed(r => r.ReadSingleIbm(), 4);
        }

        [TestMethod]
        public void ZeroShouldBeTheSame()
        {
            float expected = 0.0f;
            var bytes = BitConverter.GetBytes(expected);
            VerifyReadSingleIbm(expected, bytes);
        }

        [TestMethod]
        public void ShouldConvertFromIbmBitsExample()
        {
            var bytes = new byte[] { 0xc0, 0x1f, 0xf4, 0x62 };
            var expected = -0.124822736f;
            VerifyReadSingleIbm(expected, bytes);
        }

        // TODO: System.IO.EndOfStreamException: Unable to read beyond the end of the stream.

        private static void VerifyReadSingleIbm(float expected, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                float actual = reader.ReadSingleIbm();

                // Assert
                Assert.AreEqual(expected, actual);
            }
        }

        #endregion

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
