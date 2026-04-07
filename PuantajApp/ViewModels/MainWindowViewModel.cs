using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PuantajApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _secilenSekme = 0;

    public PersonelViewModel PersonelVM { get; } = new();
    public PdfAktarViewModel PdfAktarVM { get; } = new();
    public PuantajViewModel PuantajVM { get; } = new();
    public HakedisViewModel HakedisVM { get; } = new();
    public ExcelCiktiViewModel ExcelCiktiVM { get; } = new();
    public BelgeViewModel BelgeVM { get; } = new();

    // Sekme değiştiğinde önceki sekmedeki Ay/Yıl değerini diğer tüm sekmelere yay
    partial void OnSecilenSekmeChanged(int oldValue, int newValue)
    {
        var kaynak = GetAyYil(oldValue);
        if (kaynak == null) return;
        var (ay, yil) = kaynak.Value;
        SetAyYil(ay, yil);
    }

    private (int ay, int yil)? GetAyYil(int sekme) => sekme switch
    {
        1 => (PdfAktarVM.Ay, PdfAktarVM.Yil),
        2 => (PuantajVM.Ay, PuantajVM.Yil),
        3 => (HakedisVM.Ay, HakedisVM.Yil),
        4 => (ExcelCiktiVM.Ay, ExcelCiktiVM.Yil),
        5 => (BelgeVM.Ay, BelgeVM.Yil),
        _ => null
    };

    private void SetAyYil(int ay, int yil)
    {
        PdfAktarVM.Ay = ay;   PdfAktarVM.Yil = yil;
        PuantajVM.Ay = ay;    PuantajVM.Yil = yil;
        HakedisVM.Ay = ay;    HakedisVM.Yil = yil;
        ExcelCiktiVM.Ay = ay;  ExcelCiktiVM.Yil = yil;
        BelgeVM.Ay = ay;       BelgeVM.Yil = yil;
    }
}
