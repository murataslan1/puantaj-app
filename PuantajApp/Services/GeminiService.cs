using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PuantajApp.Models;

namespace PuantajApp.Services;

public class GeminiService
{
    private readonly HttpClient _http = new();
    private string? _apiKey;

    private const string MODEL = "gemini-2.0-flash";

    private const string PROMPT = """
        Bu bir TÜRKSAT Devam Takip Formudur. Formdaki bilgileri asagidaki JSON formatinda cikar.

        ONEMLI KURALLAR:
        - yil: sayi olarak (ornek: 2026)
        - ay: sayi olarak (ornek: 1 = Ocak, 2 = Subat, ..., 12 = Aralik)
        - gun: SADECE gun numarasi, sayi olarak (ornek: 1, 2, 3, ..., 31)
        - giris/cikis: HH:mm formatinda (ornek: "09:00", "18:00"), bos ise null
        - mi_yi_r: sadece "mi", "yi" veya "r" degeri alabilir, bos ise null
          NOT: Formda her satirdaki R harfi "resmi tatil" ifadesi icin degildir.
          Sadece izin/rapor kolonunda acikca mi/yi/r yazilmissa bu alani doldur.
          Kisinin calistigi normal gunlerde mi_yi_r = null olmalidir.
        - fazla_mesai: saat araligi string olarak (ornek: "19:03-23:33"), yoksa null
        - aciklama: metin veya null
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
            {"gun": 2, "giris": "09:00", "cikis": "18:00", "mi_yi_r": null, "fazla_mesai": "19:03-23:33", "aciklama": "Calisma"}
          ]
        }
        """;

    public void SetApiKey(string apiKey) => _apiKey = apiKey;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<PuantajParseResult?> ParsePdfAsync(byte[] pdfBytes, string dosyaAdi)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API key girilmedi.");

        var base64 = Convert.ToBase64String(pdfBytes);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = PROMPT },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "application/pdf",
                                data = base64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={_apiKey}";
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseText;
        const int maxRetry = 3;

        for (int attempt = 0; ; attempt++)
        {
            var reqContent = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _http.PostAsync(url, reqContent);
            responseText = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode == 429 && attempt < maxRetry)
            {
                // Retry-After header varsa onu kullan, yoksa exponential backoff: 5s, 15s, 45s
                var delaySec = 5 * (int)Math.Pow(3, attempt);
                if (response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
                    delaySec = (int)retryAfter.TotalSeconds + 1;
                await Task.Delay(delaySec * 1000);
                continue;
            }

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API Hatasi {(int)response.StatusCode}: {responseText[..Math.Min(200, responseText.Length)]}");

            break;
        }

        // Gemini yanit parse
        using var doc = JsonDocument.Parse(responseText);

        var finishReason = "";
        try
        {
            finishReason = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("finishReason")
                .GetString() ?? "";
        }
        catch { }

        string text;
        try
        {
            text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Gemini yanit bos (finishReason={finishReason}): {ex.Message}");
        }

        text = TemizleJson(text);

        // JSON parse - esnek, array veya obje olabilir
        try
        {
            using var parsed = JsonDocument.Parse(text);
            JsonElement root;

            // Array ise ilk elemani al
            if (parsed.RootElement.ValueKind == JsonValueKind.Array)
                root = parsed.RootElement[0];
            else
                root = parsed.RootElement;

            return ManuelParse(root);
        }
        catch (Exception ex)
        {
            var onizleme = text.Length > 300 ? text[..300] + "..." : text;
            throw new InvalidOperationException($"Parse hatasi: {ex.Message}\nYanit: {onizleme}");
        }
    }

    /// <summary>
    /// Gemini'den gelen esnek JSON'u modele donusturur.
    /// gun, yil, ay string veya int olabilir.
    /// </summary>
    private static PuantajParseResult ManuelParse(JsonElement root)
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

    private static int ParseInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = val.GetString() ?? "";
        // Sayiyi bul
        var match = Regex.Match(s, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static int ParseAy(JsonElement root)
    {
        if (!root.TryGetProperty("ay", out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = (val.GetString() ?? "").Trim().ToUpperInvariant();

        // Sayi mi?
        if (int.TryParse(s, out var num)) return num;

        // Ay adi mi?
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

    private static int ParseGunNumarasi(JsonElement g)
    {
        if (!g.TryGetProperty("gun", out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
        var s = val.GetString() ?? "";
        // Ilk sayiyi al: "1 Ocak 2026 Persembe" -> 1
        var match = Regex.Match(s, @"^\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static string? GetStringProp(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        if (val.ValueKind == JsonValueKind.Null) return null;
        var s = val.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    /// <summary>
    /// "09:0" -> "09:00", "9:00" -> "09:00" gibi normalize eder
    /// </summary>
    private static string? NormalizeSaat(string? saat)
    {
        if (saat == null) return null;
        // "18:0" gibi kisa format
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

    /// <summary>
    /// "R", "Mi", "YI" gibi degerleri normalize eder. Gecersiz degerler null olur.
    /// </summary>
    private static string? NormalizeMiYiR(string? val)
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

    private static string TemizleJson(string text)
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
