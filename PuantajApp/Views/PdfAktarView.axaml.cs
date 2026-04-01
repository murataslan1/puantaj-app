using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PuantajApp.ViewModels;

namespace PuantajApp.Views;

public partial class PdfAktarView : UserControl
{
    public PdfAktarView()
    {
        InitializeComponent();

        var btn = this.FindControl<Button>("BtnPdfSec");
        if (btn != null)
            btn.Click += BtnPdfSec_Click;
    }

    private async void BtnPdfSec_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var dosyalar = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "PDF Dosyalari Sec",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }]
        });

        if (dosyalar.Count > 0 && DataContext is PdfAktarViewModel vm)
        {
            var yollar = dosyalar
                .Select(f => f.TryGetLocalPath())
                .Where(y => y != null)
                .Select(y => y!)
                .ToArray();
            vm.PdfEkle(yollar);
        }
    }
}
