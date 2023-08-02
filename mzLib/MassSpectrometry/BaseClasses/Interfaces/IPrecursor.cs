using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;

namespace MassSpectrometry
{
    public interface IPrecursor : IHasMass
    {
        public void Fragment(DissociationType dissociationType, FragmentationTerminus fragmentationTerminus, List<IProduct> products);
    }

    public interface IBioPolymer : IHasMass
    {
        //public IEnumerable<IPrecursor> Digest(IDigestionParameters digestionParams, List<IModificaiton> allKnownFixedModifications,
        //    List<IModificaiton> variableModifications);
    }
}
