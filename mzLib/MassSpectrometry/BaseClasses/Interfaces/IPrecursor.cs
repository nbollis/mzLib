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
        string BaseSequence { get; }
        string FullSequence { get; }
        double MostAbundantMonoisotopicMass { get; }
        Dictionary<int, Modification> AllModsOneIsNterminus { get; }
        int NumMods { get; }
        int NumFixedMods { get; }
        int NumVariableMods { get; }
        int Length { get; }
        char this[int zeroBasedIndex] => BaseSequence[zeroBasedIndex];


        public void Fragment(DissociationType dissociationType, FragmentationTerminus fragmentationTerminus, List<IProduct> products);

        public string DetermineFullSequence()
        {
            var subSequence = new StringBuilder();

            // modification on peptide N-terminus
            if (AllModsOneIsNterminus.TryGetValue(1, out Modification mod))
            {
                subSequence.Append('[' + mod.ModificationType + ":" + mod.IdWithMotif + ']');
            }

            for (int r = 0; r < Length; r++)
            {
                subSequence.Append(this[r]);

                // modification on this residue
                if (AllModsOneIsNterminus.TryGetValue(r + 2, out mod))
                {
                    subSequence.Append('[' + mod.ModificationType + ":" + mod.IdWithMotif + ']');
                }
            }

            // modification on peptide C-terminus
            if (AllModsOneIsNterminus.TryGetValue(Length + 2, out mod))
            {
                subSequence.Append('[' + mod.ModificationType + ":" + mod.IdWithMotif + ']');
            }

            return subSequence.ToString();
        }
    }

    public interface IBioPolymer : IHasMass
    {
        public IEnumerable<IPrecursor> Digest(DigestionParametersBase digestionParams, List<Modification> allKnownFixedModifications,
            List<Modification> variableModifications);
    }
}
