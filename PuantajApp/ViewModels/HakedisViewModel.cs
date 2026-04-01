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

public partial class HakedisPersonelViewModel : ViewModelBase
{
    [ObservableProperty] private string _adSoyad = "";
    [ObservableProperty] private int _hakedisGun;
    [ObservableProperty] private decimal _fmSaat;
    [ObservableProperty] private decimal _fmUcret;
    [ObservableProperty] private decimal _rtFmSaat;
    [ObservableProperty] private decimal _rtFmUcret;
    [ObservableProperty] private decimal _yemekUcret;
    [ObservableProperty] private decimal _fmYemekUcret;
    [ObservableProperty] private decimal _vergiMatrahi;
    [ObservableProperty] private decimal _tssGssFarki;
    [ObservableProperty] private decimal _kantinUcreti;
    [ObservableProperty] private decimal _hakedistenKesilecek;
    [ObservableProperty] private decimal _faturalanacakHakedis;
    public int PersonelId { get; set; }
}

public partial class HakedisViewModel : ViewModelBase
{
    [ObservableProperty] private int _ay = DateTime.Now.Month;
    [ObservableProperty] private int _yil = DateTime.Now.Year;
    [ObservableProperty] private int _isGunu = 21;
    [ObservableProperty] private decimal _yemekBirimUcreti = 330;
    [ObservableProperty] private ObservableCollection<HakedisPersonelViewModel> _satirlar = [];
    [ObservableProperty] private HakedisPersonelViewModel? _secilenSatir;
    [ObservableProperty] private string _durum = "";
    [ObservableProperty] private decimal _toplamHakedis;

    [RelayCommand]
    private async Task HesaplaAsync()
    {
        using var db = new AppDbContext();
        var personeller = await db.Personeller.OrderBy(p => p.AdSoyad).ToListAsync();
        var kayitlar = await db.PuantajKayitlar
            .Where(k => k.Yil == Yil && k.Ay == Ay).ToListAsync();
        var ekVeriler = await db.HakedisEkVeriler
            .Where(e => e.Yil == Yil && e.Ay == Ay).ToListAsync();

        var liste = new ObservableCollection<HakedisPersonelViewModel>();
        decimal toplam = 0;

        foreach (var p in personeller)
        {
            var pKayitlar = kayitlar.Where(k => k.PersonelId == p.Id).ToList();
            var ekVeri = ekVeriler.FirstOrDefault(e => e.PersonelId == p.Id) ?? new HakedisEkVeri();

            int hakedisGun = IsGunu;
            decimal fmSaat = 0, rtFmSaat = 0;
            int yemekSayisi = 0, fmYemekSayisi = 0;

            foreach (var k in pKayitlar)
            {
                if (k.IzinTipi is "mi" or "r") hakedisGun--;

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
                        if ((k.FmSaat ?? 0) > 0 && yemek == 1) fmYemekSayisi++;
                    }
                }
            }

            var fmUcret = HesaplamaService.HesaplaFmUcreti(fmSaat, p.BirimUcreti);
            var rtFmUcret = HesaplamaService.HesaplaRtFmUcreti(rtFmSaat, p.BirimUcreti);
            var yemekUcret = yemekSayisi * YemekBirimUcreti;
            var fmYemekUcret = fmYemekSayisi * YemekBirimUcreti;

            var hakedis = HesaplamaService.HesaplaFaturalanacakHakedis(
                p.BirimUcreti, IsGunu, hakedisGun,
                fmUcret, rtFmUcret, yemekUcret, fmYemekUcret,
                ekVeri.KantinUcreti, ekVeri.VergiMatrahi, ekVeri.TssGssFarki, ekVeri.HakedistenKesilecek);

            toplam += hakedis;

            liste.Add(new HakedisPersonelViewModel
            {
                PersonelId = p.Id,
                AdSoyad = p.AdSoyad,
                HakedisGun = hakedisGun,
                FmSaat = fmSaat,
                FmUcret = fmUcret,
                RtFmSaat = rtFmSaat,
                RtFmUcret = rtFmUcret,
                YemekUcret = yemekUcret,
                FmYemekUcret = fmYemekUcret,
                VergiMatrahi = ekVeri.VergiMatrahi,
                TssGssFarki = ekVeri.TssGssFarki,
                KantinUcreti = ekVeri.KantinUcreti,
                HakedistenKesilecek = ekVeri.HakedistenKesilecek,
                FaturalanacakHakedis = hakedis
            });
        }

        Satirlar = liste;
        ToplamHakedis = toplam;
        Durum = $"Hesaplandi. {personeller.Count} personel.";
    }

    [RelayCommand]
    private async Task ManuelKaydetAsync()
    {
        if (SecilenSatir == null) return;

        using var db = new AppDbContext();
        var ekVeri = await db.HakedisEkVeriler
            .FirstOrDefaultAsync(e => e.PersonelId == SecilenSatir.PersonelId &&
                                      e.Yil == Yil && e.Ay == Ay);

        if (ekVeri == null)
        {
            ekVeri = new HakedisEkVeri
            {
                PersonelId = SecilenSatir.PersonelId,
                Yil = Yil,
                Ay = Ay
            };
            db.HakedisEkVeriler.Add(ekVeri);
        }

        ekVeri.VergiMatrahi = SecilenSatir.VergiMatrahi;
        ekVeri.TssGssFarki = SecilenSatir.TssGssFarki;
        ekVeri.KantinUcreti = SecilenSatir.KantinUcreti;
        ekVeri.HakedistenKesilecek = SecilenSatir.HakedistenKesilecek;

        await db.SaveChangesAsync();
        Durum = "Manuel veriler kaydedildi.";
        await HesaplaAsync();
    }
}
