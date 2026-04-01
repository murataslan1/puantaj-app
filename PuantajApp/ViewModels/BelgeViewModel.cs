using System;
using System.Collections.ObjectModel;
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

public partial class BelgeViewModel : ViewModelBase
{
    [ObservableProperty] private int _ay = DateTime.Now.Month;
    [ObservableProperty] private int _yil = DateTime.Now.Year;
    [ObservableProperty] private ObservableCollection<Personel> _personeller = [];
    [ObservableProperty] private Personel? _secilenPersonel;
    [ObservableProperty] private ObservableCollection<Belge> _belgeler = [];
    [ObservableProperty] private string _belgeTipi = "devam_takip";
    [ObservableProperty] private string _durum = "";

    public BelgeViewModel()
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
        if (value != null) _ = BelgeleriYukleAsync();
    }

    public async Task BelgeleriYukleAsync()
    {
        if (SecilenPersonel == null) return;
        using var db = new AppDbContext();
        var liste = await db.Belgeler
            .Where(b => b.PersonelId == SecilenPersonel.Id && b.Yil == Yil && b.Ay == Ay)
            .OrderByDescending(b => b.YuklenmeTarihi)
            .ToListAsync();
        Belgeler = new ObservableCollection<Belge>(liste);
    }

    public async Task BelgeYukleAsync(string dosyaYolu)
    {
        if (SecilenPersonel == null)
        {
            Durum = "Personel secilmedi.";
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var servis = new BelgeService(db);
            await servis.BelgeKaydetAsync(SecilenPersonel.Id, Yil, Ay, BelgeTipi, dosyaYolu);
            Durum = "Belge yuklendi.";
            await BelgeleriYukleAsync();
        }
        catch (Exception ex)
        {
            Durum = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BelgeAcAsync(Belge? belge)
    {
        if (belge == null) return;
        try
        {
            using var db = new AppDbContext();
            var servis = new BelgeService(db);
            await servis.BelgeAcAsync(belge.Id);
        }
        catch (Exception ex)
        {
            Durum = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BelgeSilAsync(Belge? belge)
    {
        if (belge == null) return;
        using var db = new AppDbContext();
        var b = await db.Belgeler.FindAsync(belge.Id);
        if (b != null)
        {
            db.Belgeler.Remove(b);
            await db.SaveChangesAsync();
        }
        Durum = "Belge silindi.";
        await BelgeleriYukleAsync();
    }
}
