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
    /// Hem Puantaj Mesai hem Hakedis formatini tanir.
    /// </summary>
    public static List<Personel> ImportPersonel(string dosyaYolu)
    {
        var liste = new List<Personel>();
        using var wb = new XLWorkbook(dosyaYolu);
        var ws = wb.Worksheets.Worksheet(1);

        // Sutun pozisyonlari: -1 = bulunamadi
        int colAdSoyad = -1, colTc = -1, colUnvan = -1, colBirim = -1;
        int colBaslama = -1, colUcret = -1, colIzin = -1, colGececi = -1;
        int baslangicSatir = -1;

        // Baslik satirini bul: Row 1, 2 veya 3'te olabilir
        for (int headerRow = 1; headerRow <= 3; headerRow++)
        {
            // Bu satirdaki Ad Soyad kolonunu ara
            int adSoyadBulunan = -1;
            for (int col = 1; col <= 40; col++)
            {
                var h = ws.Cell(headerRow, col).GetString().Trim();
                if (string.IsNullOrEmpty(h) || h.Length > 80) continue; // Uzun metinleri atla (baslik satiri)

                var hu = h.ToUpperInvariant();

                if (adSoyadBulunan < 0 &&
                    ((hu.Contains("AD") && (hu.Contains("SOYAD") || hu.Contains("ISIM"))) ||
                     (hu.Contains("DANISMA") && hu.Contains("AD"))))
                {
                    adSoyadBulunan = col;
                }
            }

            // Bu satirda Ad Soyad kolonu bulunduysa, diger kolonlari da bu satirda ara
            if (adSoyadBulunan >= 0)
            {
                colAdSoyad = adSoyadBulunan;
                baslangicSatir = headerRow + 1;

                for (int col = 1; col <= 40; col++)
                {
                    var h = ws.Cell(headerRow, col).GetString().Trim();
                    if (string.IsNullOrEmpty(h) || h.Length > 80) continue;

                    var hu = h.ToUpperInvariant();

                    if (colTc < 0 && (hu.Contains("T.C") || hu == "TC" || hu.Contains("KIMLIK")))
                        colTc = col;
                    else if (colUnvan < 0 && hu.Contains("UNVAN"))
                        colUnvan = col;
                    else if (colBirim < 0 && !hu.Contains("UCRET") && !hu.Contains("ÜCRET") &&
                             (hu.Contains("BIRIM") || hu.Contains("BİRİM")) &&
                             !hu.Contains("YEMEK"))
                        colBirim = col;
                    else if (colBaslama < 0 && (hu.Contains("BASLAMA") || hu.Contains("BAŞLAMA")))
                        colBaslama = col;
                    else if (colUcret < 0 &&
                             (hu.Contains("BIRIM") || hu.Contains("BİRİM")) &&
                             (hu.Contains("UCRET") || hu.Contains("ÜCRET")))
                        colUcret = col;
                    else if (colIzin < 0 && hu.Contains("YILLIK") && (hu.Contains("HAK") || hu.Contains("IZIN") || hu.Contains("İZİN")))
                        colIzin = col;
                    else if (colGececi < 0 && hu.Contains("GECE"))
                        colGececi = col;
                }
                break; // Baslik satiri bulundu
            }
        }

        // Ad Soyad kolonu bulunamadiysa import yapilamaz
        if (colAdSoyad < 0 || baslangicSatir < 0)
            return liste;

        var satir = baslangicSatir;
        int maxBos = 5;
        int bosKounter = 0;

        while (bosKounter < maxBos)
        {
            var adSoyad = ws.Cell(satir, colAdSoyad).GetString().Trim();

            // Bos satir
            if (string.IsNullOrWhiteSpace(adSoyad))
            {
                bosKounter++;
                satir++;
                continue;
            }
            bosKounter = 0;

            // Gecersiz satirlari atla
            if (int.TryParse(adSoyad, out _) ||
                adSoyad.Contains("AYI") || adSoyad.Contains("GÜNÜ") ||
                adSoyad.Contains("SAATİ") || adSoyad.Contains("SAATI") ||
                adSoyad.ToUpperInvariant().Contains("TOPLAM") ||
                adSoyad.ToUpperInvariant() == "NO" ||
                adSoyad.ToUpperInvariant().Contains("NOT:") ||
                adSoyad.ToUpperInvariant().Contains("SÖZLEŞME"))
            {
                satir++;
                continue;
            }

            var p = new Personel { AdSoyad = adSoyad };

            if (colTc > 0)
                p.TC = ws.Cell(satir, colTc).GetString().Trim();

            if (colUnvan > 0)
                p.Unvan = ws.Cell(satir, colUnvan).GetString().Trim();

            if (colBirim > 0)
                p.Birim = ws.Cell(satir, colBirim).GetString().Trim();

            if (colBaslama > 0)
            {
                var baslamaStr = ws.Cell(satir, colBaslama).GetString().Trim();
                if (DateTime.TryParse(baslamaStr, out var baslama))
                    p.BaslamaTarihi = baslama;
            }

            if (colUcret > 0)
            {
                var ucretCell = ws.Cell(satir, colUcret);
                if (ucretCell.DataType == XLDataType.Number)
                    p.BirimUcreti = (decimal)ucretCell.GetDouble();
                else if (decimal.TryParse(ucretCell.GetString().Trim(), out var ucret))
                    p.BirimUcreti = ucret;
            }

            if (colIzin > 0)
            {
                var izinCell = ws.Cell(satir, colIzin);
                if (izinCell.DataType == XLDataType.Number)
                    p.YillikIzinHakki = (decimal)izinCell.GetDouble();
                else if (decimal.TryParse(izinCell.GetString().Trim(), out var izin))
                    p.YillikIzinHakki = izin;
            }

            if (colGececi > 0)
            {
                var gececi = ws.Cell(satir, colGececi).GetString().Trim();
                p.Gececi = gececi is "1" or "E" or "e" or "Evet" or "evet" ? 1 : 0;
            }

            liste.Add(p);
            satir++;
        }

        return liste;
    }
}
