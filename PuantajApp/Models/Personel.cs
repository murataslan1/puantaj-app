using System;

namespace PuantajApp.Models;

public class Personel
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = "";
    public string TC { get; set; } = "";
    public string Unvan { get; set; } = "";
    public string Birim { get; set; } = "";
    public DateTime? BaslamaTarihi { get; set; }
    public DateTime? UnvanDegisiklikTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    public decimal BirimUcreti { get; set; }
    public decimal YillikIzinHakki { get; set; }
    public int Gececi { get; set; }
}
