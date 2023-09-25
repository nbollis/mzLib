using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using Newtonsoft.Json;
using Transcriptomics;

namespace UsefulProteomicsDatabases
{
    internal static class ModomicsLoader
    {
        internal static IEnumerable<Modification> ReadMods(string modomicsLocation)
        {
            var jsonString = File.ReadAllText(modomicsLocation);
            var modDictionary = JsonConvert.DeserializeObject<Dictionary<int, ModomicsModification>>(jsonString);


            foreach (var modification in modDictionary)
            {
                var mod = modification.Value;

                // Declare variable which must be set for construction of modification object
                string id;
                string accession;
                string modType = "";
                string featureType = null;
                string localizationRestriction;
                ChemicalFormula chemicalFormula;
                double? monoMass;
                Dictionary<string, IList<string>> databaseReference = null;
                Dictionary<string, IList<string>> taxonomicRange = null;
                List<string> keywords = new List<string>();
                Dictionary<DissociationType, List<double>> neutralLosses = null;
                Dictionary<DissociationType, List<double>> diagnosticIons = null;
                string fileOrigin = "Modomics";

                // throw out any mods that do not have the correct criteria
                if (mod.formula.IsNullOrEmpty())
                    continue;




                // parse modomics mod for above values
                id = mod.short_name;
                accession = mod.name;
                monoMass = mod.mass_monoiso;

                string chemicalFormulaString = mod.formula.Replace("+", "");
                chemicalFormula = ChemicalFormula.ParseFormula(chemicalFormulaString);





                foreach (var moiety in mod.reference_moiety)
                {
                    ModificationMotif.TryGetMotif(moiety, out ModificationMotif motif);
                    if (motif is not null)
                    {

                        ChemicalFormula adjustedFormula;
                        double adjustedMass;
                        switch (motif.ToString())
                        {
                            // Modomics formula and mass is based upon the nucleoside, adjust as necessary
                            case "C":
                                adjustedFormula = chemicalFormula - Nucleotide.CytosineBase.NucleosideChemicalFormula;
                                adjustedMass = monoMass is null ?
                                    adjustedFormula.MonoisotopicMass : monoMass.Value - Nucleotide.CytosineBase.NucleosideChemicalFormula.MonoisotopicMass;
                                break;
                            case "G":
                                adjustedFormula = chemicalFormula - Nucleotide.GuanineBase.NucleosideChemicalFormula;
                                adjustedMass = monoMass is null ? 
                                    adjustedFormula.MonoisotopicMass : monoMass.Value - Nucleotide.GuanineBase.NucleosideChemicalFormula.MonoisotopicMass;
                                break;
                            case "A":
                                adjustedFormula = chemicalFormula - Nucleotide.AdenineBase.NucleosideChemicalFormula;
                                adjustedMass = monoMass is null ? 
                                    adjustedFormula.MonoisotopicMass : monoMass.Value - Nucleotide.AdenineBase.NucleosideChemicalFormula.MonoisotopicMass;
                                break;
                            case "U":
                                adjustedFormula = chemicalFormula - Nucleotide.UracilBase.NucleosideChemicalFormula;
                                adjustedMass = monoMass is null ? 
                                    adjustedFormula.MonoisotopicMass : monoMass.Value - Nucleotide.UracilBase.NucleosideChemicalFormula.MonoisotopicMass;
                                break;
                            default:
                                Debugger.Break();
                                adjustedFormula = chemicalFormula;
                                adjustedMass = monoMass ?? adjustedFormula.MonoisotopicMass;
                                break;
                        }




                        localizationRestriction = "Anywhere.";
                        yield return new Modification(id, accession, modType, featureType, motif, localizationRestriction, adjustedFormula,
                            adjustedMass, databaseReference, taxonomicRange, keywords, neutralLosses, diagnosticIons,
                                                                                  fileOrigin);
                    }

                }
            }
        }
    }



    internal class ModomicsModification
    {
        public string abbrev { get; set; }
        public string formula { get; set; }
        public string? lc_elution_comment { get; set; }
        public string? lc_elution_time { get; set; }
        public double? mass_avg { get; set; }
        public double? mass_monoiso { get; set; }

        // protonated mass
        public double? mass_prot { get; set; }
        public string name { get; set; }
        public string product_ions { get; set; }
        public List<string> reference_moiety { get; set; }
        public string short_name { get; set; }
        public string smile { get; set; }

        public override string ToString()
        {
            return $"{short_name} ({abbrev})";
        }
    }

}
