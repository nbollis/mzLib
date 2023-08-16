using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using Easy.Common.Extensions;
using MassSpectrometry;
using MathNet.Numerics;

namespace Transcriptomics
{
    public class OligoWithSetMods : NucleolyticOligo, IPrecursor, INucleicAcid
    {
        public OligoWithSetMods(NucleicAcid nucleicAcid, RnaDigestionParams digestionParams, int oneBaseStartResidue,
            int oneBasedEndResidue, int missedCleavages, CleavageSpecificity cleavageSpecificity,
            Dictionary<int, Modification> allModsOneIsNTerminus, int numFixedMods, IHasChemicalFormula? fivePrimeTerminus = null, IHasChemicalFormula? threePrimeTerminus = null ) : base(
            nucleicAcid, oneBaseStartResidue, oneBasedEndResidue, missedCleavages,
            cleavageSpecificity, fivePrimeTerminus, threePrimeTerminus)
        {
            _digestionParams = digestionParams;
            _allModsOneIsNterminus = allModsOneIsNTerminus;
            NumFixedMods = numFixedMods;
            FullSequence = (this as IPrecursor).DetermineFullSequence();
        }

        private RnaDigestionParams _digestionParams;
        private Dictionary<int, Modification> _allModsOneIsNterminus;
        private double? _monoisotopicMass;
        private ChemicalFormula? _thisChemicalFormula;
        private double? _mostAbundantMonoisotopicMass;

        public string FullSequence { get; private set; }
        public IDigestionParams DigestionParams => _digestionParams;
        public IBioPolymer Parent => NucleicAcid;
        public IHasChemicalFormula FivePrimeTerminus
        {
            get => _fivePrimeTerminus;
            set
            {
                _fivePrimeTerminus = value;
                _monoisotopicMass = null;
                _thisChemicalFormula = null;
                _mostAbundantMonoisotopicMass = null;
            }
        }

        public IHasChemicalFormula ThreePrimeTerminus
        {
            get => _threePrimeTerminus;
            set
            {
                _threePrimeTerminus = value;
                _monoisotopicMass = null;
                _thisChemicalFormula = null;
                _mostAbundantMonoisotopicMass = null;
            }
        }

        public double MonoisotopicMass
        {
            get
            {
                if (_monoisotopicMass is null)
                {
                    _monoisotopicMass = BaseSequence.Sum(nuc => Nucleotide.GetResidue(nuc).MonoisotopicMass) +
                                        AllModsOneIsNterminus.Values.Sum(mod => mod.MonoisotopicMass.Value) +
                                        FivePrimeTerminus.MonoisotopicMass +
                                        ThreePrimeTerminus.MonoisotopicMass;
                }
                return _monoisotopicMass.Value;
            }
        }

        public ChemicalFormula ThisChemicalFormula
        {
            get
            {
                if (_thisChemicalFormula is null)
                {
                    var fullFormula = new RNA(BaseSequence, FivePrimeTerminus, ThreePrimeTerminus).GetChemicalFormula();
                    foreach (var mod in AllModsOneIsNterminus.Values)
                    {
                        if (mod.ChemicalFormula is null)
                        {
                            fullFormula = null;
                            break;
                        }
                        fullFormula.Add(mod.ChemicalFormula);
                    }
                    _thisChemicalFormula = fullFormula;
                }
                return _thisChemicalFormula!;
            }
        }

        public double MostAbundantMonoisotopicMass
        {
            get
            {
                if (_mostAbundantMonoisotopicMass is null)
                {
                    var distribution = IsotopicDistribution.GetDistribution(ThisChemicalFormula);
                    double maxIntensity = distribution.Intensities.Max();
                    _mostAbundantMonoisotopicMass =
                        distribution.Masses[distribution.Intensities.IndexOf(maxIntensity)].Round(9);
                }
                return _mostAbundantMonoisotopicMass.Value;
            }
        }

        public string SequenceWithChemicalFormulas => throw new NotImplementedException();

        public Dictionary<int, Modification> AllModsOneIsNterminus => _allModsOneIsNterminus;
        public int NumMods => AllModsOneIsNterminus.Count;
        public int NumFixedMods { get; }
        public int NumVariableMods => NumMods - NumFixedMods;


        public void Fragment(DissociationType dissociationType, FragmentationTerminus fragmentationTerminus,
            List<IProduct> products)
        {

        }

   
    }
}
