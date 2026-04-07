using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PuantajApp.Data;
using PuantajApp.Models;
using PuantajApp.Services;

namespace PuantajApp.ViewModels;

public partial class PuantajSatirViewModel : ViewModelBase
{
    [ObservableProperty] private int _gun;
    [ObservableProperty] private string _gunTipi = "";
    [ObservableProperty] private string? _girisSaati;
    [ObservableProperty] private string? _cikisSaati;
    [ObservableProperty] private string? _izinTipi;
    [ObservableProperty] private string? _fmGiris;
    [ObservableProperty] private string? _fmCikis;
    [ObservableProperty] private decimal? _fmSaat;
    [ObservableProperty] private string? _aciklama;
    [ObservableProperty] private decimal _hesaplananSure;
    [ObservableProperty] private int _yemekHakki;

    public int PersonelId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int? KayitId { get; set; }

    public string GunTipiGoster => GunTipi switch
    {
        "resmi_tatil" => "RT",
        "hafta_sonu" => "HS",
        _ => "Hi"
    };
}

public partial class PuantajViewModel : ViewModelBase
{
    [ObservableProperty] private int _ay = DateTime.Now.Month;
    [ObservableProperty] private int _yil = DateTime.Now.Year;
    [ObservableProperty] private ObservableCollection<Personel> _personeller = [];
    [ObservableProperty] private Personel? _secilenPersonel;
    [ObservableProperty] private ObservableCollection<PuantajSatirViewModel> _satirlar = [];
    [ObservableProperty] private string _durum = "";
    [ObservableProperty] private decimal _toplamSure;
    [ObservableProperty] private int _toplamYemek;
    [ObservableProperty] private decimal _toplamFm;
    [ObservableProperty] private decimal _toplamRtFm;

    public PuantajViewModel()
    {
        _ = PersonelleriYukleAsync();
    }

    public async Task PersonelleriYukleAsync()
    {
        using var db = new AppDbContext();
        var liste = await db.Personeller.OrderBy(p => p.AdSoyad).ToListAsync();
        Personeller = new ObservableCollection<Personel>(liste);
    }

    partial void OnSecilenPersonelChanged(Personel? value)
    {
        if (value != null) _ = KayitlariYukleAsync();
    }

    partial void OnAyChanged(int value) { if (SecilenPersonel != null) _ = KayitlariYukleAsync(); }
    partial void OnYilChanged(int value) { if (SecilenPersonel != null) _ = KayitlariYukleAsync(); }

    [RelayCommand]
    private async Task KayitlariYukleAsync()
    {
        if (SecilenPersonel == null) return;

        using var db = new AppDbContext();
        var kayitlar = await db.PuantajKayitlar
            .Where(k => k.PersonelId == SecilenPersonel.Id && k.Yil == Yil && k.Ay == Ay)
            .ToListAsync();

        var satirListesi = new ObservableCollection<PuantajSatirViewModel>();
        int gunSayisi = DateTime.DaysInMonth(Yil, Ay);

        for (int g = 1; g <= gunSayisi; g++)
        {
            var tarih = new DateTime(Yil, Ay, g);
            var kayit = kayitlar.FirstOrDefault(k => k.Gun == g);

            var satir = new PuantajSatirViewModel
            {
                Gun = g,
                PersonelId = SecilenPersonel.Id,
                Yil = Yil,
                Ay = Ay,
                KayitId = kayit?.Id,
                GunTipi = HesaplamaService.GunTipiBelirle(tarih, kayit?.GunTipi),
                GirisSaati = kayit?.GirisSaati,
                CikisSaati = kayit?.CikisSaati,
                IzinTipi = kayit?.IzinTipi,
                FmGiris = kayit?.FmGiris,
                FmCikis = kayit?.FmCikis,
                FmSaat = kayit?.FmSaat,
                Aciklama = kayit?.Aciklama
            };

            if (satir.GirisSaati != null && satir.CikisSaati != null &&
                TimeSpan.TryParse(satir.GirisSaati, out var girisTe) &&
                TimeSpan.TryParse(satir.CikisSaati, out var cikisTe))
            {
                satir.HesaplananSure = HesaplamaService.HesaplaSure(girisTe, cikisTe);
                satir.YemekHakki = HesaplamaService.YemekHakki(girisTe, cikisTe);
            }

            satirListesi.Add(satir);
        }

        Satirlar = satirListesi;
        HesaplaToplamlar();
    }

    private void HesaplaToplamlar()
    {
        ToplamSure = Satirlar.Sum(s => s.HesaplananSure);
        ToplamYemek = Satirlar.Sum(s => s.YemekHakki);
        ToplamFm = Satirlar.Where(s => s.GunTipi != "resmi_tatil").Sum(s => s.FmSaat ?? 0);
        ToplamRtFm = Satirlar.Where(s => s.GunTipi == "resmi_tatil").Sum(s => s.HesaplananSure);
    }

    [RelayCommand]
    private async Task KaydetAsync()
    {
        if (SecilenPersonel == null) return;

        using var db = new AppDbContext();

        foreach (var satir in Satirlar)
        {
            PuantajKayit? kayit = null;
            if (satir.KayitId.HasValue)
                kayit = await db.PuantajKayitlar.FindAsync(satir.KayitId.Value);

            bool bosKayit = string.IsNullOrEmpty(satir.GirisSaati) &&
                            string.IsNullOrEmpty(satir.IzinTipi) &&
                            !satir.FmSaat.HasValue;

            if (bosKayit)
            {
                if (kayit != null) db.PuantajKayitlar.Remove(kayit);
                continue;
            }

            if (kayit == null)
            {
                kayit = new PuantajKayit
                {
                    PersonelId = satir.PersonelId,
                    Yil = satir.Yil,
                    Ay = satir.Ay,
                    Gun = satir.Gun
                };
                db.PuantajKayitlar.Add(kayit);
            }

            kayit.GunTipi = satir.GunTipi;
            kayit.GirisSaati = satir.GirisSaati;
            kayit.CikisSaati = satir.CikisSaati;
            kayit.IzinTipi = satir.IzinTipi;
            kayit.FmGiris = satir.FmGiris;
            kayit.FmCikis = satir.FmCikis;
            kayit.FmSaat = satir.FmSaat;
            kayit.Aciklama = satir.Aciklama;
        }

        await db.SaveChangesAsync();
        Durum = "Kaydedildi.";
        await KayitlariYukleAsync();
    }
}
