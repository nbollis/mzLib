using Proteomics.Fragmentation;
using System;
using System.ComponentModel.Design;

namespace Proteomics.ProteolyticDigestion
{
    public class DigestionParams : DigestionParametersBase
    {
        // this parameterless constructor needs to exist to read the toml.
        // if you can figure out a way to get rid of it, feel free...
        public DigestionParams() : this("trypsin")
        {
        }

        public DigestionParams(string protease = "trypsin", int maxMissedCleavages = 2, int minPeptideLength = 7, int maxPeptideLength = int.MaxValue,
            int maxModificationIsoforms = 1024, InitiatorMethionineBehavior initiatorMethionineBehavior = InitiatorMethionineBehavior.Variable,
            int maxModsForPeptides = 2, CleavageSpecificity searchModeType = CleavageSpecificity.Full, FragmentationTerminus fragmentationTerminus = FragmentationTerminus.Both,
            bool generateUnlabeledProteinsForSilac = true, bool keepNGlycopeptide = false, bool keepOGlycopeptide = false) 
            : base(maxMissedCleavages, minPeptideLength, maxPeptideLength, maxModificationIsoforms, maxModsForPeptides)
        {
            Protease = ProteaseDictionary.Dictionary[protease];
            InitiatorMethionineBehavior = initiatorMethionineBehavior;
            SearchModeType = searchModeType;
            FragmentationTerminus = fragmentationTerminus;
            RecordSpecificProtease();
            GeneratehUnlabeledProteinsForSilac = generateUnlabeledProteinsForSilac;
            KeepNGlycopeptide = keepNGlycopeptide;
            KeepOGlycopeptide = keepOGlycopeptide;
        }
        public InitiatorMethionineBehavior InitiatorMethionineBehavior { get; private set; }

        public Protease Protease
        {
            get => (Protease)Enzyme;
            private set => Enzyme = value;
        }
        public CleavageSpecificity SearchModeType { get; private set; } //for fast semi and nonspecific searching of proteases
        public FragmentationTerminus FragmentationTerminus { get; private set; } //for fast semi searching of proteases
        public Protease SpecificProtease { get; private set; } //for fast semi and nonspecific searching of proteases
        public bool GeneratehUnlabeledProteinsForSilac { get; private set; } //used to look for unlabeled proteins (in addition to labeled proteins) for SILAC experiments
        public bool KeepNGlycopeptide { get; private set; }
        public bool KeepOGlycopeptide { get; private set; }

        public override bool Equals(object obj)
        {
            return obj is DigestionParams a
                && MaxMissedCleavages.Equals(a.MaxMissedCleavages)
                && MinPeptideLength.Equals(a.MinPeptideLength)
                && MaxPeptideLength.Equals(a.MaxPeptideLength)
                && InitiatorMethionineBehavior.Equals(a.InitiatorMethionineBehavior)
                && MaxModificationIsoforms.Equals(a.MaxModificationIsoforms)
                && MaxModsForPeptide.Equals(a.MaxModsForPeptide)
                && Protease.Equals(a.Protease)
                && SearchModeType.Equals(a.SearchModeType)
                && FragmentationTerminus.Equals(a.FragmentationTerminus)
                && GeneratehUnlabeledProteinsForSilac.Equals(a.GeneratehUnlabeledProteinsForSilac)
                && KeepNGlycopeptide.Equals(a.KeepNGlycopeptide)
                && KeepOGlycopeptide.Equals(a.KeepOGlycopeptide);
        }

        public override int GetHashCode()
        {
            return
                MaxMissedCleavages.GetHashCode()
                ^ InitiatorMethionineBehavior.GetHashCode()
                ^ MaxModificationIsoforms.GetHashCode()
                ^ MaxModsForPeptide.GetHashCode();
        }

        public override string ToString()
        {
            return MaxMissedCleavages + "," + InitiatorMethionineBehavior + "," + MinPeptideLength + "," + MaxPeptideLength + ","
                + MaxModificationIsoforms + "," + MaxModsForPeptide + "," + SpecificProtease.Name + "," + SearchModeType + "," + FragmentationTerminus + ","
                + GeneratehUnlabeledProteinsForSilac + "," + KeepNGlycopeptide + "," + KeepOGlycopeptide;
        }

        private void RecordSpecificProtease()
        {
            SpecificProtease = Protease;
            if (SearchModeType == CleavageSpecificity.None) //nonspecific searches, which might have a specific protease
            {
                Protease = FragmentationTerminus == FragmentationTerminus.N ?
                   ProteaseDictionary.Dictionary["singleN"] :
                   ProteaseDictionary.Dictionary["singleC"];
            }
        }
    }
}