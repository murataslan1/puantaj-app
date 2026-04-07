using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PuantajApp.Data;
using PuantajApp.Models;
using PuantajApp.Services;

namespace PuantajApp.ViewModels;

public partial class PdfOgesiViewModel : ViewModelBase
{
    [ObservableProperty] private string _dosyaAdi = "";
    [ObservableProperty] private string _durum = "Bekliyor";
    [ObservableProperty] private string _eslesenPersonel = "";
    [ObservableProperty] private bool _secili = true;
    [ObservableProperty] private PuantajParseResult? _parseResult;
    public byte[] DosyaBytes { get; set; } = [];
}

public partial class PdfAktarViewModel : ViewModelBase
{
    [ObservableProperty] private int _ay = DateTime.Now.Month;
    [ObservableProperty] private int _yil = DateTime.Now.Year;
    [ObservableProperty] private string _geminiApiKey = "";
    [ObservableProperty] private ObservableCollection<PdfOgesiViewModel> _pdfler = [];
    [ObservableProperty] private string _durum = "";
    [ObservableProperty] private int _basarili = 0;
    [ObservableProperty] private int _hata = 0;
    [ObservableProperty] private bool _isleniyor = false;
    [ObservableProperty] private PdfOgesiViewModel? _secilenPdf;
    [ObservableProperty] private string _aiBackend = "Kontrol ediliyor...";
    [ObservableProperty] private bool _ollamaAktif = false;

    private readonly GeminiService _gemini = new();
    private readonly OllamaService _ollama = new();
    private DoclingService? _docling;
    [ObservableProperty] private bool _doclingAktif = false;

    public PdfAktarViewModel()
    {
        // .env'den API key oku
        var key = EnvService.Get("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            GeminiApiKey = key;
            _gemini.SetApiKey(key);
        }

        // Ollama ve Docling kontrolu (arka planda)
        _ = CheckBackendsAsync();
    }

    private async Task CheckBackendsAsync()
    {
        try
        {
            // Ollama kontrol
            OllamaAktif = await _ollama.IsAvailableAsync();

            // Docling kontrol
            DoclingAktif = await DoclingService.IsAvailableAsync();
            if (DoclingAktif)
                _docling = new DoclingService(_gemini);

            // Backend durumu goster
            if (DoclingAktif && _gemini.HasApiKey)
                AiBackend = "Docling+Gemini (OCR+AI)";
            else if (OllamaAktif)
                AiBackend = "Ollama (Yerel AI - Limitsiz)";
            else if (_gemini.HasApiKey)
                AiBackend = "Gemini API (Bulut)";
            else
                AiBackend = "API Key gerekli";
        }
        catch
        {
            OllamaAktif = false;
            DoclingAktif = false;
            AiBackend = _gemini.HasApiKey ? "Gemini API (Bulut)" : "API Key gerekli";
        }
    }

    partial void OnGeminiApiKeyChanged(string value)
    {
        _gemini.SetApiKey(value);
        if (!string.IsNullOrWhiteSpace(value))
        {
            EnvService.Set("GEMINI_API_KEY", value);
            if (DoclingAktif)
            {
                _docling = new DoclingService(_gemini);
                AiBackend = "Docling+Gemini (OCR+AI)";
            }
            else if (!OllamaAktif)
                AiBackend = "Gemini API (Bulut)";
        }
    }

    public void PdfEkle(string[] dosyaYollari)
    {
        foreach (var yol in dosyaYollari)
        {
            if (!File.Exists(yol)) continue;
            var ogesi = new PdfOgesiViewModel
            {
                DosyaAdi = Path.GetFileName(yol),
                DosyaBytes = File.ReadAllBytes(yol),
                Durum = "Hazir"
            };
            Pdfler.Add(ogesi);
        }
        Durum = $"{Pdfler.Count} PDF yuklendi.";
    }

