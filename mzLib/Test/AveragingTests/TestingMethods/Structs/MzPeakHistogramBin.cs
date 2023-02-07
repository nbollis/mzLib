using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Test.AveragingTests
{
    public readonly record struct MzPeakHistogramBin(int BinIndex, double Start, double End, List<MzPeak> PeaksInBin)
    {
        public double AverageBinValue => PeaksInBin.Select(p => p.Intensity).Average();
        public double StandardDeviationBinValue => PeaksInBin.Select(p => p.Intensity).StandardDeviation();
        public int PeakCount => PeaksInBin.Count;
        public string BinStringForOutput => $"{Start},{End}";

        public override string ToString()
        {
            return $"Start = {Start}, End = {End}, Peaks = {PeakCount}";
        }
    }
}
