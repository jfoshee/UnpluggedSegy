using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDrivenDesign;

namespace Unplugged.IbmBits.Tests
{
    [TestClass]
    public class IbmConverterTest : TestBase<IbmConverter>
    {
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
    }
}
