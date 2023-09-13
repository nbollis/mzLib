using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.Net.Http;
using System.Text.Json.Serialization;
using Easy.Common;
using Newtonsoft.Json;
using UsefulProteomicsDatabases;

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


        public static string ApiUrl => @"https://www.genesilico.pl/modomics/api/modification";
        public string GetSpecificModApiUrl(int modId) => ApiUrl + $"?id={modId}";
        
        public void CallApiToGetAdditionalInformation()
        {
            using var httpClient = new HttpClient();
            foreach (var entry in Results)
            {
                // get api request key for specific mod
                string urlRequest = GetSpecificModApiUrl(entry.Id);

                // call api to get json
                var response = Loaders.AwaitAsync_GetSomeData(urlRequest).GetAwaiter().GetResult();

                string responseText;
                if (response.IsSuccessStatusCode)
                    responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                else
                    throw new HttpRequestException($"HTTP Error: {response.StatusCode}");

                // parse json to finish populating object

                var temp = JsonConvert.DeserializeObject< (string,ModomicsCsvEntry)>(responseText);
                JsonConvert.PopulateObject(responseText, new KeyValuePair<string, ModomicsCsvEntry>("", entry));

            }
        }

        
    }

}
