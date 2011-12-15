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
            var expectedNumberOfBytes = 4;
            Action<BinaryReader> act = (r) => r.ReadSingleIbm();
            AssertBytesConsumed(act, expectedNumberOfBytes);
        }

        //[TestMethod]
        //public void One()
        //{
        //    var expected = 1f;
        //    var bits = new BitArray(4 * 8);
        //    bits[8] = true;
        //    VerifyReadSingleIbm(expected, bits);
        //}

        //[TestMethod]
        //public void NegativeOne()
        //{
        //    var expected = -1f;
        //    var bits = new BitArray(4 * 8);
        //    bits[0] = true;
        //    bits[8] = true;
        //    VerifyReadSingleIbm(expected, bits);
        //}

        //[TestMethod]
        //public void Wikipedia()
        //{
        //    // Arrange
        //    var expected = -118.625f;
        //    // 1100-0010 0111-0110 1010-0000 0000-0000
        //    var bools = new bool[] { true, true, false, false, false, false, true, false, false, true, true, true, false, true, true, false, true, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, };
        //    var bits = new BitArray(bools);
        //    VerifyReadSingleIbm(expected, bits);
        //}

        //private static byte[] ReverseBits(BitArray bits)
        //{
        //    var newBits = new BitArray(32);
        //    for (int i = 0; i < 32; i++)
        //    {
        //        newBits[31 - i] = bits[i];

        //    }
        //    var newBytes = new byte[4];
        //    newBits.CopyTo(newBytes, 0);
        //    return newBytes;
        //}

        //private static void WriteBits(BitArray bits)
        //{
        //    int counter = 0;
        //    foreach (bool bit in bits)
        //    {
        //        Console.Write(bit ? "1" : "0");
        //        ++counter;
        //        if (counter % 8 == 0) Console.Write(" ");
        //    }
        //}

        [TestMethod]
        public void FromSegy()
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
