using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Test
{
    public readonly record struct ScanTime(int ScanNum, int ScanOrder, double TimeToRun);
    public class ScanRunTimes
    {
        public ScanRunTimes(string name, List<MsDataScan> scans)
        {
            Name = name;
            Scans = scans;
            ScanTimes = ParseScanTimes();
            AverageMs1Time = ScanTimes.Where(p => p.ScanOrder == 1).Average(p => p.TimeToRun);
            AverageMs2Time = ScanTimes.Where(p => p.ScanOrder == 2).Average(p => p.TimeToRun);
        }

        public ScanRunTimes(string importPath)
        {
            Name = Path.GetFileNameWithoutExtension(importPath).Split('_')[0];
            Scans = new List<MsDataScan>();
            ScanTimes = ImportFromCsv(importPath);
            AverageMs1Time = ScanTimes.Where(p => p.ScanOrder == 1).Average(p => p.TimeToRun);
            AverageMs2Time = ScanTimes.Where(p => p.ScanOrder == 2).Average(p => p.TimeToRun);
        }

        public string Name { get; set; }
        public List<MsDataScan> Scans { get; set; }
        public List<ScanTime> ScanTimes { get; set; }
        public double AverageMs1Time { get; }
        public double AverageMs2Time { get; }

        private List<ScanTime> ParseScanTimes()
        {
            List<ScanTime> scanTimes = new List<ScanTime>();
            for (var i = 1; i < Scans.Count; i++)
            {
                double time = (Scans[i].RetentionTime - Scans[i - 1].RetentionTime) * 60.0;
                var scanTime = new ScanTime(Scans[i].OneBasedScanNumber, Scans[i].MsnOrder, time);
                scanTimes.Add(scanTime);
            }
            return scanTimes;
        }

        public void ExportAsCsv(string outDirectory, string additonalInfoForFileName = "")
        {
            string outPath = Path.Combine(outDirectory, $"{Name}_{additonalInfoForFileName}.csv");
            using (var sw = new StreamWriter(outPath))
            {
                sw.WriteLine("Scan Number,Scan Order,Time To Acquire");
                foreach (var time in ScanTimes)
                {
                    sw.WriteLine($"{time.ScanNum},{time.ScanOrder},{time.TimeToRun}");
                }
            }
        }

        public List<ScanTime> ImportFromCsv(string path)
        {
            List<ScanTime> times = new();
            bool first = true;
            foreach (var line in File.ReadAllLines(path))
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                var splits = line.Split(',');
                var scanNum = int.Parse(splits[0]);
                var msnOrder = int.Parse(splits[1]);
                var time = double.Parse(splits[2]);
                times.Add(new ScanTime(scanNum, msnOrder, time));
            }

            return times;
        }
    }
}
