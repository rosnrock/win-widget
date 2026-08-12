using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WinWidget.Models;

namespace WinWidget.Views;

public partial class ImageWidgetView : UserControl
{
    private readonly WidgetSettings _settings;

    public ImageWidgetView(WidgetSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        Loaded += (_, _) => RefreshImage();
    }

    public void RefreshImage()
    {
        Photo.Source = null;
        if (string.IsNullOrWhiteSpace(_settings.ImagePath))
        {
            StatusText.Text = "Choose an image in WinWidget settings";
            StatusText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            if (!File.Exists(_settings.ImagePath)) throw new FileNotFoundException();
            using var stream = new FileStream(_settings.ImagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            Photo.Source = bitmap;
            StatusText.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException
                                          or NotSupportedException or ArgumentException or FileFormatException)
        {
            StatusText.Text = "Image is unavailable\nChoose another file in WinWidget settings";
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
