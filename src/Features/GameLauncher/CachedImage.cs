using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

public sealed partial class CachedImage : UserControl
{
    private readonly Image _image = new() { Stretch = Stretch.Uniform };

    private Uri? _pendingUri;

    public CachedImage()
    {
        this.Content = _image;
        SizeChanged += OnSizeChanged;
    }

    public Stretch ImageStretch
    {
        get => (Stretch)GetValue(ImageStretchProperty);
        set => SetValue(ImageStretchProperty, value);
    }

    public static readonly DependencyProperty ImageStretchProperty = DependencyProperty.Register(
        nameof(ImageStretch),
        typeof(Stretch),
        typeof(CachedImage),
        new PropertyMetadata(Stretch.Uniform, OnImageStretchChanged));

    private static void OnImageStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CachedImage control)
        {
            control._image.Stretch = (Stretch)e.NewValue;
        }
    }

    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(object),
        typeof(CachedImage),
        new PropertyMetadata(null, OnSourceChanged));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CachedImage control)
        {
            control.LoadImage(e.NewValue as Uri);
        }
    }

    private async void LoadImage(Uri? uri)
    {
        try
        {
            if (uri == null)
            {
                _pendingUri = null;
                _image.Source = null;
                return;
            }
            if (ActualWidth < 1)
            {
                _pendingUri = uri;
                return;
            }
            _pendingUri = null;
            _image.Source = await DecodeAsync(uri, ActualWidth);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_pendingUri is not null)
        {
            LoadImage(_pendingUri);
        }
    }

    private static async Task<BitmapImage> DecodeAsync(Uri uri, double displayWidth)
    {
        var bitmap = new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = (int)Math.Ceiling(displayWidth),
        };
        if (uri.IsFile && File.Exists(uri.LocalPath))
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(uri.LocalPath);
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            bitmap.SetSource(stream);
        }
        else
        {
            bitmap.UriSource = uri;
        }
        return bitmap;
    }
}
