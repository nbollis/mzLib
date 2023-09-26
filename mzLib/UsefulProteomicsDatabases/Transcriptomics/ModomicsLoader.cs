using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using Newtonsoft.Json;
using Proteomics;
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
                if (mod.formula.IsNullOrEmpty())
                    continue;

                // Declare variable which must be set for construction of modification object
                string id = mod.short_name;
                string accession = mod.name;
                string modType = "Modomics";
                string featureType = null;
                string localizationRestriction = "Anywhere";
                string fileOrigin = modomicsLocation;
                Dictionary<string, IList<string>> databaseReference = new()
                {
                    { "Modomics", new List<string>() { $"{modification.Key}:{mod.short_name}" } }
                };
                Dictionary<string, IList<string>> taxonomicRange = null;
                List<string> keywords = new List<string>();
                Dictionary<DissociationType, List<double>> neutralLosses = null;
                Dictionary<DissociationType, List<double>> diagnosticIons = null;

                ChemicalFormula chemicalFormula = ChemicalFormula.ParseFormula(mod.formula.Replace("+", ""));
                foreach (var moiety in mod.reference_moiety)
                {
                    ModificationMotif.TryGetMotif(moiety, out ModificationMotif motif);
                    if (motif is null) continue;

                    ChemicalFormula adjustedFormula;
                    double adjustedMass;
                    switch (motif.ToString())
                    {
                        case "C":
                            adjustedFormula = chemicalFormula - Nucleotide.CytosineBase.NucleosideChemicalFormula;
                            break;
                        case "G":
                            adjustedFormula = chemicalFormula - Nucleotide.GuanineBase.NucleosideChemicalFormula;
                            break;
                        case "A":
                            adjustedFormula = chemicalFormula - Nucleotide.AdenineBase.NucleosideChemicalFormula;
                            break;
                        case "U":
                            adjustedFormula = chemicalFormula - Nucleotide.UracilBase.NucleosideChemicalFormula;
                            break;
                        // X means it can apply to many nucleotide bases, must be sorted based upon name of chemical
                        case "X":
                            // Purine: return the mod for A in the switch, then use the rest of the loop for G
                            if (mod.name.Contains("purin"))
                            {
                                ModificationMotif.TryGetMotif("A", out ModificationMotif newMotif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.AdenineBase.ThisChemicalFormula;
                                adjustedMass = adjustedFormula.MonoisotopicMass;

                                yield return new Modification(id, accession, modType, featureType, newMotif,
                                    localizationRestriction, adjustedFormula,
                                    adjustedMass, databaseReference, taxonomicRange, keywords, neutralLosses,
                                    diagnosticIons,
                                    fileOrigin);

                                ModificationMotif.TryGetMotif("G", out motif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.GuanineBase.ThisChemicalFormula;
                            }
                            // Pyrimidine: return the mod for C in the switch, then use the rest of the loop for U
                            else if (mod.name.Contains("pyridin") || mod.name.Contains("pyrimidin"))
                            {
                                ModificationMotif.TryGetMotif("C", out ModificationMotif newMotif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.CytosineBase.ThisChemicalFormula;
                                adjustedMass = adjustedFormula.MonoisotopicMass;

                                yield return new Modification(id, accession, modType, featureType, newMotif,
                                    localizationRestriction, adjustedFormula,
                                    adjustedMass, databaseReference, taxonomicRange, keywords, neutralLosses,
                                    diagnosticIons,
                                    fileOrigin);

                                ModificationMotif.TryGetMotif("U", out motif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.UracilBase.ThisChemicalFormula;
                            }
                            // Adenine: only applies to A
                            else if (mod.name.Contains("adenosine"))
                            {
                                ModificationMotif.TryGetMotif("A", out motif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.AdenineBase.ThisChemicalFormula;
                            }
                            // Only pseudouridine falls in this category
                            else if (mod.name.Contains("uridine"))
                            {
                                ModificationMotif.TryGetMotif("U", out motif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.UracilBase.ThisChemicalFormula;
                            }
                            // Three mods fall outside of the naming convention, all are pyrimidine derivatives
                            else
                            {
                                ModificationMotif.TryGetMotif("C", out ModificationMotif newMotif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.CytosineBase.ThisChemicalFormula;
                                adjustedMass = adjustedFormula.MonoisotopicMass;

                                yield return new Modification(id, accession, modType, featureType, newMotif,
                                    localizationRestriction, adjustedFormula,
                                    adjustedMass, databaseReference, taxonomicRange, keywords, neutralLosses,
                                    diagnosticIons,
                                    fileOrigin);

                                ModificationMotif.TryGetMotif("U", out motif);
                                adjustedFormula = (chemicalFormula - ChemicalFormula.ParseFormula("H2O")) -
                                                  Nucleotide.UracilBase.ThisChemicalFormula;
                            }
                            break;
                        default:
                            throw new ArgumentException("Motif must be one of the canonical Rna nucleotides or X for both purines");
                    }
                    adjustedMass = adjustedFormula.MonoisotopicMass;

                    yield return new Modification(id, accession, modType, featureType, motif, localizationRestriction, adjustedFormula,
                        adjustedMass, databaseReference, taxonomicRange, keywords, neutralLosses, diagnosticIons,
                        fileOrigin);

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

        /// <summary>
        /// Protonated mass
        /// </summary>
        public double? mass_prot { get; set; }
        public string name { get; set; }

        /// <summary>
        /// Protonated product ions generated from teh precursor mass [M+H]+
        /// TODO: Consider if these should be converted to [M-H]- for negative mode and entered as diagnostic ions
        /// </summary>
        public string product_ions { get; set; }
        public string[] reference_moiety { get; set; }
        public string short_name { get; set; }
        public string smile { get; set; }

        public override string ToString()
        {
            return $"{short_name} ({abbrev})";
        }
    }

}
