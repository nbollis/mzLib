using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public interface IDigestionParams
    {
        public int MaxMissedCleavages { get; set; }
        public int MinPeptideLength { get; set; }
        public int MaxPeptideLength { get; set; }
        public int MaxModificationIsoforms { get; set; }
        public int MaxModsForPeptide { get; set; }
        public DigestionAgent Enzyme { get; }
    }
}
