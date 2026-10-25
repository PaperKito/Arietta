using Microsoft.Extensions.Logging;
using Arietta.Features.Database;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.Sync;

internal enum CloudSyncAction
{
    None,
    TestConnection,
    Push,
    Pull,
    Skipped,
}

internal sealed record CloudSyncResult(bool Success, CloudSyncAction Action, string Message, Exception? Exception = null)
{
    public static CloudSyncResult Ok(CloudSyncAction action, string message) => new(true, action, message);

    public static CloudSyncResult Fail(CloudSyncAction action, string message, Exception? exception = null)
        => new(false, action, message, exception);

    public static CloudSyncResult Skip(string message) => new(true, CloudSyncAction.Skipped, message);
}

internal sealed class CloudSyncService
{
    private const int MaxRemoteHistory = 5;
    private const int MaxLocalBackup = 3;

    private readonly ILogger<CloudSyncService> _logger;
    private readonly CloudSyncConfig _config;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CloudSyncService(ILogger<CloudSyncService> logger)
    {
        _logger = logger;
        _config = CloudSyncConfig.Load();
    }

    public CloudSyncConfig Config => _config;

    public bool IsConfigured => _config.IsConfigured;

    public DateTimeOffset? LastSyncTime => _config.LastSyncTime;

    public event EventHandler<CloudSyncResult>? StatusChanged;


    #region 配置

    public void ApplySettings(string serverUrl, string userName, string password,
        bool autoSync, SyncPreference preference)
    {
        _config.ServerUrl = serverUrl?.Trim() ?? "";
        _config.UserName = userName?.Trim() ?? "";
        if (!string.IsNullOrEmpty(password))
        {
            _config.Password = password;
        }

        _config.AutoSync = autoSync;
        _config.Preference = preference;
        _config.Save();
    }

    public void Save() => _config.Save();

    public void ApplyAutoSync(bool autoSync)
    {
        _config.AutoSync = autoSync;
        _config.Save();
    }

    #endregion


    #region 对外的四个动作

