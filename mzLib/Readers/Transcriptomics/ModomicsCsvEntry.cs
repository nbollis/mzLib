using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace Readers.Transcriptomics
{
    public class ModomicsCsvEntry
    {
        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        [Name("ID")]
        public int Id { get; set; }

        [Name("MODOMICS code")]
        public string ModomicsCode { get; set; }

        [Name("Name")]
        public string Name { get; set; }

        [Name("Reference NucleoBase")]
        public string ReferenceNucleoBase { get; set; }

        [Name("Short Name")]
        public string ShortName { get; set; }

        [Name("New RNAMods code")]
        public string RnaModsCode_New { get; set; }

        [Name("Old RNAMods code")]
        public string RnaModsCode_Old { get; set; }

        [Name("Moiety type")]
        public string MoietyType { get; set; }
    }
}
