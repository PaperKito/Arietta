using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Arietta.Features.DynamicBackground;








public sealed partial class DynamicBackgroundHost : UserControl
{
    
    public static DynamicBackgroundHost? Current { get; private set; }

    private MediaPlayer? _player;
    private string? _currentPath;

    
    private CanvasRenderTarget? _videoSurface;
    private CanvasImageSource? _videoImageSource;
    private int _surfaceWidth;
    private int _surfaceHeight;

    
    private readonly SemaphoreSlim _frameSemaphore = new(1, 1);

    
    private bool _pausedByUser;

    public DynamicBackgroundHost()
    {
        this.InitializeComponent();
        Loaded += (_, _) => Current = this;
        Unloaded += (_, _) =>
        {
            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }
        };
    }


    
    public bool IsPlaying => _player is not null
                             && Image_Video.Visibility == Visibility.Visible;


    
    public bool IsPaused => _pausedByUser;


    
    public void Play(string videoPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            {
                Stop();
                return;
            }
            if (_player is not null &&
                string.Equals(_currentPath, videoPath, StringComparison.OrdinalIgnoreCase))
            {
                ApplyVolume(null);
                return;
            }

            Stop();

            var player = new MediaPlayer
            {
                AutoPlay = true,
                IsLoopingEnabled = true,
                AudioCategory = MediaPlayerAudioCategory.GameMedia,
                
                IsVideoFrameServerEnabled = true,
            };
            player.CommandManager.IsEnabled = false;
            player.SystemMediaTransportControls.IsEnabled = false;
            player.Source = MediaSource.CreateFromUri(new Uri(Path.GetFullPath(videoPath)));
            player.VideoFrameAvailable += Player_VideoFrameAvailable;

            _player = player;
            _currentPath = videoPath;
            _pausedByUser = false;

            Image_Video.Visibility = Visibility.Visible;
            ApplyVolume(null);
            player.Play();
        }
        catch
        {
            Stop();
        }
    }


    
    public void Stop()
    {
        try
        {
            if (_player is not null)
            {
                _player.VideoFrameAvailable -= Player_VideoFrameAvailable;
                _player.Pause();
                _player.Source = null;
                _player.Dispose();
            }
        }
        catch { }
        _player = null;
        _currentPath = null;
        _pausedByUser = false;
        Image_Video.Source = null;
        Image_Video.Visibility = Visibility.Collapsed;
        _videoSurface?.Dispose();
        _videoSurface = null;
        _videoImageSource = null;
        _surfaceWidth = 0;
        _surfaceHeight = 0;
    }


    
    
    
    
    
    public void SetWindowVisible(bool visible)
    {
        if (_player is null)
        {
            return;
        }
        try
        {
            if (visible)
            {
                if (!_pausedByUser)
                {
                    _player.Play();
                }
            }
            else
            {
                _player.Pause();
            }
        }
        catch { }
    }


    
    public void TogglePause()
    {
        if (_player is null)
        {
            return;
        }
        try
        {
            if (_pausedByUser)
            {
                _player.Play();
                _pausedByUser = false;
            }
            else
            {
                _player.Pause();
                _pausedByUser = true;
            }
        }
        catch { }
    }


    
    public void SetVolume(double volume)
    {
        ApplyVolume(volume);
    }


    
    public static double GetConfiguredVolume()
    {
        return double.TryParse(AppConfig.GetConfig(DynamicBackgroundStore.VolumeConfigKey), out double v)
            ? Math.Clamp(v, 0, 100)
            : 0;
    }


    private void ApplyVolume(double? overrideValue)
    {
        if (_player is null)
        {
            return;
        }
        double volume = Math.Clamp(overrideValue ?? GetConfiguredVolume(), 0, 100) / 100d;
        try
        {
            _player.IsMuted = volume <= 0.001;
            _player.Volume = volume;
        }
        catch { }
    }


    
    
    
    private void Player_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        if (_frameSemaphore.CurrentCount == 0)
        {
            return;
        }
        _frameSemaphore.Wait();
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                int width = (int)sender.PlaybackSession.NaturalVideoWidth;
                int height = (int)sender.PlaybackSession.NaturalVideoHeight;
                if (width <= 0 || height <= 0)
                {
                    return;
                }
                if (_videoImageSource is null || width != _surfaceWidth || height != _surfaceHeight)
                {
                    _videoSurface?.Dispose();
                    _videoSurface = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
                    _videoImageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), width, height, 96);
                    _surfaceWidth = width;
                    _surfaceHeight = height;
                    Image_Video.Source = _videoImageSource;
                }
                sender.CopyFrameToVideoSurface(_videoSurface);
                using var ds = _videoImageSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent);
                ds.DrawImage(_videoSurface);
            }
            catch { }
            finally
            {
                _frameSemaphore.Release();
            }
        });
    }

}
