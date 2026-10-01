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

    
    private double _fpsIntervalMs;

    
    private long _lastDrawTicks;

    
    private string _qualityMode = "auto";

    
    private bool _windowVisible = true;

    
    private bool _gameRunning;

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
        _fpsIntervalMs = FpsToInterval(AppConfig.GetConfig(DynamicBackgroundStore.FpsCapConfigKey));
        var configuredQuality = AppConfig.GetConfig(DynamicBackgroundStore.QualityConfigKey);
        _qualityMode = configuredQuality is "high" or "medium" or "low" ? configuredQuality : "auto";
        
        Arietta.Features.GameLauncher.GameProcessMonitor.Changed += () => DispatcherQueue?.TryEnqueue(RefreshGameRunningPause);
        RefreshGameRunningPause();
    }


    
    public void SetFpsCap(int fps)
    {
        _fpsIntervalMs = fps > 0 ? 1000d / fps : 0;
    }


    
    public void SetQuality(string quality)
    {
        _qualityMode = quality is "high" or "medium" or "low" ? quality : "auto";
    }


    
    public void RefreshGameRunningPause()
    {
        bool enabled = AppConfig.GetConfig(DynamicBackgroundStore.PauseOnGameConfigKey) != "0";
        bool any = enabled && Arietta.Features.GameLauncher.GameProcessMonitor.IsAnyRunning();
        if (_gameRunning == any)
        {
            return;
        }
        _gameRunning = any;
        UpdatePlayback();
    }


    private static double FpsToInterval(string? config)
    {
        
        int fps = int.TryParse(config, out int v) ? v : 30;
        return fps > 0 ? 1000d / fps : 0;
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
                UpdatePlayback();
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
            UpdatePlayback();
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
        _windowVisible = visible;
        UpdatePlayback();
    }


    
    
    
    
    private void UpdatePlayback()
    {
        if (_player is null)
        {
            if (ShouldPlay() && _currentPath is { Length: > 0 })
            {
                
                Play(_currentPath);
            }
            return;
        }
        if (ShouldPlay())
        {
            _player.Play();
        }
        else
        {
            SuspendPlayback();
        }
    }


    private bool ShouldPlay() => _windowVisible && !_gameRunning && !_pausedByUser;


    
    
    
    
    
    private void SuspendPlayback()
    {
        if (_player is null)
        {
            return;
        }
        try
        {
            _player.VideoFrameAvailable -= Player_VideoFrameAvailable;
            _player.Pause();
            _player.Source = null;
            _player.Dispose();
        }
        catch { }
        _player = null;
    }


    
    public void TogglePause()
    {
        if (_player is null)
        {
            return;
        }
        _pausedByUser = !_pausedByUser;
        UpdatePlayback();
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
        long now = Environment.TickCount64;
        if (_fpsIntervalMs > 0 && now - _lastDrawTicks < _fpsIntervalMs - 2)
        {
            return;
        }
        _lastDrawTicks = now;
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
                
                
                double hostW = ActualWidth;
                double hostH = ActualHeight;
                double raster = XamlRoot?.RasterizationScale ?? 1;
                double cover = hostW > 0 && hostH > 0 ? Math.Max(hostW * raster / width, hostH * raster / height) : 1;
                double autoScale = Math.Min(cover, 1.0);
                double scale = _qualityMode switch
                {
                    "high" => 1.0,
                    "medium" => autoScale * 2d / 3,
                    "low" => autoScale * 0.5,
                    _ => autoScale,
                };
                int targetW = Math.Clamp((int)Math.Round(width * scale), 1, width);
                int targetH = Math.Clamp((int)Math.Round(height * scale), 1, height);
                if (_videoImageSource is null || targetW != _surfaceWidth || targetH != _surfaceHeight)
                {
                    _videoSurface?.Dispose();
                    _videoSurface = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), targetW, targetH, 96);
                    _videoImageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), targetW, targetH, 96);
                    _surfaceWidth = targetW;
                    _surfaceHeight = targetH;
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
