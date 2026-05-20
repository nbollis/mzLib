using Omics;
using Omics.Digestion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mzPlot
{
    public class DigHist
    {
        private bool _hasRun;

        public IDigestionParams DigestionParams { get; }
        public List<IBioPolymer> BioPolymers { get; }
        public int BioPolymersCount => BioPolymers.Count;
        public int DigestedProductsCount { get; private set; }
        public int UniqueProductsCount { get; private set; }
        public Dictionary<int, int> DigestionLengthHistogram { get; private set; }

        public DigHist(IDigestionParams digestionParams, IEnumerable<IBioPolymer> bioPolymers)
        {
            ArgumentNullException.ThrowIfNull(digestionParams);
            ArgumentNullException.ThrowIfNull(bioPolymers);

            DigestionParams = digestionParams;
            BioPolymers = bioPolymers.ToList();
            DigestionLengthHistogram = new Dictionary<int, int>();
        }

        public DigHistResult Run(string sourceId, DigestionPolymerType polymerType)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source id is required.", nameof(sourceId));
            }

            EnsureRun();

            return new DigHistResult(
                sourceId,
                polymerType,
                DigestionParams.DigestionAgent.Name,
                DigestionParams.MaxMissedCleavages,
                DigestionParams.MinLength,
                DigestionParams.MaxLength,
                DigestionParams.FragmentationTerminus,
                DigestionParams.SearchModeType,
                BioPolymersCount,
                DigestedProductsCount,
                UniqueProductsCount,
                DigestionLengthHistogram);
        }

        private void EnsureRun()
        {
            if (_hasRun)
            {
                return;
            }

            List<IBioPolymerWithSetMods> digestedProducts = new();
            foreach (IBioPolymer bioPolymer in BioPolymers)
            {
                IEnumerable<IBioPolymerWithSetMods> digestionProducts = bioPolymer.Digest(DigestionParams, [], []);
                digestedProducts.AddRange(digestionProducts);
            }

            DigestedProductsCount = digestedProducts.Count;
            UniqueProductsCount = digestedProducts.Select(p => p.FullSequence).Distinct().Count();
            DigestionLengthHistogram = digestedProducts
                .GroupBy(p => p.Length)
                .ToDictionary(group => group.Key, group => group.Count());

            _hasRun = true;
        }
    }
}
