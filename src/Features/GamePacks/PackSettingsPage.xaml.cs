using Arietta.Core;
using Arietta.Frameworks;
using Arietta.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arietta.Features.GamePacks;

public sealed partial class PackSettingsPage : PageBase
{

    private string? _registrySubKey;
    private bool _loading;


    public PackSettingsPage()
    {
        InitializeComponent();
    }


    protected override void OnLoaded()
    {

        try
        {
            Build();
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogError(ex, "Pack settings page failed");
            Panel_Root.Children.Add(new TextBlock { Text = ex.Message, TextWrapping = TextWrapping.Wrap });
        }
    }


    private void Build()
    {
        Panel_Root.Children.Clear();
        GamePack? pack = ViewHost.MainView.CurrentGameEntry?.Pack;
        if (pack?.Settings is null || pack.Settings.Groups.Count == 0)
        {
            Panel_Root.Children.Add(new TextBlock
            {
                Text = Lang.Instance.GamePackPage_SettingsEmpty,
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        _loading = true;
        try
        {
            _registrySubKey = NormalizeSubKey(pack.Settings.Store.Path);
            foreach (GamePackSettingGroup group in pack.Settings.Groups)
            {
                Panel_Root.Children.Add(new TextBlock
                {
                    Text = group.Label,
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                });
                foreach (GamePackSettingItem item in group.Items)
                {
                    Panel_Root.Children.Add(BuildRow(item));
                }
            }
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot build pack settings page");
        }
        finally
        {
            _loading = false;
        }
    }

    private static string NormalizeSubKey(string path)
    {
        string p = path.Trim();
        foreach (string prefix in new[] { "HKEY_CURRENT_USER\\", "HKCU\\" })
        {
            if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return p[prefix.Length..];
            }
        }
        return p;
    }


    private FrameworkElement BuildRow(GamePackSettingItem item)
    {
        long current = ReadValue(item.Key);
        FrameworkElement control = item.Type switch
        {
            "toggle" => BuildToggle(item, current),
            "enum" => BuildEnum(item, current),
            _ => BuildNumber(item, current),
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Padding = new Thickness(0, 4, 0, 4),
        };
        row.Children.Add(new TextBlock
        {
            Text = item.Label,
            Width = 180,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        row.Children.Add(control);
        return row;
    }


    private ToggleSwitch BuildToggle(GamePackSettingItem item, long current)
    {
        var toggle = new ToggleSwitch
        {
            IsOn = current != 0,
            OnContent = "",
            OffContent = "",
            MinWidth = 0,
        };
        toggle.Toggled += (_, _) =>
        {
            if (_loading)
            {
                return;
            }

            long on = item.Options?.FirstOrDefault(o => o.Value != 0)?.Value ?? 1;
            WriteValue(item.Key, toggle.IsOn ? on : 0);
        };
        return toggle;
    }


    private ComboBox BuildEnum(GamePackSettingItem item, long current)
    {
        var options = item.Options ?? new List<GamePackSettingOption>();
        var combo = new ComboBox
        {
            MinWidth = 220,
            ItemsSource = options.Select(o => o.Label).ToList(),
        };
        int index = options.FindIndex(o => o.Value == current);
        combo.SelectedIndex = index >= 0 ? index : -1;
        combo.SelectionChanged += (_, _) =>
        {
            if (_loading || combo.SelectedIndex < 0 || combo.SelectedIndex >= options.Count)
            {
                return;
            }
            WriteValue(item.Key, options[combo.SelectedIndex].Value);
        };
        return combo;
    }


    private NumberBox BuildNumber(GamePackSettingItem item, long current)
    {
        var box = new NumberBox
        {
            Value = current,
            Minimum = item.Min,
            Maximum = item.Max <= item.Min ? 1_000_000 : item.Max,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            MinWidth = 180,
        };
        box.ValueChanged += (_, args) =>
        {
            if (_loading || double.IsNaN(args.NewValue))
            {
                return;
            }
            WriteValue(item.Key, (long)args.NewValue);
        };
        return box;
    }


    private long ReadValue(string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_registrySubKey ?? "");
            object? raw = key?.GetValue(name);
            return raw switch
            {
                int i => i,
                long l => l,
                string s when long.TryParse(s, out long parsed) => parsed,
                _ => 0,
            };
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot read pack setting {Key}", name);
            return 0;
        }
    }


    private void WriteValue(string name, long value)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_registrySubKey ?? "", writable: true);
            if (key is null)
            {
                AppConfig.GetLogger("GamePack").LogWarning("Pack settings key missing: {Path}", _registrySubKey);
                return;
            }
            key.SetValue(name, value is >= int.MinValue and <= int.MaxValue ? (int)value : value,
                value is >= int.MinValue and <= int.MaxValue ? RegistryValueKind.DWord : RegistryValueKind.QWord);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogError(ex, "Cannot write pack setting {Key}", name);
        }
    }

}
