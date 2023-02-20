using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public class SnrComparer
    {
        public Dictionary<int, double> SnrByMedian { get; set; }
        public Dictionary<int, double> SnrByHist { get; set; }
        public Dictionary<int, double> SnrByMrs { get; set; }
        public double Count => SnrByMedian.Count;
        Dictionary<int, NoiseEstimationMethodComparison> NoiseEstimates { get; set; }
        public SnrComparer(List<MsDataScan> scans)
        {
            NoiseEstimates = new();
            SnrByMedian = new Dictionary<int, double>();
            SnrByHist = new Dictionary<int, double>();
            SnrByMrs = new Dictionary<int, double>();
            foreach (var scan in scans)
            {
                var scanNum = scan.OneBasedScanNumber;
                NoiseEstimates.Add(scanNum, new NoiseEstimationMethodComparison(scan, 500, 90));
            }

            var referenceMedian = NoiseEstimates.Values.Average(p => p.MedianOfAllPeaks);
            var referenceLastHist = NoiseEstimates.Values.Average(p => p.AverageOfLastHistogramNoiseBin);
            var referenceMrs = NoiseEstimates.Values.Average(p => p.MrsNoiseEstimation);

            foreach (var scan in scans)
            {
                var scanNum = scan.OneBasedScanNumber;
                var yArray = scan.MassSpectrum.YArray;
                var snrByMedian = CalculateSnr(yArray, referenceMedian, NoiseEstimates[scanNum].MedianOfAllPeaks);
                var snrByHist = CalculateSnr(yArray, referenceLastHist, NoiseEstimates[scanNum].AverageOfLastHistogramNoiseBin);
                var snrByMrs = CalculateSnr(yArray, referenceMrs, NoiseEstimates[scanNum].MrsNoiseEstimation);

                SnrByMedian.Add(scanNum, snrByMedian);
                SnrByHist.Add(scanNum, snrByHist);
                SnrByMrs.Add(scanNum, snrByMrs);
            }
        }

        public double CalculateSnr(double[] array, double referenceValue, double cutoffValue)
        {
            double scaling = referenceValue / cutoffValue;
            var stddev = array.Where(i => i < cutoffValue)
                .Select(i => i * scaling)
                .StandardDeviation();
            var mean = array.Select(i => i * scaling).Where(i => i > 3 * stddev)
                .Mean();
            return mean / stddev;
        }

        public GenericChart.GenericChart GetTicLikePlot(string title = "")
        {
            var mrsChart = Chart.Line<int, double, string>(SnrByMrs.Keys, SnrByMrs.Values, Name: "Snr By Mrs");
            var histChart = Chart.Line<int, double, string>(SnrByHist.Keys, SnrByHist.Values, Name: "Snr By Hist");
            var medChart = Chart.Line<int, double, string>(SnrByMedian.Keys, SnrByMedian.Values, Name: "Snr By Med");

            var compositePlot = Chart.Combine(new List<GenericChart.GenericChart>() { mrsChart, histChart, medChart })
                .WithTitle(title)
                .WithSize(1000, 600)
                .WithXAxisStyle(Title.init("Scan Number"))
                .WithYAxisStyle(Title.init("Snr"));
            return compositePlot;
        }
    }

    
}
