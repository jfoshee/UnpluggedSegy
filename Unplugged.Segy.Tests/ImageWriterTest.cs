using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestDrivenDesign;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class ImageWriterTest : TestBase<ImageWriter>
    {
        [TestMethod]
        public void ShouldCreateImageFile()
        {
            // Arrange
            var path = TestPath() + ".png";
            ISegyFile segyFile = new Mock<ISegyFile>().Object;

            // Act
            Subject.Write(segyFile, path);

            // Assert
            BinaryFileAssert.Exists(path);
            var image = Bitmap.FromFile(path);
            Assert.AreEqual(1, image.Width);
            Assert.AreEqual(1, image.Height);
        }
    }
}
