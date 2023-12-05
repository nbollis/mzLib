using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using Omics.SpectrumMatch;
using Proteomics;
using Proteomics.PSM;

namespace SQL.MLDatabase
{
    /// <summary>
    /// Defines the sets of objects housed within the SQL database
    /// </summary>
    public class MLData
    {
        public Lazy<List<PsmFromTsv>> AllPsms { get; set; }
        public Lazy<List<MsDataScan>> AllScans { get; set; }
        public Lazy<List<Protein>> AllProteins { get; set; }
    }

    /// <summary>
    /// All objects which contain the MLData object must implement this interface
    /// </summary>
    public interface IMLData
    {
        MLData Data { get; set; }
    }
}
