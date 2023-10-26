using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using MassSpectrometry;
using NUnit.Framework;
using Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TestDigestion
    {
        public record RnaDigestionTestCase(string BaseSequence, string Enzyme, int MissedCleavages, int MinLength,
            int MaxLength, int DigestionProductCount,
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

        public static string rnaseTsvpath =
            @"C:\Users\Nic\source\repos\mzLib\mzLib\Transcriptomics\Digestion\rnases.tsv";

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
            var digestionProducts =
                rnase.GetUnmodifiedOligos(rna, testCase.MissedCleavages, testCase.MinLength, testCase.MaxLength);

            Assert.That(digestionProducts.Count(), Is.EqualTo(testCase.DigestionProductCount));
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestRnase_GetUnmodifiedOligo_Sequence(RnaDigestionTestCase testCase)
        {
            RNA rna = new RNA(testCase.BaseSequence);
            Rnase rnase = RnaseDictionary.Dictionary[testCase.Enzyme];
            var digestionProducts =
                rnase.GetUnmodifiedOligos(rna, testCase.MissedCleavages, testCase.MinLength, testCase.MaxLength);

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

        private static (string Sequence, int FragmentNumber, ProductType Type, double Mass)[] DigestFragmentTestCases =>
            new (string Sequence, int FragmentNumber, ProductType Type, double Mass)[]
            {
                ("UAG", 0, ProductType.M, 998.134),
                ("UAG", 1, ProductType.aBaseLoss, 114.031), ("UAG", 2, ProductType.aBaseLoss, 420.056),
                ("UAG", 1, ProductType.c, 308.031), ("UAG", 2, ProductType.c, 637.093),
                ("UAG", 1, ProductType.dWaterLoss, 306.025), ("UAG", 2, ProductType.dWaterLoss, 635.077),
                ("UAG", 1, ProductType.w, 443.023), ("UAG", 2, ProductType.w, 772.075),
                ("UAG", 1, ProductType.y,  363.057), ("UAG", 2, ProductType.y, 692.109),
                ("UAG", 1, ProductType.yWaterLoss,  345.047), ("UAG", 2, ProductType.yWaterLoss, 674.100),

                ("UCG", 0, ProductType.M, 974.123),
                ("UCG", 1, ProductType.aBaseLoss, 114.031), ("UCG", 2, ProductType.aBaseLoss, 420.056),
                ("UCG", 1, ProductType.c, 308.040), ("UCG", 2, ProductType.c, 613.082),
                ("UCG", 1, ProductType.dWaterLoss, 306.025), ("UCG", 2, ProductType.dWaterLoss, 611.066),
                ("UCG", 1, ProductType.w, 443.023), ("UCG", 2, ProductType.w, 748.064),
                ("UCG", 1, ProductType.y,  363.057), ("UCG", 2, ProductType.y, 668.098),
                ("UCG", 1, ProductType.yWaterLoss,  345.047), ("UCG", 2, ProductType.yWaterLoss, 650.089),

                ("UUG", 0, ProductType.M, 975.107),
                ("UUG", 1, ProductType.aBaseLoss, 114.031), ("UUG", 2, ProductType.aBaseLoss, 420.056),
                ("UUG", 1, ProductType.c, 308.041), ("UUG", 2, ProductType.c, 614.066),
                ("UUG", 1, ProductType.dWaterLoss, 306.025), ("UUG", 2, ProductType.dWaterLoss, 612.050),
                ("UUG", 1, ProductType.w, 443.023), ("UUG", 2, ProductType.w, 749.048),
                ("UUG", 1, ProductType.y,  363.057), ("UUG", 2, ProductType.y, 669.082),
                ("UUG", 1, ProductType.yWaterLoss,  345.047), ("UUG", 2, ProductType.yWaterLoss, 651.073),

                ("AUAG", 0, ProductType.M, 1247.220),
                ("AUAG", 1, ProductType.aBaseLoss, 114.031), ("AUAG", 2, ProductType.aBaseLoss, 443.083), ("AUAG", 3, ProductType.aBaseLoss, 749.108),
                ("AUAG", 1, ProductType.c, 331.068), ("AUAG", 2, ProductType.c, 637.093), ("AUAG", 3, ProductType.c, 966.146),
                ("AUAG", 1, ProductType.dWaterLoss, 329.052), ("AUAG", 2, ProductType.dWaterLoss, 635.077), ("AUAG", 3, ProductType.dWaterLoss, 964.129),
                ("AUAG", 1, ProductType.w, 363.057), ("AUAG", 2, ProductType.w, 692.109), ("AUAG", 3, ProductType.w, 998.134),
                ("AUAG", 1, ProductType.y,  283.091), ("AUAG", 2, ProductType.y, 612.143), ("AUAG", 3, ProductType.y, 918.168),
                ("AUAG", 1, ProductType.yWaterLoss,  265.081), ("AUAG", 2, ProductType.yWaterLoss, 594.134), ("AUAG", 3, ProductType.yWaterLoss, 900.159),
            };

        [Test] // test values calculated with http://rna.rega.kuleuven.be/masspec/mongo.htm
        [TestCase("UAGUCGUUGAUAG", 4140.555, new[] {"UAG", "UCG", "UUG", "AUAG" }, 
            new[] {998.134, 974.123, 975.107, 1247.220 })]
        public static void TestDigestionAndFragmentation(string sequence, double monoMass,
            string[] digestionProductSequences, double[] digestionProductMasses)
        {
            RNA rna = new(sequence);
            Assert.That(rna.MonoisotopicMass, Is.EqualTo(monoMass).Within(0.01));

            // digest RNA
            var digestionParams = new RnaDigestionParams("RNase T1");
            var products = rna.Digest(digestionParams, new List<Modification>(), new List<Modification>())
                .Select(p => (OligoWithSetMods)p).ToList();
            Assert.That(products.Count, Is.EqualTo(digestionProductSequences.Length));

            // ensure digestion sequence and masses are correct
            for (var index = 0; index < products.Count; index++)
            {
                var digestionProduct = products[index];
                Assert.That(digestionProduct.BaseSequence, Is.EqualTo(digestionProductSequences[index]));
                Assert.That(digestionProduct.MonoisotopicMass, Is.EqualTo(digestionProductMasses[index]).Within(0.01));

                List<IProduct> fragments = new();
                digestionProduct.Fragment(DissociationType.CID, FragmentationTerminus.Both, fragments);

                List<(int FragmentNumber, ProductType Type, double Mass)[]> ughh = new();

                // test that fragments are correct
                var fragmentsToCompare = DigestFragmentTestCases
                    .Where(p => p.Sequence.Equals(digestionProduct.BaseSequence)).ToList();
                for (var i = 0; i < fragments.Count; i++)
                {
                    var fragment = fragments[i];
                    var theoreticalFragment = fragmentsToCompare[i];
                    Assert.That(fragment.MonoisotopicMass, Is.EqualTo(theoreticalFragment.Mass).Within(0.01));
                    Assert.That(fragment.FragmentNumber, Is.EqualTo(theoreticalFragment.FragmentNumber));
                    Assert.That(fragment.ProductType, Is.EqualTo(theoreticalFragment.Type));
                    Assert.That(fragment.FragmentNumber, Is.EqualTo(theoreticalFragment.FragmentNumber));
                    if (fragment.Terminus == FragmentationTerminus.FivePrime)
                        Assert.That(fragment.AminoAcidPosition, Is.EqualTo(theoreticalFragment.FragmentNumber));
                    else if (fragment.Terminus == FragmentationTerminus.None)
                        Assert.That(fragment.FragmentNumber, Is.EqualTo(0));
                    else
                        Assert.That(fragment.AminoAcidPosition, Is.EqualTo(digestionProductSequences[index].Length - theoreticalFragment.FragmentNumber));
                }
            }
        }

        [Test]
        [TestCase("UAGUCGUUGAUAG", new[] { "UAG", "UCG", "UUG", "AUAG" },
            new[] {1, 4, 7, 10}, new[] {3, 6, 9, 13}, new[] {'-', 'G', 'G', 'G'},
            new[] {'U', 'U', 'A', '-'})]
        public static void TestOligoWithSetMods_AAPositions(string sequence, string[] digestionProductSequences,
        int[] startResidue, int[] endResidue, char[] preciousResidue, char[] nextResidue)
        {
            RNA rna = new RNA(sequence);
            var digestionProducts = rna.Digest(new RnaDigestionParams("RNase T1"), new List<Modification>(),
                new List<Modification>()).Select(p => (OligoWithSetMods)p).ToList();
            for (var index = 0; index < digestionProducts.Count; index++)
            {
                var digestionProduct = digestionProducts[index];
                Assert.That(digestionProduct.BaseSequence, Is.EqualTo(digestionProductSequences[index]));
                Assert.That(digestionProduct.OneBasedStartResidue, Is.EqualTo(startResidue[index]));
                Assert.That(digestionProduct.OneBasedEndResidue, Is.EqualTo(endResidue[index]));
                Assert.That(digestionProduct.PreviousResidue, Is.EqualTo(preciousResidue[index]));
                Assert.That(digestionProduct.NextResidue, Is.EqualTo(nextResidue[index]));
            }
        }

        [Test]
        [TestCase("UAGUCGUUGAUAG")]
        public static void TestOligoWithSetMods_PropertiesWithTopDownDigestion(string sequence)
        {
            var rna = new RNA(sequence);
            var oligoWithSetMods =
                rna.Digest(new RnaDigestionParams(), new List<Modification>(), new List<Modification>())
                        .First() as OligoWithSetMods ?? throw new NullReferenceException();

            Assert.That(rna.BaseSequence, Is.EqualTo(oligoWithSetMods.BaseSequence));
            Assert.That(rna.ThreePrimeTerminus, Is.EqualTo(oligoWithSetMods.ThreePrimeTerminus));
            Assert.That(rna.FivePrimeTerminus, Is.EqualTo(oligoWithSetMods.FivePrimeTerminus));
            Assert.That(rna.ThisChemicalFormula, Is.EqualTo(oligoWithSetMods.ThisChemicalFormula));
            Assert.That(rna.Length, Is.EqualTo(oligoWithSetMods.Length));
        }

        #endregion

        #region DigestionParams

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void TestDigestionParams_Properties(RnaDigestionTestCase testCase)
        {
            var rna = new RNA(testCase.BaseSequence);
            var digestionParams = new RnaDigestionParams(testCase.Enzyme, testCase.MissedCleavages, testCase.MinLength,
                testCase.MaxLength);

            Assert.That(digestionParams.Enzyme, Is.EqualTo(RnaseDictionary.Dictionary[testCase.Enzyme]));
            Assert.That(digestionParams.MaxMissedCleavages, Is.EqualTo(testCase.MissedCleavages));
            Assert.That(digestionParams.MinLength, Is.EqualTo(testCase.MinLength));
            Assert.That(digestionParams.MaxLength, Is.EqualTo(testCase.MaxLength));

            digestionParams.MaxModificationIsoforms = 2048;
            digestionParams.MaxMods = 3;
            Assert.That(digestionParams.MaxModificationIsoforms, Is.EqualTo(2048));
            Assert.That(digestionParams.MaxMods, Is.EqualTo(3));

            var digestionProducts = rna.Digest(digestionParams, new List<Modification>(), new List<Modification>());
            Assert.That(digestionProducts.Count(), Is.EqualTo(testCase.DigestionProductCount));
        }

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
