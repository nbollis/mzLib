using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using NUnit.Framework;
using Readers;

namespace Test.FileReadingTests
{
    [TestFixture]
    public class TestMsAlign
    {

        [Test]
        [TestCase("@DataFiles/LVS_jurkat_td_rep2_fract2-calib-averaged-centroided_ms1.msalign")]
        public void FirstTestMsAlign(string filePath)
        {
            string spectraPath = Path.Combine(TestContext.CurrentContext.TestDirectory, filePath);
            MsDataFile datafile = MsDataFileReader.GetDataFile(spectraPath);
            var t = datafile.LoadAllStaticData();
        }
    }
}
