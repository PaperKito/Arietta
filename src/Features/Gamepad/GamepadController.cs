using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpGameInput.V0;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.System;
using static Vanara.PInvoke.User32;

namespace Arietta.Features.Gamepad;

internal static class GamepadController
{

    private static readonly ILogger _logger = AppConfig.GetLogger("GamepadController");

    private static readonly object _initLock = new();

    private static IGameInput? _gameInput;

    public static bool Initialized { get; private set; }

    public static bool Enabled { get; private set; }

    public static bool NeedInstallGameInputRedist { get; private set; }

    public static bool GameInputRedistOutdate { get; private set; }

    private static VirtualKey[] _guideModifiers = [];
    private static VirtualKey[] _guideKeys = [];
    private static VirtualKey[] _shareModifiers = [];
    private static VirtualKey[] _shareKeys = [];

    public static void Initialize()
    {
        lock (_initLock)
        {
            Enabled = AppConfig.GetConfig("GamepadEnabled") is "1";
            if (!Enabled || Initialized)
            {
                return;
            }
            try
            {
                bool win11 = Environment.OSVersion.Version.Build >= 22000;
                string redist = Path.Combine(Environment.SystemDirectory, "GameInputRedist.dll");
                if (File.Exists(redist))
                {
                    if (Version.TryParse(FileVersionInfo.GetVersionInfo(redist).FileVersion, out var version) && version.Major < 1)
                    {
                        GameInputRedistOutdate = true;
                        return;
                    }
                }
                else if (!win11)
                {
                    NeedInstallGameInputRedist = true;
                    return;
                }

                if (!SharpGameInput.GameInput.CreateV0(out _gameInput))
                {
                    _logger.LogWarning("Failed to create GameInput instance, gamepad mapping unavailable");
                    return;
                }
                _gameInput.RegisterSystemButtonCallback(null, GameInputSystemButtons.Guide | GameInputSystemButtons.Share,
                                                        null, GameInputSystemButtonCallback, out _, out _);

                if (SharpGameInput.GameInput.TryCast(_gameInput, out SharpGameInput.V2.IGameInput? gameInputV2))
                {
                    using (gameInputV2)
                    {
                        gameInputV2.SetFocusPolicy(SharpGameInput.V2.GameInputFocusPolicy.EnableBackgroundInput
                                                   | SharpGameInput.V2.GameInputFocusPolicy.EnableBackgroundGuideButton
                                                   | SharpGameInput.V2.GameInputFocusPolicy.EnableBackgroundShareButton);
                    }
                }
                TryParseKeys(AppConfig.GetConfig("GamepadGuideKeys"), out _guideModifiers, out _guideKeys, out _);
                TryParseKeys(AppConfig.GetConfig("GamepadShareKeys"), out _shareModifiers, out _shareKeys, out _);
                Initialized = true;
                UpdateGameBarRegistry();
                _logger.LogInformation("Gamepad mapping initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize gamepad mapping");
            }
        }
    }

    public static void SetEnabled(bool value)
    {
        AppConfig.SetConfig("GamepadEnabled", value ? "1" : "0");
        Enabled = value;
        if (value)
        {
            Initialize();
        }
        UpdateGameBarRegistry();
    }

    public static bool SetGuideKeys(string? value, out string keysTextOrErrorKey)
    {
        if (!TryParseKeys(value, out var modifiers, out var keys, out keysTextOrErrorKey))
        {
            return false;
        }
        _guideModifiers = modifiers;
        _guideKeys = keys;
        AppConfig.SetConfig("GamepadGuideKeys", keysTextOrErrorKey);
        UpdateGameBarRegistry();
        return true;
    }

    public static bool SetShareKeys(string? value, out string keysTextOrErrorKey)
    {
        if (!TryParseKeys(value, out var modifiers, out var keys, out keysTextOrErrorKey))
        {
            return false;
        }
        _shareModifiers = modifiers;
        _shareKeys = keys;
        AppConfig.SetConfig("GamepadShareKeys", keysTextOrErrorKey);
        return true;
    }

