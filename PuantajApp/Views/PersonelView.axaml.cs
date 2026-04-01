using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PuantajApp.ViewModels;

namespace PuantajApp.Views;

public partial class PersonelView : UserControl
{
    public PersonelView()
    {
        InitializeComponent();

        var btn = this.FindControl<Button>("BtnExcelImport");
        if (btn != null)
            btn.Click += BtnExcelImport_Click;
    }

    private async void BtnExcelImport_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var dosyalar = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Excel Personel Dosyasi",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Excel") { Patterns = ["*.xlsx", "*.xls"] }]
        });

        if (dosyalar.Count > 0 && DataContext is PersonelViewModel vm)
        {
            var yol = dosyalar[0].TryGetLocalPath();
            if (yol != null)
                await vm.ExcelImportDosyaAsync(yol);
        }
    }
}
