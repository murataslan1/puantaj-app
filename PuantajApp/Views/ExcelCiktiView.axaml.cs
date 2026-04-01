using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PuantajApp.ViewModels;

namespace PuantajApp.Views;

public partial class ExcelCiktiView : UserControl
{
    public ExcelCiktiView()
    {
        InitializeComponent();

        var btn = this.FindControl<Button>("BtnKayitYoluSec");
        if (btn != null)
            btn.Click += BtnKayitYoluSec_Click;
    }

    private async void BtnKayitYoluSec_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var klasorler = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Kayit Klasoru Sec",
            AllowMultiple = false
        });

        if (klasorler.Count > 0 && DataContext is ExcelCiktiViewModel vm)
        {
            var yol = klasorler[0].TryGetLocalPath();
            if (yol != null) vm.KayitYolu = yol;
        }
    }
}
