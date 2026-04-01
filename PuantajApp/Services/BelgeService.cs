using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PuantajApp.Data;
using PuantajApp.Models;

namespace PuantajApp.Services;

public class BelgeService
{
    private readonly AppDbContext _db;

    public BelgeService(AppDbContext db) => _db = db;

    public async Task BelgeKaydetAsync(int personelId, int yil, int ay, string belgeTipi, string dosyaYolu)
    {
        var bytes = await File.ReadAllBytesAsync(dosyaYolu);
        var belge = new Belge
        {
            PersonelId = personelId,
            Yil = yil,
            Ay = ay,
            BelgeTipi = belgeTipi,
            DosyaAdi = Path.GetFileName(dosyaYolu),
            DosyaIcerik = bytes,
            YuklenmeTarihi = DateTime.Now
        };
        _db.Belgeler.Add(belge);
        await _db.SaveChangesAsync();
    }

    public async Task BelgeAcAsync(int belgeId)
    {
        var belge = await _db.Belgeler.FindAsync(belgeId);
        if (belge == null) return;

        var geciciYol = Path.Combine(Path.GetTempPath(), belge.DosyaAdi);
        await File.WriteAllBytesAsync(geciciYol, belge.DosyaIcerik);

        // Platform bagimsiz ac
        var psi = new System.Diagnostics.ProcessStartInfo(geciciYol) { UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
    }
}
