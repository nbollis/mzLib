using Plotly.NET.CSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace mzPlot;

public sealed class DigHistPlotBuilder
{
    public double FontSize { get; set; } = 18;
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 700;

    public Plotly.NET.GenericChart CreateProteinChart(IEnumerable<DigHistResult> results, string chartTitle = "Protein Digestion Product Lengths")
    {
        return CreateChart(results, DigestionPolymerType.Protein, chartTitle);
    }

    public Plotly.NET.GenericChart CreateRnaChart(IEnumerable<DigHistResult> results, string chartTitle = "RNA Digestion Product Lengths")
    {
        return CreateChart(results, DigestionPolymerType.Rna, chartTitle);
    }

    public Plotly.NET.GenericChart CreateChart(IEnumerable<DigHistResult> results, DigestionPolymerType polymerType, string chartTitle)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentException.ThrowIfNullOrWhiteSpace(chartTitle);

        List<DigHistResult> typedResults = results
            .Where(result => result.PolymerType == polymerType)
            .OrderBy(result => result.SourceId)
            .ThenBy(result => result.DigestionAgentName)
            .ThenBy(result => result.MaxMissedCleavages)
            .ToList();

        if (typedResults.Count == 0)
        {
            throw new ArgumentException($"No {polymerType} digestion histogram results were provided.", nameof(results));
        }

        string[] xLabels = typedResults
            .SelectMany(result => result.DigestionLengthHistogram.Keys)
            .Distinct()
            .OrderBy(length => length)
            .Select(length => length.ToString())
            .ToArray();

        List<Plotly.NET.GenericChart> traces = new();
        foreach (DigHistResult result in typedResults)
        {
            double[] yValues = xLabels
                .Select(label => result.DigestionLengthHistogram.TryGetValue(int.Parse(label, CultureInfo.InvariantCulture), out int count) ? (double)count : 0d)
                .ToArray();

            Plotly.NET.GenericChart trace = Plotly.NET.CSharp.Chart.Column<string, double, string>(xLabels, yValues, Name: result.SeriesLabel);
            traces.Add(trace);
        }

        return Plotly.NET.CSharp.Chart.Combine(traces)
            .WithXAxisStyle<string, double, string>(Title: Plotly.NET.Title.init("Digestion Product Length"))
            .WithYAxisStyle<string, double, string>(Title: Plotly.NET.Title.init("Count"))
            .WithSize(Width: Width, Height: Height)
            .WithLegend(TicPlot.DefaultLegend)
            .WithLegendStyle(Title: Plotly.NET.Title.init(chartTitle));
    }
}
