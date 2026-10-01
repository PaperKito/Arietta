using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Arietta.Features.Database;












internal static class DatabaseService
{
    
    public const string DatabaseFileName = "Arietta.db";

    
    public const string ConfigKey_UserDataFolder = "UserDataFolder";

    private static readonly object _lock = new();

    private static string? _databasePath;
    private static bool _initialized;

    static DatabaseService()
    {
        SqlMapper.AddTypeHandler(new DapperSqlMapper.GameBizHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.DateTimeOffsetHandler());
    }

    
    public static string DatabasePath
    {
        get
        {
            lock (_lock)
            {
                return _databasePath ??= ResolveDatabasePath();
            }
        }
    }

    
    public static string DatabaseFolder => Path.GetDirectoryName(DatabasePath)!;

    private static string ResolveDatabasePath()
    {
        string? custom = AppConfig.GetConfig(ConfigKey_UserDataFolder);
        string folder = string.IsNullOrWhiteSpace(custom) ? AppConfig.LocalFolder : custom;
        string path = Path.Combine(folder, DatabaseFileName);
        return path;
    }

    
    public static SqliteConnection CreateConnection()
    {
        EnsureDatabaseCreated();
        var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False;");
        connection.Open();
        return connection;
    }

    
    public static void EnsureDatabaseCreated()
    {
        if (_initialized)
        {
            return;
        }
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            Directory.CreateDirectory(DatabaseFolder);
            using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False;");
            connection.Open();
            ApplyJournalMode(connection);
            int version = connection.QueryFirstOrDefault<int>("PRAGMA user_version;");
            for (int i = version; i < DatabaseSchema.Migrations.Count; i++)
            {
                connection.Execute(DatabaseSchema.Migrations[i]);
            }
            _initialized = true;
        }
    }

    
    public static void Reload()
    {
        lock (_lock)
        {
            _databasePath = null;
            _initialized = false;
        }
        EnsureDatabaseCreated();
    }

    private static void ApplyJournalMode(SqliteConnection connection)
    {
        try
        {
            connection.Execute("PRAGMA journal_mode = WAL;");
            connection.Execute("PRAGMA synchronous = NORMAL;");
        }
        catch
        {
            
            try
            {
                connection.Execute("PRAGMA journal_mode = DELETE;");
            }
            catch { }
        }
    }

    #region 快照与替换

    
    
    
    
    
    
    public static void VacuumInto(string targetFile, bool stripHostSpecificData = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        DeleteIfExists(targetFile);

        using (var connection = CreateConnection())
        {
            connection.Execute($"VACUUM INTO '{Escape(targetFile)}';");
        }

        
        using (var snapshot = new SqliteConnection($"Data Source={targetFile};Pooling=False;"))
        {
            snapshot.Open();
            snapshot.Execute("PRAGMA journal_mode = DELETE;");
            if (stripHostSpecificData)
            {
                foreach (string prefix in DatabaseSchema.HostSpecificKeyPrefixes)
                {
                    snapshot.Execute($"DELETE FROM {DatabaseSchema.Tables.Setting} WHERE Key LIKE @prefix;",
                        new { prefix = prefix + "%" });
                }
            }
        }

        DeleteIfExists(targetFile + "-wal");
        DeleteIfExists(targetFile + "-shm");
        NormalizeFileHeader(targetFile);
    }

    
    
    
    
    private static void NormalizeFileHeader(string databaseFile)
    {
        try
        {
            using var stream = new FileStream(databaseFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            if (stream.Length < 100)
            {
                return;
            }
            stream.Position = 18;
            int write = stream.ReadByte();
            int read = stream.ReadByte();
            if (write != 1 || read != 1)
            {
                stream.Position = 18;
                stream.WriteByte(1);
                stream.WriteByte(1);
            }
        }
        catch { }
    }

    
    public static void ReplaceDatabase(string snapshotFile)
    {
        lock (_lock)
        {
            SqliteConnection.ClearAllPools();
            string target = DatabasePath;
            string wal = target + "-wal";
            string shm = target + "-shm";

            
            DeleteIfExists(wal);
            DeleteIfExists(shm);
            File.Copy(snapshotFile, target, overwrite: true);
            DeleteIfExists(wal);
            DeleteIfExists(shm);
            _initialized = false;
        }
        EnsureDatabaseCreated();
    }

    
    public static bool IntegrityCheck(string databaseFile, out string message)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={databaseFile};Pooling=False;");
            connection.Open();
            message = connection.QueryFirstOrDefault<string>("PRAGMA integrity_check;") ?? "";
            return string.Equals(message, "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    private static string Escape(string path) => path.Replace("'", "''");

    private static void DeleteIfExists(string file)
    {
        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch { }
    }

    #endregion

    #region 数据指纹

    
    
    
    
    
    
    
    
    public static string GetContentVersion()
    {
        try
        {
            using var connection = CreateConnection();
            string? version = connection.ExecuteScalar<string>($"""
                SELECT
                  (SELECT COUNT(*) FROM {DatabaseSchema.Tables.PlayTimeItem})
                  || '-' || (SELECT IFNULL(MAX(TimeStamp), 0) FROM {DatabaseSchema.Tables.PlayTimeItem})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.PlayTimeStats})
                  || '-' || (SELECT IFNULL(MAX(StartTime), 0) FROM {DatabaseSchema.Tables.PlayTimeStats})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.GachaLogItem})
                  || '-' || (SELECT IFNULL(MAX(Time), 0) FROM {DatabaseSchema.Tables.GachaLogItem})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.Setting})
                  || '-' || (SELECT IFNULL(MAX(UpdateTime), 0) FROM {DatabaseSchema.Tables.Setting})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.ResourceRecord})
                  || '-' || (SELECT IFNULL(MAX(UpdateTime), 0) FROM {DatabaseSchema.Tables.ResourceRecord})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.CombatRecord})
                  || '-' || (SELECT IFNULL(MAX(UpdateTime), 0) FROM {DatabaseSchema.Tables.CombatRecord})
                  || '-' || (SELECT COUNT(*) FROM {DatabaseSchema.Tables.TowerFloorRecord})
                  || '-' || (SELECT IFNULL(MAX(UpdateTime), 0) FROM {DatabaseSchema.Tables.TowerFloorRecord});
                """);
            return version ?? "";
        }
        catch
        {
            
            
            return Guid.NewGuid().ToString("N");
        }
    }

    #endregion

    #region Setting 读写

    
    public static T? GetValue<T>(string key, out bool found)
    {
        found = false;
        try
        {
            using var connection = CreateConnection();
            string? value = connection.QueryFirstOrDefault<string>(
                $"SELECT Value FROM {DatabaseSchema.Tables.Setting} WHERE Key = @key LIMIT 1;", new { key });
            if (value is null)
            {
                return default;
            }
            found = true;
            return ParseValue<T>(value);
        }
        catch
        {
            return default;
        }
    }

    
    public static T? GetValue<T>(string key, T? defaultValue = default)
    {
        T? value = GetValue<T>(key, out bool found);
        return found ? value : defaultValue;
    }

    
    public static void SetValue<T>(string key, T value, DateTimeOffset? time = null)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Execute(
                $"INSERT INTO {DatabaseSchema.Tables.Setting} (Key, Value, UpdateTime) VALUES (@Key, @Value, @UpdateTime) " +
                "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value, UpdateTime = excluded.UpdateTime;",
                new
                {
                    Key = key,
                    Value = ToText(value),
                    UpdateTime = (time ?? DateTimeOffset.Now).ToUnixTimeMilliseconds(),
                });
        }
        catch { }
    }

    
    public static void RemoveValue(string key)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Execute($"DELETE FROM {DatabaseSchema.Tables.Setting} WHERE Key = @key;", new { key });
        }
        catch { }
    }

    
    public static DateTimeOffset? GetUpdateTime(string key)
    {
        try
        {
            using var connection = CreateConnection();
            long? time = connection.QueryFirstOrDefault<long?>(
                $"SELECT UpdateTime FROM {DatabaseSchema.Tables.Setting} WHERE Key = @key LIMIT 1;", new { key });
            return time is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(time.Value);
        }
        catch
        {
            return null;
        }
    }

    private static string? ToText<T>(T value)
    {
        return value switch
        {
            null => null,
            string s => s,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    private static T? ParseValue<T>(string value)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
        if (!converter.CanConvertFrom(typeof(string)))
        {
            return default;
        }
        try
        {
            object? result = converter.ConvertFromInvariantString(value);
            return result is T typed ? typed : default;
        }
        catch
        {
            return default;
        }
    }

    #endregion
}
