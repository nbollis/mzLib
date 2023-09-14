using Chemistry;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public interface IBioPolymer 
    {
        string Name { get; }
        string BaseSequence { get; }
        int Length { get; }
        string DatabaseFilePath { get; }
        bool IsDecoy { get; }
        bool IsContaminant { get; }
        public string Organism { get; }
        

        IDictionary<int, List<Modification>> OneBasedPossibleLocalizedModifications { get; }
        IEnumerable<IPrecursor> Digest(IDigestionParams digestionParams, List<Modification> allKnownFixedModifications,
            List<Modification> variableModifications, List<SilacLabel> silacLabels = null, (SilacLabel startLabel, SilacLabel endLabel)? turnoverLabels = null, bool topDownTruncationSearch = false);
    }
}