    public static void RestoreGameBarRegistry()
    {
        if (!_gameBarDisabledByUs)
        {
            return;
        }
        try
        {
            Registry.SetValue(GameBarKeyPath, "UseNexusForGameBarEnabled", 1, RegistryValueKind.DWord);
            _gameBarDisabledByUs = false;
        }
        catch { }
    }


    private static void GameInputSystemButtonCallback(LightGameInputCallbackToken callbackToken, object? context,
        LightIGameInputDevice device, ulong timestamp, GameInputSystemButtons currentState, GameInputSystemButtons previousState)
    {
        try
        {
            if (!Enabled)
            {
                return;
            }

            if (currentState.HasFlag(GameInputSystemButtons.Guide) && !previousState.HasFlag(GameInputSystemButtons.Guide))
            {
                if (_guideKeys.Length > 0)
                {
                    SendKeyCombo(_guideModifiers, _guideKeys);
                }
            }
            if (currentState.HasFlag(GameInputSystemButtons.Share) && !previousState.HasFlag(GameInputSystemButtons.Share))
            {
                if (_shareKeys.Length > 0)
                {
                    SendKeyCombo(_shareModifiers, _shareKeys);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle gamepad system key callback");
        }
    }


    #region Game Bar 注册表

    private const string GameBarKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\GameBar";

    private static bool _gameBarDisabledByUs;

    private static void UpdateGameBarRegistry()
    {
        bool needDisable = Initialized && Enabled && _guideKeys.Length > 0;
        try
        {
            if (needDisable && !_gameBarDisabledByUs)
            {
                Registry.SetValue(GameBarKeyPath, "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
                _gameBarDisabledByUs = true;
            }
            else if (!needDisable && _gameBarDisabledByUs)
            {
                Registry.SetValue(GameBarKeyPath, "UseNexusForGameBarEnabled", 1, RegistryValueKind.DWord);
                _gameBarDisabledByUs = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Game Bar registry");
        }
    }


    #endregion


    #region SendInput 组合键


    private static void SendKeyCombo(VirtualKey[] modifiers, VirtualKey[] keys)
    {
        var inputs = new List<INPUT>((modifiers.Length + keys.Length) * 2);
        foreach (VirtualKey key in modifiers)
        {
            AddKeyInput(inputs, key, down: true);
        }
        foreach (VirtualKey key in keys)
        {
            AddKeyInput(inputs, key, down: true);
        }
        for (int i = keys.Length - 1; i >= 0; i--)
        {
            AddKeyInput(inputs, keys[i], down: false);
        }
        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            AddKeyInput(inputs, modifiers[i], down: false);
        }
        _ = SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<INPUT>());
    }


    private static void AddKeyInput(List<INPUT> inputs, VirtualKey key, bool down)
    {
        ushort vk = (ushort)key;
        var flags = (KEYEVENTF)0;
        if (IsExtendedKey(key))
        {
            flags |= KEYEVENTF.KEYEVENTF_EXTENDEDKEY;
        }
        if (!down)
        {
            flags |= KEYEVENTF.KEYEVENTF_KEYUP;
        }
        inputs.Add(new INPUT
        {
            type = INPUTTYPE.INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = vk,

                wScan = (ushort)MapVirtualKey(vk, MAPVK.MAPVK_VK_TO_VSC),
                dwFlags = flags,
            },
        });
    }


    private static bool IsExtendedKey(VirtualKey key)
    {
        return key is VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right
            or VirtualKey.Insert or VirtualKey.Delete or VirtualKey.Home or VirtualKey.End
            or VirtualKey.PageUp or VirtualKey.PageDown or VirtualKey.Print;
    }


    #endregion


    #region 键名解析（键名表来自 Starward.GamepadController，裁剪）


    private static bool TryParseKeys(string? value, out VirtualKey[] modifiers, out VirtualKey[] keys, out string keysTextOrErrorKey)
    {
        modifiers = [];
        keys = [];
        var mods = new List<VirtualKey>();
        var keysList = new List<VirtualKey>();
        string[] parts = value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        foreach (string part in parts)
        {
            if (KeyNameToVirtualKey.TryGetValue(part.ToUpperInvariant(), out VirtualKey vk))
            {
                if (vk is VirtualKey.Shift or VirtualKey.Control or VirtualKey.Menu or VirtualKey.LeftWindows)
                {
                    mods.Add(vk);
                }
                else
                {
                    keysList.Add(vk);
                }
            }
            else
            {
                keysTextOrErrorKey = part;
                return false;
            }
        }
        modifiers = [.. mods];
        keys = [.. keysList];
        keysTextOrErrorKey = string.Join(' ', mods.Concat(keysList).Select(x => VirtualKeyToKeyName.GetValueOrDefault(x)));
        return true;
    }


    public static readonly Dictionary<string, VirtualKey> KeyNameToVirtualKey = new()
    {
        ["SHIFT"] = VirtualKey.Shift,
        ["CTRL"] = VirtualKey.Control,
        ["CONTROL"] = VirtualKey.Control,
        ["ALT"] = VirtualKey.Menu,
        ["MENU"] = VirtualKey.Menu,
        ["WIN"] = VirtualKey.LeftWindows,
        ["WINDOW"] = VirtualKey.LeftWindows,
        ["WINDOWS"] = VirtualKey.LeftWindows,

        ["INS"] = VirtualKey.Insert,
        ["INSERT"] = VirtualKey.Insert,
        ["DEL"] = VirtualKey.Delete,
        ["DELETE"] = VirtualKey.Delete,
        ["HOME"] = VirtualKey.Home,
        ["END"] = VirtualKey.End,
        ["PAGEUP"] = VirtualKey.PageUp,
        ["PGUP"] = VirtualKey.PageUp,
        ["PAGEDOWN"] = VirtualKey.PageDown,
        ["PGDN"] = VirtualKey.PageDown,
        ["LEFT"] = VirtualKey.Left,
        ["UP"] = VirtualKey.Up,
        ["RIGHT"] = VirtualKey.Right,
        ["DOWN"] = VirtualKey.Down,

        ["0"] = VirtualKey.Number0,
        ["1"] = VirtualKey.Number1,
        ["2"] = VirtualKey.Number2,
        ["3"] = VirtualKey.Number3,
        ["4"] = VirtualKey.Number4,
        ["5"] = VirtualKey.Number5,
        ["6"] = VirtualKey.Number6,
        ["7"] = VirtualKey.Number7,
        ["8"] = VirtualKey.Number8,
        ["9"] = VirtualKey.Number9,

        ["A"] = VirtualKey.A,
        ["B"] = VirtualKey.B,
        ["C"] = VirtualKey.C,
        ["D"] = VirtualKey.D,
        ["E"] = VirtualKey.E,
        ["F"] = VirtualKey.F,
        ["G"] = VirtualKey.G,
        ["H"] = VirtualKey.H,
        ["I"] = VirtualKey.I,
        ["J"] = VirtualKey.J,
        ["K"] = VirtualKey.K,
        ["L"] = VirtualKey.L,
        ["M"] = VirtualKey.M,
        ["N"] = VirtualKey.N,
        ["O"] = VirtualKey.O,
        ["P"] = VirtualKey.P,
        ["Q"] = VirtualKey.Q,
        ["R"] = VirtualKey.R,
        ["S"] = VirtualKey.S,
        ["T"] = VirtualKey.T,
        ["U"] = VirtualKey.U,
        ["V"] = VirtualKey.V,
        ["W"] = VirtualKey.W,
        ["X"] = VirtualKey.X,
        ["Y"] = VirtualKey.Y,
        ["Z"] = VirtualKey.Z,

        ["PAD0"] = VirtualKey.NumberPad0,
        ["PAD1"] = VirtualKey.NumberPad1,
        ["PAD2"] = VirtualKey.NumberPad2,
        ["PAD3"] = VirtualKey.NumberPad3,
        ["PAD4"] = VirtualKey.NumberPad4,
        ["PAD5"] = VirtualKey.NumberPad5,
        ["PAD6"] = VirtualKey.NumberPad6,
        ["PAD7"] = VirtualKey.NumberPad7,
        ["PAD8"] = VirtualKey.NumberPad8,
        ["PAD9"] = VirtualKey.NumberPad9,
        ["PAD+"] = VirtualKey.Add,
        ["PAD-"] = VirtualKey.Subtract,
        ["PAD*"] = VirtualKey.Multiply,
        ["PAD/"] = VirtualKey.Divide,
        ["PAD."] = VirtualKey.Decimal,

        ["F1"] = VirtualKey.F1,
        ["F2"] = VirtualKey.F2,
        ["F3"] = VirtualKey.F3,
        ["F4"] = VirtualKey.F4,
        ["F5"] = VirtualKey.F5,
        ["F6"] = VirtualKey.F6,
        ["F7"] = VirtualKey.F7,
        ["F8"] = VirtualKey.F8,
        ["F9"] = VirtualKey.F9,
        ["F10"] = VirtualKey.F10,
        ["F11"] = VirtualKey.F11,
        ["F12"] = VirtualKey.F12,

        ["`"] = (VirtualKey)192,
        ["-"] = (VirtualKey)189,
        ["="] = (VirtualKey)187,
        ["["] = (VirtualKey)219,
        ["]"] = (VirtualKey)221,
        ["\\"] = (VirtualKey)220,
        [";"] = (VirtualKey)186,
        ["'"] = (VirtualKey)222,
        [","] = (VirtualKey)188,
        ["."] = (VirtualKey)190,
        ["/"] = (VirtualKey)191,

        ["ESC"] = VirtualKey.Escape,
        ["ESCAPE"] = VirtualKey.Escape,
        ["SPACE"] = VirtualKey.Space,
        ["TAB"] = VirtualKey.Tab,
        ["ENTER"] = VirtualKey.Enter,
        ["BACKSPACE"] = VirtualKey.Back,
        ["BACK"] = VirtualKey.Back,
        ["CAPSLOCK"] = VirtualKey.CapitalLock,
        ["NUMLOCK"] = VirtualKey.NumberKeyLock,
        ["SCROLL"] = VirtualKey.Scroll,
        ["SCROLLLOCK"] = VirtualKey.Scroll,
        ["PAUSE"] = VirtualKey.Pause,
        ["PRINT"] = VirtualKey.Print,
        ["PRTSC"] = VirtualKey.Print,
        ["PRINTSCREEN"] = VirtualKey.Print,
    };


    public static readonly Dictionary<VirtualKey, string> VirtualKeyToKeyName = new()
    {
        [VirtualKey.Shift] = "Shift",
        [VirtualKey.Control] = "Ctrl",
        [VirtualKey.Menu] = "Alt",
        [VirtualKey.LeftWindows] = "Win",
        [VirtualKey.RightWindows] = "Win",

        [VirtualKey.Insert] = "Insert",
        [VirtualKey.Delete] = "Delete",
        [VirtualKey.Home] = "Home",
        [VirtualKey.End] = "End",
        [VirtualKey.PageUp] = "PageUp",
        [VirtualKey.PageDown] = "PageDown",
        [VirtualKey.Left] = "Left",
        [VirtualKey.Up] = "Up",
        [VirtualKey.Right] = "Right",
        [VirtualKey.Down] = "Down",

        [VirtualKey.Number0] = "0",
        [VirtualKey.Number1] = "1",
        [VirtualKey.Number2] = "2",
        [VirtualKey.Number3] = "3",
        [VirtualKey.Number4] = "4",
        [VirtualKey.Number5] = "5",
        [VirtualKey.Number6] = "6",
        [VirtualKey.Number7] = "7",
        [VirtualKey.Number8] = "8",
        [VirtualKey.Number9] = "9",

        [VirtualKey.A] = "A",
        [VirtualKey.B] = "B",
        [VirtualKey.C] = "C",
        [VirtualKey.D] = "D",
        [VirtualKey.E] = "E",
        [VirtualKey.F] = "F",
        [VirtualKey.G] = "G",
        [VirtualKey.H] = "H",
        [VirtualKey.I] = "I",
        [VirtualKey.J] = "J",
        [VirtualKey.K] = "K",
        [VirtualKey.L] = "L",
        [VirtualKey.M] = "M",
        [VirtualKey.N] = "N",
        [VirtualKey.O] = "O",
        [VirtualKey.P] = "P",
        [VirtualKey.Q] = "Q",
        [VirtualKey.R] = "R",
        [VirtualKey.S] = "S",
        [VirtualKey.T] = "T",
        [VirtualKey.U] = "U",
        [VirtualKey.V] = "V",
        [VirtualKey.W] = "W",
        [VirtualKey.X] = "X",
        [VirtualKey.Y] = "Y",
        [VirtualKey.Z] = "Z",

        [VirtualKey.NumberPad0] = "Pad0",
        [VirtualKey.NumberPad1] = "Pad1",
        [VirtualKey.NumberPad2] = "Pad2",
        [VirtualKey.NumberPad3] = "Pad3",
        [VirtualKey.NumberPad4] = "Pad4",
        [VirtualKey.NumberPad5] = "Pad5",
        [VirtualKey.NumberPad6] = "Pad6",
        [VirtualKey.NumberPad7] = "Pad7",
        [VirtualKey.NumberPad8] = "Pad8",
        [VirtualKey.NumberPad9] = "Pad9",
        [VirtualKey.Add] = "Pad+",
        [VirtualKey.Subtract] = "Pad-",
        [VirtualKey.Multiply] = "Pad*",
        [VirtualKey.Divide] = "Pad/",
        [VirtualKey.Decimal] = "Pad.",

        [VirtualKey.F1] = "F1",
        [VirtualKey.F2] = "F2",
        [VirtualKey.F3] = "F3",
        [VirtualKey.F4] = "F4",
        [VirtualKey.F5] = "F5",
        [VirtualKey.F6] = "F6",
        [VirtualKey.F7] = "F7",
        [VirtualKey.F8] = "F8",
        [VirtualKey.F9] = "F9",
        [VirtualKey.F10] = "F10",
        [VirtualKey.F11] = "F11",
        [VirtualKey.F12] = "F12",

        [(VirtualKey)192] = "`",
        [(VirtualKey)189] = "-",
        [(VirtualKey)187] = "=",
        [(VirtualKey)219] = "[",
        [(VirtualKey)221] = "]",
        [(VirtualKey)220] = "\\",
        [(VirtualKey)186] = ";",
        [(VirtualKey)222] = "'",
        [(VirtualKey)188] = ",",
        [(VirtualKey)190] = ".",
        [(VirtualKey)191] = "/",

        [VirtualKey.Escape] = "Esc",
        [VirtualKey.Space] = "Space",
        [VirtualKey.Tab] = "Tab",
        [VirtualKey.Enter] = "Enter",
        [VirtualKey.Back] = "Backspace",
        [VirtualKey.CapitalLock] = "CapsLock",
        [VirtualKey.NumberKeyLock] = "NumLock",
        [VirtualKey.Scroll] = "ScrollLock",
        [VirtualKey.Pause] = "Pause",
        [VirtualKey.Print] = "PrintScreen",
    };


    #endregion

}
