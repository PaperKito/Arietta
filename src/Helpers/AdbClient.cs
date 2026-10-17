using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Helpers;

public sealed class AdbClient : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly object _forwardGate = new();
    private readonly HashSet<ForwardedPort> _forwardedPorts = [];

    public string AdbPath { get; private set; }

    public AdbClient()
    {
        AdbPath = ResolveAdbExecutablePath();
    }

    public void InitAdbServer(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
        {
            throw new ArgumentException("ADB 路径不能为空", nameof(exePath));
        }
        RemoveTrackedForwardsAsync().GetAwaiter().GetResult();
        AdbPath = exePath;
    }

    public static string ResolveAdbExecutablePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string bundled = Path.Combine(current.FullName, ".tools", "platform-tools", "adb.exe");
            if (File.Exists(bundled))
            {
                return bundled;
            }
            current = current.Parent;
        }
        return "adb";
    }


    public async Task<IReadOnlyList<AdbDeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        CommandResult result = await RunAdbAsync("devices -l", ct);
        EnsureSuccess(result, "列出 adb 设备失败");
        return ParseDevices(result.StandardOutput);
    }

    private const string KuroBbsPackage = "com.kurogame.kjq";

    private static readonly string[] RelatedSocketKeywords = ["webview", "chrom", "x5", "tbs", "xweb"];

    public async Task<IReadOnlyList<WebViewSocketInfo>> GetWebViewSocketsAsync(string deviceSerial, CancellationToken ct = default)
    {
        IReadOnlyList<string> names = await ReadUnixSocketNamesAsync(deviceSerial, ct);
        WebViewSocketInfo[] sockets = names
            .Select(ParseSocketInfo)
            .Where(static s => s is not null)
            .Cast<WebViewSocketInfo>()
            .ToArray();
        if (sockets.Length == 0)
        {
            return [];
        }

        IReadOnlyList<int> pids = await GetProcessIdsAsync(deviceSerial, KuroBbsPackage, ct);
        WebViewSocketInfo[] owned = sockets
            .Where(s => pids.Contains(s.ProcessId))
            .Select(static s => s with { IsKuroBbsProcess = true })
            .ToArray();

        WebViewSocketInfo[] candidates = owned.Length > 0 ? owned : sockets;
        return candidates
            .OrderByDescending(static s => s.SocketName.Contains("webview_devtools_remote", StringComparison.Ordinal))
            .ThenBy(static s => s.SocketName, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> ReadUnixSocketNamesAsync(string deviceSerial, CancellationToken ct)
    {
        CommandResult result = await RunAdbAsync($"-s {Quote(deviceSerial)} shell cat /proc/net/unix", ct);
        EnsureSuccess(result, "读取设备 /proc/net/unix 失败");

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static line =>
            {
                int at = line.IndexOf('@');
                return at < 0 ? null : line[(at + 1)..].Trim();
            })
            .Where(static name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
    }

    public async Task<string> CollectDiagnosticsAsync(string deviceSerial, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== 库街区 Token/Did 抓取诊断 =====");
        sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"adb：{AdbPath}");
        sb.AppendLine($"设备：{deviceSerial}");
        sb.AppendLine($"型号：{await TryShellAsync(deviceSerial, "getprop ro.product.model", ct)}");
        sb.AppendLine($"系统：Android {await TryShellAsync(deviceSerial, "getprop ro.build.version.release", ct)}（SDK {await TryShellAsync(deviceSerial, "getprop ro.build.version.sdk", ct)}）");
        sb.AppendLine($"ro.debuggable：{await TryShellAsync(deviceSerial, "getprop ro.debuggable", ct)}");

        IReadOnlyList<int> pids = await GetProcessIdsAsync(deviceSerial, KuroBbsPackage, ct);
        sb.AppendLine(pids.Count > 0
            ? $"库街区进程：{string.Join(", ", pids)}（{KuroBbsPackage}）"
            : $"库街区进程：未找到 {KuroBbsPackage} —— 请先在手机上打开它并进入网页页面");

        sb.AppendLine();
        sb.AppendLine("--- 前台界面 ---");
        string activity = await TryShellAsync(deviceSerial, "dumpsys activity activities", ct);
        foreach (string line in PickLines(activity, ["mResumedActivity", "topResumedActivity", "ResumedActivity"], 4))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();
        sb.AppendLine("--- WebView 内核 ---");
        string webViewUpdate = await TryShellAsync(deviceSerial, "dumpsys webviewupdate", ct);
        foreach (string line in PickLines(webViewUpdate, ["Current WebView package", "Selected", "debuggable", "socket", "Multiprocess"], 12))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();
        sb.AppendLine("--- /proc/net/unix ---");
        string[] devtools;
        string[] related;
        IReadOnlyList<string> names;
        try
        {
            names = await ReadUnixSocketNamesAsync(deviceSerial, ct);
            devtools = names.Where(static n => n.Contains("devtools", StringComparison.OrdinalIgnoreCase)).ToArray();
            related = names
                .Where(n => RelatedSocketKeywords.Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            sb.AppendLine($"抽象 socket 总数：{names.Count}");
        }
        catch (Exception ex)
        {

            sb.AppendLine($"读取失败：{ex.Message}（该状态下 ADB 抓取在本机不可用）");
            return sb.ToString();
        }
        sb.AppendLine(devtools.Length > 0
            ? $"devtools socket：{devtools.Length} 个"
            : "devtools socket：0 个 —— 该 App 未开启 WebView 远程调试，CDP 抓取不可用");
        foreach (string name in devtools)
        {
            sb.AppendLine("  " + name);
        }
        sb.AppendLine($"含 webview/chrom/x5/tbs/xweb 的 socket：{related.Length} 个");
        foreach (string name in related.Take(40))
        {
            sb.AppendLine("  " + name);
        }

        sb.AppendLine();
        sb.AppendLine("--- 全部抽象 socket（前 60 个，用于认出未知内核） ---");
        foreach (string name in names.Take(60))
        {
            sb.AppendLine("  " + name);
        }
        return sb.ToString();
    }

    private async Task<IReadOnlyList<int>> GetProcessIdsAsync(string deviceSerial, string packageName, CancellationToken ct)
    {
        string output = await TryShellAsync(deviceSerial, "ps -A", ct);
        if (output.StartsWith('<'))
        {
            output = await TryShellAsync(deviceSerial, "ps", ct);
        }
        List<int> pids = [];
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(packageName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[1], out int pid))
            {
                pids.Add(pid);
            }
        }
        return pids;
    }

    private async Task<string> TryShellAsync(string deviceSerial, string shellCommand, CancellationToken ct)
    {
        try
        {
            CommandResult result = await RunAdbAsync($"-s {Quote(deviceSerial)} shell {shellCommand}", ct);
            string text = result.StandardOutput.Trim();
            if (text.Length == 0)
            {
                return result.ExitCode == 0 ? "<空>" : $"<执行失败：{result.StandardError.Trim()}>";
            }
            return text;
        }
        catch (Exception ex)
        {
            return $"<执行失败：{ex.Message}>";
        }
    }

    private static IEnumerable<string> PickLines(string text, string[] keywords, int max)
    {
        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => keywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Select(static line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(max);
    }

    public async Task ForwardAsync(string deviceSerial, int localPort, string socketName, CancellationToken ct = default)
    {
        await RunAndEnsureAsync($"-s {Quote(deviceSerial)} forward tcp:{localPort} localabstract:{Quote(socketName)}", "adb 端口转发失败", ct);
        lock (_forwardGate)
        {
            _forwardedPorts.Add(new ForwardedPort(deviceSerial, localPort));
        }
    }

    public async Task RemoveForwardAsync(string deviceSerial, int localPort, CancellationToken ct = default)
    {
        await RunAdbAsync($"-s {Quote(deviceSerial)} forward --remove tcp:{localPort}", ct);
        lock (_forwardGate)
        {
            _forwardedPorts.Remove(new ForwardedPort(deviceSerial, localPort));
        }
    }

    public async Task RemoveTrackedForwardsAsync(CancellationToken ct = default)
    {
        ForwardedPort[] ports;
        lock (_forwardGate)
        {
            ports = [.. _forwardedPorts];
        }
        foreach (var port in ports)
        {
            try
            {
                await RemoveForwardAsync(port.DeviceSerial, port.LocalPort, ct);
            }
            catch (InvalidOperationException) { }
        }
    }

    public async Task<IReadOnlyList<DevToolsTargetInfo>> GetDevToolsTargetsAsync(
        string deviceSerial, string socketName, int localPort, CancellationToken ct = default)
    {
        await RemoveForwardAsync(deviceSerial, localPort, ct);
        await ForwardAsync(deviceSerial, localPort, socketName, ct);
        return await GetDevToolsTargetsFromForwardedPortAsync(localPort, ct);
    }

    public async Task<IReadOnlyList<DevToolsTargetInfo>> GetDevToolsTargetsFromForwardedPortAsync(int port, CancellationToken ct)
    {
        Uri[] endpoints = [new($"http://127.0.0.1:{port}/json/list"), new($"http://127.0.0.1:{port}/json")];
        foreach (var endpoint in endpoints)
        {
            try
            {
                List<DevToolsTargetInfo>? targets = await _httpClient.GetFromJsonAsync(endpoint, CdpJsonContext.Default.ListDevToolsTargetInfo, ct);
                if (targets is { Count: > 0 })
                {
                    return targets
                        .Select(t => t with { WebSocketDebuggerUrl = NormalizeForwardedWebSocketDebuggerUrl(t.WebSocketDebuggerUrl, port) })
                        .ToArray();
                }
            }
            catch (HttpRequestException) { }
            catch (NotSupportedException) { }
            catch (JsonException) { }
        }
        throw new InvalidOperationException("无法从 WebView 端点获取 DevTools 页面列表");
    }

    private static string NormalizeForwardedWebSocketDebuggerUrl(string webSocketDebuggerUrl, int localPort)
    {
        if (string.IsNullOrWhiteSpace(webSocketDebuggerUrl) || !Uri.TryCreate(webSocketDebuggerUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("ws" or "wss"))
        {
            return webSocketDebuggerUrl;
        }
        var builder = new UriBuilder(uri) { Scheme = "ws", Host = "127.0.0.1", Port = localPort };
        return builder.Uri.ToString();
    }

    private static WebViewSocketInfo? ParseSocketInfo(string socketName)
    {
        if (!socketName.Contains("devtools", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        int lastUnderscoreIndex = socketName.LastIndexOf('_');
        string processPart = lastUnderscoreIndex >= 0 ? socketName[(lastUnderscoreIndex + 1)..] : string.Empty;
        _ = int.TryParse(processPart, out int pid);
        return new WebViewSocketInfo(socketName, pid);
    }

    public static IReadOnlyList<AdbDeviceInfo> ParseDevices(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }
        List<AdbDeviceInfo> devices = [];
        foreach (string rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var device = ParseDeviceLine(line);
            if (device is not null)
            {
                devices.Add(device);
            }
        }
        return devices;
    }

    private static AdbDeviceInfo? ParseDeviceLine(string line)
    {
        string[] parts = line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 2; i < parts.Length; i++)
        {
            int idx = parts[i].IndexOf(':');
            if (idx <= 0 || idx == parts[i].Length - 1)
            {
                continue;
            }
            properties[parts[i][..idx]] = parts[i][(idx + 1)..];
        }
        properties.TryGetValue("model", out string? model);
        properties.TryGetValue("product", out string? product);
        properties.TryGetValue("device", out string? deviceName);
        properties.TryGetValue("transport_id", out string? transportId);
        return new AdbDeviceInfo(parts[0], parts[1], product, model, deviceName, transportId, properties);
    }

    private async Task RunAndEnsureAsync(string arguments, string message, CancellationToken ct)
    {
        CommandResult result = await RunAdbAsync(arguments, ct);
        EnsureSuccess(result, message);
    }

    private async Task<CommandResult> RunAdbAsync(string arguments, CancellationToken ct)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = AdbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            }
        };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法启动 adb（{AdbPath}），请确认路径正确", ex);
        }
        Task<string> readOutput = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> readError = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return new CommandResult(process.ExitCode, await readOutput, await readError);
    }

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;

    private static void EnsureSuccess(CommandResult result, string message)
    {
        if (result.ExitCode == 0)
        {
            return;
        }
        string detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        throw new InvalidOperationException($"{message}（adb 退出码 {result.ExitCode}）：{detail.Trim()}");
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed record ForwardedPort(string DeviceSerial, int LocalPort);

    public void Dispose()
    {
        try
        {
            _ = RemoveTrackedForwardsAsync();
        }
        catch (InvalidOperationException) { }
        _httpClient.Dispose();
    }
}

public sealed record AdbDeviceInfo(
    string Serial,
    string State,
    string? Product,
    string? Model,
    string? Device,
    string? TransportId,
    IReadOnlyDictionary<string, string> Properties)
{
    public bool IsOnline => string.Equals(State, "device", StringComparison.OrdinalIgnoreCase);
    public bool IsUnauthorized => string.Equals(State, "unauthorized", StringComparison.OrdinalIgnoreCase);
}

public sealed record WebViewSocketInfo(string SocketName, int ProcessId, bool IsKuroBbsProcess = false);

public sealed record DevToolsTargetInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("webSocketDebuggerUrl")] string WebSocketDebuggerUrl)
{

    [JsonIgnore]
    public bool IsPageLike => Type is "page" or "iframe" or "webview";

    [JsonConstructor]
    public DevToolsTargetInfo() : this("", "", "", "", "") { }
}

[JsonSerializable(typeof(List<DevToolsTargetInfo>))]
[JsonSerializable(typeof(DevToolsTargetInfo))]
internal sealed partial class CdpJsonContext : JsonSerializerContext;
