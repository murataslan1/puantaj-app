using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using PuantajApp.Models;

namespace PuantajApp.Services;

public static class ExcelImportService
{
    /// <summary>
    /// Excel dosyasindan personel listesi import eder.
    /// Beklenen sutunlar: Ad Soyad, TC, Unvan, Birim, Baslama Tarihi, Birim Ucreti, Yillik Izin, Gececi
    /// </summary>
    public static List<Personel> ImportPersonel(string dosyaYolu)
    {
        var liste = new List<Personel>();
        using var wb = new XLWorkbook(dosyaYolu);
        var ws = wb.Worksheets.Worksheet(1);

        // Sutun pozisyonlarini otomatik bul (baslik satiri tarayarak)
        int colAdSoyad = 1, colTc = 2, colUnvan = 3, colBirim = 4;
        int colBaslama = 5, colUcret = 6, colIzin = 7, colGececi = 8;
        int baslangicSatir = 2;

        // Baslik satirinda "Ad" veya "Soyad" iceren kolonu bul
        for (int col = 1; col <= 15; col++)
        {
            var h = ws.Cell(1, col).GetString().Trim().ToUpperInvariant();
            if (h.Contains("AD") && (h.Contains("SOYAD") || h.Contains("İSİM") || h.Contains("ISIM")))
                colAdSoyad = col;
            else if (h.Contains("T.C") || h.Contains("TC") || h.Contains("KİMLİK") || h.Contains("KIMLIK"))
                colTc = col;
            else if (h.Contains("UNVAN"))
                colUnvan = col;
            else if (h.Contains("BİRİM") || h.Contains("BIRIM"))
                colBirim = col;
            else if (h.Contains("BAŞLAMA") || h.Contains("BASLAMA") || h.Contains("TARİH") || h.Contains("TARIH"))
                colBaslama = col;
            else if (h.Contains("ÜCRET") || h.Contains("UCRET") || h.Contains("MAAS") || h.Contains("MAAŞ"))
                colUcret = col;
            else if (h.Contains("İZİN") || h.Contains("IZIN"))
                colIzin = col;
            else if (h.Contains("GECE"))
                colGececi = col;
        }

        // "No" sutunu varsa ad soyad bir sonraki kolondadir, kontrol et
        var ilkBaslik = ws.Cell(1, 1).GetString().Trim().ToUpperInvariant();
        if (ilkBaslik is "NO" or "SIRA" or "#" or "S.NO")
        {
            // A kolonu sira no, gercek veriler B'den basliyor (eger otomatik bulunamadiysa)
            if (colAdSoyad == 1) colAdSoyad = 2;
        }

        // Baslik satirini atla
        var satir = baslangicSatir;
        while (!ws.Row(satir).IsEmpty())
        {
            var adSoyad = ws.Cell(satir, colAdSoyad).GetString().Trim();

            // Gecersiz satirlari atla (sayisal sira no, bos, veya baslik/not satirlari)
            if (string.IsNullOrWhiteSpace(adSoyad) ||
                int.TryParse(adSoyad, out _) ||
                adSoyad.Contains("AYI") || adSoyad.Contains("GÜNÜ") ||
                adSoyad.Contains("SAATİ") || adSoyad.Contains("SAATI") ||
                adSoyad.ToUpperInvariant().Contains("TOPLAM") ||
                adSoyad.ToUpperInvariant() == "NO")
            {
                satir++;
                continue;
            }

            var p = new Personel
            {
                AdSoyad = adSoyad,
                TC = ws.Cell(satir, colTc).GetString().Trim(),
                Unvan = ws.Cell(satir, colUnvan).GetString().Trim(),
                Birim = ws.Cell(satir, colBirim).GetString().Trim(),
            };

            var baslamaStr = ws.Cell(satir, colBaslama).GetString().Trim();
            if (DateTime.TryParse(baslamaStr, out var baslama))
                p.BaslamaTarihi = baslama;

            var ucretStr = ws.Cell(satir, colUcret).GetString().Trim();
            if (decimal.TryParse(ucretStr, out var ucret))
                p.BirimUcreti = ucret;

            var izinStr = ws.Cell(satir, colIzin).GetString().Trim();
            if (decimal.TryParse(izinStr, out var izin))
                p.YillikIzinHakki = izin;

            var gececi = ws.Cell(satir, colGececi).GetString().Trim();
            p.Gececi = gececi is "1" or "E" or "e" or "Evet" or "evet" ? 1 : 0;

            liste.Add(p);
            satir++;
        }

        return liste;
    }
}
