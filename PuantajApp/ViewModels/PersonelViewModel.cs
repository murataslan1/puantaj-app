using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PuantajApp.Data;
using PuantajApp.Models;
using PuantajApp.Services;

namespace PuantajApp.ViewModels;

public partial class PersonelViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Personel> _personeller = [];

    [ObservableProperty]
    private Personel? _secilenPersonel;

    // Form alanlari
    [ObservableProperty] private string _adSoyad = "";
    [ObservableProperty] private string _tcNo = "";
    [ObservableProperty] private string _unvan = "";
    [ObservableProperty] private string _birim = "";
    [ObservableProperty] private string _baslamaTarihi = "";
    [ObservableProperty] private string _birimUcreti = "";
    [ObservableProperty] private string _yillikIzinHakki = "";
    [ObservableProperty] private bool _gececi = false;
    [ObservableProperty] private string _durum = "";

    private int? _duzenlenenId = null;

    public PersonelViewModel()
    {
        _ = YukleAsync();
    }

    public async Task YukleAsync()
    {
        using var db = new AppDbContext();
        var liste = await db.Personeller.OrderBy(p => p.AdSoyad).ToListAsync();
        Personeller = new ObservableCollection<Personel>(liste);
    }

    partial void OnSecilenPersonelChanged(Personel? value)
    {
        if (value == null) return;
        FormDoldur(value);
    }

    private void FormDoldur(Personel p)
    {
        _duzenlenenId = p.Id;
        AdSoyad = p.AdSoyad;
        TcNo = p.TC;
        Unvan = p.Unvan;
        Birim = p.Birim;
        BaslamaTarihi = p.BaslamaTarihi?.ToString("dd/MM/yyyy") ?? "";
        BirimUcreti = p.BirimUcreti.ToString("F2");
        YillikIzinHakki = p.YillikIzinHakki.ToString("F0");
        Gececi = p.Gececi == 1;
    }

    [RelayCommand]
    private void Temizle()
    {
        _duzenlenenId = null;
        SecilenPersonel = null;
        AdSoyad = TcNo = Unvan = Birim = BaslamaTarihi = BirimUcreti = YillikIzinHakki = "";
        Gececi = false;
    }

    [RelayCommand]
    private async Task KaydetAsync()
    {
        if (string.IsNullOrWhiteSpace(AdSoyad))
        {
            Durum = "Ad Soyad zorunludur.";
            return;
        }

        using var db = new AppDbContext();

        Personel p;
        if (_duzenlenenId.HasValue)
        {
            p = await db.Personeller.FindAsync(_duzenlenenId.Value) ?? new Personel();
        }
        else
        {
            p = new Personel();
            db.Personeller.Add(p);
        }

        p.AdSoyad = AdSoyad.Trim().ToUpper();
        p.TC = TcNo.Trim();
        p.Unvan = Unvan.Trim();
        p.Birim = Birim.Trim();
        p.Gececi = Gececi ? 1 : 0;

        if (DateTime.TryParse(BaslamaTarihi, out var baslama))
            p.BaslamaTarihi = baslama;
        if (decimal.TryParse(BirimUcreti, out var ucret))
            p.BirimUcreti = ucret;
        if (decimal.TryParse(YillikIzinHakki, out var izin))
            p.YillikIzinHakki = izin;

        await db.SaveChangesAsync();
        Durum = "Kaydedildi.";
        Temizle();
        await YukleAsync();
    }

    [RelayCommand]
    private async Task SilAsync()
    {
        if (SecilenPersonel == null) return;
        using var db = new AppDbContext();
        var p = await db.Personeller.FindAsync(SecilenPersonel.Id);
        if (p != null)
        {
            db.Personeller.Remove(p);
            await db.SaveChangesAsync();
        }
        Durum = "Silindi.";
        Temizle();
        await YukleAsync();
    }

    [RelayCommand]
    private async Task ExcelImportAsync()
    {
        // Dosya secimi icin TopLevel kullanmak gerekir - View'dan tetiklenir
        // Burada sadece import islemi yapilir, dosya yolu parametre olarak gelir
        Durum = "Excel import icin dosya secin...";
        await Task.CompletedTask;
    }

    public async Task ExcelImportDosyaAsync(string dosyaYolu)
    {
        try
        {
            var liste = ExcelImportService.ImportPersonel(dosyaYolu);
            using var db = new AppDbContext();

            int eklenen = 0;
            foreach (var p in liste)
            {
                var mevcut = db.Personeller.FirstOrDefault(x => x.TC == p.TC && !string.IsNullOrEmpty(p.TC) && p.TC != "");
                if (mevcut == null)
                {
                    db.Personeller.Add(p);
                    eklenen++;
                }
            }
            await db.SaveChangesAsync();
            Durum = $"{eklenen} personel import edildi.";
            await YukleAsync();
        }
        catch (Exception ex)
        {
            Durum = $"Hata: {ex.Message}";
        }
    }
}
