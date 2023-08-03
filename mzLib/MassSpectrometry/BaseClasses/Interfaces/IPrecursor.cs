using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;

namespace MassSpectrometry
{
    public interface IPrecursor : IHasChemicalFormula
    {
        public string FullSequence { get;   }
        public double MostAbundantMonoisotopicMass { get; }
        public Dictionary<int, Modification> AllModsOneIsNterminus { get; }
        public int NumMods { get; }
        public int NumFixedMods { get; }
        public int NumVariableMods { get; }


        public void Fragment(DissociationType dissociationType, FragmentationTerminus fragmentationTerminus, List<IProduct> products);

        public virtual void DetermineFullSequence()
        {

        }
    }

    public interface IBioPolymer : IHasMass
    {
        //public IEnumerable<IPrecursor> Digest(IDigestionParameters digestionParams, List<IModificaiton> allKnownFixedModifications,
        //    List<IModificaiton> variableModifications);
    }
}
