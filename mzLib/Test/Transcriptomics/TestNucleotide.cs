﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using NUnit.Framework;
using pepXML.Generated;
using Transcriptomics;
using UsefulProteomicsDatabases;

namespace Test.Transcriptomics
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    internal class TestNucleotide
    {

        internal record NucleotideTestCase(Nucleotide Nucleotide, string Name, char OneLetterCode, string Symbol, ChemicalFormula Formula, double Mass);

        internal static IEnumerable<NucleotideTestCase> GetNucleotideTestCases()
        {
            Loaders.LoadElements();

            yield return new NucleotideTestCase(Nucleotide.AdenineBase, "Adenine", 'A', "Ade", ChemicalFormula.ParseFormula("C5H4N5"), 329.052523);
            yield return new NucleotideTestCase(Nucleotide.CytosineBase, "Cytosine", 'C', "Cyt", ChemicalFormula.ParseFormula("C4H4N3O1"), 305.041290);
            yield return new NucleotideTestCase(Nucleotide.GuanineBase, "Guanine", 'G', "Gua", ChemicalFormula.ParseFormula("C5H4N5O1"), 345.047438);
            yield return new NucleotideTestCase(Nucleotide.UracilBase, "Uracil", 'U', "Ura", ChemicalFormula.ParseFormula("C4H3N2O2"), 306.025306);
            yield return new NucleotideTestCase(Nucleotide.DeoxyAdenineBase, "DeoxyAdenine", 'B', "dAde", ChemicalFormula.ParseFormula("C5H4N5"), 313.057607);
            yield return new NucleotideTestCase(Nucleotide.DeoxyCytosineBase, "DeoxyCytosine", 'D', "dCyt", ChemicalFormula.ParseFormula("C4H4N3O1"), 289.046375);
            yield return new NucleotideTestCase(Nucleotide.DeoxyGuanineBase, "DeoxyGuanine", 'H', "dGua", ChemicalFormula.ParseFormula("C5H4N5O1"), 329.052523);
            yield return new NucleotideTestCase(Nucleotide.DeoxyThymineBase, "DeoxyThymine", 'V', "dThy", ChemicalFormula.ParseFormula("C5H5N2O2"), 304.046041);
        }

        [Test]
        [TestCaseSource(nameof(GetNucleotideTestCases))]
        public void TestCommonNucleotides(NucleotideTestCase testCase)
        {
            Nucleotide nucleotide = testCase.Nucleotide;

            Assert.That(nucleotide.MonoisotopicMass, Is.EqualTo(testCase.Mass).Within(0.00001));
            Assert.That(nucleotide.Letter, Is.EqualTo(testCase.OneLetterCode));
            Assert.That(nucleotide.Symbol, Is.EqualTo(testCase.Symbol));
            Assert.That(nucleotide.ToString(), Is.EqualTo($"{testCase.OneLetterCode} {testCase.Symbol} ({testCase.Name})"));

            Nucleotide newNucleotide =
                new Nucleotide(testCase.Name, testCase.OneLetterCode, testCase.Symbol, testCase.Formula);
            Assert.That(nucleotide.Equals(nucleotide));
            Assert.That(!nucleotide.Equals(null));
            Assert.That(nucleotide.Equals(newNucleotide));
            Assert.That(nucleotide.Equals((object)newNucleotide));
            Assert.That(!nucleotide.Equals((object)null));
        }

        [Test]
        [TestCaseSource(nameof(GetNucleotideTestCases))]
        public void TestGetResidue(NucleotideTestCase testCase)
        {
            Nucleotide nucleotide = testCase.Nucleotide;

            var testNucleotide = Nucleotide.GetResidue(testCase.OneLetterCode);
            Assert.That(nucleotide.Equals(testNucleotide));

            if (Nucleotide.TryGetResidue(testCase.OneLetterCode, out Nucleotide outTide))
            {
                Assert.That(nucleotide.Equals(outTide));
                Assert.That(nucleotide.MassEquals(outTide.MonoisotopicMass));
            }
            else
                Assert.Fail();

            testNucleotide = Nucleotide.GetResidue(testCase.Symbol);
            Assert.That(nucleotide.Equals(testNucleotide));
            if (Nucleotide.TryGetResidue(testCase.Symbol, out outTide))
            {
                Assert.That(nucleotide.Equals(outTide));
                Assert.That(nucleotide.MonoisotopicMass.MassEquals(outTide));
            }
            else
                Assert.Fail();
        }

        [Test]
        public static void TestCustomResidue()
        {
            string name = "FakeNucleotide";
            char oneLetter = 'F';
            string symbol = "Fke";
            string chemicalFormula = "C5H5N2O2";
            var fakeNucleotide = new Nucleotide(name, oneLetter, symbol, ChemicalFormula.ParseFormula(chemicalFormula));

            Nucleotide.AddResidue(name, oneLetter, symbol, chemicalFormula);

            // test new nucleotide is within dictionary
            if (Nucleotide.TryGetResidue('F', out Nucleotide outTide))
            {
                Assert.That(fakeNucleotide.Equals(outTide));
                Assert.That(fakeNucleotide.MonoisotopicMass.MassEquals(outTide.MonoisotopicMass));
            }
            else
                Assert.Fail();

            if (Nucleotide.TryGetResidue("Fke", out outTide))
            {
                Assert.That(fakeNucleotide.Equals(outTide));
                Assert.That(fakeNucleotide.MonoisotopicMass.MassEquals(outTide.MonoisotopicMass));
            }
            else
                Assert.Fail();

            // test false result in TryGetResidue
            if (Nucleotide.TryGetResidue('P', out outTide))
                Assert.Fail();

            if (Nucleotide.TryGetResidue("Taco", out outTide))
                Assert.Fail();
        }
    }
}