using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using MassSpectrometry;
using NUnit.Framework;
using Omics.Fragmentation;
using Omics.Modifications;
using Transcriptomics;
using Transcriptomics.Digestion;

namespace Test.Transcriptomics;

/// <summary>
/// Recreates the BaseLossFragmentation combinations with each modification sourced once from the
/// curated MetaMorpheus RNA mods (RnaMods.txt) and once from the MODOMICS load, and compares the
/// resulting oligos and their fragment sets.
/// </summary>
[TestFixture]
public static class CuratedVsModomicsFragmentation
{
    private static Dictionary<string, Modification> CuratedRnaMods =>
        Mods.MetaMorpheusRnaModifications.DistinctBy(m => m.IdWithMotif).ToDictionary(m => m.IdWithMotif);

    private static Dictionary<string, Modification> ModomicsMods =>
        Mods.ModomicsRnaModifications.ToDictionary(m => m.IdWithMotif);

    [Test]
    public static void TwoOMethyladenosine_Single()
    {
        // The curated id capitalizes "Methyl"; the MODOMICS full name is lowercase.
        CompareSources("2'-O-methyl single",
            "GUA[2'-O-Methyladenosine on A]CUG",
            "GUA[2'-O-methyladenosine on A]CUG",
            []);
    }

    [Test]
    public static void TwoOMethyladenosine_Double()
    {
        CompareSources("2'-O-methyl double",
            "GUA[2'-O-Methyladenosine on A]A[2'-O-Methyladenosine on A]UG",
            "GUA[2'-O-methyladenosine on A]A[2'-O-methyladenosine on A]UG",
            []);
    }

    [Test]
    public static void N6Methyladenosine_Single()
    {
        // Both sources share this id exactly; the dictionaries decide which entry resolves.
        // Curated declares the methyl as base-localized (BL Modified), so it leaves with the base;
        // the plain MODOMICS mod keeps it on the backbone after base loss.
        CompareSources("N6-methyl single",
            "GUA[N6-methyladenosine on A]CUG",
            "GUA[N6-methyladenosine on A]CUG",
            [(3, ChemicalFormula.ParseFormula("CH2").MonoisotopicMass)]);
    }

    [Test]
    public static void N6_2O_Dimethyladenosine_Single()
    {
        // Curated sends the N6 methyl off with the base and keeps the ribose methyl; MODOMICS keeps both.
        CompareSources("N6,2'-O-dimethyl single",
            "GUA[N6,2'-O-dimethyladenosine on A]CUG",
            "GUA[N6,2'-O-dimethyladenosine on A]CUG",
            [(3, ChemicalFormula.ParseFormula("CH2").MonoisotopicMass)]);
    }

    [Test]
    public static void TwoOMethyladenosine_WithSuppression_OnlyCuratedSuppresses()
    {
        var curated = new OligoWithSetMods("GUA[2'-O-Methyladenosine on A]CUG", CuratedRnaMods);
        var modomics = new OligoWithSetMods("GUA[2'-O-methyladenosine on A]CUG", ModomicsMods);

        IFragmentationParams fragmentationParams = new RnaFragmentationParams
        {
            ModificationsCanSuppressBaseLossIons = true
        };

        var curatedProducts = new List<Product>();
        var modomicsProducts = new List<Product>();
        curated.Fragment(DissociationType.CID, FragmentationTerminus.Both, curatedProducts, fragmentationParams);
        modomics.Fragment(DissociationType.CID, FragmentationTerminus.Both, modomicsProducts, fragmentationParams);

        // Only the curated BaseModification can suppress its base-loss ion; a plain MODOMICS mod cannot.
        Assert.That(curatedProducts.Count, Is.EqualTo(modomicsProducts.Count - 1));
        var curatedBaseLossFragments = curatedProducts.Where(p => p.ProductType.IsBaseLoss())
            .Select(p => p.FragmentNumber).ToHashSet();
        var modomicsOnly = modomicsProducts.Where(p => p.ProductType.IsBaseLoss()
            && !curatedBaseLossFragments.Contains(p.FragmentNumber)).ToList();
        Assert.That(modomicsOnly, Has.Count.EqualTo(1));
        Assert.That(modomicsOnly[0].FragmentNumber, Is.EqualTo(3));
        TestContext.Progress.WriteLine($"[2'-O-methyl, suppression on] counts: curated={curatedProducts.Count} modomics={modomicsProducts.Count}; base-loss #3 exists only in MODOMICS: {modomicsOnly[0].NeutralMass:0.####}");
    }

