using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MzLibUtil;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    internal class TestDbLoader
    {
        public static string ModomicsUnmodifedFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta");
        public static string EnsembleFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/TestEnsembleFasta_Homo_sapiens.GRCh38.ncrna.fasta");

        [Test]
        public static void TestModomicsUnmodifiedFasta()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaPath, true, DecoyType.None, false,
                out var errors);
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(oligos.Count, Is.EqualTo(5));
            Assert.That(oligos.First().BaseSequence,
                Is.EqualTo("GGGGCUAUAGCUCAGCUGGGAGAGCGCCUGCUUUGCACGCAGGAGGUCUGCGGUUCGAUCCCGCAUAGCUCCACCA"));
            var first = oligos.First();

            var expectedAdditionalDatabaseFieldsFirst = new Dictionary<string, string>()
            {
                {"Id", "1"},
                {"Type", "tRNA"},
                {"Subtype", "Ala"},
                {"Feature", "VGC"},
                {"Cellular Localization", "prokaryotic cytosol"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            var b = first.AdditionalDatabaseFields;
            Assert.That(b.Count, Is.EqualTo(first.AdditionalDatabaseFields!.Count));
            for (int i = 0; i < expectedAdditionalDatabaseFieldsFirst.Count; i++)
            {
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Keys.ElementAt(i),  Is.EqualTo(first.AdditionalDatabaseFields.Keys.ElementAt(i)));
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Values.ElementAt(i),  Is.EqualTo(first.AdditionalDatabaseFields.Values.ElementAt(i)));
            }

            Assert.That(oligos.First().Name, Is.EqualTo("tdbR00000010"));
            Assert.That(oligos.First().Accession, Is.EqualTo("SO:0000254"));
            Assert.That(oligos.First().Organism, Is.EqualTo("Escherichia coli"));
            Assert.That(oligos.First().DatabaseFilePath, Is.EqualTo(ModomicsUnmodifedFastaPath));
            Assert.That(oligos.First().IsContaminant, Is.False);
            Assert.That(oligos.First().IsDecoy, Is.False);
            Assert.That(oligos.First().AdditionalDatabaseFields!.Count, Is.EqualTo(5));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Id"], Is.EqualTo("1"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Type"], Is.EqualTo("tRNA"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Subtype"], Is.EqualTo("Ala"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Feature"], Is.EqualTo("VGC"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Cellular Localization"], Is.EqualTo("prokaryotic cytosol"));
        }


        [Test]
        public static void TestEnsembleFasta_ncrna_fa()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "TestEnsembleFasta_Homo_sapiens.GRCh38.ncrna.fa");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(4));

            var first = rna[0];
            var last = rna[3];

            string firstSequence = first.BaseSequence;
            Assert.That(firstSequence, Is.EqualTo("CAUGAAUAAAGUUGUCGUGUAUAAAAUUUAACCUAGUUAUGUCUC" +
                                                       "GUCUAUUCGUACCAAUGACGGAUCCCUACCGUGUGUUUAAGUCUUUCGUAAGGUAUAAAAC"));
            
            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("ENSG00000278625.1 U6 spliceosomal RNA"));
            Assert.That(first.Accession, Is.EqualTo("RF00026"));
            Assert.That(first.Organism, Is.EqualTo("N/A"));

            var expectedAdditionalDatabaseFieldsFirst = new Dictionary<string, string>()
            {
                {"ENST", "ENST00000616830.1"},
                {"ncrna scaffold", "GRCh38:KI270744.1:51009:51114:-1"},
                {"gene_biotype", "snRNA"},
                {"transcript_biotype", "snRNA"},
                {"gene_symbol", "U6"},
                {"RNA_Source", "RFAM"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsFirst, first.AdditionalDatabaseFields);



            // repeat for the last entry
            string originalSequance =
                "CACTTTACCTGGCAGGGGAGAGACCGTGGTCACGAAGGGGGTTCTCCCAGAGTGAAGCTT" +
                "CTTCATCGCACTCTAGAGTTGCTGATTCCTGTGATTTCCTCCATGTGGGAAACGGTGTTT" +
                "GTGCTAGAAGAGGCTGCGCTCTTT";
            string expectedTranscribe = originalSequance.Transcribe(false);
            Assert.That(last.BaseSequence, Is.EqualTo(expectedTranscribe));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(last.Name, Is.EqualTo("ENSG00000275987.1 U1 spliceosomal RNA"));
            Assert.That(last.Accession, Is.EqualTo("RF00003"));
            Assert.That(first.Organism, Is.EqualTo("N/A"));

            var expectedAdditionalDatabaseFieldsLast = new Dictionary<string, string>()
            {
                {"ENST", "ENST00000618083.1"},
                {"ncrna scaffold", "GRCh38:KI270713.1:30437:30580:-1"},
                {"gene_biotype", "snRNA"},
                {"transcript_biotype", "snRNA"},
                {"gene_symbol", "U1"},
                {"RNA_Source", "RFAM"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsLast, last.AdditionalDatabaseFields);
        }



        [Test]
        public static void TestContaminantFollowsThrough()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaPath, true, DecoyType.None, true,
                               out var errors);
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(oligos.Count, Is.EqualTo(5));
            Assert.That(oligos.First().BaseSequence,
                               Is.EqualTo("GGGGCUAUAGCUCAGCUGGGAGAGCGCCUGCUUUGCACGCAGGAGGUCUGCGGUUCGAUCCCGCAUAGCUCCACCA"));
            Assert.That(oligos.All(p => p.IsContaminant));
            Assert.That(oligos.All(p => !p.IsDecoy));
        }

        [Test]
        public static void TestNotGeneratingTargetsOrDecoys()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaPath, false, DecoyType.None, true,
                out var errors);
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(oligos.Count, Is.EqualTo(0));
        }

        [Test]
        public static void TestModomicsModifiedFasta()
        {

        }

        [Test]
        public static void Test__Fna()
        {

        }

        private static IEnumerable<(string, RnaFastaHeaderType)> DetectHeaderTestCases =>
            new List<(string, RnaFastaHeaderType)>
            {
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "DoubleProtease.tsv"), RnaFastaHeaderType.Unknown),
                (ModomicsUnmodifedFastaPath, RnaFastaHeaderType.Modomics),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/TestEnsembleFasta_Homo_sapiens.GRCh38.ncrna.fa"), RnaFastaHeaderType.Ensemble),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta"), RnaFastaHeaderType.Modomics)
            };

        [Test]
        [TestCaseSource(nameof(DetectHeaderTestCases))]
        public static void TestDetectHeaderType((string dbPath, RnaFastaHeaderType headerType) testData)
        {
            string line = File.ReadLines(testData.dbPath).First();
            var type = RnaDbLoader.DetectFastaHeaderType(line);
            Assert.AreEqual(testData.headerType, type);
        }
    }
}
