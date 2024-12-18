using BenchmarkDotNet.Attributes;
using Omics;
using Omics.Digestion;
using Omics.Modifications;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using UsefulProteomicsDatabases;

namespace Profiling
{
    public class DigestionBenchMark
    {
        private string databasePath = @"B:\Users\Nic\RadicalFragmentation\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
        private List<Protein> proteins;
        private List<Modification> FixedMods;
        private List<Modification> VariableMods;
        private DigestionParams digestionParams_BottomUp;
        private DigestionParams digestionParams_TopDown;

        [Params(0, 1, 2)]
        public int MaxVariableMods;


        [GlobalSetup]
        public void Setup()
        {
            var allMods = GlobalVariables.AllModsKnown;
            proteins = ProteinDbLoader.LoadProteinXML(databasePath, true, DecoyType.None, allMods, false, null, out var un);

            digestionParams_BottomUp = new(maxModsForPeptides: MaxVariableMods);
            digestionParams_TopDown = new("top-down", maxModsForPeptides: MaxVariableMods);

            var phosphoString = "ID   Phosphorylation\r\nTG   S or T\r\nPP   Anywhere.\r\nNL   HCD:H0 or HCD:H3 O4 P1\r\nMT   Common Biological\r\nCF   H1 O3 P1\r\nDR   Unimod; 21.\r\n//";
            var acetyl1String = "ID   Acetylation\r\nTG   X\r\nPP   N-terminal.\r\nMT   Common Biological\r\nCF   H2 C2 O1\r\nDR   Unimod; 1.\r\n//";
            var acetyl2String = "ID   Acetylation\r\nTG   K\r\nPP   Anywhere.\r\nMT   Common Biological\r\nNL   ETD:45.0204\r\nDI   HCD:C7 H11 N1 O1\r\nCF   H2 C2 O1\r\nDR   Unimod; 1.\r\n//";
            var carbamString = "ID   Carbamidomethyl\r\nTG   C\r\nPP   Anywhere.\r\nMT   Common Fixed\r\nCF   H3 C2 N1 O1\r\nDR   ";
            var phospho = PtmListLoader.ReadModsFromString(phosphoString, out List<(Modification, string)> modsOut).ToList();
            var acetyl1 = PtmListLoader.ReadModsFromString(acetyl1String, out modsOut).ToList();
            var acetyl2 = PtmListLoader.ReadModsFromString(acetyl2String, out modsOut).ToList();
            var carbam = PtmListLoader.ReadModsFromString(carbamString, out modsOut).ToList();

            VariableMods = phospho.Concat(acetyl1).Concat(acetyl2).ToList();
            FixedMods = carbam;
        }

        [Benchmark]
        public List<IBioPolymerWithSetMods> BottomUp_Digestion()
        {
            List<IBioPolymerWithSetMods> peptides = new List<IBioPolymerWithSetMods>();
            foreach (var protein in proteins)
            {
                peptides.AddRange(protein.Digest(digestionParams_BottomUp, FixedMods, VariableMods));
            }
            return peptides;
        }

        [Benchmark]
        public List<IBioPolymerWithSetMods> TopDown_Digestion()
        {
            List<IBioPolymerWithSetMods> peptides = new List<IBioPolymerWithSetMods>();
            foreach (var protein in proteins)
            {
                peptides.AddRange(protein.Digest(digestionParams_TopDown, FixedMods, VariableMods));
            }
            return peptides;
        }
    } 
}
