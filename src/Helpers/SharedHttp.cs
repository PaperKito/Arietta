using System;
using System.Net;
using System.Threading;
using System.Net.Http;

namespace Arietta.Helpers;

internal static class SharedHttp
{

    public static HttpClient Default { get; } =
        Create(decompression: false, timeout: TimeSpan.FromSeconds(15));

    public static HttpClient Sdk { get; } =
        Create(decompression: false, timeout: TimeSpan.FromSeconds(10));

    public static HttpClient Notice { get; } =
        Create(decompression: true, timeout: TimeSpan.FromSeconds(15), userAgent: ChromeUserAgent);

    public static HttpClient LauncherBackground { get; } =
        Create(decompression: true, timeout: TimeSpan.FromMinutes(10), userAgent: LauncherUserAgent);

    public static HttpClient GameUpdateJson { get; } =
        Create(decompression: true, timeout: TimeSpan.FromSeconds(30), userAgent: ChromeUserAgent,
            pooledConnectionLifetime: TimeSpan.FromMinutes(10));

    public static HttpClient GameUpdateDownload { get; } =
        Create(decompression: false, timeout: Timeout.InfiniteTimeSpan, userAgent: ChromeUserAgent,
            pooledConnectionLifetime: TimeSpan.FromMinutes(10), bypassProxy: true);

    public static HttpClient GameUpdateDownloadViaProxy { get; } =
        Create(decompression: false, timeout: Timeout.InfiniteTimeSpan, userAgent: ChromeUserAgent,
            pooledConnectionLifetime: TimeSpan.FromMinutes(10), bypassProxy: false);

    public static HttpClient SelfUpdate { get; } =
        Create(decompression: false, timeout: Timeout.InfiniteTimeSpan, userAgent: ChromeUserAgent,
            pooledConnectionLifetime: TimeSpan.FromMinutes(10), bypassProxy: false);

    public static bool IsSystemProxyActive()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            if (key is null)
            {
                return false;
            }
            object? enable = key.GetValue("ProxyEnable");
            string? server = key.GetValue("ProxyServer") as string;
            return enable is int on && on != 0 && !string.IsNullOrWhiteSpace(server);
        }
        catch
        {
            return false;
        }
    }

    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

    private const string LauncherUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

    private static HttpClient Create(
        bool decompression,
        TimeSpan timeout,
        string? userAgent = null,
        TimeSpan? pooledConnectionLifetime = null,
        bool bypassProxy = false)
    {
        var client = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = decompression ? DecompressionMethods.All : DecompressionMethods.None,
            PooledConnectionLifetime = pooledConnectionLifetime ?? TimeSpan.FromMinutes(2),

            UseProxy = !bypassProxy,
        })
        { Timeout = timeout };

        if (userAgent is not null)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        return client;
    }
}