    private static void CompareSources(string label, string curatedSequence, string modomicsSequence,
        (int FragmentNumber, double ExpectedDelta)[] expectedBaseLossDeltas)
    {
        var curated = new OligoWithSetMods(curatedSequence, CuratedRnaMods);
        var modomics = new OligoWithSetMods(modomicsSequence, ModomicsMods);

        // Both sources encode the same chemistry, so the whole-oligo monoisotopic mass must match.
        Assert.That(modomics.MonoisotopicMass, Is.EqualTo(curated.MonoisotopicMass).Within(0.001), label);
        TestContext.Progress.WriteLine($"[{label}] oligo monoisotopic mass: curated={curated.MonoisotopicMass:0.####} modomics={modomics.MonoisotopicMass:0.####}");

        var curatedProducts = new List<Product>();
        var modomicsProducts = new List<Product>();
        curated.Fragment(DissociationType.CID, FragmentationTerminus.Both, curatedProducts);
        modomics.Fragment(DissociationType.CID, FragmentationTerminus.Both, modomicsProducts);

        TestContext.Progress.WriteLine($"[{label}] fragment counts: curated={curatedProducts.Count} modomics={modomicsProducts.Count}");

        // Non-base-loss products are independent of base-loss semantics: identical count and masses.
        var curatedBackbone = curatedProducts.Where(p => !p.ProductType.IsBaseLoss())
            .OrderBy(p => p.ProductType).ThenBy(p => p.FragmentNumber).ToList();
        var modomicsBackbone = modomicsProducts.Where(p => !p.ProductType.IsBaseLoss())
            .OrderBy(p => p.ProductType).ThenBy(p => p.FragmentNumber).ToList();
        Assert.That(modomicsBackbone.Count, Is.EqualTo(curatedBackbone.Count), label);
        for (int i = 0; i < curatedBackbone.Count; i++)
        {
            Assert.That(modomicsBackbone[i].NeutralMass, Is.EqualTo(curatedBackbone[i].NeutralMass).Within(1E-6),
                $"{label}: {curatedBackbone[i].ProductType} #{curatedBackbone[i].FragmentNumber}");
        }

        TestContext.Progress.WriteLine($"[{label}] identical non-base-loss fragments: {curatedBackbone.Count}");

        // Base-loss products: identical except where curated BaseModification semantics move part of
        // the modification off with the base.
        var expectedByFragment = expectedBaseLossDeltas.ToDictionary(d => d.FragmentNumber, d => d.ExpectedDelta);
        var curatedBaseLoss = curatedProducts.Where(p => p.ProductType.IsBaseLoss())
            .OrderBy(p => p.ProductType).ThenBy(p => p.FragmentNumber).ToList();
        var modomicsBaseLoss = modomicsProducts.Where(p => p.ProductType.IsBaseLoss())
            .OrderBy(p => p.ProductType).ThenBy(p => p.FragmentNumber).ToList();
        Assert.That(modomicsBaseLoss.Count, Is.EqualTo(curatedBaseLoss.Count), label);

        var identical = new List<string>();
        foreach (var curatedIon in curatedBaseLoss)
        {
            var modomicsIon = modomicsBaseLoss.First(p => p.ProductType == curatedIon.ProductType
                && p.FragmentNumber == curatedIon.FragmentNumber);
            var delta = modomicsIon.NeutralMass - curatedIon.NeutralMass;
            if (expectedByFragment.TryGetValue(curatedIon.FragmentNumber, out var expectedDelta))
            {
                Assert.That(delta, Is.EqualTo(expectedDelta).Within(1E-6),
                    $"{label}: base-loss #{curatedIon.FragmentNumber} ({curatedIon.ProductType})");
                TestContext.Progress.WriteLine($"[{label}] differing base-loss #{curatedIon.FragmentNumber}: curated={curatedIon.NeutralMass:0.####} modomics={modomicsIon.NeutralMass:0.####} delta={delta:0.####}");
            }
            else
            {
                Assert.That(delta, Is.EqualTo(0).Within(1E-6),
                    $"{label}: base-loss #{curatedIon.FragmentNumber} ({curatedIon.ProductType})");
                identical.Add($"#{curatedIon.FragmentNumber}={curatedIon.NeutralMass:0.####}");
            }
        }

        TestContext.Progress.WriteLine($"[{label}] identical base-loss fragments ({identical.Count}): {string.Join(", ", identical)}");
    }
}
