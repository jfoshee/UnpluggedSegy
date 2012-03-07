using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDrivenDesign;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class SegyOptionsTest : TestBase<SegyOptions>
    {
        [TestMethod]
        public void ImplementsInterface()
        {
            Assert.IsInstanceOfType(Subject, typeof(ISegyOptions));
        }

        [TestMethod]
        public void DefaultValues()
        {
            Assert.AreEqual(true, Subject.IsEbcdic);
            //Assert.AreEqual(false, Subject.IsLittleEndian);

            Assert.AreEqual(80, Subject.TextHeaderColumnCount);
            Assert.AreEqual(40, Subject.TextHeaderRowCount);
            Assert.AreEqual(true, Subject.TextHeaderInsertNewLines);

            //Assert.AreEqual(400, Subject.BinaryHeaderLength);
            //Assert.AreEqual(25, Subject.BinaryHeaderLocationForSampleFormat);

            //Assert.AreEqual(240, Subject.TraceHeaderLength);
            //Assert.AreEqual(115, Subject.TraceHeaderLocationForSampleCount);
            //Assert.AreEqual(189, Subject.TraceHeaderLocationForInlineNumber, "According to SEGY Rev 1, byte 189 - 192 in the trace header should be used for the in-line number");
            //Assert.AreEqual(193, Subject.TraceHeaderLocationForCrosslineNumber, "According to SEGY Rev 1, byte 193 - 196 in the trace header should be used for the cross-line number");
        }
    }
}
