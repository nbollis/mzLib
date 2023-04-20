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

    public class MsAlign : MsDataFile
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

            ReadingProgress headerProgress = ReadingProgress.NotFound;
            ReadingProgress entryProgress = ReadingProgress.NotFound;
            using (StreamReader sr = new StreamReader(FilePath))
            {
                // get header
                // ### -> ###
                string? line;
                while ((line = sr.ReadLine()) is not null)
                {
                    // get header
                    if (headerProgress != ReadingProgress.Finished)
                    {
                        if (line.Contains("#####Parameters####") && headerProgress == ReadingProgress.NotFound)
                            headerProgress = ReadingProgress.Found;
                        else if (line.Contains("#####Parameters####") && headerProgress == ReadingProgress.Found)
                            headerProgress = ReadingProgress.Finished;


                    }


                    // do each spectrum
                    // BEGIN IONS -> END IONS

                }


            }






            return new GenericMsDataFile();
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
    }
}
