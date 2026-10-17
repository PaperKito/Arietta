using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Arietta.Features.KuroBbs;

namespace Arietta.Features.GachaLog;

public static class GachaIconSource
{

    private static readonly ILogger _logger = AppConfig.GetLogger<LogTag>();

    private sealed class LogTag;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private static readonly TimeSpan IndexLifetime = TimeSpan.FromHours(24);

    public static string CacheFolder => Path.Combine(AppConfig.LocalFolder, "GachaIcons");

    private static string IndexFolder => Path.Combine(AppConfig.LocalFolder, "KuroBbs");

    private static string IndexFile => Path.Combine(IndexFolder, "icon-index.json");

    private static readonly SemaphoreSlim _indexLock = new(1, 1);

    private static Dictionary<string, string>? _index;

    private static DateTimeOffset _indexFetchedAt;


    #region 索引：内存 → 磁盘 → 库街区

    public static async Task<Dictionary<string, string>?> GetIndexAsync(bool force = false, CancellationToken ct = default)
    {
        if (!force && _index is not null)
        {
            return _index;
        }

        await _indexLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!force && _index is not null)
            {
                return _index;
            }

            IndexFileModel? disk = TryLoadDiskIndex();
            if (!force && disk is not null && DateTimeOffset.UtcNow - disk.FetchedAt < IndexLifetime)
            {
                _index = disk.Items;
                _indexFetchedAt = disk.FetchedAt;
                return _index;
            }

            Dictionary<string, string>? fresh = await FetchIndexAsync(ct).ConfigureAwait(false);
            if (fresh is { Count: > 0 })
            {
                _index = fresh;
                _indexFetchedAt = DateTimeOffset.UtcNow;
                TrySaveDiskIndex(fresh);
                return fresh;
            }

            if (disk is not null)
            {
                _index = disk.Items;
                _indexFetchedAt = disk.FetchedAt;
                return _index;
            }
            return null;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static async Task<Dictionary<string, string>?> FetchIndexAsync(CancellationToken ct)
    {
        var account = KuroBbsAccountStore.Load();
        if (account is null || !account.IsValid)
        {
            return null;
        }

        try
        {
            var client = KuroBbsClient.Instance;

            _ = await client.RefreshCalculatorDataAsync(account, ct).ConfigureAwait(false);

            var index = new Dictionary<string, string>(StringComparer.Ordinal);

            List<KuroCalculatorRole> roleList = [];
            var roles = await client.GetCalculatorRoleListAsync(account, ct).ConfigureAwait(false);
            if (roles is { IsSuccess: true, Data: { } fetched })
            {
                roleList = fetched;
                foreach (var role in roleList)
                {
                    Add(index, role.RoleName, role.RoleIconUrl);
                }

                foreach (var role in roleList)
                {
                    Add(index, role.Acronym, role.RoleIconUrl);
                }
            }
            else if (roles.IsTokenExpired)
            {
                return null;
            }

            var weapons = await client.GetCalculatorWeaponListAsync(account, ct).ConfigureAwait(false);
            if (weapons is { IsSuccess: true, Data: { } weaponList })
            {
                foreach (var weapon in weaponList)
                {
                    Add(index, weapon.WeaponName, weapon.WeaponIcon);
                }
                foreach (var weapon in weaponList)
                {
                    Add(index, weapon.Acronym, weapon.WeaponIcon);
                }
            }

            await HarvestMaterialIconsAsync(account, index, roleList, ct).ConfigureAwait(false);

            if (index.Count == 0)
            {
                return null;
            }
            _logger.LogInformation("库街区养成计算器图标索引已刷新：{Count} 条", index.Count);
            return index;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "拉取库街区养成计算器图标索引失败");
            return null;
        }
    }

    private static async Task HarvestMaterialIconsAsync(KuroBbsAccount account,
        Dictionary<string, string> index, List<KuroCalculatorRole> roles, CancellationToken ct)
    {
        try
        {
            if (roles.Count == 0)
            {
                return;
            }
            var aims = roles
                .Where(r => !r.IsPreview && r.RoleId > 0)
                .Take(3)
                .Select(r => new KuroCalculatorRoleAim { RoleId = r.RoleId, RoleStartLevel = 1, RoleEndLevel = 90 })
                .ToList();
            if (aims.Count == 0)
            {
                return;
            }
            var result = await KuroBbsClient.Instance.BatchRoleCostAsync(account, aims, ct).ConfigureAwait(false);
            if (result is not { IsSuccess: true, Data: { } data })
            {
                return;
            }
            int before = index.Count;
            foreach (var costList in data.CostList ?? [])
            {
                foreach (var cost in costList.AllCost ?? [])
                {
                    Add(index, cost.Name, cost.IconUrl);
                }
            }
            int added = index.Count - before;
            if (added > 0)
            {
                _logger.LogInformation("养成消耗清单补充材料图标 {Added} 条（含贝币等资源）", added);
            }
        }
        catch (Exception e)
        {

            _logger.LogDebug(e, "收集养成材料图标失败");
        }
    }


    private static void Add(Dictionary<string, string> index, string? name, string? url)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        index.TryAdd(name.Trim(), url.Trim());
    }

    #endregion


    #region 解析与下载

    public static async Task<string?> ResolveUrlAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var index = await GetIndexAsync(false, ct).ConfigureAwait(false);
        if (index is not null && index.TryGetValue(name.Trim(), out string? url))
        {
            return url;
        }
        return await GachaIconService.LookupWikiUrlAsync(name, ct).ConfigureAwait(false);
    }

    public static async Task<string?> EnsureCachedAsync(int resourceId, string name, bool overwrite = false, CancellationToken ct = default)
    {
        if (resourceId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        Directory.CreateDirectory(CacheFolder);
        string file = Path.Combine(CacheFolder, $"{resourceId}.png");
        if (File.Exists(file) && !overwrite)
        {
            return file;
        }
        string? url = await ResolveUrlAsync(name, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }
        try
        {
            byte[] bytes = await Http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
            await File.WriteAllBytesAsync(file, bytes, ct).ConfigureAwait(false);
            return file;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "图标下载失败：{Name} #{ResourceId}", name, resourceId);
            return null;
        }
    }

    #endregion


    #region 索引落盘

    private static IndexFileModel? TryLoadDiskIndex()
    {
        try
        {
            if (!File.Exists(IndexFile))
            {
                return null;
            }
            using FileStream stream = File.OpenRead(IndexFile);
            return JsonSerializer.Deserialize(stream, IconIndexJsonContext.Default.IndexFileModel);
        }
        catch
        {
            return null;
        }
    }


    private static void TrySaveDiskIndex(Dictionary<string, string> items)
    {
        try
        {
            Directory.CreateDirectory(IndexFolder);
            var model = new IndexFileModel { FetchedAt = DateTimeOffset.UtcNow, Items = items };
            using FileStream stream = File.Create(IndexFile);
            JsonSerializer.Serialize(stream, model, IconIndexJsonContext.Default.IndexFileModel);
        }
        catch
        {

        }
    }

    #endregion


    internal sealed class IndexFileModel
    {
        [JsonPropertyName("fetchedAt")]
        public DateTimeOffset FetchedAt { get; set; }

        [JsonPropertyName("items")]
        public Dictionary<string, string> Items { get; set; } = [];
    }

}

[JsonSerializable(typeof(GachaIconSource.IndexFileModel))]
internal sealed partial class IconIndexJsonContext : JsonSerializerContext;
