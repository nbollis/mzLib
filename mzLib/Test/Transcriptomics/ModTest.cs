using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Readers.Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    public class ModTest
    {
        public static string ModomicsPath = @"D:\Projects\RNA\TestData\Databases\Modomics_NaturalModifications.csv";

        [Test]
        public static void TESTNAME()
        {
            var temp = new ModomicsCsvFile(ModomicsPath);
            temp.LoadResults();
        }

    }
}
