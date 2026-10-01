using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Arietta.Helpers;

public class InAppToast : Behavior<StackPanel>
{

    
    private const float SlideUpDistance = 64f;

    private readonly DispatcherQueueTimer _dismissTimer;


    public string Tag
    {
        get { return (string)GetValue(TagProperty); }
        set { SetValue(TagProperty, value); }
    }
    public static readonly DependencyProperty TagProperty =
        DependencyProperty.Register("Tag", typeof(string), typeof(InAppToast), new PropertyMetadata(default));


    public static InAppToast? MainWindow { get; private set; }




    public InAppToast()
    {
        _dismissTimer = DispatcherQueue.CreateTimer();
        _dismissTimer.Interval = TimeSpan.FromSeconds(30);
        _dismissTimer.IsRepeating = true;
        _dismissTimer.Tick += _dismissTimer_Tick;
    }


    protected override void OnAttached()
    {
        base.OnAttached();
        if (Tag is nameof(MainWindow))
        {
            MainWindow = this;
        }
    }


    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (Tag is nameof(MainWindow))
        {
            MainWindow = null;
        }
    }



    private void _dismissTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            int i = 0;
            var count = AssociatedObject.Children.Count;
            while (i < count)
            {
                var item = AssociatedObject.Children[i] as InfoBar;
                if (item != null && !item.IsOpen)
                {
                    AssociatedObject.Children.RemoveAt(i);
                    count--;
                }
                else
                {
                    i++;
                }
            }
        }
        catch { }
    }



    public void Show(InfoBar infoBar, int duration = 0, int index = -1)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                infoBar.IsOpen = true;
                infoBar.CloseButtonClick += InfoBar_CloseButtonClick;
                if (index > 0)
                {
                    AssociatedObject.Children.Insert(index, infoBar);
                }
                else
                {
                    AssociatedObject.Children.Add(infoBar);
                }
                AnimateIn(infoBar);
                if (duration > 0)
                {
                    await Task.Delay(duration);
                    await HideAsync(infoBar);
                }
            }
            catch { }
        });
    }


    
    private void InfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        _ = HideAsync(sender);
    }


    
    public async Task HideAsync(InfoBar infoBar)
    {
        try
        {
            if (!AssociatedObject.Children.Contains(infoBar))
            {
                return;
            }
            var visual = ElementCompositionPreview.GetElementVisual(infoBar);
            var compositor = visual.Compositor;
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(1f, 0.6f));

            var offset = compositor.CreateVector3KeyFrameAnimation();
            offset.Duration = TimeSpan.FromMilliseconds(180);
            offset.InsertKeyFrame(1f, new Vector3(0, 24, 0), easing);
            visual.StartAnimation(nameof(visual.Offset), offset);

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.Duration = TimeSpan.FromMilliseconds(180);
            opacity.InsertKeyFrame(1f, 0f, easing);
            visual.StartAnimation(nameof(visual.Opacity), opacity);

            await Task.Delay(190);
            AssociatedObject.Children.Remove(infoBar);
        }
        catch { }
    }


    
    private static void AnimateIn(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

        visual.Opacity = 0f;
        visual.Offset = new Vector3(0, SlideUpDistance, 0);

        var offset = compositor.CreateVector3KeyFrameAnimation();
        offset.Duration = TimeSpan.FromMilliseconds(320);
        offset.InsertKeyFrame(1f, Vector3.Zero, easing);
        visual.StartAnimation(nameof(visual.Offset), offset);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.Duration = TimeSpan.FromMilliseconds(240);
        opacity.InsertKeyFrame(1f, 1f, easing);
        visual.StartAnimation(nameof(visual.Opacity), opacity);
    }


    private void AddInfoBar(InfoBarSeverity severity, string? title, string? message, int duration = 0)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
            };
            if (severity == InfoBarSeverity.Informational)
            {
                infoBar.Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush;
            }
            Show(infoBar, duration);
        });
    }




    public void Information(string? title, string? message = null, int duration = 3000)
    {
        AddInfoBar(InfoBarSeverity.Informational, title, message, duration);
    }



    public void Success(string? title, string? message = null, int duration = 3000)
    {
        AddInfoBar(InfoBarSeverity.Success, title, message, duration);
    }



    public void Warning(string? title, string? message = null, int duration = 5000)
    {
        AddInfoBar(InfoBarSeverity.Warning, title, message, duration);
    }



    public void Error(string? title, string? message = null, int duration = 5000)
    {
        AddInfoBar(InfoBarSeverity.Error, title, message, duration);
    }



    public void Error(Exception ex, string? message = null, int duration = 5000)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            AddInfoBar(InfoBarSeverity.Error, ex.GetType().Name, ex.Message, duration);
        }
        else
        {
            AddInfoBar(InfoBarSeverity.Error, $"{ex.GetType().Name} - {message}", ex.Message, duration);
        }
    }


    public void ShowWithButton(InfoBarSeverity severity, string? title, string? message, string buttonContent, Action buttonAction, Action? closedAction = null, int duration = 0)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = Create(severity, title, message, buttonContent, buttonAction, closedAction);
            Show(infoBar, duration);
        });
    }


    private InfoBar Create(InfoBarSeverity severity, string? title, string? message = null, string? buttonContent = null, Action? buttonAction = null, Action? closedAction = null)
    {
        Button? button = null;
        if (!string.IsNullOrWhiteSpace(buttonContent) && buttonAction != null)
        {
            button = new Button
            {
                Content = buttonContent,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            button.Click += (_, _) =>
            {
                try
                {
                    buttonAction();
                }
                catch { }
            };
        }
        var infoBar = new InfoBar
        {
            Severity = severity,
            Title = title,
            Message = message,
            ActionButton = button,
            IsOpen = true,
        };
        if (closedAction is not null)
        {
            infoBar.CloseButtonClick += (_, _) =>
            {
                try
                {
                    closedAction();
                }
                catch { }
            };
        }
        return infoBar;
    }



}
