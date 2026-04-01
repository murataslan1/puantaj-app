using System;

namespace PuantajApp.Models;

public class Belge
{
    public int Id { get; set; }
    public int PersonelId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string BelgeTipi { get; set; } = "";   // rapor, izin_formu, devam_takip
    public string DosyaAdi { get; set; } = "";
    public byte[] DosyaIcerik { get; set; } = [];
    public DateTime YuklenmeTarihi { get; set; }

    public Personel? Personel { get; set; }
}
