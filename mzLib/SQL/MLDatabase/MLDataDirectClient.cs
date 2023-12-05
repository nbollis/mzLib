using MassSpectrometry;
using Proteomics;
using Proteomics.PSM;

namespace SQL.MLDatabase;

/// <summary>
/// This class is used to access the MLData object directly from the database
/// </summary>
public class MLDataDirectClient : IMLData
{
    private MLData? _data;
    private MLDataAccess _dbAccess;

    public MLDataDirectClient(bool getAllData = false)
    {
        _dbAccess = new MLDataAccess();
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
                AllPsms = new Lazy<List<PsmFromTsv>>(() => _dbAccess.GetAllPsms()),
                AllScans = new Lazy<List<MsDataScan>>(() => _dbAccess.GetAllScans()),
                AllProteins = new Lazy<List<Protein>>(() => _dbAccess.GetAllProteins())
            };
            return data;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}