using MassSpectrometry;
using Proteomics;
using Proteomics.PSM;

namespace SQL.MLDatabase;

/// <summary>
/// This class is used to access the MLData object directly from the database
/// </summary>
public class MLDataAccess
{
    private MLDataDbContext _context;

    public MLDataAccess()
    {
        _context = new MLDataDbContext();
    }

    public List<PsmFromTsv> GetAllPsms()
    {
        try
        {
            List<PsmFromTsv> psms = _context.Psms.ToList();
            return psms;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public List<MsDataScan> GetAllScans()
    {
        try
        {
            List<MsDataScan> scans = _context.Scans.ToList();
            return scans;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public List<Protein> GetAllProteins()
    {
        try
        {
            List<Protein> proteins = _context.Proteins.ToList();
            return proteins;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}