using System;
using System.Collections;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class BinaryReaderExtensionMethodsTest
    {
        [TestMethod]
        public void ZeroShouldBeTheSame()
        {
            float expected = 0.0f;
            var bytes = BitConverter.GetBytes(expected);
            VerifyReadSingleIbm(expected, bytes);
        }

        [TestMethod]
        public void ShouldConsume4Bytes()
        {
            AssertBytesConsumed(r => r.ReadSingleIbm(), 4);
        }

        [TestMethod]
        public void One()
        {
            var expected = 1f;
            var bytes = new byte[4];
            bytes[0] = 64 + 1; // 16^1 with bias of 64
            bytes[1] = 16;     // 16 to the right of the decimal 
            VerifyReadSingleIbm(expected, bytes);
        }

        [TestMethod]
        public void NegativeOne()
        {
            var expected = -1f;
            var bytes = new byte[4];
            bytes[0] = 128 + 64 + 1; // +128 for negative sign in first bit
            bytes[1] = 16;           // 16 to the right of the decimal 
            VerifyReadSingleIbm(expected, bytes);
        }

        [TestMethod]
        public void Wikipedia()
        {
            // This test comes from the example described here: http://en.wikipedia.org/wiki/IBM_Floating_Point_Architecture#An_Example
            // The difference is the bits have to be reversed per byte because the highest order bit is on the right
            // Arrange
            var expected = -118.625f;
            // 0100 0011 0110 1110 0000 0101 0000 0000
            var bools = new bool[] 
            {
                false, true, false, false,  false, false, true, true,  
                false, true, true, false,  true, true, true, false,  
                false, false, false, false,  false, true, false, true,  
                false, false, false, false,  false, false, false, false, 
            };
            var bits = new BitArray(bools);
            VerifyReadSingleIbm(expected, bits);
        }

        [TestMethod]
        public void SampleValueFromSegy()
        {
            // Arrange
            var bytes = new byte[] { 0xc0, 0x1f, 0xf4, 0x62 };

            // Act
            var ibm = BinaryReaderExtensionMethods.ConvertFromIbmToIeee(bytes);

            // Assert
            Assert.AreEqual(-0.1248, ibm, 0.0001);
        }

        private static void VerifyReadSingleIbm(float expected, BitArray bits)
        {
            var bytes = new byte[4];
            bits.CopyTo(bytes, 0);
            VerifyReadSingleIbm(expected, bytes);
        }

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

        internal static void AssertBytesConsumed(Action<BinaryReader> act, int expectedNumberOfBytes)
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
