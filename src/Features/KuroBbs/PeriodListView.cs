using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Arietta.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Numerics;

namespace Arietta.Features.KuroBbs;

public sealed class PeriodListView : ListView
{

    private UIElement? _activeIndicator;

    private UIElement? _prevIndicator;
    private UIElement? _nextIndicator;

    private int _animationGeneration;

    private bool _syncPending;
    private int _syncTicks;

    private static readonly Vector2 EaseInP1 = new(0.9f, 0.1f);
    private static readonly Vector2 EaseInP2 = new(1.0f, 0.2f);
    private static readonly Vector2 EaseOutP1 = new(0.1f, 0.9f);
    private static readonly Vector2 EaseOutP2 = new(0.2f, 1.0f);

    private const float IndicatorHeight = 16f;

    public PeriodListView()
    {
        SelectionChanged += (_, _) =>
        {
            try
            {
                AnimateSelectionChanged();
            }
            catch (Exception ex)
            {

                AppConfig.GetLogger("PeriodListView").LogError(ex, "AnimateSelectionChanged");
            }
        };

        ContainerContentChanging += (_, args) =>
        {
            try
            {
                SyncContainer(args);
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger("PeriodListView").LogError(ex, "ContainerContentChanging");
            }
        };
    }

    private void SyncContainer(ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is not ListViewItem container)
        {
            return;
        }
        if (FindSelectionIndicator(container) is not { } bar)
        {
            return;
        }

        if (args.InRecycleQueue)
        {

            if (ReferenceEquals(bar, _prevIndicator) || ReferenceEquals(bar, _nextIndicator))
            {
                if (ReferenceEquals(_activeIndicator, bar))
                {
                    _activeIndicator = null;
                }
                return;
            }
            ResetElementAnimationProperties(bar, 0.0f);
            if (ReferenceEquals(_activeIndicator, bar))
            {
                _activeIndicator = null;
            }
            return;
        }

        if (ReferenceEquals(bar, _prevIndicator) || ReferenceEquals(bar, _nextIndicator))
        {
            return;
        }

        var isSelected = ReferenceEquals(args.Item, SelectedItem);
        if (!isSelected)
        {
            ResetElementAnimationProperties(bar, 0.0f);
            if (ReferenceEquals(_activeIndicator, bar))
            {

                _activeIndicator = null;
            }
            return;
        }

