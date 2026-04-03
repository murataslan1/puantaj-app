using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PuantajApp.Models;

namespace PuantajApp.Services;

public class GeminiService
{
    private readonly HttpClient _http = new();
    private string? _apiKey;

    private const string MODEL = "gemini-2.5-flash";

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
                        new { text = AiParseHelper.PROMPT },
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

        return await SendRequestAsync(requestBody);
    }

    /// <summary>
    /// Sadece metin (prompt) gonderir. Docling ciktisi icin kullanilir.
    /// </summary>
    public async Task<PuantajParseResult?> ParseTextAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API key girilmedi.");

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        return await SendRequestAsync(requestBody);
    }

    private async Task<PuantajParseResult?> SendRequestAsync(object requestBody)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={_apiKey}";
        var json = JsonSerializer.Serialize(requestBody);

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
}
