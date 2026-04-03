using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PuantajApp.Models;

namespace PuantajApp.Services;

/// <summary>
/// Docling (Python) ile PDF'i Markdown'a cevirir, sonra Gemini ile JSON'a parse eder.
/// Docling yerel OCR + tablo tanima yapar, Gemini sadece yapisal metin uzerinden calisir.
/// </summary>
public class DoclingService
{
    private readonly GeminiService _gemini;

    private const string DOCLING_PROMPT = """
        Asagida bir TÜRKSAT Devam Takip Formu'nun Markdown ciktisi vardir.
        Bu formdan personel bilgilerini ve gunluk devam kayitlarini cikar.

        EL YAZISI OKUMA NOTU: Metin OCR ile okunmustur, kucuk hatalar olabilir.
        Belirsiz rakamlari baglama gore degerlendir (giris 08-10 arasi, cikis 17-19 arasi beklenir).

        VERI KURALLARI:
        - yil: sayi olarak (ornek: 2026)
        - ay: sayi olarak (1-12)
        - gun: SADECE gun numarasi (1-31)
        - giris/cikis: HH:mm formatinda, bos ise null
        - mi_yi_r: sadece "mi", "yi" veya "r", bos ise null
          Sadece izin/rapor kolonunda acikca yazilmissa doldur.
        - fazla_mesai: saat araligi (ornek: "19:03-23:33"), yoksa null
        - aciklama: metin veya null
        - Sadece veri olan gunleri dahil et

        TEK bir JSON objesi don:
        {
          "ad_soyad": "ADI SOYADI",
          "unvan": "Unvan",
          "birim": "Birim",
          "yil": 2026,
          "ay": 1,
          "gunler": [
            {"gun": 1, "giris": "09:00", "cikis": "18:00", "mi_yi_r": null, "fazla_mesai": null, "aciklama": null}
          ]
        }
        """;

    public DoclingService(GeminiService gemini)
    {
        _gemini = gemini;
    }

    /// <summary>
    /// Docling kurulu ve calisir durumda mi kontrol eder.
    /// </summary>
    public static async Task<bool> IsAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "-c \"import docling; print('ok')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0 && output.Trim() == "ok";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 1) Docling ile PDF -> Markdown
    /// 2) Gemini ile Markdown -> PuantajParseResult JSON
    /// </summary>
    public async Task<PuantajParseResult?> ParsePdfAsync(byte[] pdfBytes, string dosyaAdi)
    {
        if (!_gemini.HasApiKey)
            throw new InvalidOperationException("Docling+Gemini icin Gemini API key gerekli.");

        // Gecici PDF dosyasina yaz
        var tempPdf = Path.Combine(Path.GetTempPath(), $"docling_{Guid.NewGuid()}.pdf");
        try
        {
            await File.WriteAllBytesAsync(tempPdf, pdfBytes);

            // Docling scripti calistir
            var markdown = await RunDoclingAsync(tempPdf);

            if (string.IsNullOrWhiteSpace(markdown))
                throw new InvalidOperationException("Docling bos cikti uretti.");

            // Gemini'ye markdown metin olarak gonder
            return await ParseWithGeminiAsync(markdown);
        }
        finally
        {
            if (File.Exists(tempPdf))
                File.Delete(tempPdf);
        }
    }

    private static async Task<string> RunDoclingAsync(string pdfPath)
    {
        // Script yolunu bul
        var scriptPath = FindDoclingScript();

        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{scriptPath}\" \"{pdfPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["OMP_NUM_THREADS"] = "4";

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("python3 baslatilamadi.");

        var output = await proc.StandardOutput.ReadToEndAsync();
        var error = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"Docling hatasi (exit {proc.ExitCode}): {error[..Math.Min(300, error.Length)]}");

        return output;
    }

    private static string FindDoclingScript()
    {
        // 1. Uygulama dizinindeki Scripts klasoru
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var scriptInApp = Path.Combine(appDir, "Scripts", "pdf_to_markdown.py");
        if (File.Exists(scriptInApp)) return scriptInApp;

        // 2. Proje dizinindeki Scripts klasoru (development)
        var devScript = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "pdf_to_markdown.py");
        if (File.Exists(devScript)) return devScript;

        // 3. Kaynak kodun oldugu yer
        var srcScript = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "Scripts", "pdf_to_markdown.py"));
        if (File.Exists(srcScript)) return srcScript;

        throw new FileNotFoundException("pdf_to_markdown.py bulunamadi.");
    }

    private async Task<PuantajParseResult?> ParseWithGeminiAsync(string markdown)
    {
        // Gemini'ye metin olarak gonder (PDF/gorsel degil, sadece text)
        var fullPrompt = DOCLING_PROMPT + "\n\n--- FORM ICERIGI ---\n\n" + markdown;

        // GeminiService'in text-only endpoint'ini kullan
        return await _gemini.ParseTextAsync(fullPrompt);
    }
}
