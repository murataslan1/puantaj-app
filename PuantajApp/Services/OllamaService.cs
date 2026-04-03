using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PDFtoImage;
using PuantajApp.Models;
using SkiaSharp;

namespace PuantajApp.Services;

public class OllamaService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };
    private const string BASE_URL = "http://localhost:11434";
    private const string MODEL = "llama3.2-vision";

    /// <summary>
    /// Ollama'nin kurulu ve calisiyor olup olmadigini kontrol eder.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{BASE_URL}/api/tags");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            // Model yuklu mu kontrol et
            return json.Contains("llama3.2-vision", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// PDF'i yerel Ollama vision modeli ile parse eder.
    /// PDF once image'a cevrilir cunku Ollama vision modelleri PDF desteklemez.
    /// </summary>
    public async Task<PuantajParseResult?> ParsePdfAsync(byte[] pdfBytes, string dosyaAdi)
    {
        // PDF'in ilk sayfasini PNG'ye cevir
        var imageBase64 = ConvertPdfToImageBase64(pdfBytes);

        var requestBody = new
        {
            model = MODEL,
            prompt = AiParseHelper.PROMPT + "\n\nYANITINI SADECE JSON OLARAK VER, baska bir sey yazma.",
            images = new[] { imageBase64 },
            stream = false,
            options = new
            {
                temperature = 0.1,
                num_predict = 4096
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{BASE_URL}/api/generate", content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ollama Hatasi {(int)response.StatusCode}: {responseText[..Math.Min(200, responseText.Length)]}");

        // Ollama yanit parse
        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement.GetProperty("response").GetString() ?? "";

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Ollama bos yanit dondu.");

        try
        {
            return AiParseHelper.ParseJsonResponse(text);
        }
        catch (Exception ex)
        {
            var onizleme = text.Length > 300 ? text[..300] + "..." : text;
            throw new InvalidOperationException($"Parse hatasi: {ex.Message}\nYanit: {onizleme}");
        }
    }

    /// <summary>
    /// PDF'in ilk sayfasini PNG formatinda base64'e cevir.
    /// </summary>
    private static string ConvertPdfToImageBase64(byte[] pdfBytes)
    {
        // PDF'in ilk sayfasini SKBitmap olarak al (byte[] overload)
        var bitmap = PDFtoImage.Conversion.ToImage(pdfBytes, 0, null, new PDFtoImage.RenderOptions(Dpi: 200));

        // PNG olarak encode et
        using var imageStream = new MemoryStream();
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        data.SaveTo(imageStream);

        return Convert.ToBase64String(imageStream.ToArray());
    }
}
