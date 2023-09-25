using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void TESTNAME()
        {

            foreach (var mod in Loaders.LoadModomics(ModomicsPath))
            {
                
            }


            
        }

        [Test]
        public static void NucleotideTest()
        {

        }

    }
}
