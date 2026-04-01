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

        // Baslik satirini atla
        var satir = 2;
        while (!ws.Row(satir).IsEmpty())
        {
            var p = new Personel
            {
                AdSoyad = ws.Cell(satir, 1).GetString().Trim(),
                TC = ws.Cell(satir, 2).GetString().Trim(),
                Unvan = ws.Cell(satir, 3).GetString().Trim(),
                Birim = ws.Cell(satir, 4).GetString().Trim(),
            };

            var baslamaStr = ws.Cell(satir, 5).GetString().Trim();
            if (DateTime.TryParse(baslamaStr, out var baslama))
                p.BaslamaTarihi = baslama;

            var ucretStr = ws.Cell(satir, 6).GetString().Trim();
            if (decimal.TryParse(ucretStr, out var ucret))
                p.BirimUcreti = ucret;

            var izinStr = ws.Cell(satir, 7).GetString().Trim();
            if (decimal.TryParse(izinStr, out var izin))
                p.YillikIzinHakki = izin;

            var gececi = ws.Cell(satir, 8).GetString().Trim();
            p.Gececi = gececi is "1" or "E" or "e" or "Evet" or "evet" ? 1 : 0;

            if (!string.IsNullOrWhiteSpace(p.AdSoyad))
                liste.Add(p);

            satir++;
        }

        return liste;
    }
}
