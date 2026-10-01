using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace Arietta.Features.GameLauncher;

public sealed partial class CachedImage : UserControl
{
    private readonly Image _image = new() { Stretch = Stretch.Uniform };

    public CachedImage()
    {
        this.Content = _image;
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
                _image.Source = null;
                return;
            }
            if (uri.IsFile && File.Exists(uri.LocalPath))
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(uri.LocalPath);
                using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                var bitmap = new BitmapImage();
                bitmap.SetSource(stream);
                _image.Source = bitmap;
            }
            else
            {
                _image.Source = new BitmapImage(uri);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
        }
    }
}
