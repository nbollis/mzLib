// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work copyright 2016 Stefan Solntsev
//
// This file (TestAminoAcids.cs) is part of Proteomics.
//
// Proteomics is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Proteomics is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
// License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with Proteomics. If not, see <http://www.gnu.org/licenses/>.

using Chemistry;
using NUnit.Framework;
using Proteomics.AminoAcidPolymer;
using System;
using System.Diagnostics.CodeAnalysis;
using IO.MzML;
using MassSpectrometry;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;

namespace Test
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public sealed class TestAminoAcids
    {
        private static Stopwatch Stopwatch { get; set; }

        [SetUp]
        public static void Setup()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        [TearDown]
        public static void TearDown()
        {
            Console.WriteLine($"Analysis time: {Stopwatch.Elapsed.Hours}h {Stopwatch.Elapsed.Minutes}m {Stopwatch.Elapsed.Seconds}s");
        }

        [Test]
        public static void TESTNAME()
        {
            string spectraPath = @"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytCHgh\Sample28_Avg(20)CaliOpenModern\Task2-CalibrateTask\221110_CaMyoUbiqCytCHgh_130541641_5%_Sample28_25IW_-averaged-calib.mzML";
            MsDataFile myMSDataFile = Mzml.LoadAllStaticData(spectraPath);
            var msNScans = myMSDataFile.GetAllScansList().Where(x => x.MsnOrder > 1).ToArray();
            var ms2Scans = msNScans.Where(p => p.MsnOrder == 2).ToArray();
            var ms3Scans = msNScans.Where(p => p.MsnOrder == 3).ToArray();

            var ms2Scan = myMSDataFile.GetOneBasedScan(12);
            var precursorScan = myMSDataFile.GetOneBasedScan(ms2Scan.OneBasedPrecursorScanNumber.Value);


            ms2Scan.RefineSelectedMzAndIntensity(precursorScan.MassSpectrum);

            var envelopes = ms2Scan.GetIsolatedMassesAndCharges(precursorScan.MassSpectrum, 1, 60, 4, 3);

        }
        
        [Test]
        public void GetResidueByCharacter()
        {
            Residue aa = Residue.GetResidue('A');

            Assert.AreEqual("Alanine", aa.Name);
        }

        [Test]
        public void GetResidueByCharacterString()
        {
            Residue aa = Residue.GetResidue("A");

            Assert.AreEqual(aa.Name, "Alanine");
        }

        [Test]
        public void GetResidueByName()
        {
            Residue aa = Residue.GetResidue("Alanine");

            Assert.AreEqual("Alanine", aa.Name);
        }

        [Test]
        public void GetResidueNotInDictionary()
        {
            Assert.IsFalse(Residue.TryGetResidue("?", out Residue r));
            Assert.IsFalse(Residue.TryGetResidue('?', out r));
        }

        [Test]
        public void ResidueMonoisotopicMassTest()
        {
            Assert.AreEqual(Residue.ResidueMonoisotopicMass['A'], Residue.GetResidue('A').MonoisotopicMass, 1e-9);
        }
    }
}