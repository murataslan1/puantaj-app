using System.IO;
using Microsoft.EntityFrameworkCore;
using PuantajApp.Models;

namespace PuantajApp.Data;

public class AppDbContext : DbContext
{
    public static string DbPath => Path.Combine(Directory.GetCurrentDirectory(), "puantaj.db");

    public DbSet<Personel> Personeller { get; set; }
    public DbSet<PuantajKayit> PuantajKayitlar { get; set; }
    public DbSet<Belge> Belgeler { get; set; }
    public DbSet<HakedisParametre> HakedisParametreler { get; set; }
    public DbSet<HakedisEkVeri> HakedisEkVeriler { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=puantaj.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PuantajKayit>()
            .HasIndex(p => new { p.PersonelId, p.Yil, p.Ay, p.Gun })
            .IsUnique();

        modelBuilder.Entity<HakedisEkVeri>()
            .HasIndex(h => new { h.PersonelId, h.Yil, h.Ay })
            .IsUnique();
    }
}
