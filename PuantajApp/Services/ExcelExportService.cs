using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using PuantajApp.Models;

namespace PuantajApp.Services;

public static class ExcelExportService
{
    private static readonly string[] AyAdlari =
    [
        "", "OCAK", "ŞUBAT", "MART", "NİSAN", "MAYIS", "HAZİRAN",
        "TEMMUZ", "AĞUSTOS", "EYLÜL", "EKİM", "KASIM", "ARALIK"
    ];

    /// <summary>
    /// Puantaj Excel dosyasi olusturur
    /// </summary>
    public static void OlusturPuantajExcel(
        string kayitYolu,
        int yil,
        int ay,
        List<Personel> personeller,
        List<PuantajKayit> kayitlar)
    {
        var ayAdi = AyAdlari[ay];
        var dosyaAdi = Path.Combine(kayitYolu, $"Puantaj_{ayAdi}_{yil}_Mesai.xlsx");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Puantaj");

        // Baslik
        ws.Cell(1, 1).Value = $"PUANTAJ - {ayAdi} {yil}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        // Kolon basliklari
        var cols = new[] { "Ad Soyad", "Unvan", "Birim" };
        int gunSayisi = DateTime.DaysInMonth(yil, ay);
        for (int i = 0; i < cols.Length; i++)
        {
            ws.Cell(2, i + 1).Value = cols[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }
        for (int g = 1; g <= gunSayisi; g++)
        {
            ws.Cell(2, 3 + g).Value = g;
            ws.Cell(2, 3 + g).Style.Font.Bold = true;
        }
        ws.Cell(2, 3 + gunSayisi + 1).Value = "Toplam Saat";
        ws.Cell(2, 3 + gunSayisi + 2).Value = "Yemek";
        ws.Cell(2, 3 + gunSayisi + 3).Value = "FM Saat";
        ws.Cell(2, 3 + gunSayisi + 4).Value = "RT FM Saat";

        // Veri satirlari
        int satirNo = 3;
        foreach (var p in personeller.OrderBy(p => p.AdSoyad))
        {
            var pKayitlar = kayitlar.Where(k => k.PersonelId == p.Id).ToList();

            ws.Cell(satirNo, 1).Value = p.AdSoyad;
            ws.Cell(satirNo, 2).Value = p.Unvan;
            ws.Cell(satirNo, 3).Value = p.Birim;

            decimal toplamSaat = 0, fmSaat = 0, rtFmSaat = 0;
            int yemekSayisi = 0;

            for (int g = 1; g <= gunSayisi; g++)
            {
                var kayit = pKayitlar.FirstOrDefault(k => k.Gun == g);
                if (kayit != null)
                {
                    var cell = ws.Cell(satirNo, 3 + g);
                    if (kayit.IzinTipi != null)
                    {
                        cell.Value = kayit.IzinTipi.ToUpper();
                    }
                    else if (kayit.GirisSaati != null && kayit.CikisSaati != null &&
                             TimeSpan.TryParse(kayit.GirisSaati, out var girisTe) &&
                             TimeSpan.TryParse(kayit.CikisSaati, out var cikisTe))
                    {
                        var sure = HesaplamaService.HesaplaSure(girisTe, cikisTe);
                        cell.Value = (double)sure;
                        toplamSaat += sure;
                        yemekSayisi += HesaplamaService.YemekHakki(girisTe, cikisTe);

                        if (kayit.GunTipi == "resmi_tatil")
                            rtFmSaat += HesaplamaService.HesaplaFazlaMesai(sure);
                        else
                            fmSaat += kayit.FmSaat ?? 0;
                    }
                }
                else if (kayit == null)
                {
                    var tarih = new DateTime(yil, ay, g);
                    if (tarih.DayOfWeek == DayOfWeek.Saturday || tarih.DayOfWeek == DayOfWeek.Sunday)
                        ws.Cell(satirNo, 3 + g).Value = "HS";
                }
            }

            ws.Cell(satirNo, 3 + gunSayisi + 1).Value = (double)toplamSaat;
            ws.Cell(satirNo, 3 + gunSayisi + 2).Value = yemekSayisi;
            ws.Cell(satirNo, 3 + gunSayisi + 3).Value = (double)fmSaat;
            ws.Cell(satirNo, 3 + gunSayisi + 4).Value = (double)rtFmSaat;

            satirNo++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(dosyaAdi);
    }

    /// <summary>
    /// Hakedis kapak Excel dosyasi olusturur
    /// </summary>
    public static void OlusturHakedisExcel(
        string kayitYolu,
        int yil,
        int ay,
        List<Personel> personeller,
        List<PuantajKayit> kayitlar,
        List<HakedisEkVeri> ekVeriler,
        HakedisParametre parametreler)
    {
        var ayAdi = AyAdlari[ay];
        var dosyaAdi = Path.Combine(kayitYolu, $"NVI_TD_{ayAdi}_{yil}_HAKEDIS.xlsx");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Hakedis");

        // Baslik
        ws.Cell(1, 1).Value = $"NVİ TD - {ayAdi} {yil} ALTYÜKLENİCİ HAKEDİŞİ";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 15).Merge();

        // Parametreler
        ws.Cell(2, 1).Value = $"İş Günü: {parametreler.IsGunu}";
        ws.Cell(2, 3).Value = $"Yemek Birim: {parametreler.YemekBirimUcreti} TL";

        // Kolon basliklari
        var basliklar = new[]
        {
            "No", "Ad Soyad", "Unvan", "Birim Ücreti", "Hakedis Gün",
            "FM Saat", "FM Ücret", "RT FM Saat", "RT FM Ücret",
            "Yemek", "FM Yemek", "Vergi Matr.", "TSS-GSS",
            "Kantin", "Kesilecek", "HAKEDİŞ"
        };
        for (int i = 0; i < basliklar.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = basliklar[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int satirNo = 5;
        decimal toplamHakedis = 0;

        foreach (var p in personeller.OrderBy(p => p.AdSoyad))
        {
            var pKayitlar = kayitlar.Where(k => k.PersonelId == p.Id).ToList();
            var ekVeri = ekVeriler.FirstOrDefault(e => e.PersonelId == p.Id)
                         ?? new HakedisEkVeri();

            // Hakedis gun hesapla
            int hakedisGun = parametreler.IsGunu;
            int gunSayisi = DateTime.DaysInMonth(yil, ay);
            decimal fmSaat = 0, rtFmSaat = 0;
            int yemekSayisi = 0, fmYemekSayisi = 0;

            foreach (var k in pKayitlar)
            {
                if (k.IzinTipi is "mi" or "r")
                    hakedisGun--;

                if (k.GirisSaati != null && k.CikisSaati != null &&
                    TimeSpan.TryParse(k.GirisSaati, out var girisTe) &&
                    TimeSpan.TryParse(k.CikisSaati, out var cikisTe))
                {
                    var sure = HesaplamaService.HesaplaSure(girisTe, cikisTe);
                    var yemek = HesaplamaService.YemekHakki(girisTe, cikisTe);

                    if (k.GunTipi == "resmi_tatil")
                    {
                        rtFmSaat += sure;
                        if (yemek == 1) fmYemekSayisi++;
                    }
                    else
                    {
                        fmSaat += k.FmSaat ?? 0;
                        if (k.FmSaat > 0 && yemek == 1) fmYemekSayisi++;
                        yemekSayisi += yemek;
                    }
                }
            }

            var fmUcret = HesaplamaService.HesaplaFmUcreti(fmSaat, p.BirimUcreti);
            var rtFmUcret = HesaplamaService.HesaplaRtFmUcreti(rtFmSaat, p.BirimUcreti);
            var yemekUcret = yemekSayisi * parametreler.YemekBirimUcreti;
            var fmYemekUcret = fmYemekSayisi * parametreler.YemekBirimUcreti;

            var hakedis = HesaplamaService.HesaplaFaturalanacakHakedis(
                p.BirimUcreti, parametreler.IsGunu, hakedisGun,
                fmUcret, rtFmUcret, yemekUcret, fmYemekUcret,
                ekVeri.KantinUcreti, ekVeri.VergiMatrahi, ekVeri.TssGssFarki, ekVeri.HakedistenKesilecek);

            toplamHakedis += hakedis;

            ws.Cell(satirNo, 1).Value = satirNo - 4;
            ws.Cell(satirNo, 2).Value = p.AdSoyad;
            ws.Cell(satirNo, 3).Value = p.Unvan;
            ws.Cell(satirNo, 4).Value = (double)p.BirimUcreti;
            ws.Cell(satirNo, 5).Value = hakedisGun;
            ws.Cell(satirNo, 6).Value = (double)fmSaat;
            ws.Cell(satirNo, 7).Value = (double)fmUcret;
            ws.Cell(satirNo, 8).Value = (double)rtFmSaat;
            ws.Cell(satirNo, 9).Value = (double)rtFmUcret;
            ws.Cell(satirNo, 10).Value = (double)yemekUcret;
            ws.Cell(satirNo, 11).Value = (double)fmYemekUcret;
            ws.Cell(satirNo, 12).Value = (double)ekVeri.VergiMatrahi;
            ws.Cell(satirNo, 13).Value = (double)ekVeri.TssGssFarki;
            ws.Cell(satirNo, 14).Value = (double)ekVeri.KantinUcreti;
            ws.Cell(satirNo, 15).Value = (double)ekVeri.HakedistenKesilecek;
            ws.Cell(satirNo, 16).Value = (double)hakedis;
            ws.Cell(satirNo, 16).Style.Font.Bold = true;

            satirNo++;
        }

        // Toplam satiri
        ws.Cell(satirNo, 2).Value = "TOPLAM";
        ws.Cell(satirNo, 2).Style.Font.Bold = true;
        ws.Cell(satirNo, 16).Value = (double)toplamHakedis;
        ws.Cell(satirNo, 16).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        wb.SaveAs(dosyaAdi);
    }
}
