using Easy.Common.Extensions;
using MassSpectrometry;
using MzLibUtil;
using Plotly.NET;
using Plotly.NET.CSharp;
using Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using Plotly.NET.LayoutObjects;

namespace mzPlot;
public class TicPlot
{
    protected readonly MsDataFile DataFile;
    public double[] XArray { get; set; }
    public double[] YArray { get; set; }
    public bool Normalize { get; set; } = false;
    public double FontSize = 18;
    public double SplineSmoothing = 0.5;

    public TicPlot(MsDataFile dataFile, double minRt= 0, double maxRt = double.MaxValue, bool ms1Only = true)
    {
        DataFile = dataFile;

        var scans = dataFile.GetAllScansList().Where(s => (!ms1Only || s.MsnOrder == 1) && s.RetentionTime >= minRt && s.RetentionTime <= maxRt).ToArray();
        XArray = scans.Select(s => (double)s.RetentionTime).ToArray();
        YArray = scans.Select(s => s.TotalIonCurrent).ToArray();
    }

    public virtual void Plot() => Plotly.NET.CSharp.GenericChartExtensions.Show(ToChart());

    public virtual Plotly.NET.GenericChart ToChart()
    {
        string yAxisTitle; 
        double[] yArray;
        if (Normalize)
        {
            double maxIntensity = YArray.Max();
            yArray = YArray.Select(y => y / maxIntensity).ToArray();
            yAxisTitle = "Relative Intensity";
        }
        else
        {
            yArray = YArray;
            yAxisTitle = "Intensity";
        }

        var layout = Plotly.NET.Layout.init<string>(
            PaperBGColor: Plotly.NET.Color.fromKeyword(Plotly.NET.ColorKeyword.White),
            PlotBGColor: Plotly.NET.Color.fromKeyword(Plotly.NET.ColorKeyword.White),
            ShowLegend: true,
            Font: Plotly.NET.Font.init(StyleParam.FontFamily.Arial, FontSize, null));

        var chart = Plotly.NET.CSharp.Chart.Spline<double, double, string>(XArray, yArray, Smoothing: SplineSmoothing, LineColor: Plotly.NET.Color.fromKeyword(Plotly.NET.ColorKeyword.Black), Name: "Total Ion Chromatogram")
            .WithXAxisStyle<double, double, string>(Title: Plotly.NET.Title.init("Retention Time (min)"))
            .WithYAxisStyle<double, double, string>(Title: Plotly.NET.Title.init(yAxisTitle))
            .WithSize(Width: 1200, Height: 500)
            .WithLegend(DefaultLegend)
            .WithLayout(layout);
        return chart;
    }

    public static Legend DefaultLegend => Legend.init(
        X: 0.5, Y: 1.1,
        Orientation: StyleParam.Orientation.Horizontal, 
        VerticalAlign: StyleParam.VerticalAlign.Top,
        XAnchor: StyleParam.XAnchorPosition.Center, 
        YAnchor: StyleParam.YAnchorPosition.Top
    );
}

public class AnnotatedTicPlot : TicPlot
{
    PlotlyColorQueue _colorQueue = new PlotlyColorQueue(16);

    List<ExtractedIonChromatogram> Xics { get; set; } = new();
    List<string> Labels = new();

    public AnnotatedTicPlot(MsDataFile dataFile, double minRt = 0, double maxRt = Double.MaxValue, bool ms1Only = true) : base(dataFile, minRt, maxRt, ms1Only)
    {
    }

    public bool AddXic(double neutralMass, int charge, Tolerance massTolerance, double retentionTimeInMinutes, double retentionTimeWindowWidthInMinutes = 5, string label = "")
    {
        var xic = DataFile.ExtractEnvelopeIonChromatogram(neutralMass, charge, massTolerance, retentionTimeInMinutes, 1, retentionTimeWindowWidthInMinutes);

        if (!xic.Peaks.Any(p => p.Intensity > 0))
            return false;
        Xics.Add(xic);
        Labels.Add(label);
        return true;
    }

    public bool AddXicGroup(List<double> masses, List<int> charges, double retentionTimeInMinutes, Tolerance massTolerance, double retentionTimeWindowWidthInMinutes = 5, string label = "")
    {
        List<ExtractedIonChromatogram> xics = new();
        List<ExtractedIonChromatogram> innerXics = new();

        foreach (var mass in masses)
        {
            innerXics.Clear();
            foreach (var charge in charges)
            {
                var xic = DataFile.ExtractEnvelopeIonChromatogram(mass, charge, massTolerance, retentionTimeInMinutes, 1, retentionTimeWindowWidthInMinutes);

                if (xic.Peaks.Count > 0)
                    innerXics.Add(xic);
            }
            var xicGroup = ExtractedIonChromatogram.Sum(innerXics);
            if (xicGroup != null)
                xics.Add(xicGroup);
        }

        if (xics.Count == 0)
            return false;

        Xics.Add(ExtractedIonChromatogram.Sum(xics));
        Labels.Add(label);

        return true;
    }

    public override Plotly.NET.GenericChart ToChart()
    {
        var baseChart = base.ToChart();

        double scaler = 1;
        List<Plotly.NET.GenericChart> xicCharts = new List<Plotly.NET.GenericChart>();
        for (var index = 0; index < Xics.Count; index++)
        {
            var xic = Xics[index];
            if (Normalize)
                scaler = xic.Peaks.Max(p => p.Intensity);

            var color = _colorQueue.Dequeue();

            var rtValues = xic.Peaks.Select(p => (double)p.RetentionTime).ToArray();
            var intensityValues = xic.Peaks.Select(p => p.Intensity / scaler).ToArray();

            var label = Labels[index].IsNotNullOrEmpty()
                ? Labels[index] : $"XIC: m/z {xic.AveragedMassOrMz:F4}";

            var xicChart = Plotly.NET.CSharp.Chart.Spline<double, double, string>(rtValues, intensityValues, Smoothing: SplineSmoothing, LineColor: color, Name: label);

            xicCharts.Add(xicChart);
        }
        var layout = Plotly.NET.Layout.init<string>(
            PaperBGColor: Plotly.NET.Color.fromKeyword(Plotly.NET.ColorKeyword.White),
            PlotBGColor: Plotly.NET.Color.fromKeyword(Plotly.NET.ColorKeyword.White),
            ShowLegend: true,
            Font: Plotly.NET.Font.init(StyleParam.FontFamily.Arial, FontSize, null));

        var combined = Plotly.NET.CSharp.Chart.Combine(xicCharts.Prepend(baseChart))
            .WithSize(Width: 1200, Height: 500)
            .WithLegend(DefaultLegend)
            .WithLayout(layout);
        return combined;
    }
}