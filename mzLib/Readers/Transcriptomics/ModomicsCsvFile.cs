using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace Readers.Transcriptomics
{
    public class ModomicsCsvFile : ResultFile<ModomicsCsvEntry>, IResultFile
    {
        public override SupportedFileType FileType => SupportedFileType.ModomicsCsv;
        public override Software Software { get; set; }

        public ModomicsCsvFile(string filePath, Software software = Software.Unspecified) : base(filePath, software)
        {
            
        }

        public override void LoadResults()
        {
            using var csv = new CsvReader(new StreamReader(FilePath), ModomicsCsvEntry.CsvConfiguration);
            Results = csv.GetRecords<ModomicsCsvEntry>().ToList();
        }

        public override void WriteResults(string outputPath)
        {
            if (!CanRead(outputPath))
                outputPath += FileType.GetFileExtension();

            using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ModomicsCsvEntry.CsvConfiguration);
            
            csv.WriteHeader<ModomicsCsvEntry>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }
    }
}
