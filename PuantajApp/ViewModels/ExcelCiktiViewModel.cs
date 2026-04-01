using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PuantajApp.Data;
using PuantajApp.Models;
using PuantajApp.Services;

namespace PuantajApp.ViewModels;

public partial class ExcelCiktiViewModel : ViewModelBase
{
    [ObservableProperty] private int _ay = DateTime.Now.Month;
    [ObservableProperty] private int _yil = DateTime.Now.Year;
    [ObservableProperty] private int _isGunu = 21;
    [ObservableProperty] private decimal _yemekBirimUcreti = 330;
    [ObservableProperty] private string _kayitYolu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    [ObservableProperty] private string _durum = "";

    [RelayCommand]
    private async Task PuantajExcelOlusturAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var personeller = await db.Personeller.OrderBy(p => p.AdSoyad).ToListAsync();
            var kayitlar = await db.PuantajKayitlar
                .Where(k => k.Yil == Yil && k.Ay == Ay).ToListAsync();

            ExcelExportService.OlusturPuantajExcel(KayitYolu, Yil, Ay, personeller, kayitlar);
            Durum = "Puantaj Excel olusturuldu.";
        }
        catch (Exception ex)
        {
            Durum = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task HakedisExcelOlusturAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var personeller = await db.Personeller.OrderBy(p => p.AdSoyad).ToListAsync();
            var kayitlar = await db.PuantajKayitlar
                .Where(k => k.Yil == Yil && k.Ay == Ay).ToListAsync();
            var ekVeriler = await db.HakedisEkVeriler
                .Where(e => e.Yil == Yil && e.Ay == Ay).ToListAsync();

            var parametreler = new HakedisParametre
            {
                Yil = Yil,
                Ay = Ay,
                IsGunu = IsGunu,
                YemekBirimUcreti = YemekBirimUcreti
            };

            ExcelExportService.OlusturHakedisExcel(KayitYolu, Yil, Ay, personeller, kayitlar, ekVeriler, parametreler);
            Durum = "Hakedis Excel olusturuldu.";
        }
        catch (Exception ex)
        {
            Durum = $"Hata: {ex.Message}";
        }
    }
}
