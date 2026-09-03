using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using Omics.Digestion;
using Omics.Modifications;

using Transcriptomics;
using Transcriptomics.Digestion;

using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test.Transcriptomics;

[TestFixture]
public static class ModomicsOneLetterCodeEquivalenceTests
{
    private const string ModomicsTestHeader = ">id:999|Name:modomics-coded|SOterm:SO:999|Type:tRNA|Subtype:Test|Feature:Demo|Cellular_Localization:cytosol|Species:Test organism";

    [Test]
    public static void OneLetterCodeFastaAndExplicitRna_CreateIdenticalRnaState()
    {
        var modomicsLoaded = LoadSingleRnaFromFasta("AKLM");
        var explicitRna = BuildExplicitRna();

        AssertEquivalentRna(modomicsLoaded, explicitRna);
    }

    [Test]
    public static void OneLetterCodeFastaAndExplicitRna_GenerateIdenticalModifiedOligo()
    {
        var modomicsLoaded = LoadSingleRnaFromFasta("AKLM");
        var explicitOligo = BuildFullLengthOligo(BuildExplicitRna());

        var modomicsOligo = BuildFullLengthOligo(modomicsLoaded);

        AssertEquivalentOligo(modomicsOligo, explicitOligo);
    }

    private static RNA LoadSingleRnaFromFasta(string codedSequence)
    {
        var fastaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, $"modomics_equivalence_{Guid.NewGuid():N}.fasta");
        File.WriteAllText(fastaPath, ModomicsTestHeader + Environment.NewLine + codedSequence);

        try
        {
            var rnas = RnaDbLoader.LoadRnaFasta(fastaPath, true, DecoyType.None, false, out var errors);
            Assert.That(errors, Is.Empty);
            return rnas.Single();
        }
        finally
        {
            File.Delete(fastaPath);
        }
    }

    private static RNA BuildExplicitRna()
    {
        var codeMap = Mods.ModomicsLoadReport.OneLetterCodeToMod;
        return new RNA("AGGC", new Dictionary<int, List<Modification>>
        {
            { 2, [codeMap['K']] },
            { 3, [codeMap['L']] },
            { 4, [codeMap['M']] },
        });
    }

    private static OligoWithSetMods BuildFullLengthOligo(RNA rna)
    {
        var allModsOneIsNterminus = new Dictionary<int, Modification>();
        foreach (var kvp in rna.OneBasedPossibleLocalizedModifications)
        {
            foreach (var modification in kvp.Value)
            {
                allModsOneIsNterminus.Add(kvp.Key + 1, modification);
            }
        }

        return new OligoWithSetMods(rna, new RnaDigestionParams(), 1, rna.Length, 0, CleavageSpecificity.Full,
            allModsOneIsNterminus, numFixedMods: 0, rna.FivePrimeTerminus, rna.ThreePrimeTerminus);
    }

    private static void AssertEquivalentRna(RNA actual, RNA expected)
    {
        Assert.That(actual.BaseSequence, Is.EqualTo(expected.BaseSequence));
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        Assert.That(actual.MonoisotopicMass, Is.EqualTo(expected.MonoisotopicMass).Within(1e-9));
        Assert.That(actual.ThisChemicalFormula, Is.EqualTo(expected.ThisChemicalFormula));
        Assert.That(actual.OneBasedPossibleLocalizedModifications.Keys.OrderBy(p => p),
            Is.EqualTo(expected.OneBasedPossibleLocalizedModifications.Keys.OrderBy(p => p)));

        foreach (var kvp in expected.OneBasedPossibleLocalizedModifications)
        {
            Assert.That(actual.OneBasedPossibleLocalizedModifications[kvp.Key].Count,
                Is.EqualTo(kvp.Value.Count));

            for (var i = 0; i < kvp.Value.Count; i++)
            {
                Assert.That(actual.OneBasedPossibleLocalizedModifications[kvp.Key][i].IdWithMotif,
                    Is.EqualTo(kvp.Value[i].IdWithMotif));
            }
        }
    }

    private static void AssertEquivalentOligo(OligoWithSetMods actual, OligoWithSetMods expected)
    {
        Assert.That(actual.BaseSequence, Is.EqualTo(expected.BaseSequence));
        Assert.That(actual.FullSequence, Is.EqualTo(expected.FullSequence));
        Assert.That(actual.NumMods, Is.EqualTo(expected.NumMods));
        Assert.That(actual.MonoisotopicMass, Is.EqualTo(expected.MonoisotopicMass).Within(1e-9));
        Assert.That(actual.ThisChemicalFormula, Is.EqualTo(expected.ThisChemicalFormula));
        CollectionAssert.AreEquivalent(expected.AllModsOneIsNterminus.Keys, actual.AllModsOneIsNterminus.Keys);

        foreach (var kvp in expected.AllModsOneIsNterminus)
        {
            Assert.That(actual.AllModsOneIsNterminus[kvp.Key].IdWithMotif, Is.EqualTo(kvp.Value.IdWithMotif));
        }
    }
}
