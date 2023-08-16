using Chemistry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public interface IBioPolymer : IHasMass
    {
        public IEnumerable<IPrecursor> Digest(DigestionParametersBase digestionParams, List<Modification> allKnownFixedModifications,
            List<Modification> variableModifications);
    }
}
