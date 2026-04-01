using System;

namespace PuantajApp.Services;

public static class HesaplamaService
{
    /// <summary>
    /// Giris-cikis arasindaki net calisma suresini hesaplar (dinlenme dusulur)
    /// </summary>
    public static decimal HesaplaSure(TimeSpan giris, TimeSpan cikis)
    {
        var fark = (cikis - giris).TotalHours;
        if (fark <= 0) return 0;
        if (fark > 15) return (decimal)(fark - 2.0);
        if (fark > 11) return (decimal)(fark - 1.5);
        if (fark > 7.5) return (decimal)(fark - 1.0);
        if (fark > 4) return (decimal)(fark - 0.5);
        return (decimal)(fark - 0.25);
    }

    /// <summary>
    /// Yemek hakkini hesaplar: 3 saat ve uzeri calisma = 1 yemek
    /// </summary>
    public static int YemekHakki(TimeSpan giris, TimeSpan cikis)
    {
        return (cikis - giris).TotalSeconds >= 10800 ? 1 : 0;
    }

    /// <summary>
    /// Fazla mesai saatini hesaplar: gunluk 8 saat usteri
    /// </summary>
    public static decimal HesaplaFazlaMesai(decimal toplamSure, int gunlukCalismaSaati = 8)
    {
        var fazla = toplamSure - gunlukCalismaSaati;
        return fazla > 0 ? Math.Round(fazla, 2) : 0;
    }

    /// <summary>
    /// FM ucretini hesaplar: FmSaat * BirimUcreti / 225 * 1.5
    /// </summary>
    public static decimal HesaplaFmUcreti(decimal fmSaat, decimal birimUcreti)
    {
        return Math.Round(fmSaat * birimUcreti / 225 * 1.5m, 2);
    }

    /// <summary>
    /// Resmi tatil FM ucretini hesaplar: RtSaat * BirimUcreti / 225 * 2
    /// </summary>
    public static decimal HesaplaRtFmUcreti(decimal rtSaat, decimal birimUcreti)
    {
        return Math.Round(rtSaat * birimUcreti / 225 * 2m, 2);
    }

    /// <summary>
    /// Faturalanacak hakedisi hesaplar
    /// </summary>
    public static decimal HesaplaFaturalanacakHakedis(
        decimal birimUcreti,
        int isGunu,
        int hakedisGun,
        decimal fmUcret,
        decimal rtFmUcret,
        decimal yemekUcreti,
        decimal fmYemekUcreti,
        decimal kantinUcreti,
        decimal vergiMatrahi,
        decimal gssFarki,
        decimal kesilecek)
    {
        var temel = Math.Round(birimUcreti / isGunu * hakedisGun, 2);
        return temel + fmUcret + rtFmUcret + yemekUcreti + fmYemekUcreti
               + kantinUcreti + vergiMatrahi + gssFarki - kesilecek;
    }

    /// <summary>
    /// Fazla mesai saati araligini parse eder: "19:03-23:33" -> (saat, dakika, saat, dakika)
    /// </summary>
    public static (TimeSpan? Baslangic, TimeSpan? Bitis) ParseFmAralik(string? fmAralik)
    {
        if (string.IsNullOrWhiteSpace(fmAralik)) return (null, null);

        var parts = fmAralik.Split('-');
        if (parts.Length == 2 &&
            TimeSpan.TryParse(parts[0].Trim(), out var baslangic) &&
            TimeSpan.TryParse(parts[1].Trim(), out var bitis))
        {
            return (baslangic, bitis);
        }
        return (null, null);
    }

    /// <summary>
    /// Fazla mesai araliginin suresini saat olarak hesaplar
    /// </summary>
    public static decimal HesaplaFmSaatAralik(string? fmAralik)
    {
        var (baslangic, bitis) = ParseFmAralik(fmAralik);
        if (baslangic == null || bitis == null) return 0;
        var sure = bitis.Value - baslangic.Value;
        if (sure < TimeSpan.Zero) sure = sure.Add(TimeSpan.FromHours(24));
        return Math.Round((decimal)sure.TotalHours, 2);
    }
}
