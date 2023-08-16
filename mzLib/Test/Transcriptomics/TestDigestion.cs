using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using NuGet.Frameworks;
using NUnit.Framework;
using Proteomics.ProteolyticDigestion;
using Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TestDigestion
    {
        public record RnaDigestionTestCase(string BaseSequence, string Enzyme, int MissedCleavages, int MinLength, int MaxLength, int DigestionProductCount,
            double[] MonoMasses, string[] Sequences);

        public static IEnumerable<RnaDigestionTestCase> GetTestCases()
        {
            // 6bp Top Down
            yield return new RnaDigestionTestCase("GUACUG", "top-down", 
                0, 1, 6, 1, 
                new[] { 1874.28 }, 
                new[] { "GUACUG" });
            // 6bp Rnase T1, normal
            yield return new RnaDigestionTestCase("GUACUG", "RNase T1", 
                0, 1, 6, 2, 
                new[] { 363.057, 1529.234 },
                new[] { "G", "UACUG" });
            // 6bp Cusativin, normal
            yield return new RnaDigestionTestCase("GUACUG", "Cusativin", 
                0, 1, 6, 2, 
                new[] { 1303.175, 589.116 },
                new[] { "GUAC", "UG" });
            // 6bp Rnase T1, one product too short
            yield return new RnaDigestionTestCase("GUACUG", "RNase T1", 
                0, 3, 6, 1, 
                new[] { 1529.234 },
                new[] { "UACUG" });
            // 6bp Rnase T1, one product too long
            yield return new RnaDigestionTestCase("GUACUG", "RNase T1", 
                0, 1, 2, 1, 
                new[] { 363.057 },
                new[] { "G" });
            // 6bp Rnase T1, 1 missed cleavage
            yield return new RnaDigestionTestCase("GUACUG", "RNase T1",
                1, 1, 6, 3,
                new[] { 363.057, 1529.234, 1874.28 },
                new[] { "G", "UACUG", "GUACUG" });
            // 6bp Rnase A
            yield return new RnaDigestionTestCase("GUACUG", "RNase A",
                0, 1, 6, 4,
                new[] { 669.082, 652.103, 324.035, 283.091 },
                new[] { "GU", "AC", "U", "G" });
            // 6bp Rnase A, 1 missed cleavage
            yield return new RnaDigestionTestCase("GUACUG", "RNase A",
                1, 1, 6, 7,
                new[] { 669.082, 652.103, 324.035, 283.091, 1303.175, 958.128, 589.116 },
                new[] { "GU", "AC", "U", "G", "GUAC", "ACU", "UG" });
            // 6bp Rnase A, 2 missed cleavages
            yield return new RnaDigestionTestCase("GUACUG", "RNase A",
                2, 1, 6, 9,
                new[] { 669.082, 652.103, 324.035, 283.091, 1303.175, 958.128, 589.116, 1609.200, 1223.209 },
                new[] { "GU", "AC", "U", "G", "GUAC", "ACU", "UG", "GUACU", "ACUG" });
            // 20bp top-down
            yield return new RnaDigestionTestCase("GUACUGCCUCUAGUGAAGCA", "top-down",
                0, 1, int.MaxValue, 1,
                new[] { 6363.871 },
                new[] { "GUACUGCCUCUAGUGAAGCA" });
            // 20bp Rnase T1, normal
            yield return new RnaDigestionTestCase("GUACUGCCUCUAGUGAAGCA", "RNase T1",
                0, 1, int.MaxValue, 6,
                new[] { 363.057, 1609.200, 2219.282, 669.082, 1021.161, 572.137 },
                new[] { "G", "UACUG", "CCUCUAG", "UG", "AAG", "CA" });

  
        }

        public static string rnaseTsvpath = @"C:\Users\Nic\source\repos\mzLib\mzLib\Transcriptomics\Digestion\rnases.tsv";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RnaseDictionary.Dictionary = RnaseDictionary.LoadRnaseDictionary(rnaseTsvpath);
        }

        #region Rnase

        [Test]
        public void TestRnaseDictionaryLoading()
        {
            var rnaseCountFromTsv = File.ReadAllLines(rnaseTsvpath).Length - 1;
            Assert.AreEqual(RnaseDictionary.Dictionary.Count, rnaseCountFromTsv);
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestRnase_GetUnmodifiedOligos_Counts(RnaDigestionTestCase testCase)
        {
            RNA rna = new RNA(testCase.BaseSequence);
            Rnase rnase = RnaseDictionary.Dictionary[testCase.Enzyme];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, testCase.MissedCleavages, testCase.MinLength, testCase.MaxLength);

            Assert.That(digestionProducts.Count(), Is.EqualTo(testCase.DigestionProductCount));
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestRnase_GetUnmodifiedOligo_Sequence(RnaDigestionTestCase testCase)
        {

            RNA rna = new RNA(testCase.BaseSequence);
            Rnase rnase = RnaseDictionary.Dictionary[testCase.Enzyme];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, testCase.MissedCleavages, testCase.MinLength, testCase.MaxLength);

            Assert.That(digestionProducts.Count, Is.EqualTo(testCase.Sequences.Length));
            for (var i = 0; i < digestionProducts.Count; i++)
            {
                var product = digestionProducts[i];
                var testCaseCaseSequence = testCase.Sequences[i];
                Assert.That(product.BaseSequence == testCaseCaseSequence);
            }
        }

        [Test]
        public void TestRnaseEqualityProperties()
        {
            Rnase t1 = RnaseDictionary.Dictionary["RNase T1"];
            Rnase t1Duplicate = RnaseDictionary.Dictionary["RNase T1"];
            Rnase t2 = RnaseDictionary.Dictionary["RNase T2"];

            Assert.That(t1.Equals(t1Duplicate));
            Assert.That(t1.Equals(t1));
            Assert.That(!t1.Equals(t2));
            Assert.That(!t1.Equals(null));
            Assert.That(t1.GetHashCode(), Is.EqualTo(t1Duplicate.GetHashCode()));
            Assert.That(t1.GetHashCode(), Is.Not.EqualTo(t2.GetHashCode()));
            Assert.That(t1.Equals((object)t1Duplicate));
            Assert.That(t1.Equals((object)t1));
            Assert.That(!t1.Equals((object)t2));
            Assert.That(!t1.Equals((object)null));
            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.That(!t1.Equals((object)new RNA("GUA")));
        }

        [Test]
        public void TestRnase_UnmodifiedOligos_Exception()
        {
            Rnase rnase = new Rnase("Bad", CleavageSpecificity.SingleC, new List<DigestionMotif>());
            Assert.Throws<ArgumentException>(() => { rnase.GetUnmodifiedOligos(new RNA("GUACUG"), 0, 1, 6); });
        }

        #endregion

        #region NucleolyticOligo

        [Test] 
        public void TestNucleolyticOligoProperties_FivePrimeDigestionProduct()
        {
            RNA rna = new("GUACUG");
            Rnase rnase = RnaseDictionary.Dictionary["RNase U2"];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, 0, 1, 6);
            Assert.That(digestionProducts.Count, Is.EqualTo(3));

            var oligo = digestionProducts[0];
            Assert.That(oligo.BaseSequence, Is.EqualTo("G"));
            Assert.That(oligo.OneBasedStartResidue, Is.EqualTo(1));
            Assert.That(oligo.OneBasedEndResidue, Is.EqualTo(1));
            Assert.That(oligo.MissedCleavages, Is.EqualTo(0));
            Assert.That(oligo.CleavageSpecificityForFdrCategory, Is.EqualTo(CleavageSpecificity.Full));
            Assert.That(oligo.NextResidue, Is.EqualTo('U'));
            Assert.That(oligo.PreviousResidue, Is.EqualTo('-'));
            Assert.That(oligo.ToString(), Is.EqualTo(oligo.BaseSequence));
        }

        [Test]
        public void TestNucleolyticOligoProperties_ThreePrimeDigestionProduct()
        {
            RNA rna = new("GUACUG");
            Rnase rnase = RnaseDictionary.Dictionary["RNase U2"];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, 0, 1, 6);
            Assert.That(digestionProducts.Count, Is.EqualTo(3));

            NucleolyticOligo oligo = digestionProducts[2];
            Assert.That(oligo.BaseSequence, Is.EqualTo("CUG"));
            Assert.That(oligo.OneBasedStartResidue, Is.EqualTo(4));
            Assert.That(oligo.OneBasedEndResidue, Is.EqualTo(6));
            Assert.That(oligo.MissedCleavages, Is.EqualTo(0));
            Assert.That(oligo.CleavageSpecificityForFdrCategory, Is.EqualTo(CleavageSpecificity.Full));
            Assert.That(oligo.NextResidue, Is.EqualTo('-'));
            Assert.That(oligo.PreviousResidue, Is.EqualTo('A'));
            Assert.That(oligo.ToString(), Is.EqualTo(oligo.BaseSequence));
        }

        [Test]
        public void TestNucleolyticOligoProperties_InternalDigestionProduct()
        {
            RNA rna = new("GUACUG");
            Rnase rnase = RnaseDictionary.Dictionary["RNase U2"];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, 0, 1, 6);
            Assert.That(digestionProducts.Count, Is.EqualTo(3));

            NucleolyticOligo oligo = digestionProducts[1];
            Assert.That(oligo.BaseSequence, Is.EqualTo("UA"));
            Assert.That(oligo.OneBasedStartResidue, Is.EqualTo(2));
            Assert.That(oligo.OneBasedEndResidue, Is.EqualTo(3));
            Assert.That(oligo.MissedCleavages, Is.EqualTo(0));
            Assert.That(oligo.CleavageSpecificityForFdrCategory, Is.EqualTo(CleavageSpecificity.Full));
            Assert.That(oligo.NextResidue, Is.EqualTo('C'));
            Assert.That(oligo.PreviousResidue, Is.EqualTo('G'));
            Assert.That(oligo.ToString(), Is.EqualTo(oligo.BaseSequence));
        }
        [Test]
        public void TestNucleolyticOligoProperties_TopDownDigestionProduct()
        {
            RNA rna = new("GUACUG");
            Rnase rnase = RnaseDictionary.Dictionary["top-down"];
            var digestionProducts = rnase.GetUnmodifiedOligos(rna, 0, 1, 6);
            Assert.That(digestionProducts.Count, Is.EqualTo(1));

            NucleolyticOligo oligo = digestionProducts[0];
            Assert.That(oligo.BaseSequence, Is.EqualTo("GUACUG"));
            Assert.That(oligo.OneBasedStartResidue, Is.EqualTo(1));
            Assert.That(oligo.OneBasedEndResidue, Is.EqualTo(6));
            Assert.That(oligo.MissedCleavages, Is.EqualTo(0));
            Assert.That(oligo.CleavageSpecificityForFdrCategory, Is.EqualTo(CleavageSpecificity.Full));
            Assert.That(oligo.NextResidue, Is.EqualTo('-'));
            Assert.That(oligo.PreviousResidue, Is.EqualTo('-'));
            Assert.That(oligo.ToString(), Is.EqualTo(oligo.BaseSequence));
        }

        #endregion

        #region OligoWithSetMods

        // TODO: this class



        #endregion

        #region NucleicAcid


        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestNucleicAcid_Digestion_WithoutMods_Counts(RnaDigestionTestCase testCase)
        {
            var rna = new RNA(testCase.BaseSequence);
            var digestionParams = new RnaDigestionParams(testCase.Enzyme, testCase.MissedCleavages, testCase.MinLength,
                testCase.MaxLength);

            var digestionProducts = rna.Digest(digestionParams, new List<Modification>(), new List<Modification>());
            Assert.That(digestionProducts.Count(), Is.EqualTo(testCase.DigestionProductCount));
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestNucleicAcid_Digestion_WithoutMods_Sequences(RnaDigestionTestCase testCase)
        {
            var rna = new RNA(testCase.BaseSequence);
            var digestionParams = new RnaDigestionParams(testCase.Enzyme, testCase.MissedCleavages, testCase.MinLength,
                testCase.MaxLength);

            var digestionProducts = rna.Digest(digestionParams, new List<Modification>(), new List<Modification>())
                .ToList();

            Assert.That(digestionProducts.Count, Is.EqualTo(testCase.Sequences.Length));
            for (var i = 0; i < digestionProducts.Count; i++)
            {
                var product = digestionProducts[i];
                var testCaseCaseSequence = testCase.Sequences[i];
                Assert.That(product.BaseSequence, Is.EqualTo(testCaseCaseSequence));
                Assert.That(product.FullSequence, Is.EqualTo(testCaseCaseSequence));
            }
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestNucleicAcid_Digestion_WithoutMods_MonoMasses(RnaDigestionTestCase testCase)
        {
            var rna = new RNA(testCase.BaseSequence);
            var digestionParams = new RnaDigestionParams(testCase.Enzyme, testCase.MissedCleavages, testCase.MinLength,
                testCase.MaxLength);

            var digestionProducts = rna.Digest(digestionParams, new List<Modification>(), new List<Modification>())
                .ToList();

            Assert.That(digestionProducts.Count, Is.EqualTo(testCase.Sequences.Length));
            for (var i = 0; i < digestionProducts.Count; i++)
            {
                var productMass = digestionProducts[i].MonoisotopicMass;
                var testCaseCaseMass = testCase.MonoMasses[i];
                Assert.That(productMass, Is.EqualTo(testCaseCaseMass).Within(0.01));
            }
        }


        #endregion
    }
}
