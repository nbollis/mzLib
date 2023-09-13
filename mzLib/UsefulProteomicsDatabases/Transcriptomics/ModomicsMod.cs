using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsefulProteomicsDatabases.Transcriptomics
{
    internal class ModomicsMod
    {
        public string abbrev { get; set; }
        public string formula { get; set; }
        public string lc_elution_comment { get; set; }
        public string lc_elution_time { get; set; }
        public double mass_avg { get; set; }
        public double mass_monoiso { get; set; }
        public double mass_prot { get; set; }
        public string modomics_code { get; set; }
        public string name { get; set; }
        public string product_ions { get; set; }
        public string[] reference_moiety { get; set; }
        public string short_name { get; set; }
        public string smile { get; set; }
    }
}