        if (!ReferenceEquals(_activeIndicator, bar))
        {
            ResetElementAnimationProperties(_activeIndicator, 0.0f);
        }
        ResetElementAnimationProperties(bar, 1.0f);
        _activeIndicator = bar;
    }

    private void AnimateSelectionChanged()
    {
        var nextIndicator = SelectedItem is null ? null : FindSelectionIndicator(SelectedItem);

        if (nextIndicator is null && SelectedItem is not null)
        {
            ScheduleIndicatorSync();
            return;
        }

        if (_prevIndicator is not null || _nextIndicator is not null)
        {
            if (nextIndicator is not null && ReferenceEquals(nextIndicator, _nextIndicator))
            {
                return;
            }
            OnAnimationComplete();
        }

        var prevIndicator = _activeIndicator;

        if (prevIndicator is null || nextIndicator is null || ReferenceEquals(prevIndicator, nextIndicator))
        {

            ResetElementAnimationProperties(prevIndicator, 0.0f);
            ResetElementAnimationProperties(nextIndicator, 1.0f);
            _activeIndicator = nextIndicator;
            return;
        }

        ResetElementAnimationProperties(prevIndicator, 1.0f);
        ResetElementAnimationProperties(nextIndicator, 1.0f);

        double prevPos;
        double nextPos;
        try
        {
            prevPos = prevIndicator.TransformToVisual(this).TransformPoint(default).Y;
            nextPos = nextIndicator.TransformToVisual(this).TransformPoint(default).Y;
        }
        catch (Exception ex)
        {

            AppConfig.GetLogger("PeriodListView").LogDebug(ex, "TransformToVisual");
            ResetElementAnimationProperties(prevIndicator, 0.0f);
            ResetElementAnimationProperties(nextIndicator, 1.0f);
            _activeIndicator = nextIndicator;
            return;
        }

        var isNextBelow = nextPos > prevPos;

        var compositor = ElementCompositionPreview.GetElementVisual(nextIndicator).Compositor;
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        PlayIndicatorAnimations(prevIndicator, isOutgoing: true, fromTop: !isNextBelow);
        PlayIndicatorAnimations(nextIndicator, isOutgoing: false, fromTop: isNextBelow);
        batch.End();

        _prevIndicator = prevIndicator;
        _nextIndicator = nextIndicator;
        _activeIndicator = nextIndicator;

        var generation = ++_animationGeneration;
        batch.Completed += (_, _) =>
        {

            if (generation != _animationGeneration)
            {
                return;
            }
            OnAnimationComplete();
        };
    }

    private void ScheduleIndicatorSync()
    {
        if (_syncPending)
        {
            return;
        }
        _syncPending = true;
        _syncTicks = 0;
        LayoutUpdated += OnLayoutUpdatedForSync;
    }

    private void OnLayoutUpdatedForSync(object? sender, object e)
    {
        if (SelectedItem is null)
        {
            StopIndicatorSync();
            return;
        }
        if (FindSelectionIndicator(SelectedItem) is not { } bar)
        {
            if (++_syncTicks > 20)
            {
                StopIndicatorSync();
            }
            return;
        }

        StopIndicatorSync();

        if (_prevIndicator is not null || _nextIndicator is not null)
        {
            return;
        }

        if (!ReferenceEquals(_activeIndicator, bar))
        {
            ResetElementAnimationProperties(_activeIndicator, 0.0f);
        }
        ResetElementAnimationProperties(bar, 1.0f);
        _activeIndicator = bar;
    }

    private void StopIndicatorSync()
    {
        if (!_syncPending)
        {
            return;
        }
        _syncPending = false;
        LayoutUpdated -= OnLayoutUpdatedForSync;
    }

    private static void PlayIndicatorAnimations(UIElement indicator, bool isOutgoing, bool fromTop)
    {
        var visual = ElementCompositionPreview.GetElementVisual(indicator);
        var comp = visual.Compositor;
        var easeIn = comp.CreateCubicBezierEasingFunction(EaseInP1, EaseInP2);
        var easeOut = comp.CreateCubicBezierEasingFunction(EaseOutP1, EaseOutP2);

        var scaleAnim = comp.CreateScalarKeyFrameAnimation();
        if (isOutgoing)
        {

            scaleAnim.InsertKeyFrame(0.0f, 1.0f);
            scaleAnim.InsertKeyFrame(1.0f, 0.0f, easeIn);
            scaleAnim.Duration = TimeSpan.FromMilliseconds(200);
        }
        else
        {

            scaleAnim.InsertKeyFrame(0.0f, 0.0f);
            scaleAnim.InsertKeyFrame(0.333f, 0.0f);
            scaleAnim.InsertKeyFrame(1.0f, 1.0f, easeOut);
            scaleAnim.Duration = TimeSpan.FromMilliseconds(600);
        }
        visual.StartAnimation("Scale.Y", scaleAnim);

        var centerPoint = visual.CenterPoint;
        centerPoint.Y = fromTop ? 0.0f : IndicatorHeight;
        visual.CenterPoint = centerPoint;
    }

    private void OnAnimationComplete()
    {
        if (_prevIndicator is { } prev)
        {
            ResetElementAnimationProperties(prev, 0.0f);
        }
        if (_nextIndicator is { } next)
        {
            ResetElementAnimationProperties(next, 1.0f);
        }
        _prevIndicator = null;
        _nextIndicator = null;
    }

    private static void ResetElementAnimationProperties(UIElement? element, float opacity)
    {
        if (element is null)
        {
            return;
        }
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.StopAnimation("Offset");
            visual.StopAnimation("Scale");
            visual.StopAnimation("CenterPoint");
            visual.StopAnimation("Opacity");
            visual.Offset = Vector3.Zero;
            visual.Scale = Vector3.One;
            visual.CenterPoint = Vector3.Zero;
            visual.Opacity = opacity;
            element.Opacity = opacity;
        }
        catch (Exception ex)
        {

            AppConfig.GetLogger("PeriodListView").LogDebug(ex, "ResetElementAnimationProperties");
        }
    }

    private UIElement? FindSelectionIndicator(object item)
    {
        if (ContainerFromItem(item) is ListViewItem container)
        {
            container.ApplyTemplate();
            return FindDescendantByName(container, "SelectionIndicator");
        }
        return null;
    }

    private static UIElement? FindDescendantByName(DependencyObject parent, string name)
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i) is FrameworkElement child)
            {
                if (child.Name == name)
                {
                    return child;
                }
                if (FindDescendantByName(child, name) is { } found)
                {
                    return found;
                }
            }
        }
        return null;
    }
}