    public async Task<CloudSyncResult> TestConnectionAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return CloudSyncResult.Fail(CloudSyncAction.TestConnection, "请先填写服务器地址、账号与密码");
        }
        try
        {
            using var client = CreateClient();
            var (success, message) = await client.TestAsync(ct);
            return success
                ? CloudSyncResult.Ok(CloudSyncAction.TestConnection, message)
                : CloudSyncResult.Fail(CloudSyncAction.TestConnection, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test WebDAV connection");
            return CloudSyncResult.Fail(CloudSyncAction.TestConnection, ex.Message, ex);
        }
    }

    public async Task<CloudSyncResult> SyncAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return CloudSyncResult.Skip(NotConfiguredMessage);
        }
        await _gate.WaitAsync(ct);
        try
        {
            using var client = CreateClient();
            string remoteFile = RemoteFilePath;

            WebDavResource? resource = await client.GetResourceAsync(remoteFile, ct);
            bool hasRemote = resource is not null;
            bool localChanged = !string.Equals(DatabaseService.GetContentVersion(), _config.LastLocalVersion, StringComparison.Ordinal);
            bool remoteChanged = hasRemote
                                 && !string.Equals(resource!.ETag, _config.LastRemoteETag, StringComparison.OrdinalIgnoreCase);
            bool firstTime = string.IsNullOrEmpty(_config.LastLocalVersion) && string.IsNullOrEmpty(_config.LastRemoteETag);

            CloudSyncResult result;
            if (!hasRemote)
            {

                result = await PushInternalAsync(client, allowMergeRetry: false, ct);
            }
            else if (firstTime)
            {

                result = await PullInternalAsync(client, ct);
                if (result.Success)
                {
                    result = await PushInternalAsync(client, allowMergeRetry: false, ct);
                }
            }
            else if (localChanged && remoteChanged)
            {
                result = await PullInternalAsync(client, ct);
                if (result.Success)
                {
                    result = await PushInternalAsync(client, allowMergeRetry: true, ct);
                }
            }
            else if (localChanged)
            {
                result = await PushInternalAsync(client, allowMergeRetry: true, ct);
            }
            else if (remoteChanged)
            {
                result = await PullInternalAsync(client, ct);
            }
            else
            {
                result = CloudSyncResult.Skip("本地与云端都没有变化");
            }

            Raise(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud sync");
            var result = CloudSyncResult.Fail(CloudSyncAction.None, ex.Message, ex);
            Raise(result);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CloudSyncResult> PushAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return CloudSyncResult.Fail(CloudSyncAction.Push, NotConfiguredMessage);
        }
        await _gate.WaitAsync(ct);
        try
        {
            using var client = CreateClient();
            var result = await PushInternalAsync(client, allowMergeRetry: true, ct);
            Raise(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push to WebDAV");
            var result = CloudSyncResult.Fail(CloudSyncAction.Push, ex.Message, ex);
            Raise(result);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CloudSyncResult> PullAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return CloudSyncResult.Fail(CloudSyncAction.Pull, NotConfiguredMessage);
        }
        await _gate.WaitAsync(ct);
        try
        {
            using var client = CreateClient();
            var result = await PullInternalAsync(client, ct);
            Raise(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull from WebDAV");
            var result = CloudSyncResult.Fail(CloudSyncAction.Pull, ex.Message, ex);
            Raise(result);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    #endregion


    #region 内部实现

    private string RemoteFilePath => $"{CloudSyncConfig.RemoteFolder}/{CloudSyncConfig.RemoteFileName}";

    private string NotConfiguredMessage => "尚未配置 WebDAV";

    private string RemoteHistoryFolder => $"{CloudSyncConfig.RemoteFolder}/history";

    private WebDavClient CreateClient() => new(_config.ServerUrl, _config.UserName, _config.Password);


    private async Task<CloudSyncResult> PushInternalAsync(WebDavClient client, bool allowMergeRetry, CancellationToken ct)
    {
        await client.EnsureFolderAsync(CloudSyncConfig.RemoteFolder, ct);

        WebDavResource? existing = await client.GetResourceAsync(RemoteFilePath, ct);
        if (existing is not null)
        {
            var (old, _) = await client.DownloadAsync(RemoteFilePath, ct);
            if (old is { Length: > 0 })
            {
                await client.EnsureFolderAsync(RemoteHistoryFolder, ct);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                await client.UploadAsync($"{RemoteHistoryFolder}/{stamp}_{CloudSyncConfig.RemoteFileName}", old, null, ct);
            }
        }

        byte[] payload = DatabaseSnapshot.Create(out string plainHash);
        var upload = await client.UploadAsync(RemoteFilePath, payload, existing?.ETag, ct);

        if (upload.PreconditionFailed && allowMergeRetry)
        {

            _logger.LogInformation("ETag mismatch, merge remote then retry upload");
            var pull = await PullInternalAsync(client, ct);
            if (!pull.Success)
            {
                return CloudSyncResult.Fail(CloudSyncAction.Push, $"云端已被其他设备更新，且合并失败：{pull.Message}");
            }
            return await PushInternalAsync(client, allowMergeRetry: false, ct);
        }

        if (!upload.Success)
        {
            return CloudSyncResult.Fail(CloudSyncAction.Push, $"上传失败：HTTP {upload.StatusCode}");
        }

        SaveLocalBackup();
        _config.LastLocalVersion = DatabaseService.GetContentVersion();

        _config.LastRemoteETag = upload.ETag ?? "";
        _config.LastSyncTime = DateTimeOffset.Now;
        _config.Save();

        await PruneRemoteHistoryAsync(client, ct);
        _logger.LogInformation("Pushed snapshot {Size} bytes, plain hash {Hash}", payload.Length, plainHash);
        return CloudSyncResult.Ok(CloudSyncAction.Push, $"已上传（{payload.Length / 1024.0:F0} KB）");
    }


    private async Task<CloudSyncResult> PullInternalAsync(WebDavClient client, CancellationToken ct)
    {
        var (content, etag) = await client.DownloadAsync(RemoteFilePath, ct);
        if (content is null)
        {
            return CloudSyncResult.Skip("云端还没有备份");
        }

        string tempDatabase = Path.Combine(Path.GetTempPath(), "Arietta", $"merge_{Guid.NewGuid():N}.db");
        try
        {
            byte[] database = DatabaseSnapshot.Extract(content);
            Directory.CreateDirectory(Path.GetDirectoryName(tempDatabase)!);
            await File.WriteAllBytesAsync(tempDatabase, database, ct);
            if (!DatabaseService.IntegrityCheck(tempDatabase, out string message))
            {
                return CloudSyncResult.Fail(CloudSyncAction.Pull, $"云端备份已损坏：{message}");
            }

            string mergeMessage = DatabaseMergeService.MergeFromFile(tempDatabase, _config.Preference);
            SaveLocalBackup();

            _config.LastRemoteETag = etag ?? "";
            _config.LastLocalVersion = DatabaseService.GetContentVersion();
            _config.LastSyncTime = DateTimeOffset.Now;
            _config.Save();

            return CloudSyncResult.Ok(CloudSyncAction.Pull, mergeMessage);
        }
        catch (InvalidDataException ex)
        {
            return CloudSyncResult.Fail(CloudSyncAction.Pull, ex.Message, ex);
        }
        finally
        {
            DatabaseSnapshot.TryDelete(tempDatabase);
        }
    }

    private void SaveLocalBackup()
    {
        try
        {
            Directory.CreateDirectory(CloudSyncConfig.BackupFolder);
            string file = Path.Combine(CloudSyncConfig.BackupFolder, $"Arietta_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            DatabaseService.VacuumInto(file);
            var old = Directory.GetFiles(CloudSyncConfig.BackupFolder, "Arietta_*.db")
                .OrderByDescending(File.GetLastWriteTime)
                .Skip(MaxLocalBackup)
                .ToList();
            foreach (string path in old)
            {
                DatabaseSnapshot.TryDelete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create local backup failed");
        }
    }


    private async Task PruneRemoteHistoryAsync(WebDavClient client, CancellationToken ct)
    {
        try
        {
            var items = await client.ListAsync(RemoteHistoryFolder, ct);
            var files = items
                .Where(x => !x.Href.TrimEnd('/').EndsWith("history", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.LastModified ?? DateTimeOffset.MinValue)
                .Skip(MaxRemoteHistory)
                .ToList();
            foreach (var file in files)
            {
                string path = Uri.UnescapeDataString(file.Href);
                int index = path.IndexOf(RemoteHistoryFolder, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    path = path[(index + RemoteHistoryFolder.Length)..].TrimStart('/');
                    await client.DeleteAsync($"{RemoteHistoryFolder}/{path}", ct);
                }
            }
        }
        catch (Exception ex)
        {

            _logger.LogWarning(ex, "Prune remote history failed");
        }
    }


    private void Raise(CloudSyncResult result)
    {
        try
        {
            StatusChanged?.Invoke(this, result);
        }
        catch { }
    }

    #endregion
}
