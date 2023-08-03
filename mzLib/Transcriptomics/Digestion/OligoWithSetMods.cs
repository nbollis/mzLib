using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;

namespace Transcriptomics
{
    public class OligoWithSetMods : NucleolyticOligo, IPrecursor
    {
        internal OligoWithSetMods(NucleicAcid nucleicAcid, int oneBaseStartResidueInNucleicAcid,
            int oneBasedEndResidueInNucleicAcid, int missedCleavages, CleavageSpecificity cleavageSpecificity) : base(
            nucleicAcid, oneBaseStartResidueInNucleicAcid, oneBasedEndResidueInNucleicAcid, missedCleavages,
            cleavageSpecificity)
        {

        }

        private RnaDigestionParams _digestionParams;
        private Dictionary<int, Modification> _allModsOneIsNterminus;
        private double? _monoisotopicMass;
        private ChemicalFormula? _thisChemicalFormula;
        private double? _mostAbundantMonoisotopicMass;

        public string FullSequence { get; private set; }
        public RnaDigestionParams DigestionParams => _digestionParams;

        public double MonoisotopicMass
        {
            get
            {
                if (_monoisotopicMass is null)
                {
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                }
                return _thisChemicalFormula;
            }
        }

        public double MostAbundantMonoisotopicMass
        {
            get
            {
                if (_mostAbundantMonoisotopicMass is null)
                {
                    throw new NotImplementedException();
                }

                return _mostAbundantMonoisotopicMass.Value;
            }
        }

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
