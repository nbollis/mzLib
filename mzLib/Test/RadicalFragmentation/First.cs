using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using Omics.Fragmentation;
using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using Readers;
using UsefulProteomicsDatabases;

namespace Test.RadicalFragmentation
{
    internal class First
    {

        // neutral lossese are Iodine loss and loss of entire tag plus sulfur from the cysteine
        public static Modification CysteineTag =
            PtmListLoader
                .ReadModsFromString(
                    "ID   Radical Fragmentation Tag\r\nTG   C\r\nPP   Anywhere.\r\n" +
                    "MT   Common Fixed\r\nCF   C8 H5 O I\r\nNL   RadicalUVPD:126.904473\r\nNL   RadicalUVPD:275.910578\r\nNL   RadicalUVPD:56.0626\r\n//",
                    out _).First();

        [Test]
        public static void TESTNAME()
        {
            var tag = CysteineTag;


            //HEMOGLOBIN GLUTAMER-256
            string sequence =
                "VLSPADKTNVKAAWGKVGAHAGEYGAEALERMFLSFPTTKTYFPHFDLSHGSAQVKGHGKKVADALTNAVAHVDDMPNALSALSDLHAHKLRVDPVNFKLLSHCLLVTLAAHLPAEFTPAVHASLDKFLASVSTVLTSKYR";
            var protein = new Protein(sequence, "ABCD");
            var fixedMods = new List<Modification>() { tag };
            var digestionParams = new DigestionParams("top-down");
            var digestionProduct = protein.Digest(digestionParams, fixedMods, new List<Modification>()).First();
            var scanPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "RadicalFragmentation", "TestData", "alpha_hemo4IPE18+_50ms.raw");
            var file = MsDataFileReader.GetDataFile(scanPath).LoadAllStaticData();


            var products = new List<Product>();
            digestionProduct.Fragment(DissociationType.RadicalUVPD, FragmentationTerminus.Both, products);
            foreach (var scan in file.Scans)
            {
                var matched = MatchFragmentIons(scan, products).OrderBy(p => p.Mz).ToList();
            }


        }

        public static List<MatchedFragmentIon> MatchFragmentIons(MsDataScan ms2Scan, List<Product> theoreticalProducts)
        {
            var matchedFragmentIons = new List<MatchedFragmentIon>();
            var ions = new List<string>();
            var productMassTolerance = new PpmTolerance(20);

            var envelopes = Deconvoluter.Deconvolute(ms2Scan, new ClassicDeconvolutionParameters(1, 20, 20, 3),
                ms2Scan.ScanWindowRange).OrderBy(p => p.MonoisotopicMass).ToList();
            foreach (var product in theoreticalProducts)
            {
                if (double.IsNaN(product.NeutralMass))
                    continue;

                var closestEnvelope = GetClosestIsotopicEnvelope(envelopes, product.NeutralMass);
                if (closestEnvelope != null && (
                    productMassTolerance.Within(product.NeutralMass, closestEnvelope.MonoisotopicMass) 
                    /*|| productMassTolerance.Within(product.NeutralMass -57, closestEnvelope.MonoisotopicMass)*/))
                {
                    matchedFragmentIons.Add(new MatchedFragmentIon(product, closestEnvelope.MonoisotopicMass.ToMz(closestEnvelope.Charge), closestEnvelope.Peaks.First().intensity, closestEnvelope.Charge));
                    ions.Add(product.ToString());
                }
            }




            return matchedFragmentIons;
        }

        public static IsotopicEnvelope GetClosestIsotopicEnvelope(List<IsotopicEnvelope> dict, double mass)
        {
            var values = dict.Select(p => p.MonoisotopicMass).ToArray();
            int index = Array.BinarySearch(values, mass);
            if (index >= 0)
            {
                return dict[index];
            }
            index = ~index;

            if (index == values.Length)
            {
                return dict[index - 1];
            }
            if (index == 0 || mass - values[index - 1] > values[index] - mass)
            {
                return dict[index];
            }

            return dict[index - 1];
        }
    }
}
