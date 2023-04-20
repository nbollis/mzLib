using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using System.IO;

namespace Readers
{
    public enum MsAlignType
    {
        Combined = 0,
        Ms1 = 1,
        Ms2 = 2,
        Unknown,
    }

    public class MsAlign : MassSpectrometry.MsDataFile
    {

        public MsAlignType MsAlignType { get; set; }

        public MsAlign(string filePath) : base(filePath)
        {
            if (filePath.ToLower().Contains("ms1.msalign"))
            {
                MsAlignType = MsAlignType.Ms1;
            }
            else if (filePath.ToLower().Contains("ms2.msalign"))
            {
                MsAlignType = MsAlignType.Ms2;
            }
            else
            {
                MsAlignType = MsAlignType.Unknown;
            }
        }

        internal enum ReadingProgress
        {
            NotFound,
            Found,
            Finished
        }

        public override MsDataFile LoadAllStaticData(FilteringParams filteringParams = null, int maxThreads = 1)
        {
            // TODO: Figure out dynamic connection and have static call dynamic

            List<MsDataScan> scans = new();
            ReadingProgress headerProgress = ReadingProgress.NotFound;
            ReadingProgress entryProgress = ReadingProgress.NotFound;
            using (StreamReader sr = new StreamReader(FilePath))
            {
                string? line;
                while ((line = sr.ReadLine()) is not null)
                {
                    List<string> linesToProcess = new();

                    // get header
                    if (headerProgress != ReadingProgress.Finished)
                    {
                        if (headerProgress == ReadingProgress.NotFound && line.Contains("#####Parameters#####"))
                            headerProgress = ReadingProgress.Found;
                        else if (headerProgress == ReadingProgress.Found && line.Contains("#####Parameters#####"))
                        {
                            headerProgress = ReadingProgress.Finished;
                            ParseHeaderLines(linesToProcess); // TODO: this method
                            linesToProcess.Clear();
                        }
                        else
                        {
                            linesToProcess.Add(line);
                            continue;
                        }
                    }

                    // each entry after header
                    if (entryProgress == ReadingProgress.NotFound && line.Contains("BEGIN IONS"))
                        entryProgress = ReadingProgress.Found;
                    else if (entryProgress == ReadingProgress.Found && line.Contains("END IONS"))
                    {
                        entryProgress = ReadingProgress.NotFound;
                        scans.Add(ParseEntryLines(linesToProcess)); // TODO: this method
                        linesToProcess.Clear();
                    }
                    else
                    {
                        linesToProcess.Add(line);
                        continue;
                    }
                }
            }

            Scans = scans.ToArray();
            return this;
        }

        public override SourceFile GetSourceFile()
        {
            throw new NotImplementedException();
        }

        public override MsDataScan GetOneBasedScanFromDynamicConnection(int oneBasedScanNumber, IFilteringParams filterParams = null)
        {
            throw new NotImplementedException();
        }

        public override void CloseDynamicConnection()
        {
            throw new NotImplementedException();
        }

        public override void InitiateDynamicConnection()
        {
            throw new NotImplementedException();
        }



        private void ParseHeaderLines(List<string> headerLines)
        {
            throw new NotImplementedException();
        }

        private MsDataScan ParseEntryLines(List<string> entryLines)
        {
            throw new NotImplementedException();
        }
    }
}
