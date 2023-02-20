using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
using Plotly.NET;
using SpectralAveraging;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public readonly record struct NoiseImprovement(double ScanNum, double Median, double Mrs, double Hist);
    public class WholeFileSnrComparer
    {
        public Dictionary<string, List<NoiseImprovement>> NoiseImprovements { get; set; }
        public Dictionary<string, SnrComparer> SnrComparisons { get; set; }
        /// <summary>
        /// First filepath should be the reference path
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="filter"></param>
        public WholeFileSnrComparer(List<string> filePaths, bool filter = true)
        {
            SnrComparisons = new();
            foreach (var path in filePaths)
            {
                var ms1Scans = SpectraFileHandler.LoadAllScansFromFile(path).Where(p => p.MsnOrder == 1);
                var trimmedScans = new List<MsDataScan>();
                if (filter)
                {
                    trimmedScans = ms1Scans
                        .Where(p => p.RetentionTime is >= 30 and <= 40)
                        .ToList();
                }
                else
                {
                    trimmedScans = ms1Scans.ToList();
                }
                SnrComparisons.Add(Path.GetFileNameWithoutExtension(path), new SnrComparer(trimmedScans));
            }

            NoiseImprovements = CompareSnr();
        }

        private Dictionary<string, List<NoiseImprovement>> CompareSnr()
        {
            var original = SnrComparisons.FirstOrDefault();
            var reference = original.Value;

            var snrChanges = new Dictionary<string, List<NoiseImprovement>>();
            foreach (var comparison in SnrComparisons)
            {
                if (comparison.Key.Equals(original.Key)) continue;

                snrChanges.Add(comparison.Key, new List<NoiseImprovement>());
                var keys = reference.SnrByMrs.Select(p => p.Key);

                foreach (var key in keys)
                {
                    var med = comparison.Value.SnrByMrs[key] / reference.SnrByMrs[key];
                    var hist = comparison.Value.SnrByHist[key] / reference.SnrByHist[key];
                    var mrs = comparison.Value.SnrByMrs[key] / reference.SnrByMrs[key];
                    snrChanges[comparison.Key].Add(new NoiseImprovement(key, med, mrs, hist));
                }
            }
            return snrChanges;
        }

        public GenericChart.GenericChart GetPlot()
        {
            var mrsCharts = new List<GenericChart.GenericChart>();
            var histCharts = new List<GenericChart.GenericChart>();
            var medCharts = new List<GenericChart.GenericChart>();
            foreach (var improvement in NoiseImprovements)
            {
                var mrsY = improvement.Value.Select(p => p.Mrs);
                var histY = improvement.Value.Select(p => p.Hist);
                var medY = improvement.Value.Select(p => p.Median);
                var x = improvement.Value.Select(p => p.ScanNum);

                var mrs = Chart.Line<double, double, string>(x, mrsY, Name: "Mrs as Cutoff Improvement")
                    .WithTitle(improvement.Key);
                mrsCharts.Add(mrs);
                var hit = Chart.Line<double, double, string>(x, histY, Name: "Hist as Cutoff Improvement")
                    .WithTitle(improvement.Key);
                histCharts.Add(hit);
                var med = Chart.Line<double, double, string>(x, medY, Name: "Median as Cutoff Improvement")
                    .WithTitle(improvement.Key);
                medCharts.Add(med);
            }

            var allMrs = Chart.Combine(mrsCharts).WithTitle("Mrs as Cutoff Improvement").WithSize(1000, 600);
            var allHist = Chart.Combine(histCharts).WithTitle("Hist as Cutoff Improvement").WithSize(1000, 600);
            var allMed = Chart.Combine(medCharts).WithTitle("Median as Cutoff Improvement").WithSize(1000, 600);

            var chart = Chart.Grid(new List<GenericChart.GenericChart>() { allMrs, allHist, allMed }, 3, 1)
                .WithSize(1000, 1800);
            return chart;
        }
    }
}
