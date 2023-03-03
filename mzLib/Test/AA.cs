using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Easy.Common.Extensions;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;
using List = NUnit.Framework.List;

namespace Test
{
    [TestFixture]
    public class AA
    {

        public const string HelaDirectory = @"D:\DataFiles\Hela_1";
        private Dictionary<string, List<MsDataScan>> helaScans;
        public Dictionary<string, List<MsDataScan>> HelaScans
        {
            get
            {
                if (helaScans != null) return helaScans;
                var paths = Directory.GetFiles(HelaDirectory).Where(p => p.Contains("20100611_Velos1_TaGe"));
                helaScans = paths.ToDictionary(Path.GetFileNameWithoutExtension, p => 
                    ThermoRawFileReader.LoadAllStaticData(p).GetAllScansList());
                return helaScans;
            }
        }

        public const string JurkatDirectory = @"D:\DataFiles\JurkatTopDown";
        private Dictionary<string, List<MsDataScan>> jurkatScans;
        public Dictionary<string, List<MsDataScan>> JurkatScans
        {
            get
            {
                if (jurkatScans != null) return jurkatScans;
                var paths = Directory.GetFiles(JurkatDirectory).Where(p => p.Contains("032017.raw"));
                jurkatScans = paths.ToDictionary(Path.GetFileNameWithoutExtension, p =>
                    ThermoRawFileReader.LoadAllStaticData(p).GetAllScansList());
                return jurkatScans;
            }
        }

        [Test]
        public void GettingTimeInRun()
        {
            string outDirectory = @"D:\Projects\InstrumentControl\ScanTimeComparison\IndividualScanData";
            List<ScanRunTimes> times = new();
            foreach (var run in HelaScans)
            {
                var t = new ScanRunTimes(run.Key, run.Value);
                t.ExportAsCsv(outDirectory);
                times.Add(t);
            }
            times.ExportAsCsv(outDirectory, "FullHelaComparison.csv");
        }

        [Test]
        public static void TESTNAME()
        {
            string icPath = @"B:\Users\Nic\InstrumentControlData\DiscoveryStageRuns\230223_InstrumentControlSecondTest_K562.raw";
            string controlpath = @"B:\Users\Nic\InstrumentControlData\DiscoveryStageRuns\230223_InstrumentControl_Control2_K562.raw";
            var icScans = ThermoRawFileReader.LoadAllStaticData(icPath).GetAllScansList();
            var controlScans = ThermoRawFileReader.LoadAllStaticData(controlpath).GetAllScansList();

            var icMs1 = icScans.Count(p => p.MsnOrder == 1);
            var icMs2 = icScans.Count(p => p.MsnOrder == 2);

            var controlMs1 = controlScans.Count(p => p.MsnOrder == 1);
            var controlMs2 = controlScans.Count(p => p.MsnOrder == 2);

            var icScanRunTime = new ScanRunTimes("InstrumentControl", icScans);
            var controlScanRunTime = new ScanRunTimes("Control", controlScans);

        }

        [Test]
        public static void CreateBarChart()
        {
            var values = new int[] { 5862, 5917 };
            var keys = new string[] { "MetaDrive", "XCalibur" };
            var chart = Chart.Bar<int, string, string>(values, keys);
            GenericChartExtensions.Show(chart);
        }


        [Test]
        public void ScanTimeFigureGeneration()
        {
            List<Shape> shapes1 = new List<Shape>();
            List<Annotation> annotations1 = new List<Annotation>();
            List<Shape> shapes2 = new List<Shape>();
            List<Annotation> annotations2 = new List<Annotation>();
            int maxSecondsToMakeGraph = 18;
            int width = 1300;
            int height = 600;

            var xAxis = new[] { 1.0, 2.0, 3, 4, 5, 6, 7 };
            var yAxis = new[] { 1, 2, 3, 4, 5, 6 };

            var layout = Layout.init<int>
            (
                PaperBGColor: new FSharpOption<Color>(Color.fromKeyword(ColorKeyword.White)),
                PlotBGColor: new FSharpOption<Color>(Color.fromKeyword(ColorKeyword.White)),
                Width: width,
                Height: height

            );
            var yLinearAxis = LinearAxis.init<int, int, int, int, int, int>(false);
            var xLinearAxis = LinearAxis.init<int, int, int, int, int, int>(Title: Title.init("Seconds", X: maxSecondsToMakeGraph / 2));


            // as is
            int ms1Micro = 7;
            double ms1MicroTime = 0.512959;
            int ms1Count = 1;

            int ms2Micro = 3;
            double ms2MicroTime = 0.292882;
            int ms2Count = 3;

            double endX = 0;
            var ms1AnnotationOffset = ms1Micro * ms1MicroTime / 2.0;
            var ms2AnnotationOffset = ms2Micro * ms2MicroTime ;
            while (endX < maxSecondsToMakeGraph)
            {
                var ms1Shapes = GenerateMsShape(ms1Micro, ms1MicroTime, ms1Count, 1, endX, 6, out endX);
                shapes1.AddRange(ms1Shapes);
                var ms1Annotation =
                    Annotation.init<double, double, int, int, int, int, int, int, int, int>(endX - ms1AnnotationOffset, 6,
                        Text: "Ms1", Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Center));
                annotations1.Add(ms1Annotation);

                var ms2Shapes = GenerateMsShape(ms2Micro, ms2MicroTime, ms2Count, 2,endX, 5, out endX);
                shapes1.AddRange(ms2Shapes);
                for (int i = 0; i < ms2Count; i++)
                {
                    var ms2Annotation =
                        Annotation.init<double, double, int, int, int, int, int, int, int, int>(
                            endX - (0.5 * ms2AnnotationOffset) - (ms2AnnotationOffset * i), 5,
                            Text: "Ms2", Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Center));
                    annotations1.Add(ms2Annotation);
                }
            }

