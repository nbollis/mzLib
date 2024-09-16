using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
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
        public static string ModomicsUnmodifedFastaGzPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta.gz");
        public static string ModomicsUnmodifedFastaZipPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta.gz");
        public static string EnsembleFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Transcriptomics/TestData/TestEnsembleFasta_Homo_sapiens.GRCh38.ncrna.fasta");

        /// <summary>
        /// Test if program supports modomics
        /// </summary>
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

        /// <summary>
        /// Test if program supports modomics compressed as gz
        /// </summary>
        [Test]
        public static void TestModomicsUnmodifiedFastaGz()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaGzPath, true, DecoyType.None, false,
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
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Keys.ElementAt(i), Is.EqualTo(first.AdditionalDatabaseFields.Keys.ElementAt(i)));
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Values.ElementAt(i), Is.EqualTo(first.AdditionalDatabaseFields.Values.ElementAt(i)));
            }

            Assert.That(oligos.First().Name, Is.EqualTo("tdbR00000010"));
            Assert.That(oligos.First().Accession, Is.EqualTo("SO:0000254"));
            Assert.That(oligos.First().Organism, Is.EqualTo("Escherichia coli"));
            Assert.That(oligos.First().DatabaseFilePath, Is.EqualTo(ModomicsUnmodifedFastaGzPath));
            Assert.That(oligos.First().IsContaminant, Is.False);
            Assert.That(oligos.First().IsDecoy, Is.False);
            Assert.That(oligos.First().AdditionalDatabaseFields!.Count, Is.EqualTo(5));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Id"], Is.EqualTo("1"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Type"], Is.EqualTo("tRNA"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Subtype"], Is.EqualTo("Ala"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Feature"], Is.EqualTo("VGC"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Cellular Localization"], Is.EqualTo("prokaryotic cytosol"));
        }

        /// <summary>
        /// Test if program supports modomics compressed as zip
        /// </summary>
        [Test]
        public static void TestModomicsUnmodifiedFastaZip()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaZipPath, true, DecoyType.None, false,
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
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Keys.ElementAt(i), Is.EqualTo(first.AdditionalDatabaseFields.Keys.ElementAt(i)));
                Assert.That(expectedAdditionalDatabaseFieldsFirst.Values.ElementAt(i), Is.EqualTo(first.AdditionalDatabaseFields.Values.ElementAt(i)));
            }

            Assert.That(oligos.First().Name, Is.EqualTo("tdbR00000010"));
            Assert.That(oligos.First().Accession, Is.EqualTo("SO:0000254"));
            Assert.That(oligos.First().Organism, Is.EqualTo("Escherichia coli"));
            Assert.That(oligos.First().DatabaseFilePath, Is.EqualTo(ModomicsUnmodifedFastaZipPath));
            Assert.That(oligos.First().IsContaminant, Is.False);
            Assert.That(oligos.First().IsDecoy, Is.False);
            Assert.That(oligos.First().AdditionalDatabaseFields!.Count, Is.EqualTo(5));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Id"], Is.EqualTo("1"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Type"], Is.EqualTo("tRNA"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Subtype"], Is.EqualTo("Ala"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Feature"], Is.EqualTo("VGC"));
            Assert.That(oligos.First().AdditionalDatabaseFields!["Cellular Localization"], Is.EqualTo("prokaryotic cytosol"));
        }

        /// <summary>
        /// Test if program supports ensemble
        /// </summary>
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

        /// <summary>
        /// Test if program supports NCBIassembly
        /// </summary>
        [Test]
        public static void TestNCBIassemblyRna()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "NCBIassemblyRna.fna");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(5));

            var first = rna[0];
            var last = rna[4];

            string firstSequence = first.BaseSequence;
            string originalSequence_First =
                "GGGACCAGATGGATTGTAGGGAGTAGGGTACAATACAGTCTGTTCTCCTCCAGCTCCTTCTTTCTGCAACATGGGGAAG" +
                "AACAAACTCCTTCATCCAAGTCTGGTTCTTCTCCTCTTGGTCCTCCTGCCCACAGACGCCTCAGTCTCTGGAAAACCGC" +
                "AGTATATGGTTCTGGTCCCCTCCCTGCTCCACACTGAGACCACTGAGAAGGGCTGTGTCCTTCTGAGCTACCTGAATGA" +
                "GACAGTGACTGTAAGTGCTTCCTTGGAGTCTGTCAGGGGAAACAGGAGCCTCTTCACTGACCTGGAGGCGGAGAATGAC" +
                "GTACTCCACTGTGTCGCCTTCGCTGTCCCAAAGTCTTCATCCAATGAGGAGGTAATGTTCCTCACTGTCCAAGTGAAAG" +
                "GACCAACCCAAGAATTTAAGAAGCGGACCACAGTGATGGTTAAGAACGAGGACAGTCTGGTCTTTGTCCAGACAGACAA" +
                "ATCAATCTACAAACCAGGGCAGACAGTGAAATTTCGTGTTGTCTCCATGGATGAAAACTTTCACCCCCTGAATGAGTTG" +
                "ATTCCACTAGTATACATTCAGGATCCCAAAGGAAATCGCATCGCACAATGGCAGAGTTTCCAGTTAGAGGGTGGCCTCA" +
                "AGCAATTTTCTTTTCCCCTCTCATCAGAGCCCTTCCAGGGCTCCTACAAGGTGGTGGTACAGAAGAAATCAGGTGGAAG" +
                "GACAGAGCACCCTTTCACCGTGGAGGAATTTGTTCTTCCCAAGTTTGAAGTACAAGTAACAGTGCCAAAGATAATCACC" +
                "ATCTTGGAAGAAGAGATGAATGTATCAGTGTGTGGCCTATACACATATGGGAAGCCTGTCCCTGGACATGTGACTGTGA" +
                "GCATTTGCAGAAAGTATAGTGACGCTTCCGACTGCCACGGTGAAGATTCACAGGCTTTCTGTGAGAAATTCAGTGGACA" +
                "GCTAAACAGCCATGGCTGCTTCTATCAGCAAGTAAAAACCAAGGTCTTCCAGCTGAAGAGGAAGGAGTATGAAATGAAA" +
                "CTTCACACTGAGGCCCAGATCCAAGAAGAAGGAACAGTGGTGGAATTGACTGGAAGGCAGTCCAGTGAAATCACAAGAA" +
                "CCATAACCAAACTCTCATTTGTGAAAGTGGACTCACACTTTCGACAGGGAATTCCCTTCTTTGGGCAGGTGCGCCTAGT" +
                "AGATGGGAAAGGCGTCCCTATACCAAATAAAGTCATATTCATCAGAGGAAATGAAGCAAACTATTACTCCAATGCTACC" +
                "ACGGATGAGCATGGCCTTGTACAGTTCTCTATCAACACCACCAATGTTATGGGTACCTCTCTTACTGTTAGGGTCAATT" +
                "ACAAGGATCGTAGTCCCTGTTACGGCTACCAGTGGGTGTCAGAAGAACACGAAGAGGCACATCACACTGCTTATCTTGT" +
                "GTTCTCCCCAAGCAAGAGCTTTGTCCACCTTGAGCCCATGTCTCATGAACTACCCTGTGGCCATACTCAGACAGTCCAG" +
                "GCACATTATATTCTGAATGGAGGCACCCTGCTGGGGCTGAAGAAGCTCTCCTTCTATTATCTGATAATGGCAAAGGGAG" +
                "GCATTGTCCGAACTGGGACTCATGGACTGCTTGTGAAGCAGGAAGACATGAAGGGCCATTTTTCCATCTCAATCCCTGT" +
                "GAAGTCAGACATTGCTCCTGTCGCTCGGTTGCTCATCTATGCTGTTTTACCTACCGGGGACGTGATTGGGGATTCTGCA" +
                "AAATATGATGTTGAAAATTGTCTGGCCAACAAGGTGGATTTGAGCTTCAGCCCATCACAAAGTCTCCCAGCCTCACACG" +
                "CCCACCTGCGAGTCACAGCGGCTCCTCAGTCCGTCTGCGCCCTCCGTGCTGTGGACCAAAGCGTGCTGCTCATGAAGCC" +
                "TGATGCTGAGCTCTCGGCGTCCTCGGTTTACAACCTGCTACCAGAAAAGGACCTCACTGGCTTCCCTGGGCCTTTGAAT" +
                "GACCAGGACAATGAAGACTGCATCAATCGTCATAATGTCTATATTAATGGAATCACATATACTCCAGTATCAAGTACAA" +
                "ATGAAAAGGATATGTACAGCTTCCTAGAGGACATGGGCTTAAAGGCATTCACCAACTCAAAGATTCGTAAACCCAAAAT" +
                "GTGTCCACAGCTTCAACAGTATGAAATGCATGGACCTGAAGGTCTACGTGTAGGTTTTTATGAGTCAGATGTAATGGGA" +
                "AGAGGCCATGCACGCCTGGTGCATGTTGAAGAGCCTCACACGGAGACCGTACGAAAGTACTTCCCTGAGACATGGATCT" +
                "GGGATTTGGTGGTGGTAAACTCAGCAGGTGTGGCTGAGGTAGGAGTAACAGTCCCTGACACCATCACCGAGTGGAAGGC" +
                "AGGGGCCTTCTGCCTGTCTGAAGATGCTGGACTTGGTATCTCTTCCACTGCCTCTCTCCGAGCCTTCCAGCCCTTCTTT" +
                "GTGGAGCTCACAATGCCTTACTCTGTGATTCGTGGAGAGGCCTTCACACTCAAGGCCACGGTCCTAAACTACCTTCCCA" +
                "AATGCATCCGGGTCAGTGTGCAGCTGGAAGCCTCTCCCGCCTTCCTAGCTGTCCCAGTGGAGAAGGAACAAGCGCCTCA" +
                "CTGCATCTGTGCAAACGGGCGGCAAACTGTGTCCTGGGCAGTAACCCCAAAGTCATTAGGAAATGTGAATTTCACTGTG" +
                "AGCGCAGAGGCACTAGAGTCTCAAGAGCTGTGTGGGACTGAGGTGCCTTCAGTTCCTGAACACGGAAGGAAAGACACAG" +
                "TCATCAAGCCTCTGTTGGTTGAACCTGAAGGACTAGAGAAGGAAACAACATTCAACTCCCTACTTTGTCCATCAGGTGG" +
                "TGAGGTTTCTGAAGAATTATCCCTGAAACTGCCACCAAATGTGGTAGAAGAATCTGCCCGAGCTTCTGTCTCAGTTTTG" +
                "GGAGACATATTAGGCTCTGCCATGCAAAACACACAAAATCTTCTCCAGATGCCCTATGGCTGTGGAGAGCAGAATATGGTCCTCTTTGCTCCTAACATCTATGTACTGGATTATCTA\nAATGAAACACAGCAGCTTACTCCAGAGATCAAGTCCAAGGCCATTGGCTATCTCAACACTGGTTACCAGAGACAGTTGAA\nCTACAAACACTATGATGGCTCCTACAGCACCTTTGGGGAGCGATATGGCAGGAACCAGGGCAACACCTGGCTCACAGCCT\nTTGTTCTGAAGACTTTTGCCCAAGCTCGAGCCTACATCTTCATCGATGAAGCACACATTACCCAAGCCCTCATATGGCTC\nTCCCAGAGGCAGAAGGACAATGGCTGTTTCAGGAGCTCTGGGTCACTGCTCAACAATGCCATAAAGGGAGGAGTAGAAGA\nTGAAGTGACCCTCTCCGCCTATATCACCATCGCCCTTCTGGAGATTCCTCTCACAGTCACTCACCCTGTTGTCCGCAATG\nCCCTGTTTTGCCTGGAGTCAGCCTGGAAGACAGCACAAGAAGGGGACCATGGCAGCCATGTATATACCAAAGCACTGCTG\nGCCTATGCTTTTGCCCTGGCAGGTAACCAGGACAAGAGGAAGGAAGTACTCAAGTCACTTAATGAGGAAGCTGTGAAGAA\nAGACAACTCTGTCCATTGGGAGCGCCCTCAGAAACCCAAGGCACCAGTGGGGCATTTTTACGAACCCCAGGCTCCCTCTG\nCTGAGGTGGAGATGACATCCTATGTGCTCCTCGCTTATCTCACGGCCCAGCCAGCCCCAACCTCGGAGGACCTGACCTCT\nGCAACCAACATCGTGAAGTGGATCACGAAGCAGCAGAATGCCCAGGGCGGTTTCTCCTCCACCCAGGACACAGTGGTGGC\nTCTCCATGCTCTGTCCAAATATGGAGCAGCCACATTTACCAGGACTGGGAAGGCTGCACAGGTGACTATCCAGTCTTCAG\nGGACATTTTCCAGCAAATTCCAAGTGGACAACAACAACCGCCTGTTACTGCAGCAGGTCTCATTGCCAGAGCTGCCTGGG\nGAATACAGCATGAAAGTGACAGGAGAAGGATGTGTCTACCTCCAGACATCCTTGAAATACAATATTCTCCCAGAAAAGGA\nAGAGTTCCCCTTTGCTTTAGGAGTGCAGACTCTGCCTCAAACTTGTGATGAACCCAAAGCCCACACCAGCTTCCAAATCT\nCCCTAAGTGTCAGTTACACAGGGAGCCGCTCTGCCTCCAACATGGCGATCGTTGATGTGAAGATGGTCTCTGGCTTCATT\nCCCCTGAAGCCAACAGTGAAAATGCTTGAAAGATCTAACCATGTGAGCCGGACAGAAGTCAGCAGCAACCATGTCTTGAT\nTTACCTTGATAAGGTGTCAAATCAGACACTGAGCTTGTTCTTCACGGTTCTGCAAGATGTCCCAGTAAGAGATCTGAAAC\nCAGCCATAGTGAAAGTCTATGATTACTACGAGACGGATGAGTTTGCAATTGCTGAGTACAATGCTCCTTGCAGCAAAGAT\nCTTGGAAATGCTTGAAGACCACAAGGCTGAAAAGTGCTTTGCTGGAGTCCTGTTCTCAGAGCTCCACAGAAGACACGTGT\nTTTTGTATCTTTAAAGACTTGATGAATAAACACTTTTTCTGGTCAATGTC";
            string oneLine_First = originalSequence_First.Replace("\n", "");
            string expectedTranscribe_First = oneLine_First.Transcribe(false);
            Assert.That(firstSequence, Is.EqualTo(expectedTranscribe_First));

            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("alpha-2-macroglobulin (A2M)"));
            Assert.That(first.Accession, Is.EqualTo("NM_000014.6"));
            Assert.That(first.Organism, Is.EqualTo("Homo sapiens"));

            var expectedAdditionalDatabaseFieldsFirst = new Dictionary<string, string>()
            {
                {"description", "transcript variant 1, mRNA"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsFirst, first.AdditionalDatabaseFields);



            // repeat for the last entry
            string lastSequence = last.BaseSequence;
            string originalSequence_Last =
                "AGAGCTGGGTCAGAGCTCGAGCCAGCGGCGCCCGGAGAGATTCGGAGATGCAGGCGGCTCGGATGGCCGCGAGCTTGGGG\nCGGCAGCTGCTGAGGCTCGGGGGCGGAAGCTCGCGGCTCACGGCGCTCCTGGGGCAGCCCCGGCCCGGCCCTGCCCGGCG\nGCCCTATGCCGGGGGTGCCGCTCAGCTGGCTCTGGACAAGTCAGATTCCCACCCCTCTGACGCTCTGACCAGGAAAAAAC\nCGGCCAAGGCGGAATCTAAGTCCTTTGCTGTGGGAATGTTCAAAGGCCAGCTCACCACAGATCAGGTGTTCCCATACCCG\nTCCGTGCTCAACGAAGAGCAGACACAGTTTCTTAAAGAGCTGGTGGAGCCTGTGTCCCGTTTCTTCGAGGAAGTGAACGA\nTCCCGCCAAGAATGACGCTCTGGAGATGGTGGAGGAGACCACTTGGCAGGGCCTCAAGGAGCTGGGGGCCTTTGGTCTGC\nAAGTGCCCAGTGAGCTGGGTGGTGTGGGCCTTTGCAACACCCAGTACGCCCGTTTGGTGGAGATCGTGGGCATGCATGAC\nCTTGGCGTGGGCATTACCCTGGGGGCCCATCAGAGCATCGGTTTCAAAGGCATCCTGCTCTTTGGCACAAAGGCCCAGAA\nAGAAAAATACCTCCCCAAGCTGGCATCTGGGGAGACTGTGGCCGCTTTCTGTCTAACCGAGCCCTCAAGCGGGTCAGATG\nCAGCCTCCATCCGAACCTCTGCTGTGCCCAGCCCCTGTGGAAAATACTATACCCTCAATGGAAGCAAGCTTTGGATCAGT\nAATGGGGGCCTAGCAGACATCTTCACGGTCTTTGCCAAGACACCAGTTACAGATCCAGCCACAGGAGCCGTGAAGGAGAA\nGATCACAGCTTTTGTGGTGGAGAGGGGCTTCGGGGGCATTACCCATGGGCCCCCTGAGAAGAAGATGGGCATCAAGGCTT\nCAAACACAGCAGAGGTGTTCTTTGATGGAGTACGGGTGCCATCGGAGAACGTGCTGGGTGAGGTTGGGAGTGGCTTCAAG\nGTTGCCATGCACATCCTCAACAATGGAAGGTTTGGCATGGCTGCGGCCCTGGCAGGTACCATGAGAGGCATCATTGCTAA\nGGCGGTAGATCATGCCACTAATCGTACCCAGTTTGGGGAGAAAATTCACAACTTTGGGCTGATCCAGGAGAAGCTGGCAC\nGGATGGTTATGCTGCAGTATGTAACTGAGTCCATGGCTTACATGGTGAGTGCTAACATGGACCAGGGAGCCACGGACTTC\nCAGATAGAGGCCGCCATCAGCAAAATCTTTGGCTCGGAGGCAGCCTGGAAGGTGACAGATGAATGCATCCAAATCATGGG\nGGGTATGGGCTTCATGAAGGAACCTGGAGTAGAGCGTGTGCTCCGAGATCTTCGCATCTTCCGGATCTTTGAGGGGACAA\nATGACATTCTTCGGCTGTTTGTGGCTCTGCAGGGCTGTATGGACAAAGGAAAGGAGCTCTCTGGGCTTGGCAGTGCTCTA\nAAGAATCCCTTTGGGAATGCTGGCCTCCTGCTAGGAGAGGCAGGCAAACAGCTGAGGCGGCGGGCAGGGCTGGGCAGCGG\nCCTGAGTCTCAGCGGACTTGTCCACCCGGAGTTGAGTCGGAGTGGCGAGCTGGCAGTACGGGCTCTGGAGCAGTTTGCCA\nCTGTGGTGGAGGCCAAGCTGATAAAACACAAGAAGGGGATTGTCAATGAACAGTTTCTGCTGCAGCGGCTGGCAGACGGG\nGCCATCGACCTCTATGCCATGGTGGTGGTTCTCTCGAGGGCCTCAAGATCCCTGAGTGAGGGCCACCCCACGGCCCAGCA\nTGAGAAAATGCTCTGTGACACCTGGTGTATCGAGGCTGCAGCTCGGATCCGAGAGGGCATGGCCGCCCTGCAGTCTGACC\nCCTGGCAGCAAGAGCTCTACCGCAACTTCAAAAGCATCTCCAAGGCCTTGGTGGAGCGGGGTGGTGTGGTCACCAGCAAC\nCCACTTGGCTTCTGAATACTCCCGGCCAGGGCCTGTCCCAGTTATGTGCCTTCCCTCAAGCCAAAGCCGAAGCCCCTTTC\nCTTAAGGCCCTGGTTTGTCCCGAAGGGGCCTAGTGTTCCCAGCACTGTGCCTGCTCTCAAGAGCACTTACTGCCTCGCAA\nATAATAAAAATTTCTAGCCAGTCA";
            string oneLine_Last = originalSequence_Last.Replace("\n", "");
            string expectedTranscribe_Last = oneLine_Last.Transcribe(false);
            Assert.That(lastSequence, Is.EqualTo(expectedTranscribe_Last));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(last.Name, Is.EqualTo("acyl-CoA dehydrogenase very long chain (ACADVL)"));
            Assert.That(last.Accession, Is.EqualTo("NM_000018.4"));
            Assert.That(first.Organism, Is.EqualTo("Homo sapiens"));

            var expectedAdditionalDatabaseFieldsLast = new Dictionary<string, string>()
            {
                {"description", "transcript variant 1, mRNA; nuclear gene for mitochondrial product"},
            };
            Assert.That(last.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsLast, last.AdditionalDatabaseFields);
        }

        /// <summary>
        /// Test if program supports NCBIRefSeqGene
        /// </summary>
        [Test]
        public static void TestNCBIRefSeqGene()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "ncbi_RefSeqGene.fna");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(1));

            var first = rna[0];

            string firstSequence = first.BaseSequence;
            string sequencePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "NCBIRefGene_Sequence.txt");
            StreamReader reader = new StreamReader(sequencePath);
            string line = File.ReadAllText(sequencePath);
            //line = reader.ReadLine();
            string originalSequence_First = line;
            string oneLine_First = originalSequence_First.Replace("\r\n", "");
            string expectedTranscribe_First = oneLine_First.Transcribe(false);
            Assert.That(firstSequence, Is.EqualTo(expectedTranscribe_First));

            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("196799494-196831740 Muc2"));
            Assert.That(first.Accession, Is.EqualTo("NC_051336.1"));
            Assert.That(first.Organism, Is.EqualTo("Rattus norvegicus"));
            var expectedAdditionalDatabaseFieldsLast = new Dictionary<string, string>()
            {
                {"GeneID", "24572"},
                {"chromosome", "1"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsLast, first.AdditionalDatabaseFields);
        }

        /// <summary>
        /// Test if program supports NCBISeqRNA
        /// </summary>
        [Test]
        public static void TestNCBIRefSeqRNA()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "rnaMuc2.fna");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(1));

            var first = rna[0];

            string firstSequence = first.BaseSequence;
            string sequencePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "NCBIRefRNA_Sequence.txt");
            StreamReader reader = new StreamReader(sequencePath);
            string line = File.ReadAllText(sequencePath);
            string originalSequence_First = line;
            string oneLine_First = originalSequence_First.Replace("\r\n", "").Trim();
            string expectedTranscribe_First = oneLine_First.Transcribe(false);
            Assert.That(firstSequence, Is.EqualTo(expectedTranscribe_First));

            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("Muc2"));
            Assert.That(first.Accession, Is.EqualTo("NM_022174.1"));
            Assert.That(first.Organism, Is.EqualTo("Rattus norvegicus"));
            var expectedAdditionalDatabaseFieldsLast = new Dictionary<string, string>()
            {
                {"GeneID", "24572"},
            };
            Assert.That(first.AdditionalDatabaseFields, Is.Not.EqualTo(null));
            CollectionAssert.AreEqual(expectedAdditionalDatabaseFieldsLast, first.AdditionalDatabaseFields);
        }

        /// <summary>
        /// Test if program supports NCBISRA
        /// </summary>
        [Test]
        public static void TestNCBISRA()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "SRR27833655.fastq");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(3));

            var first = rna[0];

            string firstSequence = first.BaseSequence;
            string originalSequence_First = "TGTGCCAGCAGCCGCGGTAATACGGAGGATCCGAGCGTTTTCCGGATTTATTGGGTTTAAAGGGAGCGTAGGCGGGTTGTTAAGTCAGTTGTGAAAGTTTGCGGCTCTACCGTAAAATTGCAGTTGATACTGGCGACCTTGTGTGCAACAGAGGTAGGCGGAATTCGTGTTGTAGCGGTGTAATGCTTAGATATCACGAAGAACTCCGATTGCGAAGGCAGCTTACTGGATTGTAACTGACGCTGATGCT\n";
            string oneLine_First = originalSequence_First.Replace("\n", "");
            string expectedTranscribe_First = oneLine_First.Transcribe(false);
            Assert.That(firstSequence, Is.EqualTo(expectedTranscribe_First));

            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("M03992:702:000000000-K7LV5:1:1101:18660:1985"));
            Assert.That(first.Accession, Is.EqualTo("@SRR27833655.1.1"));
            Assert.That(first.Organism, Is.EqualTo("N/A"));

            var last = rna[2];

            string lastSequence = last.BaseSequence;
            string originalSequence_last = "CAAGTGCCAGCCGCCGCGGTAATACGGAGGATCCGAGCGTTATCCGGATTTATTGGGTTTAAAGGGAGCGTAGGCGGGTTGTTAAGTCAGTTGTGAAAGTTTGCGGCTCAACCGTAATATTGCAGTTGATACTGGCGACCTTGTGTGCAACAGTTGTATGCGGAATTCGTGGTGTAGCGGTGTAATGCTTAGTTATCACGAAGAACTCCGATTGCGAAGGCAGCTTACTGGTTTGTAGCTGACGCTGTTT\n";
            string oneLine_last = originalSequence_last.Replace("\n", "");
            string expectedTranscribe_last = oneLine_last.Transcribe(false);
            Assert.That(lastSequence, Is.EqualTo(expectedTranscribe_last));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(last.Name, Is.EqualTo("M03992:702:000000000-K7LV5:1:1101:15438:2067"));
            Assert.That(last.Accession, Is.EqualTo("@SRR27833655.2.1"));
            Assert.That(last.Organism, Is.EqualTo("N/A"));
        }

        /// <summary>
        /// Test if program supports NCBIPD
        /// </summary>
        [Test]
        public static void TestNCBIPD()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics", "TestData",
                "PDS000097765.1.dnapars_input.dnapars");
            var rna = RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out List<string> errors);
            Assert.That(!errors.Any());
            Assert.That(rna.Count(), Is.EqualTo(33));

            var first = rna[0];

            string firstSequence = first.BaseSequence;
            string originalSequence_First = "ACCATGGCTCGACAAACCCGCTGCCCGCCGCGCCGGCGATGGGGCACTACCCTGCAGCCCGGCCCCTAGCGGGAGTGTGGGCCGGCCCCCCCGAGCTCGGCCGCCTCCGGC" +
                                            "CGCACGCAGTGGAGGTGCAAACCTGCGCCCGGCCGGGGAGGCCGCCCCCCGACCCGGCCCCCTGCATGGATACGCCCCTACCCGGG";
            string oneLine_First = originalSequence_First.Replace("\n", "");
            string expectedTranscribe_First = oneLine_First.Transcribe(false);
            Assert.That(firstSequence, Is.EqualTo(expectedTranscribe_First));

            Assert.That(first.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(first.Name, Is.EqualTo("PDT000101747.3"));
            Assert.That(first.Accession, Is.EqualTo("PDT000101747.3"));
            Assert.That(first.Organism, Is.EqualTo("N/A"));

            var last = rna[32];

            string lastSequence = last.BaseSequence;
            string originalSequence_last = "GCCGGCGTTCGGCACAACCGCTGCCCGCCGCGCCGCCGGCGGGGCGCTACCCGGCGGTCTGGCCCCTTGCAGGAGTATGGGCCTGACTCCCACAGCTCGGCCATGTCCGCTC" +
                                           "GCGCGCCGCCGGGGAGCCAACCTGTGGCGGGTCGGGGATGCCGCACACCGATCCGGGTCCCCACCGGGAGACGCCCCGTCCCGAG\n\n";
            string oneLine_last = originalSequence_last.Replace("\n", "");
            string expectedTranscribe_last = oneLine_last.Transcribe(false);
            Assert.That(lastSequence, Is.EqualTo(expectedTranscribe_last));

            Assert.That(last.DatabaseFilePath, Is.EqualTo(path));
            Assert.That(last.Name, Is.EqualTo("PDT001160858.1"));
            Assert.That(last.Accession, Is.EqualTo("PDT001160858.1"));
            Assert.That(last.Organism, Is.EqualTo("N/A"));
        }

        /// <summary>
        /// Test the correctness when the data is contaminated
        /// </summary>
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

        /// <summary>
        /// Test the correctness of generating target or decoys
        /// </summary>
        [Test]
        public static void TestNotGeneratingTargetsOrDecoys()
        {
            var oligos = RnaDbLoader.LoadRnaFasta(ModomicsUnmodifedFastaPath, false, DecoyType.None, true,
                out var errors);
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(oligos.Count, Is.EqualTo(0));
        }

        //[Test]
        //public static void TestModomicsModifiedFasta()
        //{

        //}

        //[Test]
        //public static void Test__Fna()
        //{

        //}
        /// <summary>
        /// Detect the headertype of the test cases
        /// </summary>
        private static IEnumerable<(string, RnaFastaHeaderType)> DetectHeaderTestCases =>
            new List<(string, RnaFastaHeaderType)>
            {
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "DoubleProtease.tsv"), RnaFastaHeaderType.Unknown),
                (ModomicsUnmodifedFastaPath, RnaFastaHeaderType.Modomics),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/TestEnsembleFasta_Homo_sapiens.GRCh38.ncrna.fa"), RnaFastaHeaderType.Ensemble),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/ModomicsUnmodifiedTrimmed.fasta"), RnaFastaHeaderType.Modomics),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/ncbi_RefSeqGene.fna"), RnaFastaHeaderType.NCBIRefSeqGene),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/NCBIassemblyRna.fna"), RnaFastaHeaderType.NCBIassembly),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/SRR27833655.fastq"), RnaFastaHeaderType.NCBISRA),
                (Path.Combine(TestContext.CurrentContext.TestDirectory, "Transcriptomics/TestData/PDS000097765.1.dnapars_input.dnapars"), RnaFastaHeaderType.NCBIPD),
            };

        /// <summary>
        /// Test the correctness of checking headertype
        /// </summary>
        /// <param name="testData"></param>
        [Test]
        [TestCaseSource(nameof(DetectHeaderTestCases))]
        public static void TestDetectHeaderType((string dbPath, RnaFastaHeaderType headerType) testData)
        {
            string line = File.ReadLines(testData.dbPath).First();
            if (char.IsDigit(line.First()))
            {
                line = File.ReadLines(testData.dbPath).Skip(1).First();
            }
            var type = RnaDbLoader.DetectFastaHeaderType(line);
            Assert.AreEqual(testData.headerType, type);
        }
    }
}
