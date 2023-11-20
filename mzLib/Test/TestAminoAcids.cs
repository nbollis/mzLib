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
using Proteomics;
using Proteomics.AminoAcidPolymer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;
using Stopwatch = System.Diagnostics.Stopwatch;

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
        public static void GetDbInfo()
        {
            Dictionary<string, string> databases = new()
            {
                {"Human", @"D:\Databases\Human_uniprotkb_proteome_UP000005640_AND_revi_2023_09_29.fasta"},
                {"Yeast", @"D:\Databases\Yeast_uniprotkb_proteome_UP000002311_2023_10_05.fasta" },
                
            };

            string outpath = @"C:\Users\Nic\Downloads\proteinLength2.csv";

            using (StreamWriter output = new StreamWriter(outpath))
            {
                output.WriteLine("Organism,Length,Count");
                foreach (var database in databases)
                {
                    List<Protein> proteins;
                    proteins = database.Value.EndsWith(".fasta") ?
                        ProteinDbLoader.LoadProteinFasta(database.Value, true, DecoyType.None, false, out List<string> errors)
                        : ProteinDbLoader.LoadProteinXML(database.Value, true, DecoyType.None, null, false, null, out var dbOptions);
                    var proteinsGroupedBySize = proteins.GroupBy(p => p.Length)
                        .OrderBy(p => p.Key)
                        .ToDictionary(p => p.Key, p => p.Count());

                    foreach (var group in proteinsGroupedBySize)
                    {
                        output.WriteLine(database.Key + "," + group.Key + "," + group.Value);
                    }
                }
            }
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