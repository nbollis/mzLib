using Chemistry;
using NUnit.Framework;
using Omics.BioPolymer;
using Omics.Modifications;
using Omics.SequenceConversion;
using Proteomics;
using Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;

namespace Test.DatabaseTests
{
    [TestFixture]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TestModificationConversionToUniprot
    {
        [Test]
        public static void TestDatabaseWriteReadWithModificationConversion()
        {
            // Create dummy proteins with MetaMorpheus-style modifications
            ModificationMotif.TryGetMotif("S", out var motifS);
            ModificationMotif.TryGetMotif("T", out var motifT);
            ModificationMotif.TryGetMotif("K", out var motifK);

            var phosphoMM = new Modification(
                _originalId: "Phosphorylation",
                _modificationType: "Common Biological",
                _target: motifS,
                _locationRestriction: "Anywhere.",
                _chemicalFormula: ChemicalFormula.ParseFormula("H1O3P1"));

            var acetylMM = new Modification(
                _originalId: "Acetylation",
                _modificationType: "Common Biological",
                _target: motifK,
                _locationRestriction: "Anywhere.",
                _chemicalFormula: ChemicalFormula.ParseFormula("C2H2O1"));

            // Create proteins with mods
            var protein1Mods = new Dictionary<int, List<Modification>>
            {
                { 2, new List<Modification> { acetylMM } },
                { 5, new List<Modification> { phosphoMM } }
            };

            var protein2Mods = new Dictionary<int, List<Modification>>
            {
                { 3, new List<Modification> { phosphoMM } },
                { 7, new List<Modification> { acetylMM } }
            };

            var protein1 = new Protein(
                "MKPSIDE",
                "Protein1",
                oneBasedModifications: protein1Mods);

            var protein2 = new Protein(
                "MASKTDE",
                "Protein2",
                oneBasedModifications: protein2Mods);

            var proteins = new List<Protein> { protein1, protein2 };

            // Write MetaMorpheus proteins to XML
            string mmXmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_metamorpheus_mods.xml");
            ProteinDbWriter.WriteXmlDatabase(
                new Dictionary<string, HashSet<System.Tuple<int, Modification>>>(),
                proteins,
                mmXmlPath);

            // Read back MetaMorpheus proteins
            var mmProteins = ProteinDbLoader.LoadProteinXML(
                mmXmlPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods1);

            // Collect all modification strings from MetaMorpheus file
            var mmModStrings = new List<string>();
            var mmFileLines = File.ReadAllLines(mmXmlPath);
            foreach (var line in mmFileLines)
            {
                if (line.Contains("modified residue"))
                {
                    mmModStrings.Add(line.Trim());
                }
            }

            // Convert proteins to UniProt naming convention
            foreach (var protein in proteins)
            {
                protein.ConvertModifications(UniProtSequenceSerializer.Instance);
            }

            // Write converted proteins to second XML file
            string uniprotXmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_uniprot_mods.xml");
            ProteinDbWriter.WriteXmlDatabase(
                new Dictionary<string, HashSet<System.Tuple<int, Modification>>>(),
                proteins,
                uniprotXmlPath);

            // Read back UniProt proteins
            var uniprotProteins = ProteinDbLoader.LoadProteinXML(
                uniprotXmlPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods2);

            // Collect all modification strings from UniProt file
            var uniprotModStrings = new List<string>();
            var uniprotFileLines = File.ReadAllLines(uniprotXmlPath);
            foreach (var line in uniprotFileLines)
            {
                if (line.Contains("modified residue"))
                {
                    uniprotModStrings.Add(line.Trim());
                }
            }

            // Verify that modification naming conventions are different
            var mmHasPhosphorylation = mmModStrings.Any(s => s.Contains("Phosphorylation")) && !mmModStrings.Any(s => s.Contains("Phosphoserine"));
            var uniprotHasPhosphoserine = uniprotModStrings.Any(s => s.Contains("Phosphoserine")) && !uniprotModStrings.Any(s => s.Contains("Phosphorylation"));

            Assert.That(mmHasPhosphorylation, Is.True, "MetaMorpheus file should have 'Phosphorylation' mod type");
            Assert.That(uniprotHasPhosphoserine, Is.True, "UniProt file should have 'UniProt' mod type");

            // Verify both sets of proteins have the same number
            Assert.That(mmProteins.Count, Is.EqualTo(uniprotProteins.Count));

            // Verify chemical equivalence for each protein
            for (int i = 0; i < mmProteins.Count; i++)
            {
                // Same sequence
                Assert.That(mmProteins[i].BaseSequence, Is.EqualTo(uniprotProteins[i].BaseSequence));

                // Same modification positions
                var mmModPositions = mmProteins[i].OneBasedPossibleLocalizedModifications.Keys.OrderBy(k => k).ToList();
                var upModPositions = uniprotProteins[i].OneBasedPossibleLocalizedModifications.Keys.OrderBy(k => k).ToList();
                Assert.That(mmModPositions, Is.EqualTo(upModPositions));

                // Chemical formulas match at each position
                foreach (var position in mmModPositions)
                {
                    var mmMod = mmProteins[i].OneBasedPossibleLocalizedModifications[position][0];
                    var upMod = uniprotProteins[i].OneBasedPossibleLocalizedModifications[position][0];

                    if (mmMod.ChemicalFormula != null && upMod.ChemicalFormula != null)
                    {
                        Assert.That(mmMod.ChemicalFormula.Equals(upMod.ChemicalFormula), Is.True,
                            $"Chemical formulas should match for protein {i} at position {position}");
                    }

                    // Targets should match
                    Assert.That(mmMod.Target.ToString(), Is.EqualTo(upMod.Target.ToString()),
                        $"Targets should match for protein {i} at position {position}");
                }
            }

            // Verify the modification type strings are actually different
            var mmFirstModType = mmProteins[0].OneBasedPossibleLocalizedModifications.First().Value[0].ModificationType;
            var upFirstModType = uniprotProteins[0].OneBasedPossibleLocalizedModifications.First().Value[0].ModificationType;
            Assert.That(mmFirstModType, Is.Not.EqualTo(upFirstModType),
                "Modification types should be different between conventions");

            // Clean up
            File.Delete(mmXmlPath);
            File.Delete(uniprotXmlPath);
        }

        [Test]
        public static void TestDatabaseWriteReadWithSequenceVariationMods()
        {
            // Test that sequence variation mods survive write/read cycle
            ModificationMotif.TryGetMotif("S", out var motifS);

            var phosphoMM = new Modification(
                _originalId: "Phosphorylation",
                _modificationType: "Common Biological",
                _target: motifS,
                _locationRestriction: "Anywhere.",
                _chemicalFormula: ChemicalFormula.ParseFormula("H1O3P1"));

            var variantMods = new Dictionary<int, List<Modification>>
            {
                { 1, new List<Modification> { phosphoMM } }
            };

            var sequenceVariation = new SequenceVariation(
                oneBasedBeginPosition: 3,
                oneBasedEndPosition: 3,
                originalSequence: "A",
                variantSequence: "S",
                description: "A3S",
                oneBasedModifications: variantMods);

            var protein = new Protein(
                "MAAADE",
                "TestProteinWithVariant",
                sequenceVariations: new List<SequenceVariation> { sequenceVariation });

            var proteins = new List<Protein> { protein };

            // Write to XML
            string xmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_variant_mods.xml");
            ProteinDbWriter.WriteXmlDatabase(
                new Dictionary<string, HashSet<System.Tuple<int, Modification>>>(),
                proteins,
                xmlPath);

            // Read back
            var readProteins = ProteinDbLoader.LoadProteinXML(
                xmlPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods);

            // Verify sequence variation and its mods were preserved
            Assert.That(readProteins[0].SequenceVariations.Count, Is.EqualTo(1));
            Assert.That(readProteins[0].SequenceVariations.First().OneBasedModifications.Count, Is.EqualTo(1));

            var readMod = readProteins[0].SequenceVariations.First().OneBasedModifications[1][0];
            Assert.That(readMod.ChemicalFormula.Equals(ChemicalFormula.ParseFormula("H1O3P1")), Is.True);

            // Now test conversion
            readProteins[0].ConvertModifications(UniProtSequenceSerializer.Instance);
            Assert.That(readProteins[0].SequenceVariations.First().OneBasedModifications[1][0].ModificationType,
                Is.EqualTo("UniProt"));

            // Clean up
            File.Delete(xmlPath);
        }

        [Test]
        public static void TestDatabaseWriteReadPreservesModificationDetails()
        {
            // Test that all mod details (neutral losses, diagnostic ions) are preserved
            ModificationMotif.TryGetMotif("S", out var motifS);

            var phosphoWithNL = new Modification(
                _originalId: "Phosphorylation",
                _modificationType: "Common Biological",
                _target: motifS,
                _locationRestriction: "Anywhere.",
                _chemicalFormula: ChemicalFormula.ParseFormula("H1O3P1"),
                _neutralLosses: new Dictionary<MassSpectrometry.DissociationType, List<double>>
                {
                    { MassSpectrometry.DissociationType.HCD, new List<double> { 97.976896 } }
                });

            var oneBasedMods = new Dictionary<int, List<Modification>>
            {
                { 3, new List<Modification> { phosphoWithNL } }
            };

            var protein = new Protein(
                "MASIDE",
                "TestProteinWithNL",
                oneBasedModifications: oneBasedMods);

            var proteins = new List<Protein> { protein };

            // Write to XML
            string xmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_nl_mods.xml");
            ProteinDbWriter.WriteXmlDatabase(
                new Dictionary<string, HashSet<System.Tuple<int, Modification>>>(),
                proteins,
                xmlPath);

            // Read back
            var readProteins = ProteinDbLoader.LoadProteinXML(
                xmlPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods);

            // Verify neutral losses were preserved
            var readMod = readProteins[0].OneBasedPossibleLocalizedModifications[3][0];
            Assert.That(readMod.NeutralLosses, Is.Not.Null);
            Assert.That(readMod.NeutralLosses.ContainsKey(MassSpectrometry.DissociationType.HCD), Is.True);

            readProteins.ForEach(p => p.ConvertModifications(UniProtSequenceSerializer.Instance));
            Assert.That(readProteins[0].OneBasedPossibleLocalizedModifications[3][0].ModificationType,
                Is.EqualTo("UniProt"));

            // Clean up
            File.Delete(xmlPath);
        }

        [Test]
        public void GptmdDatabase_AllMzlibGetConverted()
        {
            string dbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DatabaseTests", "cRAP_databaseGPTMD.xml");
            var proteins = ProteinDbLoader.LoadProteinXML(
                dbPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods);

            foreach (var protein in proteins)
                protein.ConvertModifications(UniProtSequenceSerializer.Instance);

            string tempDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "cRAP_databaseGPTMD_converted.xml");
            ProteinDbWriter.WriteXmlDatabase(
                new Dictionary<string, HashSet<System.Tuple<int, Modification>>>(),
                proteins,
                tempDbPath);

            var convertedProteins = ProteinDbLoader.LoadProteinXML(
                tempDbPath,
                true,
                DecoyType.None,
                new List<Modification>(),
                false,
                new List<string>(),
                out var unknownMods2);

            Assert.That(convertedProteins.Count, Is.EqualTo(proteins.Count));

            // One Base Possible
            for (int i = 0; i < proteins.Count; i++)
            {
                var originalMods = proteins[i].OneBasedPossibleLocalizedModifications;
                var convertedMods = convertedProteins[i].OneBasedPossibleLocalizedModifications;
                Assert.That(convertedMods.Keys, Is.EquivalentTo(originalMods.Keys));
                foreach (var position in originalMods.Keys)
                {
                    var originalMod = originalMods[position][0];
                    var convertedMod = convertedMods[position][0];
                    Assert.That(convertedMod.ChemicalFormula.Equals(originalMod.ChemicalFormula), Is.True,
                        $"Chemical formulas should match for protein {i} at position {position}");
                    Assert.That(convertedMod.Target.ToString(), Is.EqualTo(originalMod.Target.ToString()),
                        $"Targets should match for protein {i} at position {position}");
                }
            }

            // Original Non-Variant
            for (int i = 0; i < proteins.Count; i++)
            {
                var originalMods = proteins[i].OriginalNonVariantModifications;
                var convertedMods = convertedProteins[i].OriginalNonVariantModifications;
                Assert.That(convertedMods.Keys, Is.EquivalentTo(originalMods.Keys));
                foreach (var position in originalMods.Keys)
                {
                    var originalMod = originalMods[position][0];
                    var convertedMod = convertedMods[position][0];
                    Assert.That(convertedMod.ChemicalFormula.Equals(originalMod.ChemicalFormula), Is.True,
                        $"Chemical formulas should match for protein {i} at position {position}");
                    Assert.That(convertedMod.Target.ToString(), Is.EqualTo(originalMod.Target.ToString()),
                        $"Targets should match for protein {i} at position {position}");
                }
            }

            // Cleanup 
            File.Delete(tempDbPath);
        }
    }
}
