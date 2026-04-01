namespace PuantajApp.Models;

public class HakedisEkVeri
{
    public int Id { get; set; }
    public int PersonelId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public decimal YillikIzinKullanilan { get; set; }
    public int UcretsizIzin { get; set; }
    public int YasalIzin { get; set; }
    public int RaporGun { get; set; }
    public decimal IzinSaat { get; set; }
    public decimal VergiMatrahi { get; set; }        // Manuel
    public decimal TssGssFarki { get; set; }         // Manuel
    public decimal KantinUcreti { get; set; }        // Manuel
    public decimal HakedistenKesilecek { get; set; } // Manuel

    public Personel? Personel { get; set; }
}
