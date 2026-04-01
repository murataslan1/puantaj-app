namespace PuantajApp.Models;

public class PuantajKayit
{
    public int Id { get; set; }
    public int PersonelId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int Gun { get; set; }
    public string GunTipi { get; set; } = "";     // hafta_ici, hafta_sonu, resmi_tatil
    public string? GirisSaati { get; set; }        // HH:mm
    public string? CikisSaati { get; set; }        // HH:mm
    public string? FmGiris { get; set; }           // Fazla mesai giris
    public string? FmCikis { get; set; }           // Fazla mesai cikis
    public decimal? FmSaat { get; set; }           // Dogrudan saat girisi
    public string? IzinTipi { get; set; }          // mi, yi, r
    public string? Aciklama { get; set; }

    public Personel? Personel { get; set; }
}
