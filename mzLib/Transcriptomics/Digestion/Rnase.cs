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
            int maxLength, List<IHasChemicalFormula>? potentialDigestedFivePrimeCaps = null, List<IHasChemicalFormula>? potentialDigestedThreePrimeCaps = null)
        {
            var oligos = new List<NucleolyticOligo>();
            if (potentialDigestedFivePrimeCaps is null or { Count: 0 })
                potentialDigestedFivePrimeCaps = new List<IHasChemicalFormula> { ChemicalFormula.ParseFormula("O-3P-1") };
            if (potentialDigestedThreePrimeCaps is null or { Count: 0 })
                potentialDigestedThreePrimeCaps = new List<IHasChemicalFormula> { ChemicalFormula.ParseFormula("H2O4P") };

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
                oligos.AddRange(FullDigestion(nucleicAcid, maxMissedCleavages, minLength, maxLength, potentialDigestedFivePrimeCaps, potentialDigestedThreePrimeCaps));
            }
            else
            {
                throw new ArgumentException(
                    "Cleave Specificity not defined for Rna digestion, currently supports Full and None");
            }

            return oligos;
        }

        private IEnumerable<NucleolyticOligo> FullDigestion(NucleicAcid nucleicAcid, int maxMissedCleavages,
            int minLength, int maxLength, List<IHasChemicalFormula> potentialFivePrimeCaps, List<IHasChemicalFormula> potentialThreePrimeCaps)
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

                        switch (oneBasedStartResidue)
                        {
                            // contains either original termini
                            case 1 when oneBasedEndResidue == nucleicAcid.Length:
                                fivePrimeTerminus = nucleicAcid.FivePrimeTerminus ?? NucleicAcid.DefaultFivePrimeTerminus;
                                threePrimeTerminus = nucleicAcid.ThreePrimeTerminus ?? NucleicAcid.DefaultThreePrimeTerminus;
                                yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                                    missedCleavages, CleavageSpecificity.Full, fivePrimeTerminus, threePrimeTerminus);
                                break;
                            // contains original 5' terminus? keep it then iterate through potential 3' termini
                            case 1:
                            {
                                fivePrimeTerminus = nucleicAcid.FivePrimeTerminus ?? NucleicAcid.DefaultFivePrimeTerminus;
                                foreach (var threePrime in potentialThreePrimeCaps)
                                    yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                                        missedCleavages, CleavageSpecificity.Full, fivePrimeTerminus, threePrime);
                                break;
                            }
                            // contains original 3' terminus? keep it then iterate through potential 5' termini
                            default:
                            {
                                if (oneBasedEndResidue == nucleicAcid.Length)
                                {
                                    threePrimeTerminus = nucleicAcid.ThreePrimeTerminus ?? NucleicAcid.DefaultThreePrimeTerminus;
                                    foreach (var fivePrime in potentialFivePrimeCaps)
                                        yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                                            missedCleavages, CleavageSpecificity.Full, fivePrime, threePrimeTerminus);
                                }
                                // contains nether original terminus ? iterate through both potential 5' and 3' termini
                                else
                                {
                                    foreach (var fivePrime in potentialFivePrimeCaps)
                                        foreach (var threePrime in potentialThreePrimeCaps)
                                                yield return new NucleolyticOligo(nucleicAcid, oneBasedStartResidue, oneBasedEndResidue,
                                                    missedCleavages, CleavageSpecificity.Full, fivePrime, threePrime);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