            var currentChart = Chart.Line<double, int, string>(xAxis, new [] {4, 5, 6}, false, Opacity: 0,
                    LineColor: new Optional<Color>(Color.fromARGB(0, 1, 1, 1), true))
                .WithShapes(shapes1)
                .WithYAxis(yLinearAxis)
                .WithYAxis(xLinearAxis)
                .WithAnnotations(annotations1)
                .WithLayout(layout)
                .WithTitle("Current Top-Down Method");
            GenericChartExtensions.Show(currentChart);


            // proposed
            ms1Micro = 2;
            ms1MicroTime = 0.512959;
            ms1Count = 1;

            ms2Micro = 3;
            ms2MicroTime = 0.292882;
            ms2Count = 3;

            endX = 0;
            ms1AnnotationOffset = ms1Micro * ms1MicroTime / 2.0;
            ms2AnnotationOffset = ms2Micro * ms2MicroTime;
            while (endX < maxSecondsToMakeGraph)
            {
                var ms1Shapes = GenerateMsShape(ms1Micro, ms1MicroTime, ms1Count, 1, endX, 3, out endX);
                shapes2.AddRange(ms1Shapes);
                var ms1Annotation =
                    Annotation.init<double, double, int, int, int, int, int, int, int, int>(endX - ms1AnnotationOffset, 3,
                        Text: "Ms1", Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Center));
                annotations2.Add(ms1Annotation);
                annotations2.Add(ms1Annotation);

                var ms2Shapes = GenerateMsShape(ms2Micro, ms2MicroTime, ms2Count, 2, endX, 2, out endX);
                shapes2.AddRange(ms2Shapes);
                for (int i = 0; i < ms2Count; i++)
                {
                    var ms2Annotation =
                        Annotation.init<double, double, int, int, int, int, int, int, int, int>(
                            endX - (0.5 * ms2AnnotationOffset) - (ms2AnnotationOffset * i), 2,
                            Text: "Ms2", Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Center));
                    annotations2.Add(ms2Annotation);
                }
            }

            var proposedChart = Chart.Line<double, int, string>(xAxis, new []{1, 2, 3}, false, Opacity: 0,
                    LineColor: new Optional<Color>(Color.fromARGB(0, 1, 1, 1), true))
                .WithShapes(shapes2)
                .WithYAxis(yLinearAxis)
                .WithYAxis(xLinearAxis)
                .WithAnnotations(annotations2)
                .WithLayout(layout)
                .WithTitle("Proposed Top-Down Method");
            GenericChartExtensions.Show(proposedChart);

            annotations1.AddRange(annotations2);
            shapes1.AddRange(shapes2);
            var bigChart = Chart.Line<double, int, string>(xAxis, yAxis, false, Opacity: 0,
                    LineColor: new Optional<Color>(Color.fromARGB(0, 1, 1, 1), true))
                .WithShapes(shapes1)
                .WithYAxis(yLinearAxis)
                .WithYAxis(xLinearAxis)
                .WithAnnotations(annotations1)
                .WithLayout(layout);
            GenericChartExtensions.Show(bigChart);
        }

        private List<Color> Ms1Colors = new List<Color>()
        {
            Color.fromHex("#4c86e5"),
            Color.fromHex("#95b3e4"),
        };

        private List<Color> Ms2Colors = new List<Color>()
        {
            Color.fromHex("#c046de"),
            Color.fromHex("#cc82de"),
        };

        private List<Shape> GenerateMsShape(int microScanCount, double microScanTime, int scanCount, int msnOrder, double startX, int startY, out double endXValue)
        {
            List<Shape> shapes = new List<Shape>();
            var colors = msnOrder == 1 ? Ms1Colors : Ms2Colors;

            endXValue = startX + microScanTime;
            for (int i = 0; i < scanCount; i++)
            {
                var scanStartX = startX;
                for (int j = 0; j < microScanCount; j++)
                {
                    var microShape =
                        Shape.init<double, double, double, double>
                            (StyleParam.ShapeType.Rectangle, startX, endXValue, startY, startY - 1,
                                Fillcolor: new FSharpOption<Color>(colors[j % 2]));
                    shapes.Add(microShape);

                    startX += microScanTime;
                    endXValue += microScanTime;
                }

                var ms2Shape =
                    Shape.init<double, double, double, double>
                    (StyleParam.ShapeType.Rectangle, scanStartX, endXValue - microScanTime, startY, startY - 1,
                        Line: new FSharpOption<Line>(Line.init(Width: 5)));
                shapes.Add(ms2Shape);
            }
            endXValue -= microScanTime;
            return shapes;
        }
    }
}
