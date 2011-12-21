using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unplugged.IbmBits.Tests
{
    [TestClass]
    public class IbmConverterTest
    {
        #region ToString()

        const byte _comma = 0x6B;
        const byte _bang = 0x5A;
        const byte _H = 0xC8;
        const byte _e = 0x85;
        const byte _l = 0x93;
        const byte _o = 0x96;

        [TestMethod]
        public void ShouldConvertFromEbcdicToUnicode()
        {
            // Arrange
            var bytes = new byte[] { _H, _e, _l, _l, _o, _comma };

            // Act
            string result = IbmConverter.ToString(bytes);

            // Assert
            Assert.AreEqual("Hello,", result);
        }

        [TestMethod]
        public void ShouldConvertFromEbcdicToUnicode2()
        {
            // Arrange
            var bytes = new byte[] { _l, _o, _l, _bang };

            // Act
            string result = IbmConverter.ToString(bytes);

            // Assert
            Assert.AreEqual("lol!", result);
        }

        [TestMethod]
        public void ShouldConvertToStringGivenStartingIndex()
        {
            // Arrange
            var bytes = new byte[] { _bang, _bang, _H, _e };
            var startingIndex = 2;

            // Act
            string result = IbmConverter.ToString(bytes, startingIndex);

            // Assert
            Assert.AreEqual(result, "He");
        }

        [TestMethod]
        public void ShouldConvertToStringGivenStartingIndex2()
        {
            // Arrange
            var bytes = new byte[] { _H, _e, _l, _l, _o };
            var startingIndex = 1;

            // Act
            string result = IbmConverter.ToString(bytes, startingIndex);

            // Assert
            Assert.AreEqual(result, "ello");
        }

        [TestMethod]
        public void ShouldConvertToStringGivenIndexAndLength()
        {
            // Arrange
            var bytes = new byte[] { _H, _o, _H, _o, _H, _o, _l, _e, _bang };
            var startingIndex = 4;
            var length = 4;

            // Act
            string result = IbmConverter.ToString(bytes, startingIndex, length);

            // Assert
            Assert.AreEqual("Hole", result);
        }

        [TestMethod]
        public void ShouldConvertToStringGivenIndexAndLength2()
        {
            // Arrange
            var bytes = new byte[] { _H, _o, _H, _o, _H, _o, _l, _e, _bang };
            var startingIndex = 0;
            var length = 6;

            // Act
            string result = IbmConverter.ToString(bytes, startingIndex, length);

            // Assert
            Assert.AreEqual("HoHoHo", result);
        }

        #endregion

        #region ToInt16()

        [TestMethod]
        public void ShouldConvertZeroInt16()
        {
            // Arrange
            var bytes = new byte[2];

            // Act
            Int16 result = IbmConverter.ToInt16(bytes);

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void ShouldConvertNegativeInt16()
        {
            // Arrange
            var bytes = new byte[] { 0xAB, 0xCD };

            // Act
            var result = IbmConverter.ToInt16(bytes);

            // Assert
            Assert.AreEqual(-21555, result);
        }

        [TestMethod]
        public void ShouldIgnoreTrailingBytesInt16()
        {
            // Arrange
            var bytes = new byte[] { 0, 1, 99, 99 };

            // Act
            var result = IbmConverter.ToInt16(bytes);

            // Assert
            Assert.AreEqual(1, result);
        }

        #endregion

        #region ToInt32()

        [TestMethod]
        public void ShouldConvertZeroInt32()
        {
            // Arrange
            var bytes = new byte[4];

            // Act
            Int32 result = IbmConverter.ToInt32(bytes);

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void ShouldConvertNegativeInt32()
        {
            // Arrange
            var bytes = new byte[] { 0x89, 0xAB, 0xCD, 0xEF };

            // Act
            var result = IbmConverter.ToInt32(bytes);

            // Assert
            Assert.AreEqual(-1985229329, result);
        }

        [TestMethod]
        public void ShouldIgnoreTrailingBytesInt32()
        {
            // Arrange
            var bytes = new byte[] { 0, 0, 0, 1, 99, 99 };

            // Act
            var result = IbmConverter.ToInt32(bytes);

            // Assert
            Assert.AreEqual(1, result);
        }

        #endregion

        #region ToSingle()

        private static void VerifyToSingleReturns(float expected, byte[] bytes)
        {
            // Act
            float result = IbmConverter.ToSingle(bytes);

            // Assert
            Assert.AreEqual(expected, result, 0.0001);
        }

        [TestMethod]
        public void ZeroShouldBeTheSame()
        {
            float expected = 0.0f;
            var bytes = BitConverter.GetBytes(expected);
            VerifyToSingleReturns(expected, bytes);
        }

        [TestMethod]
        public void One()
        {
            var expected = 1f;
            var bytes = new byte[4];
            bytes[0] = 64 + 1; // 16^1 with bias of 64
            bytes[1] = 16;     // 16 to the right of the decimal 
            VerifyToSingleReturns(expected, bytes);
        }

        [TestMethod]
        public void NegativeOne()
        {
            var expected = -1f;
            var bytes = new byte[4];
            bytes[0] = 128 + 64 + 1; // +128 for negative sign in first bit
            bytes[1] = 16;           // 16 to the right of the decimal 
            VerifyToSingleReturns(expected, bytes);
        }

        [TestMethod]
        public void SampleValueFromSegy()
        {
            var bytes = new byte[] { 0xc0, 0x1f, 0xf4, 0x62 };
            VerifyToSingleReturns(-0.1248f, bytes);
        }

        [TestMethod]
        public void SampleValueFromWikipedia()
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
            var bytes = new byte[4];
            bits.CopyTo(bytes, 0);

            VerifyToSingleReturns(expected, bytes);
        }

        #endregion

        // TODO: Support for running on Big Endian architecture
    }
}
