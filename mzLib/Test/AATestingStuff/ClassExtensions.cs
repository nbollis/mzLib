using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public static class ClassExtensions
    {
        public static void ExportAsCsv(this List<ScanRunTimes> runTimes, string outDirectory, string fileName)
        {
            if (!fileName.EndsWith(".csv"))
                fileName += ".csv";

            var outPath = Path.Combine(outDirectory, fileName);
            using (var sw = new StreamWriter(outPath))
            {
                sw.WriteLine("Name,AverageMs1Time,AverageMs2Time");
                foreach (var time in runTimes)
                {
                    sw.WriteLine($"{time.Name},{time.AverageMs1Time},{time.AverageMs2Time}");
                }
            }
        }
    }
}
