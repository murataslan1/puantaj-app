using PuantajApp.Services;

namespace PuantajApp.Tests;

public class HesaplamaServiceTests
{
    // === HesaplaSure (Net calisma suresi, dinlenme dusulmus) ===

    [Fact]
    public void HesaplaSure_8SaatlikGun_7SaatDoner()
    {
        // 09:00 - 18:00 = 9 saat, 7.5 < 9 < 11 → 1 saat dinlenme = 8 saat
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(9), TimeSpan.FromHours(18));
        Assert.Equal(8m, result);
    }

    [Fact]
    public void HesaplaSure_KisaGun_DinlenmeDusuler()
    {
        // 09:00 - 12:00 = 3 saat, <= 4 → 0.25 saat dinlenme = 2.75
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        Assert.Equal(2.75m, result);
    }

    [Fact]
    public void HesaplaSure_4_7Arasi_YarimSaatDinlenme()
    {
        // 09:00 - 14:00 = 5 saat, 4 < 5 < 7.5 → 0.5 saat dinlenme = 4.5
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(9), TimeSpan.FromHours(14));
        Assert.Equal(4.5m, result);
    }

    [Fact]
    public void HesaplaSure_UzunGun_1BucukSaatDinlenme()
    {
        // 08:00 - 20:00 = 12 saat, 11 < 12 < 15 → 1.5 saat dinlenme = 10.5
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(8), TimeSpan.FromHours(20));
        Assert.Equal(10.5m, result);
    }

    [Fact]
    public void HesaplaSure_CokUzunGun_2SaatDinlenme()
    {
        // 06:00 - 23:00 = 17 saat, > 15 → 2 saat dinlenme = 15
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(6), TimeSpan.FromHours(23));
        Assert.Equal(15m, result);
    }

    [Fact]
    public void HesaplaSure_CikisGiristeOnce_SifirDoner()
    {
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(18), TimeSpan.FromHours(9));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void HesaplaSure_AyniSaat_SifirDoner()
    {
        var result = HesaplamaService.HesaplaSure(
            TimeSpan.FromHours(9), TimeSpan.FromHours(9));
        Assert.Equal(0m, result);
    }

    // === YemekHakki ===

    [Fact]
    public void YemekHakki_3SaatVeUzeri_1Doner()
    {
        // 09:00 - 12:00 = 3 saat = 10800 saniye → 1 yemek
        Assert.Equal(1, HesaplamaService.YemekHakki(
            TimeSpan.FromHours(9), TimeSpan.FromHours(12)));
    }

    [Fact]
    public void YemekHakki_3SaatAlti_0Doner()
    {
        // 09:00 - 11:59 < 3 saat → 0 yemek
        Assert.Equal(0, HesaplamaService.YemekHakki(
            TimeSpan.FromHours(9), new TimeSpan(11, 59, 59)));
    }

    [Fact]
    public void YemekHakki_TamGun_1Doner()
    {
        Assert.Equal(1, HesaplamaService.YemekHakki(
            TimeSpan.FromHours(9), TimeSpan.FromHours(18)));
    }

    // === HesaplaFazlaMesai ===

    [Fact]
    public void HesaplaFazlaMesai_8SaattenFazla_FarkiDoner()
    {
        Assert.Equal(2m, HesaplamaService.HesaplaFazlaMesai(10m));
    }

    [Fact]
    public void HesaplaFazlaMesai_8SaatVeAlti_SifirDoner()
    {
        Assert.Equal(0m, HesaplamaService.HesaplaFazlaMesai(7.5m));
    }

    [Fact]
    public void HesaplaFazlaMesai_Tam8Saat_SifirDoner()
    {
        Assert.Equal(0m, HesaplamaService.HesaplaFazlaMesai(8m));
    }

    [Fact]
    public void HesaplaFazlaMesai_CustomGunlukSaat_Hesaplar()
    {
        // 9 - 7 = 2 saat fazla mesai
        Assert.Equal(2m, HesaplamaService.HesaplaFazlaMesai(9m, gunlukCalismaSaati: 7));
    }

    // === HesaplaFmUcreti ===

    [Fact]
    public void HesaplaFmUcreti_FormulDogruCalisiyor()
    {
        // 2 saat FM * 15000 / 225 * 1.5 = 200
        var result = HesaplamaService.HesaplaFmUcreti(2m, 15000m);
        Assert.Equal(200m, result);
    }

    [Fact]
    public void HesaplaFmUcreti_SifirSaat_SifirDoner()
    {
        Assert.Equal(0m, HesaplamaService.HesaplaFmUcreti(0m, 15000m));
    }

    // === HesaplaRtFmUcreti ===

    [Fact]
    public void HesaplaRtFmUcreti_FormulDogruCalisiyor()
    {
        // 2 saat RT * 15000 / 225 * 2 = 266.67
        var result = HesaplamaService.HesaplaRtFmUcreti(2m, 15000m);
        Assert.Equal(266.67m, result);
    }

    [Fact]
    public void HesaplaRtFmUcreti_SifirSaat_SifirDoner()
    {
        Assert.Equal(0m, HesaplamaService.HesaplaRtFmUcreti(0m, 15000m));
    }

    // === HesaplaFaturalanacakHakedis ===

    [Fact]
    public void HesaplaFaturalanacakHakedis_TemelHesaplama()
    {
        // Temel: 15000 / 21 * 21 = 15000
        // + FM 200 + RT 0 + Yemek 3150 + FmYemek 0 + Kantin 0 + Vergi 0 + GSS 0 - Kesilecek 0
        var result = HesaplamaService.HesaplaFaturalanacakHakedis(
            birimUcreti: 15000m, isGunu: 21, hakedisGun: 21,
            fmUcret: 200m, rtFmUcret: 0m, yemekUcreti: 3150m,
            fmYemekUcreti: 0m, kantinUcreti: 0m,
            vergiMatrahi: 0m, gssFarki: 0m, kesilecek: 0m);
        Assert.Equal(18350m, result);
    }

    [Fact]
    public void HesaplaFaturalanacakHakedis_KesilecekDusuluyor()
    {
        var result = HesaplamaService.HesaplaFaturalanacakHakedis(
            birimUcreti: 10000m, isGunu: 20, hakedisGun: 20,
            fmUcret: 0m, rtFmUcret: 0m, yemekUcreti: 0m,
            fmYemekUcreti: 0m, kantinUcreti: 0m,
            vergiMatrahi: 0m, gssFarki: 0m, kesilecek: 500m);
        Assert.Equal(9500m, result);
    }

    // === ParseFmAralik ===

    [Fact]
    public void ParseFmAralik_ValidRange_ParsesCorrectly()
    {
        var (baslangic, bitis) = HesaplamaService.ParseFmAralik("19:03-23:33");
        Assert.Equal(new TimeSpan(19, 3, 0), baslangic);
        Assert.Equal(new TimeSpan(23, 33, 0), bitis);
    }

    [Fact]
    public void ParseFmAralik_Null_ReturnsNulls()
    {
        var (baslangic, bitis) = HesaplamaService.ParseFmAralik(null);
        Assert.Null(baslangic);
        Assert.Null(bitis);
    }

    [Fact]
    public void ParseFmAralik_Empty_ReturnsNulls()
    {
        var (baslangic, bitis) = HesaplamaService.ParseFmAralik("");
        Assert.Null(baslangic);
        Assert.Null(bitis);
    }

    [Fact]
    public void ParseFmAralik_InvalidFormat_ReturnsNulls()
    {
        var (baslangic, bitis) = HesaplamaService.ParseFmAralik("invalid");
        Assert.Null(baslangic);
        Assert.Null(bitis);
    }

    // === HesaplaFmSaatAralik ===

    [Fact]
    public void HesaplaFmSaatAralik_ValidRange_CalculatesHours()
    {
        // 19:00 - 23:00 = 4 saat
        Assert.Equal(4m, HesaplamaService.HesaplaFmSaatAralik("19:00-23:00"));
    }

    [Fact]
    public void HesaplaFmSaatAralik_CrossMidnight_CalculatesCorrectly()
    {
        // 22:00 - 02:00 = 4 saat (gece yarisi gecisi)
        Assert.Equal(4m, HesaplamaService.HesaplaFmSaatAralik("22:00-02:00"));
    }

    [Fact]
    public void HesaplaFmSaatAralik_Null_ReturnsZero()
    {
        Assert.Equal(0m, HesaplamaService.HesaplaFmSaatAralik(null));
    }

    [Fact]
    public void HesaplaFmSaatAralik_PartialHours_RoundsTo2Decimals()
    {
        // 19:03 - 23:33 = 4 saat 30 dk = 4.5
        Assert.Equal(4.5m, HesaplamaService.HesaplaFmSaatAralik("19:03-23:33"));
    }
}
