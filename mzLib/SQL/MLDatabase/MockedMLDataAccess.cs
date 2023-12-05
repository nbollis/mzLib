using MassSpectrometry;
using Proteomics;
using Proteomics.PSM;
using Readers;
using UsefulProteomicsDatabases;

namespace SQL.MLDatabase;

public class MockedMLDataAccess : IMLData
{
    private MLData? _data;

    private static string _mockedPsmsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources",
        "MLDatabase", "MockedData", "AllPSMs.psmtsv");
    private static string _mockedScansPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources",
        "MLDatabase", "MockedData", "PrunedDbSpectra.mzML");
    private static string _mockedProteinsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources",
        "MLDatabase", "MockedData", "DbForPrunedDb.fasta");

    public MockedMLDataAccess(bool getAllData = false)
    {
        if (getAllData)
            Data = GetMLData();
    }

    public MLData Data
    {
        get => _data ??= GetMLData();
        set => _data = value;
    }

    private MLData GetMLData()
    {
        try
        {
            MLData data = new MLData()
            {
                AllPsms = new Lazy<List<PsmFromTsv>>(GetMockPsms),
                AllScans = new Lazy<List<MsDataScan>>(GetMockScans),
                AllProteins = new Lazy<List<Protein>>(GetMockProteins)
            };
            return data;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    internal static List<PsmFromTsv> GetMockPsms() => SpectrumMatchTsvReader.ReadPsmTsv(_mockedPsmsPath, out _);
    internal static List<MsDataScan> GetMockScans() => MsDataFileReader.GetDataFile(_mockedScansPath).GetAllScansList();
    internal static List<Protein> GetMockProteins() => ProteinDbLoader.LoadProteinFasta(_mockedProteinsPath, true, DecoyType.None, false, out _);
}