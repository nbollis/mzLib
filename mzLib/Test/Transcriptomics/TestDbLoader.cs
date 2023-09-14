using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    internal class TestDbLoader
    {
        public static string ModomicsUnmodifedFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta");


        [Test]
        public static void TestModomicsUnmodifiedFasta()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaPath, true, DecoyType.None, false,
                out var errors);
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(oligos.Count, Is.EqualTo(5));
            Assert.That(oligos.First().BaseSequence,
                Is.EqualTo("GGGGCUAUAGCUCAGCUGGGAGAGCGCCUGCUUUGCACGCAGGAGGUCUGCGGUUCGAUCCCGCAUAGCUCCACCA"));

        }

        [Test]
        public static void TestModomicsModifiedFasta()
        {

        }

        [Test]
        public static void Test__Fna()
        {

        }
    }
}
