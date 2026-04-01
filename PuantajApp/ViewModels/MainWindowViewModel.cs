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
}
