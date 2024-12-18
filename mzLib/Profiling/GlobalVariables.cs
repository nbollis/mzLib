global using PsmFromTsv = Proteomics.PSM.PsmFromTsv;
global using SpectrumMatchTsv = Omics.SpectrumMatch.SpectrumMatchFromTsv;
global using SpectrumMatchTsvReader = Readers.SpectrumMatchTsvReader;
using Omics.Modifications;
using UsefulProteomicsDatabases;

namespace ResultAnalyzerUtil
{
    public static class GlobalVariables
    {
        public static string DataDir { get; private set; }
        private static List<Modification> _AllModsKnown;
        private static HashSet<string> _AllModTypesKnown;

        public static List<string> ErrorsReadingMods;
        public static IEnumerable<Modification> AllModsKnown => _AllModsKnown.AsEnumerable();
        public static IEnumerable<string> AllModTypesKnown => _AllModTypesKnown.AsEnumerable();
        public static Dictionary<string, Modification> AllModsKnownDictionary { get; private set; }
        public static UsefulProteomicsDatabases.Generated.obo PsiModDeserialized { get; private set; }
        public static IEnumerable<Modification> UnimodDeserialized { get; private set; }
        public static IEnumerable<Modification> UniprotDeseralized { get; private set; }
        public static List<Modification> ProteaseMods = new List<Modification>();

        static GlobalVariables()
        {
            Loaders.LoadElements();
            DataDir = AppDomain.CurrentDomain.BaseDirectory;

            // Load Modifications
            _AllModsKnown = new List<Modification>();
            _AllModTypesKnown = new HashSet<string>();
            ErrorsReadingMods = new List<string>();
            AllModsKnownDictionary = new Dictionary<string, Modification>();

            UnimodDeserialized = Loaders.LoadUnimod(Path.Combine(DataDir, @"Resources", @"unimod.xml")).ToList();
            PsiModDeserialized = Loaders.LoadPsiMod(Path.Combine(DataDir, @"Resources", @"PSI-MOD.obo.xml"));
            var formalChargesDictionary = Loaders.GetFormalChargesDictionary(PsiModDeserialized);
            UniprotDeseralized = Loaders.LoadUniprot(Path.Combine(DataDir, @"Resources", @"ptmlist.txt"), formalChargesDictionary).ToList();

            foreach (var modFile in Directory.GetFiles(Path.Combine(DataDir, @"Resources")))
            {
                AddMods(PtmListLoader.ReadModsFromFile(modFile, out var errorMods), false);
            }

            AddMods(UniprotDeseralized.OfType<Modification>(), false);
            AddMods(UnimodDeserialized.OfType<Modification>(), false);

            foreach (Modification mod in AllModsKnown)
            {
                if (!AllModsKnownDictionary.ContainsKey(mod.IdWithMotif))
                {
                    AllModsKnownDictionary.Add(mod.IdWithMotif, mod);
                }
                // no error thrown if multiple mods with this ID are present - just pick one
            }
            //ProteaseMods = PtmListLoader.ReadModsFromFile(Path.Combine(DataDir, @"Mods", @"ProteaseMods.txt"), out var errors).ToList();
            //ProteaseDictionary.Dictionary = ProteaseDictionary.LoadProteaseDictionary(Path.Combine(DataDir, @"ProteolyticDigestion", @"proteases.tsv"), ProteaseMods);
        }


        public static void AddMods(IEnumerable<Modification> modifications, bool modsAreFromTheTopOfProteinXml)
        {
            foreach (var mod in modifications)
            {
                if (string.IsNullOrEmpty(mod.ModificationType) || string.IsNullOrEmpty(mod.IdWithMotif))
                {
                    ErrorsReadingMods.Add(mod.ToString() + Environment.NewLine + " has null or empty modification type");
                    continue;
                }
                if (AllModsKnown.Any(b => b.IdWithMotif.Equals(mod.IdWithMotif) && b.ModificationType.Equals(mod.ModificationType) && !b.Equals(mod)))
                {
                    if (modsAreFromTheTopOfProteinXml)
                    {
                        _AllModsKnown.RemoveAll(p => p.IdWithMotif.Equals(mod.IdWithMotif) && p.ModificationType.Equals(mod.ModificationType) && !p.Equals(mod));
                        _AllModsKnown.Add(mod);
                        _AllModTypesKnown.Add(mod.ModificationType);
                    }
                    else
                    {
                        ErrorsReadingMods.Add("Modification id and type are equal, but some fields are not! " +
                            "The following mod was not read in: " + Environment.NewLine + mod.ToString());
                    }
                    continue;
                }
                else if (AllModsKnown.Any(b => b.IdWithMotif.Equals(mod.IdWithMotif) && b.ModificationType.Equals(mod.ModificationType)))
                {
                    // same ID, same mod type, and same mod properties; continue and don't output an error message
                    // this could result from reading in an XML database with mods annotated at the top
                    // that are already loaded in MetaMorpheus
                    continue;
                }
                else if (AllModsKnown.Any(m => m.IdWithMotif == mod.IdWithMotif))
                {
                    // same ID but different mod types. This can happen if the user names a mod the same as a UniProt mod
                    // this is problematic because if a mod is annotated in the database, all we have to go on is an ID ("description" tag).
                    // so we don't know which mod to use, causing unnecessary ambiguity
                    if (modsAreFromTheTopOfProteinXml)
                    {
                        _AllModsKnown.RemoveAll(p => p.IdWithMotif.Equals(mod.IdWithMotif) && !p.Equals(mod));
                        _AllModsKnown.Add(mod);
                        _AllModTypesKnown.Add(mod.ModificationType);
                    }
                    else if (!mod.ModificationType.Equals("Unimod"))
                    {
                        ErrorsReadingMods.Add("Duplicate mod IDs! Skipping " + mod.ModificationType + ":" + mod.IdWithMotif);
                    }
                    continue;
                }
                else
                {
                    // no errors! add the mod
                    _AllModsKnown.Add(mod);
                    _AllModTypesKnown.Add(mod.ModificationType);
                }
            }
        }
    }
}
