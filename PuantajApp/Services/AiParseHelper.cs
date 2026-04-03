using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using PuantajApp.Models;

namespace PuantajApp.Services;

/// <summary>
/// Gemini ve Ollama servislerinin ortak kullandigi JSON parse yardimcilari.
/// </summary>
public static class AiParseHelper
{
    public const string PROMPT = """
        Bu bir TÜRKSAT Devam Takip Formudur. Form EL YAZISI ile doldurulmus olabilir.
        Formdaki bilgileri asagidaki JSON formatinda cikar.

        EL YAZISI OKUMA KURALLARI:
        - Form el yazisi, baskili veya karisik olabilir. Dikkatli oku.
        - Saatler genellikle 08:00-10:00 arasi giris, 17:00-19:00 arasi cikis olur.
        - Belirsiz rakamlari baglama gore degerlendir (ornegin giris saati 18:00 olamaz, muhtemelen 08:00'dir).
        - El yazisinda 1/7, 0/6, 5/3, 9/4 karismasi olabilir, dikkat et.
        - Imza, kase, mühür gibi alanlari yoksay, sadece tablo verisini oku.
        - Ustü cizilmis veya düzeltilmis degerlerde son yazilan degeri al.
        - Formda birden fazla sayfa varsa TÜM sayfalari isle.

        VERI KURALLARI:
        - yil: sayi olarak (ornek: 2026)
        - ay: sayi olarak (ornek: 1 = Ocak, 2 = Subat, ..., 12 = Aralik)
        - gun: SADECE gun numarasi, sayi olarak (ornek: 1, 2, 3, ..., 31)
        - giris/cikis: HH:mm formatinda (ornek: "09:00", "18:00"), bos ise null
        - mi_yi_r: sadece "mi", "yi" veya "r" degeri alabilir, bos ise null
          NOT: Formda her satirdaki R harfi "resmi tatil" ifadesi icin degildir.
          Sadece izin/rapor kolonunda acikca mi/yi/r yazilmissa bu alani doldur.
          Kisinin calistigi normal gunlerde mi_yi_r = null olmalidir.
        - fazla_mesai: saat araligi string olarak (ornek: "19:03-23:33"), yoksa null.
          Bazen sadece saat yazilabilir (ornek: "3" veya "3 saat"), bu durumda saati string olarak yaz.
        - aciklama: metin veya null (el yazisi notlari dahil)
        - Sadece formda veri olan gunleri dahil et, bos satirlari atlayabilirsin

        TEK bir JSON objesi don (dizi DEGIL). Ornek format:
        {
          "ad_soyad": "ADI SOYADI",
          "unvan": "Unvan",
          "birim": "Birim",
          "yil": 2026,
          "ay": 1,
          "gunler": [
            {"gun": 1, "giris": "09:00", "cikis": "18:00", "mi_yi_r": null, "fazla_mesai": null, "aciklama": null},
            {"gun": 2, "giris": "09:00", "cikis": "18:00", "mi_yi_r": null, "fazla_mesai": "19:03-23:33", "aciklama": "Calisma"},
            {"gun": 5, "giris": null, "cikis": null, "mi_yi_r": "yi", "fazla_mesai": null, "aciklama": "Yillik izin"}
          ]
        }
        """;

    public static PuantajParseResult? ParseJsonResponse(string text)
    {
        text = TemizleJson(text);

        using var parsed = JsonDocument.Parse(text);
        JsonElement root;

        if (parsed.RootElement.ValueKind == JsonValueKind.Array)
            root = parsed.RootElement[0];
        else
            root = parsed.RootElement;

        return ManuelParse(root);
    }

    public static PuantajParseResult ManuelParse(JsonElement root)
    {
        var result = new PuantajParseResult
        {
            AdSoyad = root.TryGetProperty("ad_soyad", out var ad) ? ad.GetString() ?? "" : "",
            Unvan = root.TryGetProperty("unvan", out var unvan) ? unvan.GetString() ?? "" : "",
            Birim = root.TryGetProperty("birim", out var birim) ? birim.GetString() ?? "" : "",
            Yil = ParseInt(root, "yil"),
            Ay = ParseAy(root)
        };

        if (root.TryGetProperty("gunler", out var gunler) && gunler.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in gunler.EnumerateArray())
            {
                var gunResult = new GunParseResult
                {
                    Gun = ParseGunNumarasi(g),
                    Giris = NormalizeSaat(GetStringProp(g, "giris")),
                    Cikis = NormalizeSaat(GetStringProp(g, "cikis")),
                    MiYiR = NormalizeMiYiR(GetStringProp(g, "mi_yi_r")),
                    FazlaMesai = GetStringProp(g, "fazla_mesai"),
                    Aciklama = GetStringProp(g, "aciklama")
                };
                if (gunResult.Gun > 0)
                    result.Gunler.Add(gunResult);
            }
        }

        return result;
    }

    public static int ParseInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = val.GetString() ?? "";
        var match = Regex.Match(s, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    public static int ParseAy(JsonElement root)
    {
        if (!root.TryGetProperty("ay", out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = (val.GetString() ?? "").Trim().ToUpperInvariant();

        if (int.TryParse(s, out var num)) return num;

        var ayMap = new Dictionary<string, int>
        {
            ["OCAK"] = 1, ["ŞUBAT"] = 2, ["SUBAT"] = 2, ["MART"] = 3,
            ["NİSAN"] = 4, ["NISAN"] = 4, ["MAYIS"] = 5, ["HAZİRAN"] = 6, ["HAZIRAN"] = 6,
            ["TEMMUZ"] = 7, ["AĞUSTOS"] = 8, ["AGUSTOS"] = 8, ["EYLÜL"] = 9, ["EYLUL"] = 9,
            ["EKİM"] = 10, ["EKIM"] = 10, ["KASIM"] = 11, ["ARALIK"] = 12
        };

        foreach (var kv in ayMap)
        {
            if (s.Contains(kv.Key)) return kv.Value;
        }
        return 0;
    }

    public static int ParseGunNumarasi(JsonElement g)
    {
        if (!g.TryGetProperty("gun", out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = val.GetString() ?? "";
        var match = Regex.Match(s, @"^\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    public static string? GetStringProp(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        if (val.ValueKind == JsonValueKind.Null) return null;
        var s = val.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    public static string? NormalizeSaat(string? saat)
    {
        if (saat == null) return null;
        var match = Regex.Match(saat, @"^(\d{1,2}):(\d{1,2})$");
        if (match.Success)
        {
            var h = int.Parse(match.Groups[1].Value);
            var m = int.Parse(match.Groups[2].Value);
            if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                return $"{h:D2}:{m:D2}";
        }
        return saat;
    }

    public static string? NormalizeMiYiR(string? val)
    {
        if (val == null) return null;
        var lower = val.Trim().ToLowerInvariant();
        return lower switch
        {
            "mi" => "mi",
            "yi" => "yi",
            "r" => "r",
            _ => null
        };
    }

    public static string TemizleJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text[7..];
        else if (text.StartsWith("```"))
            text = text[3..];
        if (text.EndsWith("```"))
            text = text[..^3];
        return text.Trim();
    }
}
