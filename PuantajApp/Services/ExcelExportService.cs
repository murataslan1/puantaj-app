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

    private static string ColLetter(int col)
    {
        string result = "";
        while (col > 0)
        {
            col--;
            result = (char)('A' + col % 26) + result;
            col /= 26;
        }
        return result;
    }

    private static string CR(int row, int col) => $"{ColLetter(col)}{row}";
    private static string CRAbs(int row, int col) => $"${ColLetter(col)}${row}";

    // ========================================================================
    // PUANTAJ MESAİ EXCEL
    // ========================================================================
    public static void OlusturPuantajExcel(
        string kayitYolu,
        int yil,
        int ay,
        List<Personel> personeller,
        List<PuantajKayit> kayitlar,
        decimal yemekBirimUcreti = 330)
    {
        var ayAdi = AyAdlari[ay];
        var dosyaAdi = Path.Combine(kayitYolu, $"Puantaj_{ayAdi}_{yil}_Mesai.xlsx");
        int gunSayisi = DateTime.DaysInMonth(yil, ay);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"{ayAdi}{yil}");

        // Hangi günler resmi tatil?
        var rtGunler = new HashSet<int>();
        foreach (var k in kayitlar.Where(k => k.GunTipi == "resmi_tatil"))
            rtGunler.Add(k.Gun);

        // Normal günler (hafta içi + hafta sonu) ve RT günler
        var normalGunler = new List<int>();
        var rtGunlerList = new List<int>();
        for (int g = 1; g <= gunSayisi; g++)
        {
            if (rtGunler.Contains(g))
                rtGunlerList.Add(g);
            else
                normalGunler.Add(g);
        }

        // Sütun düzeni: A(1)-R(18) sabit, S(19)+ tarih sütunları
        const int dateStartCol = 19; // S sütunu

        var gunToCol = new Dictionary<int, int>();
        int col = dateStartCol;
        foreach (int g in normalGunler)
        {
            gunToCol[g] = col;
            col++;
        }
        int toplamMesaiCol = col++;
        int rtStartCol = col;
        foreach (int g in rtGunlerList)
        {
            gunToCol[g] = col;
            col++;
        }
        int toplamRtCol = col++;
        int yemekHakkiCol = col++;
        int yemekTutarCol = col;

        // ---- Renk tanımları (referans Excel'den) ----
        var lightBlue = XLColor.FromHtml("#B4C6E7");       // A-M header
        var amber = XLColor.FromHtml("#FFC000");            // N-Q header (toplam sütunları)
        var greenFill = XLColor.FromHtml("#C6EFCE");        // Tarih sütunları
        var greenFont = XLColor.FromHtml("#006100");        // Tarih font rengi
        var mediumBlue = XLColor.FromHtml("#8DB4E2");       // Toplam Mesai header
        var skyBlue = XLColor.FromHtml("#00B0F0");          // Yemek Tutarı

        // ---- ROW 1: Başlıklar ----
        ws.Cell(1, 1).Value = "Adı Soyadı";
        ws.Cell(1, 3).Value = "Gececi";
        ws.Cell(1, 4).Value = "Hafta İçi";
        ws.Cell(1, 5).Value = "Hafta Sonu";
        ws.Cell(1, 6).Value = "Hafta İçi Mesai";
        ws.Cell(1, 7).Value = "Gün";
        ws.Cell(1, 8).Value = "Resmi Tatil Mesai";
        ws.Cell(1, 9).Value = "İdari İzin";
        ws.Cell(1, 10).Value = "Kurum İzni";
        ws.Cell(1, 11).Value = "Mazeret İzni";
        ws.Cell(1, 12).Value = "Yıllık İzin";
        ws.Cell(1, 13).Value = "Raporlu";
        ws.Cell(1, 14).Value = "Toplam Hafta İçi-Sonu Mesai Saati";
        ws.Cell(1, 15).Value = "Toplam Resmi Tatil Mesai Saati";
        ws.Cell(1, 16).Value = "Toplam İdari İzin Mesai Saati";
        ws.Cell(1, 17).Value = "Toplam Yemek Tutarı TL";
        ws.Cell(1, 18).Value = "Puantaj";

        // Tarih başlıkları
        foreach (var kvp in gunToCol)
        {
            var tarih = new DateTime(yil, ay, kvp.Key);
            ws.Cell(1, kvp.Value).Value = tarih;
            ws.Cell(1, kvp.Value).Style.DateFormat.Format = "MM-dd-yy";
        }

        ws.Cell(1, toplamMesaiCol).Value = "Toplam Mesai";
        ws.Cell(1, toplamRtCol).Value = "Tolam Resmi tatil";
        ws.Cell(1, yemekHakkiCol).Value = "Yemek Hakkı";
        ws.Cell(1, yemekTutarCol).Value = "Yemek Yatırılacak Tutar";

        // Row 1 yükseklik ve formatlama
        ws.Row(1).Height = 167;
        for (int c = 1; c <= yemekTutarCol; c++)
        {
            var cell = ws.Cell(1, c);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontName = "Aptos Narrow";
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.TextRotation = 90;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }

        // A-M: Açık mavi header, font 11pt
        for (int c = 1; c <= 13; c++)
        {
            ws.Cell(1, c).Style.Font.FontSize = 11;
            ws.Cell(1, c).Style.Fill.BackgroundColor = lightBlue;
        }

        // N-Q: Amber header, font 14pt
        for (int c = 14; c <= 17; c++)
        {
            ws.Cell(1, c).Style.Font.FontSize = 14;
            ws.Cell(1, c).Style.Fill.BackgroundColor = amber;
        }

        // R + Tarih sütunları: Yeşil header, koyu yeşil font, 14pt
        ws.Cell(1, 18).Style.Font.FontSize = 14;
        ws.Cell(1, 18).Style.Fill.BackgroundColor = greenFill;
        ws.Cell(1, 18).Style.Font.FontColor = greenFont;
        foreach (var kvp in gunToCol)
        {
            var c = kvp.Value;
            ws.Cell(1, c).Style.Font.FontSize = 14;
            ws.Cell(1, c).Style.Fill.BackgroundColor = greenFill;
            ws.Cell(1, c).Style.Font.FontColor = greenFont;
        }

        // Toplam Mesai: Orta mavi, 14pt
        ws.Cell(1, toplamMesaiCol).Style.Font.FontSize = 14;
        ws.Cell(1, toplamMesaiCol).Style.Fill.BackgroundColor = mediumBlue;

        // Toplam RT: Açık mavi, 14pt
        ws.Cell(1, toplamRtCol).Style.Font.FontSize = 14;
        ws.Cell(1, toplamRtCol).Style.Fill.BackgroundColor = lightBlue;

        // Yemek Hakkı: Açık mavi, 14pt
        ws.Cell(1, yemekHakkiCol).Style.Font.FontSize = 14;
        ws.Cell(1, yemekHakkiCol).Style.Fill.BackgroundColor = lightBlue;

        // Yemek Tutarı: Gök mavisi, 14pt
        ws.Cell(1, yemekTutarCol).Style.Font.FontSize = 14;
        ws.Cell(1, yemekTutarCol).Style.Fill.BackgroundColor = skyBlue;

        // ---- ROW 2: Kategori etiketleri ----
        if (normalGunler.Count > 0)
        {
            ws.Cell(2, dateStartCol).Value = "Hafta İçi-Sonu Mesai";
            ws.Cell(2, dateStartCol).Style.Font.Bold = true;
            ws.Cell(2, dateStartCol).Style.Font.FontSize = 14;
            ws.Cell(2, dateStartCol).Style.Font.FontName = "Aptos Narrow";
            ws.Cell(2, dateStartCol).Style.Fill.BackgroundColor = greenFill;
            ws.Cell(2, dateStartCol).Style.Font.FontColor = greenFont;
        }
        if (rtGunlerList.Count > 0)
        {
            ws.Cell(2, rtStartCol).Value = "Resmi Tatil";
            ws.Cell(2, rtStartCol).Style.Font.Bold = true;
            ws.Cell(2, rtStartCol).Style.Font.FontSize = 14;
            ws.Cell(2, rtStartCol).Style.Font.FontName = "Aptos Narrow";
        }

        // ---- VERİ SATIRLARI: Her kişi 4 satır ----
        int satirNo = 3;
        var sortedPersoneller = personeller.OrderBy(p => p.AdSoyad).ToList();

        foreach (var p in sortedPersoneller)
        {
            var pKayitlar = kayitlar.Where(k => k.PersonelId == p.Id).ToList();
            int girisRow = satirNo;
            int cikisRow = satirNo + 1;
            int sureRow = satirNo + 2;
            int yemekRow = satirNo + 3;

            // R sütunu etiketleri
            ws.Cell(girisRow, 18).Value = "Giriş";
            ws.Cell(cikisRow, 18).Value = "Çıkış";
            ws.Cell(sureRow, 18).Value = "Süre";
            ws.Cell(yemekRow, 18).Value = "Yemek Hakkı";

            // 4 satırın tüm hücrelerine temel font uygula
            for (int c = 1; c <= yemekTutarCol; c++)
            {
                for (int r = girisRow; r <= yemekRow; r++)
                {
                    ws.Cell(r, c).Style.Font.FontName = "Aptos Narrow";
                    ws.Cell(r, c).Style.Font.FontSize = 14;
                }
            }

            // Ad Soyad (4 satır birleştir)
            ws.Cell(girisRow, 1).Value = p.AdSoyad;
            ws.Cell(girisRow, 1).Style.Font.Bold = true;
            ws.Cell(girisRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, 1, yemekRow, 1).Merge();

            // Özet sayılar hesapla
            int haftaIciCount = 0, haftaSonuCount = 0, haftaIciMesaiCount = 0;
            int rtMesaiCount = 0, idariIzin = 0, kurumIzni = 0, mazeretIzni = 0;
            int yillikIzin = 0, raporlu = 0;

            foreach (var k in pKayitlar)
            {
                bool hasEntry = k.GirisSaati != null && k.CikisSaati != null;
                if (k.GunTipi == "hafta_ici" && hasEntry) haftaIciCount++;
                if (k.GunTipi == "hafta_sonu" && hasEntry) haftaSonuCount++;
                if (k.GunTipi == "hafta_ici" && (k.FmSaat ?? 0) > 0) haftaIciMesaiCount++;
                if (k.GunTipi == "resmi_tatil" && hasEntry) rtMesaiCount++;
                if (k.IzinTipi == "yi") yillikIzin++;
                if (k.IzinTipi == "r") raporlu++;
                if (k.IzinTipi == "mi") idariIzin++;
            }

            // Özet hücrelere yaz ve 4 satır birleştir
            var ozetDegerler = new (int col, object val)[]
            {
                (3, p.Gececi), (4, haftaIciCount), (5, haftaSonuCount),
                (6, haftaIciMesaiCount), (7, 0), (8, rtMesaiCount),
                (9, idariIzin), (10, kurumIzni), (11, mazeretIzni),
                (12, yillikIzin), (13, raporlu)
            };
            foreach (var (c, val) in ozetDegerler)
            {
                ws.Cell(girisRow, c).Value = Convert.ToInt32(val);
                ws.Range(girisRow, c, yemekRow, c).Merge();
            }

            // N: Toplam Hİ-Sonu Mesai Saati = Toplam Mesai sütunu
            ws.Cell(girisRow, 14).FormulaA1 = CR(girisRow, toplamMesaiCol);
            ws.Cell(girisRow, 14).Style.DateFormat.Format = "[h]:mm";
            ws.Cell(girisRow, 14).Style.Fill.BackgroundColor = amber;
            ws.Cell(girisRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, 14, yemekRow, 14).Merge();

            // O: Toplam RT Mesai Saati
            ws.Cell(girisRow, 15).FormulaA1 = CR(girisRow, toplamRtCol);
            ws.Cell(girisRow, 15).Style.DateFormat.Format = "[h]:mm";
            ws.Cell(girisRow, 15).Style.Fill.BackgroundColor = amber;
            ws.Cell(girisRow, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, 15).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, 15, yemekRow, 15).Merge();

            // P: Toplam İdari İzin Mesai Saati
            ws.Cell(girisRow, 16).Value = 0;
            ws.Cell(girisRow, 16).Style.Fill.BackgroundColor = amber;
            ws.Cell(girisRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, 16).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, 16, yemekRow, 16).Merge();

            // Q: Toplam Yemek Tutarı TL
            ws.Cell(girisRow, 17).FormulaA1 = CR(girisRow, yemekTutarCol);
            ws.Cell(girisRow, 17).Style.DateFormat.Format = "\"₺\"#,##0.00";
            ws.Cell(girisRow, 17).Style.Fill.BackgroundColor = amber;
            ws.Cell(girisRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, 17).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, 17, yemekRow, 17).Merge();

            // Giriş/Çıkış DateTime değerleri yaz
            foreach (var k in pKayitlar)
            {
                if (!gunToCol.ContainsKey(k.Gun)) continue;
                int dateCol = gunToCol[k.Gun];

                if (k.GirisSaati != null && TimeSpan.TryParse(k.GirisSaati, out var giris))
                {
                    ws.Cell(girisRow, dateCol).Value = new DateTime(yil, ay, k.Gun, giris.Hours, giris.Minutes, 0);
                    ws.Cell(girisRow, dateCol).Style.DateFormat.Format = "yyyy/MM/dd\\ HH:mm";
                    ws.Cell(girisRow, dateCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                if (k.CikisSaati != null && TimeSpan.TryParse(k.CikisSaati, out var cikis))
                {
                    ws.Cell(cikisRow, dateCol).Value = new DateTime(yil, ay, k.Gun, cikis.Hours, cikis.Minutes, 0);
                    ws.Cell(cikisRow, dateCol).Style.DateFormat.Format = "yyyy/MM/dd\\ HH:mm";
                    ws.Cell(cikisRow, dateCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }

            // Süre ve Yemek Hakkı formülleri her tarih sütunu için
            foreach (var kvp in gunToCol)
            {
                int dateCol = kvp.Value;
                string g = CR(girisRow, dateCol);
                string c = CR(cikisRow, dateCol);

                // Süre formülü (mola kesintili)
                ws.Cell(sureRow, dateCol).FormulaA1 =
                    $"IF(OR({g}=\"\",{c}=\"\"), \"\", " +
                    $"IF({c}-{g}>TIME(15,0,0), {c}-{g}-TIME(2,0,0), " +
                    $"IF({c}-{g}>TIME(11,0,0), {c}-{g}-TIME(1,30,0), " +
                    $"IF({c}-{g}>TIME(7,30,0), {c}-{g}-TIME(1,0,0), " +
                    $"IF({c}-{g}>TIME(4,0,0), {c}-{g}-TIME(0,30,0), " +
                    $"{c}-{g}-TIME(0,15,0))))))";
                ws.Cell(sureRow, dateCol).Style.DateFormat.Format = "hh:mm;@";
                ws.Cell(sureRow, dateCol).Style.Font.Bold = true;
                ws.Cell(sureRow, dateCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(sureRow, dateCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Yemek Hakkı formülü (>=3 saat = 10800 saniye)
                ws.Cell(yemekRow, dateCol).FormulaA1 =
                    $"IF(OR({g}=\"\", {c}=\"\"), 0, " +
                    $"IF(ROUND(({c}+({c}<{g})-{g})*86400,0)>=10800, 1, 0))";
                ws.Cell(yemekRow, dateCol).Style.Font.Bold = true;
                ws.Cell(yemekRow, dateCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(yemekRow, dateCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Toplam Mesai: normal gün sürelerinin toplamı
            if (normalGunler.Count > 0)
            {
                string rangeStart = CR(sureRow, dateStartCol);
                string rangeEnd = CR(sureRow, dateStartCol + normalGunler.Count - 1);
                ws.Cell(girisRow, toplamMesaiCol).FormulaA1 = $"SUM({rangeStart}:{rangeEnd})";
                ws.Cell(girisRow, toplamMesaiCol).Style.DateFormat.Format = "[h]:mm";
                ws.Cell(girisRow, toplamMesaiCol).Style.Font.Bold = true;
                ws.Cell(girisRow, toplamMesaiCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(girisRow, toplamMesaiCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(girisRow, toplamMesaiCol, sureRow, toplamMesaiCol).Merge();

                // Normal yemek toplamı
                string yRangeStart = CR(yemekRow, dateStartCol);
                string yRangeEnd = CR(yemekRow, dateStartCol + normalGunler.Count - 1);
                ws.Cell(yemekRow, toplamMesaiCol).FormulaA1 = $"SUM({yRangeStart}:{yRangeEnd})";
                ws.Cell(yemekRow, toplamMesaiCol).Style.Font.Bold = true;
                ws.Cell(yemekRow, toplamMesaiCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Toplam RT
            if (rtGunlerList.Count > 0)
            {
                string rtRangeStart = CR(sureRow, rtStartCol);
                string rtRangeEnd = CR(sureRow, rtStartCol + rtGunlerList.Count - 1);
                ws.Cell(girisRow, toplamRtCol).FormulaA1 = $"SUM({rtRangeStart}:{rtRangeEnd})";
                ws.Cell(girisRow, toplamRtCol).Style.DateFormat.Format = "[h]:mm";
                ws.Cell(girisRow, toplamRtCol).Style.Font.Bold = true;
                ws.Cell(girisRow, toplamRtCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(girisRow, toplamRtCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(girisRow, toplamRtCol, sureRow, toplamRtCol).Merge();

                string rtYStart = CR(yemekRow, rtStartCol);
                string rtYEnd = CR(yemekRow, rtStartCol + rtGunlerList.Count - 1);
                ws.Cell(yemekRow, toplamRtCol).FormulaA1 = $"SUM({rtYStart}:{rtYEnd})";
                ws.Cell(yemekRow, toplamRtCol).Style.Font.Bold = true;
                ws.Cell(yemekRow, toplamRtCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                ws.Cell(girisRow, toplamRtCol).Value = 0;
                ws.Cell(girisRow, toplamRtCol).Style.Font.Bold = true;
                ws.Cell(girisRow, toplamRtCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(girisRow, toplamRtCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(girisRow, toplamRtCol, sureRow, toplamRtCol).Merge();
                ws.Cell(yemekRow, toplamRtCol).Value = 0;
                ws.Cell(yemekRow, toplamRtCol).Style.Font.Bold = true;
                ws.Cell(yemekRow, toplamRtCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Yemek Hakkı: normal + RT yemek toplamı
            string normalYemekRef = CR(yemekRow, toplamMesaiCol);
            string rtYemekRef = CR(yemekRow, toplamRtCol);
            ws.Cell(girisRow, yemekHakkiCol).FormulaA1 = $"SUM({normalYemekRef},{rtYemekRef})";
            ws.Cell(girisRow, yemekHakkiCol).Style.Font.Bold = true;
            ws.Cell(girisRow, yemekHakkiCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, yemekHakkiCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, yemekHakkiCol, yemekRow, yemekHakkiCol).Merge();

            // Yemek Yatırılacak Tutar
            ws.Cell(girisRow, yemekTutarCol).FormulaA1 =
                $"PRODUCT({CR(girisRow, yemekHakkiCol)}*{(double)yemekBirimUcreti})";
            ws.Cell(girisRow, yemekTutarCol).Style.DateFormat.Format = "\"₺\"#,##0.00";
            ws.Cell(girisRow, yemekTutarCol).Style.Font.Bold = true;
            ws.Cell(girisRow, yemekTutarCol).Style.Fill.BackgroundColor = skyBlue;
            ws.Cell(girisRow, yemekTutarCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(girisRow, yemekTutarCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(girisRow, yemekTutarCol, yemekRow, yemekTutarCol).Merge();

            satirNo += 4;
        }

        // Sütun genişlikleri (referans Excel'den)
        ws.Column(1).Width = 15.4;
        ws.Column(2).Width = 12.6;
        ws.Column(3).Width = 4.9;
        for (int c = 4; c <= 13; c++) ws.Column(c).Width = 3.8;
        ws.Column(14).Width = 8.2;
        ws.Column(15).Width = 6.8;
        ws.Column(16).Width = 13;
        ws.Column(17).Width = 11.7;
        ws.Column(18).Width = 15.2;
        // Tarih sütunları: 19.4 genişlik (referanstan)
        foreach (var kvp in gunToCol)
            ws.Column(kvp.Value).Width = 19.4;
        ws.Column(toplamMesaiCol).Width = 8.3;
        ws.Column(toplamRtCol).Width = 7.0;
        ws.Column(yemekHakkiCol).Width = 5.7;
        ws.Column(yemekTutarCol).Width = 11.7;

        wb.SaveAs(dosyaAdi);
    }

    // ========================================================================
    // HAKEDİŞ EXCEL
    // ========================================================================
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
        var donem = ay <= 6 ? "I" : "II";

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"{ayAdi}{yil}");

        int personCount = personeller.Count;
        int firstDataRow = 3;
        int lastDataRow = 2 + personCount;
        int toplamRow = lastDataRow + 1;
        int notaFaturaRow = toplamRow + 2;
        int isGunuRow = toplamRow + 3;
        int gunlukSaatRow = isGunuRow + 1;
        int aylikSaatRow = gunlukSaatRow + 1;
        int sozlesmeNotRow = aylikSaatRow + 2;

        // ---- ROW 1: Başlık ----
        string baslik = $"T.C. İÇİŞLERİ BAKANLIĞI NÜFUS VE VATANDAŞLIK İŞLERİ GENEL MÜDÜRLÜĞÜ İLE TÜRKSAT ARASINDA İMZALANAN\n BİLGİ TEKNOLOJİLERİ DANIŞMANLIK VE DESTEK HİZMET ALIMI SÖZLEŞMESİ \nAYLIK HAKEDİŞ {ayAdi} {yil} (30.12.2025 Tarihli Sözleşme)";
        ws.Cell(1, 1).Value = baslik;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Alignment.WrapText = true;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(1, 1, 1, 34).Merge(); // A1:AH1

        // AI1: Ay adı, AI2: Yıl, AJ1: etiket
        ws.Cell(1, 35).Value = ayAdi;  // AI1
        ws.Cell(2, 35).Value = yil;     // AI2
        ws.Cell(1, 36).Value = "İş Günü Ayı"; // AJ1

        ws.Row(1).Height = 81;

        // ---- ROW 2: Sütun Başlıkları (34 sütun A-AH) ----
        var basliklar = new (int col, string text, double fontSize)[]
        {
            (1, "No", 11),
            (2, "Danışman Adı Soyadı", 12),
            (3, "T.C.", 12),
            (4, "Unvanı", 12),
            (5, "Başlama Tarihi", 12),
            (6, "Unvan Değişikliği Tarihi", 12),
            (7, "Çıkış Tarihi", 12),
            (8, "Hakedişe Esas İş Günü ", 11),
            (9, "Hakedişe Esas Çalışma Saati", 11),
            (10, "Aylık Kullanılan Yıllık İzin (Gün)", 11),
            (11, "Yıllık İzin Hakkı  (Gün)", 11),
            (12, "Kullanılan Toplam Yıllık İzin (Gün)", 11),
            (13, "Kalan Yıllık İzin (Gün)", 11),
            (14, "Ücretsiz\nİzin", 11),
            (15, "Yasal İzin\n(Evlilik, ölüm vb.)", 10),
            (16, "Fazla Mesai", 10),
            (17, "Fazla Mesai Ücreti", 10),
            (18, "Resmi Tatil FM Saat", 10),
            (19, "Resmi Tatil FM Ücret", 10),
            (20, "Yemek Ücreti(İzin Rapor Düşülmüş)", 10),
            (21, "3 Saat\n ve Üzeri Fazla Mesai", 10),
            (22, "Fazla Mesai Yemek Ücreti", 10),
            (23, "Kantin Ücreti", 10),
            (24, "Gelir Damga İstisnasını Aşan Tutar", 10),
            (25, "Bordradaki Vergi Matrağı", 10),
            (26, "Yemek Gelir ve Damga Vergisi(İstisnayı Aşan)", 10),
            (27, "Rapor (Gün)", 11),
            (28, "İzin (Saat)", 11),
            (29, "Yıl İçerisinde Kullanılan Toplam Saatlik İzin", 11),
            (30, "TSS-GSS Farkı/12", 11),
            (31, "GSS Farkı Damga Vergisi Yükü", 11),
            (32, $"{yil}/{donem}. Dönem  Birim Ücreti", 12),
            (33, "Hakedişten Kesilecek Ücret", 12),
            (34, "Faturalanacak \nAylık Hakediş", 12),
        };

        foreach (var (c, text, size) in basliklar)
        {
            var cell = ws.Cell(2, c);
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = size;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(2).Height = 61;

        // ---- VERİ SATIRLARI (Row 3+) ----
        int siraNo = 1;
        foreach (var p in personeller.OrderBy(p => p.AdSoyad))
        {
            int r = firstDataRow + siraNo - 1;
            var pKayitlar = kayitlar.Where(k => k.PersonelId == p.Id).ToList();
            var ekVeri = ekVeriler.FirstOrDefault(e => e.PersonelId == p.Id) ?? new HakedisEkVeri();

            // FM ve RT hesapla
            decimal fmSaat = 0, rtFmSaat = 0;
            int yemekSayisi = 0, fmYemekSayisi = 0;
            int hakedisGun = parametreler.IsGunu;
            int raporGun = 0;

            foreach (var k in pKayitlar)
            {
                if (k.IzinTipi is "mi" or "r") hakedisGun--;
                if (k.IzinTipi == "r") raporGun++;

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
                        yemekSayisi += yemek;
                        if ((k.FmSaat ?? 0) >= 3) fmYemekSayisi++;
                    }
                }
            }

            var yemekUcret = yemekSayisi * parametreler.YemekBirimUcreti;
            var fmYemekUcret = fmYemekSayisi * parametreler.YemekBirimUcreti;

            // Rapor günü: ekVeri veya hesaplanan
            int raporGunFinal = ekVeri.RaporGun > 0 ? ekVeri.RaporGun : raporGun;

            // A: No
            ws.Cell(r, 1).Value = siraNo;
            // B: Ad Soyad
            ws.Cell(r, 2).Value = p.AdSoyad;
            // C: TC
            ws.Cell(r, 3).Value = p.TC;
            // D: Unvan
            ws.Cell(r, 4).Value = p.Unvan;
            // E: Başlama Tarihi
            if (p.BaslamaTarihi.HasValue)
            {
                ws.Cell(r, 5).Value = p.BaslamaTarihi.Value;
                ws.Cell(r, 5).Style.DateFormat.Format = "yyyy-MM-dd";
            }
            // F: Unvan Değişikliği Tarihi
            if (p.UnvanDegisiklikTarihi.HasValue)
            {
                ws.Cell(r, 6).Value = p.UnvanDegisiklikTarihi.Value;
                ws.Cell(r, 6).Style.DateFormat.Format = "yyyy-MM-dd";
            }
            // G: Çıkış Tarihi
            if (p.CikisTarihi.HasValue)
            {
                ws.Cell(r, 7).Value = p.CikisTarihi.Value;
                ws.Cell(r, 7).Style.DateFormat.Format = "yyyy-MM-dd";
            }

            // H: Hakedişe Esas İş Günü (formül: =$D$isGunuRow)
            ws.Cell(r, 8).FormulaA1 = CRAbs(isGunuRow, 4);

            // I: Hakedişe Esas Çalışma Saati (formül: =H*gunlukSaat)
            ws.Cell(r, 9).FormulaA1 = $"{CR(r, 8)}*{CRAbs(gunlukSaatRow, 4)}";

            // J: Aylık Kullanılan Yıllık İzin (Gün)
            if (ekVeri.YillikIzinKullanilan > 0)
                ws.Cell(r, 10).Value = (double)ekVeri.YillikIzinKullanilan;

            // K: Yıllık İzin Hakkı (Gün)
            ws.Cell(r, 11).Value = (double)p.YillikIzinHakki;

            // L: Kullanılan Toplam Yıllık İzin (Gün)
            if (ekVeri.YillikIzinKullanilan > 0)
                ws.Cell(r, 12).Value = (double)ekVeri.YillikIzinKullanilan;

            // M: Kalan Yıllık İzin (formül: =K-L)
            ws.Cell(r, 13).FormulaA1 = $"{CR(r, 11)}-{CR(r, 12)}";

            // N: Ücretsiz İzin
            ws.Cell(r, 14).Value = ekVeri.UcretsizIzin;

            // O: Yasal İzin
            if (ekVeri.YasalIzin > 0)
                ws.Cell(r, 15).Value = ekVeri.YasalIzin;

            // P: Fazla Mesai (saat)
            if (fmSaat > 0)
                ws.Cell(r, 16).Value = (double)fmSaat;

            // Q: Fazla Mesai Ücreti (formül: =P*AF/225*1.5)
            ws.Cell(r, 17).FormulaA1 = $"{CR(r, 16)}*{CR(r, 32)}/225*1.5";

            // R: Resmi Tatil FM Saat
            if (rtFmSaat > 0)
                ws.Cell(r, 18).Value = (double)rtFmSaat;

            // S: Resmi Tatil FM Ücret (formül: =R*AF/225*2)
            ws.Cell(r, 19).FormulaA1 = $"{CR(r, 18)}*{CR(r, 32)}/225*2";

            // T: Yemek Ücreti (İzin Rapor Düşülmüş)
            ws.Cell(r, 20).Value = (double)yemekUcret;

            // U: 3 Saat ve Üzeri Fazla Mesai (gün sayısı)
            if (fmYemekSayisi > 0)
                ws.Cell(r, 21).Value = fmYemekSayisi;

            // V: Fazla Mesai Yemek Ücreti
            if (fmYemekUcret > 0)
                ws.Cell(r, 22).Value = (double)fmYemekUcret;

            // W: Kantin Ücreti
            if (ekVeri.KantinUcreti > 0)
                ws.Cell(r, 23).Value = (double)ekVeri.KantinUcreti;

            // X: Gelir Damga İstisnasını Aşan Tutar (formül)
            ws.Cell(r, 24).FormulaA1 =
                $"IF(({CR(r, 20)}+{CR(r, 22)}+{CR(r, 23)}) - (({CR(r, 8)}-{CR(r, 27)}-{CR(r, 10)})*330) > 0," +
                $"({CR(r, 20)}+{CR(r, 22)}+{CR(r, 23)}) - (({CR(r, 8)}-{CR(r, 27)}-{CR(r, 10)})*330),0)";

            // Y: Bordradaki Vergi Matrağı (manuel)
            if (ekVeri.VergiMatrahi > 0)
                ws.Cell(r, 25).Value = (double)ekVeri.VergiMatrahi;

            // Z: Yemek Gelir ve Damga Vergisi (formül - vergi dilimi)
            ws.Cell(r, 26).FormulaA1 =
                $"ROUND(IF({CR(r, 25)}<=190000, {CR(r, 24)}*(0.15+0.00759)," +
                $"IF({CR(r, 25)}<=400000, {CR(r, 24)}*(0.2+0.00759)," +
                $"IF({CR(r, 25)}<=1500000, {CR(r, 24)}*(0.27+0.00759)," +
                $"IF({CR(r, 25)}<=5300000, {CR(r, 24)}*(0.35+0.00759)," +
                $"{CR(r, 24)}*(0.4+0.00759))))),2)";

            // AA: Rapor (Gün)
            if (raporGunFinal > 0)
                ws.Cell(r, 27).Value = raporGunFinal;

            // AB: İzin (Saat)
            if (ekVeri.IzinSaat > 0)
                ws.Cell(r, 28).Value = (double)ekVeri.IzinSaat;

            // AC: Yıl İçerisinde Kullanılan Toplam Saatlik İzin
            if (ekVeri.IzinSaat > 0)
                ws.Cell(r, 29).Value = (double)ekVeri.IzinSaat;

            // AD: TSS-GSS Farkı/12
            if (ekVeri.TssGssFarki > 0)
                ws.Cell(r, 30).Value = (double)ekVeri.TssGssFarki;

            // AE: GSS Farkı Damga Vergisi Yükü (formül)
            ws.Cell(r, 31).FormulaA1 = $"({CR(r, 30)}/0.99241)*0.00759";

            // AF: Birim Ücreti
            ws.Cell(r, 32).Value = (double)p.BirimUcreti;

            // AG: Hakedişten Kesilecek Ücret
            ws.Cell(r, 33).Value = (double)ekVeri.HakedistenKesilecek;

            // AH: Faturalanacak Aylık Hakediş (formül)
            ws.Cell(r, 34).FormulaA1 =
                $"ROUND(({CR(r, 32)}/{CRAbs(isGunuRow, 4)}*{CR(r, 8)}),2)" +
                $"+{CR(r, 17)}+{CR(r, 19)}+{CR(r, 20)}+{CR(r, 22)}+{CR(r, 23)}" +
                $"+{CR(r, 26)}+{CR(r, 30)}+{CR(r, 31)}-{CR(r, 33)}";
            ws.Cell(r, 34).Style.Font.Bold = true;

            // Satır yüksekliği
            ws.Row(r).Height = 55;

            // Font boyutu: A-Z(1-26)=12pt, AA-AH(27-34)=14pt (referansa uygun)
            for (int c = 1; c <= 26; c++)
                ws.Cell(r, c).Style.Font.FontSize = 12;
            for (int c = 27; c <= 34; c++)
                ws.Cell(r, c).Style.Font.FontSize = 14;

            siraNo++;
        }

        // ---- TOPLAM SATIRI ----
        ws.Cell(toplamRow, 33).Value = "Toplam";
        ws.Cell(toplamRow, 33).Style.Font.Bold = true;
        ws.Cell(toplamRow, 33).Style.Font.FontSize = 14;

        ws.Cell(toplamRow, 34).FormulaA1 =
            $"ROUND(SUM({CR(firstDataRow, 34)}:{CR(lastDataRow, 34)}),2)";
        ws.Cell(toplamRow, 34).Style.Font.Bold = true;
        ws.Cell(toplamRow, 34).Style.Font.FontSize = 14;

        // B toplam satırı birleştir
        ws.Range(toplamRow, 2, toplamRow, 32).Merge();

        // AJ3: İmza Tarihi
        ws.Cell(firstDataRow, 36).Value = "İmza Tarihi";

        // ---- NOT VE PARAMETRELER ----
        // Not: Fatura KDV
        ws.Cell(notaFaturaRow, 32).Value = "Not: Fatura edilirken KDV ilave edilecektir.";

        // İş Günü satırı
        ws.Cell(isGunuRow, 1).Value = $"{ayAdi} {yil} AYI İŞ GÜNÜ:";
        ws.Range(isGunuRow, 1, isGunuRow, 2).Merge();
        ws.Cell(isGunuRow, 4).Value = parametreler.IsGunu;

        // Günlük Çalışma Saati
        ws.Cell(gunlukSaatRow, 1).Value = "GÜNLÜK ÇALIŞMA SAATİ: ";
        ws.Cell(gunlukSaatRow, 4).Value = parametreler.GunlukCalismaSaati > 0
            ? parametreler.GunlukCalismaSaati : 8;

        // Aylık Çalışma Saati
        ws.Cell(aylikSaatRow, 1).Value = "AYLIK ÇALIŞMA SAATİ:";
        ws.Cell(aylikSaatRow, 4).FormulaA1 =
            $"{CR(isGunuRow, 4)}*{CR(gunlukSaatRow, 4)}";

        // Sözleşme notu
        ws.Cell(sozlesmeNotRow, 1).Value =
            "Not: Sözleşmenin 10.7 maddesine göre personelin kullandığı saatlik izinlerin çalışma yılı içinde toplam 45 iş saatini geçen kısmı için ilgili personel yönünden hakedişten kesinti yapılır." +
            "Sözleşmenin 10.9 maddesine göre personelin tek seferde 3 gün ve üzeri süreyle alacağı sağlık raporları için ilgili personel yönünden hakedişten kesinti yapılır.";
        ws.Range(sozlesmeNotRow, 1, sozlesmeNotRow, 29).Merge();
        ws.Cell(sozlesmeNotRow, 1).Style.Alignment.WrapText = true;

        // ---- SATIR YÜKSEKLİKLERİ (parametre/not satırları) ----
        ws.Row(toplamRow).Height = 55;
        ws.Row(notaFaturaRow).Height = 45;
        ws.Row(isGunuRow).Height = 45;
        ws.Row(gunlukSaatRow).Height = 35;
        ws.Row(aylikSaatRow).Height = 35;
        ws.Row(sozlesmeNotRow).Height = 35;

        // ---- SÜTUN GENİŞLİKLERİ ----
        var colWidths = new Dictionary<int, double>
        {
            {1, 7.9}, {2, 36.7}, {3, 16.3}, {4, 47.6}, {5, 13.7}, {6, 13.3},
            {7, 14.9}, {8, 9.6}, {9, 9.3}, {10, 12.7}, {11, 10.1}, {12, 11.1},
            {13, 9.1}, {14, 8.1}, {15, 9.1}, {16, 13}, {17, 12.9}, {18, 9.1},
            {19, 12.6}, {20, 13}, {21, 9.1}, {22, 12.7}, {23, 9.9}, {24, 15},
            {25, 13}, {26, 15}, {27, 8.9}, {28, 7.4}, {29, 13.7}, {30, 13},
            {31, 13}, {32, 19}, {33, 18.6}, {34, 22.4}, {35, 35.1}, {36, 13.1}
        };
        foreach (var (c, w) in colWidths)
            ws.Column(c).Width = w;

        wb.SaveAs(dosyaAdi);
    }
}
