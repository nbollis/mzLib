using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Windows.Documents;
using NUnit.Framework;
using ScottPlot;
using ScottPlot.Ticks;
using static Plotly.NET.StyleParam;

namespace Test.AveragingTests
{

    public static class Plots
    {





        // Not functional as cleveland only allows 2 data points in ScottPlots
        public static void GetClevelandDotPlot(this IEnumerable<WholeSpectraFileNoiseEstimationMethodComparison> comparisons, string title)
        {
            var wholeSpectraFileNoiseEstimationMethodComparisons =
                comparisons as WholeSpectraFileNoiseEstimationMethodComparison[] ?? comparisons.ToArray();

            var headers = new[]
            {
                "AverageTic", "MrsNoiseEstimation", 
                "AverageOfMostAbundantHistogramBin", "AverageOfLastHistogramNoiseBin",
                "MaxSignalOverMrsNoise", "MaxSignalOverMaxHistogramNoise",
                "HistogramNoiseOverSignalIntegration", "AverageOverStDevOfPeaks",
            };
            var values = new Dictionary<string, double[]>();
            for (var i = 0; i < wholeSpectraFileNoiseEstimationMethodComparisons.Length; i++)
            {
                var comparison = wholeSpectraFileNoiseEstimationMethodComparisons[i];
                values.Add(comparison.Name,
                    new[]
                    {
                        comparison.AverageTic, comparison.MrsNoiseEstimation,
                        comparison.AverageOfMostAbundantHistogramBin, comparison.AverageOfLastHistogramNoiseBin,
                        comparison.MaxSignalOverMrsNoise, comparison.MaxSignalOverMaxHistogramNoise,
                        comparison.HistogramNoiseOverSignalIntegrationByHist, comparison.AverageOverStDevOfPeaks
                    });
            }

            var plot = new Plot(800, 600);
        }
    }
}
