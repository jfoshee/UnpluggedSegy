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
    }
}
