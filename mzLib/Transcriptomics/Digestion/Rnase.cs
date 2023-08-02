using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class Rnase : DigestionEnzyme, IEquatable<Rnase>
    {
        public Rnase(string name, CleavageSpecificity cleaveSpecificity, List<DigestionMotif> motifList) :
            base(name, cleaveSpecificity, motifList)
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
                        0, CleavageSpecificity.Full));
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
                        yield return new NucleolyticOligo(nucleicAcid, oneBasedIndicesToCleaveAfter[i] + 1, oneBasedIndicesToCleaveAfter[i + missedCleavages + 1],
                            missedCleavages, CleavageSpecificity.Full);
                    }
                }
            }
        }
    }
}
