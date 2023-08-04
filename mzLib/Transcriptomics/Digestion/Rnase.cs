using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;

namespace Transcriptomics
{
    public class Rnase : DigestionAgent, IEquatable<Rnase>
    {
        public Rnase(string name, CleavageSpecificity cleaveSpecificity, List<DigestionMotif> motifList, Modification cleavageMod = null) :
            base(name, cleaveSpecificity, motifList, cleavageMod)
        {
            Name = name;
            CleavageSpecificity = cleaveSpecificity;
            DigestionMotifs = motifList;
        }

        public bool Equals(Rnase? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Rnase)obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public List<NucleolyticOligo> GetUnmodifiedOligos(NucleicAcid nucleicAcid, int maxMissedCleavages, int minLength,
            int maxLength)
        {
            var oligos = new List<NucleolyticOligo>();

            // top down
            if (CleavageSpecificity == CleavageSpecificity.None)
            {
                if (OkayLength(nucleicAcid.Length, minLength, maxLength))
                    oligos.Add(new NucleolyticOligo(nucleicAcid, 1, nucleicAcid.Length,
                        0, CleavageSpecificity.Full, nucleicAcid.FivePrimeTerminus, nucleicAcid.ThreePrimeTerminus));
            }
            // full cleavage
            else if (CleavageSpecificity == CleavageSpecificity.Full)
            {
                oligos.AddRange(FullDigestion(nucleicAcid, maxMissedCleavages, minLength, maxLength));
            }
            else
            {
                throw new ArgumentException(
                    "Cleave Specificity not defined for Rna digestion, currently supports Full and None");
            }

            return oligos;
        }

        private IEnumerable<NucleolyticOligo> FullDigestion(NucleicAcid nucleicAcid, int maxMissedCleavages,
            int minLength, int maxLength)
        {
            List<int> oneBasedIndicesToCleaveAfter = GetDigestionSiteIndices(nucleicAcid.BaseSequence);
            for (int missedCleavages = 0; missedCleavages <= maxMissedCleavages; missedCleavages++)
            {
                for (int i = 0; i < oneBasedIndicesToCleaveAfter.Count - missedCleavages - 1; i++)
                {
                    if (OkayLength(oneBasedIndicesToCleaveAfter[i + missedCleavages + 1] - oneBasedIndicesToCleaveAfter[i], minLength, maxLength))
                    {
                        int oneBasedStartResidue = oneBasedIndicesToCleaveAfter[i] + 1;
                        int oneBasedEndResidue = oneBasedIndicesToCleaveAfter[i + missedCleavages + 1];

                        IHasChemicalFormula fivePrimeTerminus;
                        IHasChemicalFormula threePrimeTerminus;



                        // contains original 5' terminus
                        if (oneBasedStartResidue == 1)
                            fivePrimeTerminus = nucleicAcid.FivePrimeTerminus;
                        
                        else
                            fivePrimeTerminus = ChemicalFormula.ParseFormula("O-3P-1");

                        // contains original 3' terminus
                        if (oneBasedEndResidue == nucleicAcid.Length)
                            threePrimeTerminus = nucleicAcid.ThreePrimeTerminus;
                        else
                            threePrimeTerminus = ChemicalFormula.ParseFormula("H2O4P");


                        yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                            missedCleavages, CleavageSpecificity.Full, fivePrimeTerminus, threePrimeTerminus);











                        //// no digestion occurs
                        //if (oneBasedEndResidue - oneBasedStartResidue == nucleicAcid.Length - 1)
                        //{
                        //    fivePrimeTerminus = nucleicAcid.FivePrimeTerminus;
                        //    threePrimeTerminus = nucleicAcid.ThreePrimeTerminus;

                        //    yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                        //        missedCleavages, CleavageSpecificity.Full, fivePrimeTerminus, threePrimeTerminus);
                        //}

                        //// contains original 5' terminus
                        //else if (oneBasedStartResidue == 1)
                        //{
                        //    fivePrimeTerminus = nucleicAcid.FivePrimeTerminus;
                        //    threePrimeTerminus = ChemicalFormula.ParseFormula("H2O4P");
                        //}

                        //// contains original 3' terminus
                        //else if (oneBasedEndResidue == nucleicAcid.Length)
                        //{
                        //    fivePrimeTerminus = ChemicalFormula.ParseFormula("OH");
                        //    threePrimeTerminus = nucleicAcid.ThreePrimeTerminus;
                        //}

                        //// central digestion product
                        //else
                        //{
                        //    fivePrimeTerminus = NucleicAcid.DefaultFivePrimeTerminus;
                        //    threePrimeTerminus = ChemicalFormula.ParseFormula("HO3P");
                        //}

                        //yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                        //    missedCleavages, CleavageSpecificity.Full, fivePrimeTerminus, threePrimeTerminus);
                    }
                }
            }
        }
    }
}
