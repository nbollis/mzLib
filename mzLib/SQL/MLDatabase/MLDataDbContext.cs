using MassSpectrometry;
using Microsoft.EntityFrameworkCore;
using Proteomics;
using Proteomics.PSM;

namespace SQL.MLDatabase;

/// <summary>
/// This class is used to access the MLData object directly from the database
/// </summary>
public class MLDataDbContext : DbContext
{
    public MLDataDbContext()
    {

    }

    public virtual DbSet<PsmFromTsv> Psms { get; set; }
    public virtual DbSet<MsDataScan> Scans { get; set; }
    public virtual DbSet<Protein> Proteins { get; set; }

    // TODO: Fix this connection
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=MLData.db");
    }

    // TODO: Fix this model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PsmFromTsv>().ToTable("Psms");
        modelBuilder.Entity<MsDataScan>().ToTable("Scans");
        modelBuilder.Entity<Protein>().ToTable("Proteins");
    }
}