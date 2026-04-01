using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PuantajApp.ViewModels;

namespace PuantajApp.Views;

public partial class BelgeView : UserControl
{
    public BelgeView()
    {
        InitializeComponent();

        var btn = this.FindControl<Button>("BtnBelgeYukle");
        if (btn != null)
            btn.Click += BtnBelgeYukle_Click;
    }

    private async void BtnBelgeYukle_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var dosyalar = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Belge Sec",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("PDF ve Resim")
                {
                    Patterns = ["*.pdf", "*.png", "*.jpg", "*.jpeg"]
                }
            ]
        });

        if (dosyalar.Count > 0 && DataContext is BelgeViewModel vm)
        {
            var yol = dosyalar[0].TryGetLocalPath();
            if (yol != null)
                await vm.BelgeYukleAsync(yol);
        }
    }
}
