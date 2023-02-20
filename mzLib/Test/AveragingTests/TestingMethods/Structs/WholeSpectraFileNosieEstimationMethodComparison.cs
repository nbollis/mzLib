using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using Plotly.NET;
using Plotly.NET.CSharp;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public record struct WholeSpectraFileNoiseEstimationMethodComparison : ITsv
    {
        #region private

        // I told you this was private, why are you looking here? Rude.

        #endregion

        public string Name { get; init; }
        public List<NoiseEstimationMethodComparison> IndividualComparisons { get; init; }
        public double AverageTic => IndividualComparisons.Average(p => p.Tic);
        public double MrsNoiseEstimation => IndividualComparisons.Average(p => p.MrsNoiseEstimation);
        public double AverageOfMostAbundantHistogramBin => IndividualComparisons.Average(p => p.AverageOfLastHistogramNoiseBin);
        public double AverageOfLastHistogramNoiseBin => IndividualComparisons.Average(p => p.AverageOfLastHistogramNoiseBin);
        public double MaxSignalOverMrsNoise => IndividualComparisons.Average(p => p.MaxSignalOverMrsNoise);
        public double MaxSignalOverMaxHistogramNoise => IndividualComparisons.Average(p => p.MaxSignalOverMaxHistogramNoise);
        public double HistogramNoiseOverSignalIntegrationByHist => IndividualComparisons.Average(p => p.HistSignalOverNoiseByHist);
        public double HistogramNoiseOverSignalIntegrationByMrs => IndividualComparisons.Average(p => p.HistSignalOverNoiseByMrs);
        public double AverageOverStDevOfPeaks => IndividualComparisons.Average(p => p.AverageOverStDevOfPeaks);


        public WholeSpectraFileNoiseEstimationMethodComparison(string name, List<MsDataScan> scans, int numberOfBins,
            int percentToKeep)
        {
            Name = name;
            IndividualComparisons = new();
            foreach (var scan in scans) 
                IndividualComparisons.Add(new (scan, numberOfBins, percentToKeep));

        }

        public WholeSpectraFileNoiseEstimationMethodComparison(string name,
            IEnumerable<NoiseEstimationMethodComparison> individualComparisons)
        {
            Name = name;
            IndividualComparisons = individualComparisons.ToList();
        }

        public void ShowBoxPlot()
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetBoxPlot());
        }


        private static Queue<Optional<Color>> colorQueue;

        static WholeSpectraFileNoiseEstimationMethodComparison()
        {
            colorQueue = new();
            colorQueue.Enqueue(new Optional<Color>(Color.fromKeyword(ColorKeyword.DodgerBlue), true));
            colorQueue.Enqueue(new Optional<Color>(Color.fromKeyword(ColorKeyword.Crimson), true));
            colorQueue.Enqueue(new Optional<Color>(Color.fromKeyword(ColorKeyword.SpringGreen), true));
            colorQueue.Enqueue(new Optional<Color>(Color.fromKeyword(ColorKeyword.Fuchsia), true));
            colorQueue.Enqueue(new Optional<Color>(Color.fromKeyword(ColorKeyword.Blueviolet), true));
        }

        public GenericChart.GenericChart GetBoxPlot()
        {
            var color = colorQueue.Dequeue();
            List<string> xValues = new();
            List<double> yValues = new();
            foreach (var comparison in IndividualComparisons)
            {
                xValues.Add(nameof(MrsNoiseEstimation));
                yValues.Add(comparison.MrsNoiseEstimation);
                xValues.Add(nameof(AverageOfMostAbundantHistogramBin));
                yValues.Add(comparison.AverageOfMostAbundantHistogramBin);
                xValues.Add(nameof(AverageOfLastHistogramNoiseBin));
                yValues.Add(comparison.AverageOfLastHistogramNoiseBin);
            }
            var smallerPlot = Chart.BoxPlot<string, double, string>(
                xValues, 
                yValues,
                Name,
                BoxPoints: new Optional<StyleParam.BoxPoints>(StyleParam.BoxPoints.All, true),
                OutlineColor: color);

            List<string> xValues2 = new();
            List<double> yValues2 = new();
            foreach (var comparison in IndividualComparisons)
            {
                xValues2.Add(nameof(HistogramNoiseOverSignalIntegrationByHist));
                yValues2.Add(comparison.HistSignalOverNoiseByHist);
                xValues2.Add(nameof(AverageOverStDevOfPeaks));
                yValues2.Add(comparison.AverageOverStDevOfPeaks);
            }

            var mediumPlot = Chart.BoxPlot<string, double, string>(
                xValues2,
                yValues2,
                Name,
                BoxPoints: new Optional<StyleParam.BoxPoints>(StyleParam.BoxPoints.All, true),
            OutlineColor: color);

            List<string> xValues3 = new();
            List<double> yValues3 = new();
            foreach (var comparison in IndividualComparisons)
            {
                xValues3.Add(nameof(MaxSignalOverMrsNoise));
                yValues3.Add(comparison.MaxSignalOverMrsNoise);
                xValues3.Add(nameof(MaxSignalOverMaxHistogramNoise));
                yValues3.Add(comparison.MaxSignalOverMaxHistogramNoise);
            }
            var biggerPlot = Chart.BoxPlot<string, double, string>(
                xValues3,
                yValues3,
                Name,
                BoxPoints: new Optional<StyleParam.BoxPoints>(StyleParam.BoxPoints.All, true),
                OutlineColor: color);

            return Chart.Grid(new List<GenericChart.GenericChart>() { smallerPlot, mediumPlot, biggerPlot }, 1, 3)
                .WithTitle(Name)
                .WithSize(1000, 1200);
        }

        public static void ShowOverlaidBoxPlot(List<WholeSpectraFileNoiseEstimationMethodComparison> comparisons)
        {
            List<GenericChart.GenericChart> charts = new List<GenericChart.GenericChart>();
            comparisons.ForEach(comparison => charts.Add(comparison.GetBoxPlot()));

            var chart = Chart.Combine(charts).WithSize(1200, 1000);
            Plotly.NET.CSharp.GenericChartExtensions.Show(chart);
        }

        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Name\t");
                //sb.Append("Average Tic\t");
                sb.Append("Mrs Noise\t");
                //sb.Append("Most Abundant Hist\t");
                sb.Append("Last Noise Hist\t");
                sb.Append("Max Signal / Mrs\t");
                sb.Append("Max Signal / Last Hist\t");
                sb.Append("Hist Signal / Noise By Last Hist\t");
                sb.Append("Hist Signal / Noise By Mrs\t");
                //sb.Append("Avg / Stdev\t");

                var tsvString = sb.ToString().TrimEnd('\t');
                return tsvString;
            }
        }

        public string ToTsvString()
        {
            var sb = new StringBuilder();
            sb.Append($"{Name}\t");
            sb.Append($"{AverageTic}\t");
            sb.Append($"{MrsNoiseEstimation}\t");
            //sb.Append($"{AverageOfMostAbundantHistogramBin}\t");
            sb.Append($"{AverageOfLastHistogramNoiseBin}\t");
            sb.Append($"{MaxSignalOverMrsNoise}\t");
            sb.Append($"{MaxSignalOverMaxHistogramNoise}\t");
            sb.Append($"{HistogramNoiseOverSignalIntegrationByHist}\t");
            sb.Append($"{HistogramNoiseOverSignalIntegrationByMrs}\t");
            //sb.Append($"{AverageOverStDevOfPeaks}\t");

            var tsvString = sb.ToString().TrimEnd('\t');
            return tsvString;
        }
    }
}