    [RelayCommand]
    private async Task TumunuParseEtAsync()
    {
        // Backend hazir mi kontrol et
        if (!DoclingAktif && !OllamaAktif && !_gemini.HasApiKey)
        {
            Durum = "Docling/Ollama kurulu degil ve Gemini API key girilmedi!";
            return;
        }

        var seciliPdfler = Pdfler.Where(p => p.Secili && p.Durum != "Onaylandi").ToList();
        if (!seciliPdfler.Any())
        {
            Durum = "Secili PDF yok.";
            return;
        }

        Isleniyor = true;
        Basarili = Hata = 0;
        var backend = DoclingAktif && _gemini.HasApiKey ? "Docling+Gemini"
                    : OllamaAktif ? "Ollama"
                    : "Gemini";
        Durum = $"0/{seciliPdfler.Count} isleniyor ({backend})...";

        using var db = new AppDbContext();
        var personeller = await db.Personeller.ToListAsync();

        for (int i = 0; i < seciliPdfler.Count; i++)
        {
            var pdf = seciliPdfler[i];
            pdf.Durum = $"Isleniyor ({backend})...";

            try
            {
                PuantajParseResult? result;

                if (DoclingAktif && _docling != null && _gemini.HasApiKey)
                    result = await _docling.ParsePdfAsync(pdf.DosyaBytes, pdf.DosyaAdi);
                else if (OllamaAktif)
                    result = await _ollama.ParsePdfAsync(pdf.DosyaBytes, pdf.DosyaAdi);
                else
                    result = await _gemini.ParsePdfAsync(pdf.DosyaBytes, pdf.DosyaAdi);

                if (result != null)
                {
                    pdf.ParseResult = result;
                    // PDF'den gelen ay/yıl bilgisini sekmeye yansıt
                    if (result.Ay > 0) Ay = result.Ay;
                    if (result.Yil > 0) Yil = result.Yil;
                    var eslesen = BulEnEslesenPersonel(result.AdSoyad, personeller);
                    pdf.EslesenPersonel = eslesen?.AdSoyad ?? "Eslesmedi";
                    pdf.Durum = eslesen != null ? "Eslesti" : "Eslesmedi";
                    Basarili++;
                }
                else
                {
                    pdf.Durum = "Parse Hatasi";
                    Hata++;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message.Contains("429")
                    ? "API kota limiti doldu. Birkac dakika bekleyip tekrar deneyin veya yeni API key alin."
                    : ex.Message[..Math.Min(120, ex.Message.Length)];
                pdf.Durum = $"Hata: {msg}";
                Durum = $"HATA ({pdf.DosyaAdi}): {msg}";
                Hata++;
            }

            Durum = $"{i + 1}/{seciliPdfler.Count} islendi ({backend}). Basarili: {Basarili}, Hata: {Hata}";

            // Docling+Gemini ve Gemini icin rate limiting
            if (!OllamaAktif || (DoclingAktif && _gemini.HasApiKey))
                await Task.Delay(4500);
        }

        Isleniyor = false;
        Durum = $"Tamamlandi ({backend}). Basarili: {Basarili}, Hata: {Hata}";
    }

    [RelayCommand]
    private async Task OnaylaVeKaydetAsync(PdfOgesiViewModel? pdf)
    {
        pdf ??= SecilenPdf;
        if (pdf?.ParseResult == null) return;

        using var db = new AppDbContext();
        var personeller = await db.Personeller.ToListAsync();
        var eslesen = BulEnEslesenPersonel(pdf.ParseResult.AdSoyad, personeller);

        if (eslesen == null)
        {
            pdf.Durum = "Personel bulunamadi - elle eslestirilmeli";
            return;
        }

        // Parse result'taki ay/yıl bilgisini kullan (PDF'den gelen gerçek değerler)
        var kayitYil = pdf.ParseResult.Yil > 0 ? pdf.ParseResult.Yil : Yil;
        var kayitAy = pdf.ParseResult.Ay > 0 ? pdf.ParseResult.Ay : Ay;

        var mevcutKayitlar = db.PuantajKayitlar
            .Where(k => k.PersonelId == eslesen.Id && k.Yil == kayitYil && k.Ay == kayitAy);
        db.PuantajKayitlar.RemoveRange(mevcutKayitlar);

        int gunSayisi = DateTime.DaysInMonth(kayitYil, kayitAy);
        foreach (var gun in pdf.ParseResult.Gunler)
        {
            if (gun.Gun < 1 || gun.Gun > gunSayisi) continue;

            var tarih = new DateTime(kayitYil, kayitAy, gun.Gun);
            var gunTipi = HesaplamaService.GunTipiBelirle(tarih);

            string? fmGiris = null, fmCikis = null;
            if (!string.IsNullOrEmpty(gun.FazlaMesai))
            {
                var parts = gun.FazlaMesai.Split('-');
                if (parts.Length == 2) { fmGiris = parts[0].Trim(); fmCikis = parts[1].Trim(); }
            }

            db.PuantajKayitlar.Add(new PuantajKayit
            {
                PersonelId = eslesen.Id,
                Yil = kayitYil,
                Ay = kayitAy,
                Gun = gun.Gun,
                GunTipi = gunTipi,
                GirisSaati = gun.Giris,
                CikisSaati = gun.Cikis,
                IzinTipi = gun.MiYiR,
                FmGiris = fmGiris,
                FmCikis = fmCikis,
                FmSaat = !string.IsNullOrEmpty(fmGiris) && !string.IsNullOrEmpty(fmCikis)
                    ? HesaplamaService.HesaplaFmSaatAralik(gun.FazlaMesai) : null,
                Aciklama = gun.Aciklama
            });
        }

        await db.SaveChangesAsync();
        pdf.Durum = "Onaylandi";
        Durum = $"{eslesen.AdSoyad} kayitlari kaydedildi. ({kayitAy}/{kayitYil})";
    }

    private static Personel? BulEnEslesenPersonel(string adSoyad, System.Collections.Generic.List<Personel> personeller)
    {
        if (string.IsNullOrWhiteSpace(adSoyad)) return null;
        var aranan = adSoyad.Trim().ToUpperInvariant();

        // 1. Tam eşleşme
        var tam = personeller.FirstOrDefault(p =>
            p.AdSoyad.ToUpperInvariant() == aranan);
        if (tam != null) return tam;

        // 2. Kelime bazlı eşleşme (mevcut)
        var arananKelimeler = aranan.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kelimeEslesen = personeller
            .Select(p => new
            {
                Personel = p,
                Skor = arananKelimeler.Count(k => p.AdSoyad.ToUpperInvariant().Contains(k))
            })
            .Where(x => x.Skor > 0)
            .OrderByDescending(x => x.Skor)
            .FirstOrDefault()?.Personel;
        if (kelimeEslesen != null) return kelimeEslesen;

        // 3. Levenshtein mesafesi ile bulanık eşleşme (el yazısı hataları için)
        var enIyi = personeller
            .Select(p => new
            {
                Personel = p,
                Mesafe = LevenshteinMesafe(aranan, p.AdSoyad.ToUpperInvariant())
            })
            .OrderBy(x => x.Mesafe)
            .FirstOrDefault();

        // İsim uzunluğunun %40'ından az hata varsa eşleştir
        if (enIyi != null && enIyi.Mesafe <= Math.Max(aranan.Length, enIyi.Personel.AdSoyad.Length) * 0.4)
            return enIyi.Personel;

        return null;
    }

    private static int LevenshteinMesafe(string s, string t)
    {
        int n = s.Length, m = t.Length;
        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
