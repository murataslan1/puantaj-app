namespace PuantajApp.Models;

public class HakedisParametre
{
    public int Id { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int IsGunu { get; set; }
    public int GunlukCalismaSaati { get; set; } = 8;
    public decimal YemekBirimUcreti { get; set; } = 330;
}
