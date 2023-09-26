using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;
using NUnit.Framework;
using Readers.Transcriptomics;
using Test.DatabaseTests;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test.Transcriptomics
{
    [TestFixture]
    public class ModTest
    {
        public static string ModomicsPath =
            @"C:\Users\Nic\source\repos\mzLib\mzLib\Test\Transcriptomics\TestData\modomicsmods.json";

        [Test]
        public void TestSpecificModomicsModsLoading()
        {
            List<Modification> loadedMods = new();

            foreach (var mod in Loaders.LoadModomics(ModomicsPath))
            {
                loadedMods.Add(mod);
            }
           


            // test values where Motif is X (multiple locations)
            // Purines
            // 2-amino-9-[2-deoxyribofuranosyl]-9H-purine-5'-monophosphate
            var modsToTest = loadedMods.Where(p => p.OriginalId == "2PR").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("A")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("O-1").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("G")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("O-2").MonoisotopicMass).Within(0.001));

            // purine riboside-5'-monophosphate
            modsToTest = loadedMods.Where(p => p.OriginalId == "P5P").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("A")).MonoisotopicMass,
                               Is.EqualTo(ChemicalFormula.ParseFormula("H-1N-1").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("G")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("H-1N-1O-1").MonoisotopicMass).Within(0.001));

            // [(2~{R},3~{S},4~{R},5~{R})-5-(2-azanyl-6-diazanyl-purin-9-yl)-3,4-bis(oxidanyl)oxolan-2-yl]methoxyphosphinic acid
            modsToTest = loadedMods.Where(p => p.OriginalId == "O2Z").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("A")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("H2N2").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("G")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("H2N2O-1").MonoisotopicMass).Within(0.001));


            // pyridin && pyrimidin
            // 1-(beta-D-ribofuranosyl)-pyridin-4-one-5'-phosphate
            modsToTest = loadedMods.Where(p => p.OriginalId == "ONE").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("CN-2").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("CHN-1O-1").MonoisotopicMass).Within(0.001));
            //  1-(beta-D-ribofuranosyl)-pyrimidin-2-one-5'-phosphate
            modsToTest = loadedMods.Where(p => p.OriginalId == "PYO").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("H-1N-1").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("O-1").MonoisotopicMass).Within(0.001));
            // (5S)-5-{3-[(3S)-3-amino-3-carboxypropyl]-1-methyl-2,4-dioxo-1,2,3,4-tetrahydropyrimidin-5-yl}-2,5-anhydro-1-O-phosphono-L-arabinitol
            modsToTest = loadedMods.Where(p => p.OriginalId == "C4J").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C5H8O3").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C5H9NO2").MonoisotopicMass).Within(0.001));


            // other
            // (1S)-1,4-anhydro-1-(2,4-difluoro-5-methylphenyl)-5-O-phosphono-D-ribitol
            modsToTest = loadedMods.Where(p => p.OriginalId == "NF2").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C3HN-3O-1F2").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C3H2N-2O-2F2").MonoisotopicMass).Within(0.001));

            // D-ribofuranosyl-benzene-5'-monophosphate
            modsToTest = loadedMods.Where(p => p.OriginalId == "PYY").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C2HN-3O-1").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("C2H2N-2O-2").MonoisotopicMass).Within(0.001));

            // 8-aza-nebularine-5'-monophosphate
            modsToTest = loadedMods.Where(p => p.OriginalId == "8AZ").ToList();
            Assert.That(modsToTest.Count, Is.EqualTo(2));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("C")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("N2").MonoisotopicMass).Within(0.001));
            Assert.That(modsToTest.First(p => p.Target.ToString().Equals("U")).MonoisotopicMass,
                Is.EqualTo(ChemicalFormula.ParseFormula("HN3O-1").MonoisotopicMass).Within(0.001));

        }


    }
}
