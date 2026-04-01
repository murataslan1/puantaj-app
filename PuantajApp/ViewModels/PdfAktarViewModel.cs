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

    public PdfAktarViewModel()
    {
        // .env'den API key oku
        var key = EnvService.Get("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            GeminiApiKey = key;
            _gemini.SetApiKey(key);
        }

        // Ollama kontrolu (arka planda)
        _ = CheckOllamaAsync();
    }

    private async Task CheckOllamaAsync()
    {
        try
        {
            OllamaAktif = await _ollama.IsAvailableAsync();
            AiBackend = OllamaAktif
                ? "Ollama (Yerel AI - Limitsiz)"
                : _gemini.HasApiKey ? "Gemini API (Bulut)" : "API Key gerekli";
        }
        catch
        {
            OllamaAktif = false;
            AiBackend = _gemini.HasApiKey ? "Gemini API (Bulut)" : "API Key gerekli";
        }
    }

    partial void OnGeminiApiKeyChanged(string value)
    {
        _gemini.SetApiKey(value);
        if (!string.IsNullOrWhiteSpace(value))
        {
            EnvService.Set("GEMINI_API_KEY", value);
            if (!OllamaAktif)
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
        // Ollama veya Gemini hazir mi kontrol et
        if (!OllamaAktif && !_gemini.HasApiKey)
        {
            Durum = "Ollama kurulu degil ve Gemini API key girilmedi!";
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
        var backend = OllamaAktif ? "Ollama" : "Gemini";
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

                if (OllamaAktif)
                    result = await _ollama.ParsePdfAsync(pdf.DosyaBytes, pdf.DosyaAdi);
                else
                    result = await _gemini.ParsePdfAsync(pdf.DosyaBytes, pdf.DosyaAdi);

                if (result != null)
                {
                    pdf.ParseResult = result;
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

            // Gemini icin rate limiting, Ollama icin gerekmez
            if (!OllamaAktif)
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

        var mevcutKayitlar = db.PuantajKayitlar
            .Where(k => k.PersonelId == eslesen.Id && k.Yil == Yil && k.Ay == Ay);
        db.PuantajKayitlar.RemoveRange(mevcutKayitlar);

        int gunSayisi = DateTime.DaysInMonth(Yil, Ay);
        foreach (var gun in pdf.ParseResult.Gunler)
        {
            if (gun.Gun < 1 || gun.Gun > gunSayisi) continue;

            var tarih = new DateTime(Yil, Ay, gun.Gun);
            var gunTipi = tarih.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? "hafta_sonu" : "hafta_ici";

            string? fmGiris = null, fmCikis = null;
            if (!string.IsNullOrEmpty(gun.FazlaMesai))
            {
                var parts = gun.FazlaMesai.Split('-');
                if (parts.Length == 2) { fmGiris = parts[0].Trim(); fmCikis = parts[1].Trim(); }
            }

            db.PuantajKayitlar.Add(new PuantajKayit
            {
                PersonelId = eslesen.Id,
                Yil = Yil,
                Ay = Ay,
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
        Durum = $"{eslesen.AdSoyad} kayitlari kaydedildi.";
    }

    private static Personel? BulEnEslesenPersonel(string adSoyad, System.Collections.Generic.List<Personel> personeller)
    {
        if (string.IsNullOrWhiteSpace(adSoyad)) return null;
        var aranan = adSoyad.Trim().ToUpperInvariant();

        var tam = personeller.FirstOrDefault(p =>
            p.AdSoyad.ToUpperInvariant() == aranan);
        if (tam != null) return tam;

        var arananKelimeler = aranan.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return personeller
            .Select(p => new
            {
                Personel = p,
                Skor = arananKelimeler.Count(k => p.AdSoyad.ToUpperInvariant().Contains(k))
            })
            .Where(x => x.Skor > 0)
            .OrderByDescending(x => x.Skor)
            .FirstOrDefault()?.Personel;
    }
}
