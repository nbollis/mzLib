using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using MassSpectrometry;
using Newtonsoft.Json;

namespace Readers.Transcriptomics
{
    /// <summary>
    /// Representation of the Modomics Database objects
    /// Optional fields are those which need an API call to get
    /// </summary>
    [JsonObject]
    public class ModomicsCsvEntry
    {
        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        public Modification ToModification()
        {
            throw new NotImplementedException();
        }

        [Name("ID")]
        [JsonIgnore]
        public int Id { get; set; }

        [Name("MODOMICS code")]
        [JsonProperty("modomics_code")]
        public string ModomicsCode { get; set; }

        [Name("Name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [Name("Reference NucleoBase")]
        [JsonProperty("reference_moiety")]
        public string[] ReferenceNucleoBase { get; set; }

        [Name("Short Name")]
        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [Name("New RNAMods code")]
        [JsonProperty("abbrev")]
        public string RnaModsCode_New { get; set; }

        [Name("Old RNAMods code")]
        [JsonIgnore]
        public string RnaModsCode_Old { get; set; }

        [Name("Moiety type")]
        [JsonIgnore]
        public string MoietyType { get; set; }

        [Optional]
        [Name("ChemicalFormula")]
        [JsonProperty("formula")]
        public string ChemicalFormula { get; set; }

        [Optional]
        [Name("Lc Elution Comment")]
        [JsonProperty("lc_elution_comment")]
        public string? LcElutionComment { get; set; }

        [Optional]
        [Name("Lc Elution Time")]
        [JsonProperty("lc_elution_time")]
        public string? LcElutionTime { get; set; }

        [Optional]
        [Name("Monoisotopic Mass (Da)")]
        [JsonProperty("mass_monoiso")]
        public double MonoisotopicMass { get; set; }

        [Optional]
        [Name("Average Mass (Da)")]
        [JsonProperty("mass_avg")]
        public double AverageMass { get; set; }

        [Optional]
        [Name("Prot Mass (Da)")]
        [JsonProperty("mass_prot")]
        public double? ProtMass { get; set; }

        [Optional]
        [Name("Product Ions")]
        [JsonProperty("product_ions")]
        public double? ProductIons { get; set; }

        [Optional]
        [Name("Smile")]
        [JsonProperty("smile")]
        public string Smile { get; set; }
    }
}
