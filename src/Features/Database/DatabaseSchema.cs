using System.Collections.Generic;

namespace Arietta.Features.Database;

internal static class DatabaseSchema
{

    public static readonly string[] HostSpecificKeyPrefixes =
    [
        "host_",
        "DeviceId",
        "WindowSizeMode",
        "WindowPlacement",
        "GameInstallPath",
        "LogLevel",
    ];

    public static class Tables
    {
        public const string Setting = "Setting";
        public const string PlayTimeItem = "PlayTimeItem";
        public const string PlayTimeStats = "PlayTimeStats";
        public const string GachaLogItem = "GachaLogItem";
        public const string CombatRecord = "CombatRecord";
        public const string ResourceRecord = "ResourceRecord";
        public const string TowerFloorRecord = "TowerFloorRecord";
    }

    public static readonly IReadOnlyList<string> Migrations = [Sql_v1, Sql_v2, Sql_v3, Sql_v4, Sql_v5, Sql_v6];

    private const string Sql_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS Setting
        (
            Key        TEXT    NOT NULL PRIMARY KEY,
            Value      TEXT,
            UpdateTime INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS PlayTimeItem
        (
            TimeStamp INTEGER NOT NULL PRIMARY KEY,
            GameBiz   TEXT    NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL DEFAULT 0,
            Message   TEXT
        );
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem (GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem (Pid);

        CREATE TABLE IF NOT EXISTS PlayTimeStats
        (
            Id           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            GameBiz      TEXT    NOT NULL,
            Pid          INTEGER NOT NULL,
            StartTime    INTEGER NOT NULL,
            EndTime      INTEGER NOT NULL,
            Interruption INTEGER NOT NULL DEFAULT 0,
            Type         INTEGER NOT NULL DEFAULT 0
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_PlayTimeStats_Key ON PlayTimeStats (GameBiz, StartTime, Pid);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeStats_GameBiz ON PlayTimeStats (GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeStats_StartTime ON PlayTimeStats (StartTime);

        CREATE TABLE IF NOT EXISTS GachaLogItem
        (
            PlayerId     TEXT    NOT NULL,
            GachaName    TEXT    NOT NULL,
            CardPoolType TEXT,
            ResourceId   INTEGER NOT NULL,
            QualityLevel INTEGER NOT NULL,
            ResourceType TEXT,
            Name         TEXT,
            Count        INTEGER NOT NULL DEFAULT 1,
            Time         INTEGER NOT NULL,
            PRIMARY KEY (PlayerId, GachaName, ResourceId, Time)
        );
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_PlayerId ON GachaLogItem (PlayerId);
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_Time ON GachaLogItem (Time);

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v2 = """
        BEGIN TRANSACTION;

        DROP TABLE IF EXISTS GachaLogItem;

        CREATE TABLE GachaLogItem
        (
            PlayerId     TEXT    NOT NULL,
            CardPoolType TEXT    NOT NULL,
            ResourceId   INTEGER NOT NULL,
            QualityLevel INTEGER NOT NULL,
            ResourceType TEXT,
            Name         TEXT,
            Count        INTEGER NOT NULL DEFAULT 1,
            Time         TEXT    NOT NULL,
            SubSeq       INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (PlayerId, CardPoolType, ResourceId, Time, SubSeq)
        );
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_PlayerId ON GachaLogItem (PlayerId);
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_Time ON GachaLogItem (Time);

        PRAGMA USER_VERSION = 2;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v3 = """
        BEGIN TRANSACTION;

        ALTER TABLE PlayTimeItem RENAME TO PlayTimeItem_Old;

        CREATE TABLE PlayTimeItem
        (
            TimeStamp INTEGER NOT NULL,
            GameBiz   TEXT    NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL DEFAULT 0,
            Message   TEXT,
            PRIMARY KEY (GameBiz, Pid, TimeStamp)
        );

        INSERT OR IGNORE INTO PlayTimeItem (TimeStamp, GameBiz, Pid, State, CursorPos, Message)
            SELECT TimeStamp, GameBiz, Pid, State, CursorPos, Message FROM PlayTimeItem_Old;

        DROP TABLE PlayTimeItem_Old;

        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem (GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem (Pid);

        PRAGMA USER_VERSION = 3;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v4 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS CombatRecord
        (
            Mode       TEXT    NOT NULL,
            RoleId     TEXT    NOT NULL,
            PeriodEnd  INTEGER NOT NULL,
            Star       INTEGER NOT NULL DEFAULT 0,
            MaxStar    INTEGER NOT NULL DEFAULT 0,
            Score      INTEGER NOT NULL DEFAULT 0,
            Detail     TEXT    NOT NULL,
            Chain      TEXT,
            UpdateTime INTEGER NOT NULL,
            PRIMARY KEY (Mode, RoleId, PeriodEnd)
        );
        CREATE INDEX IF NOT EXISTS IX_CombatRecord_Mode_Role ON CombatRecord (Mode, RoleId);

        PRAGMA USER_VERSION = 4;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v5 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS ResourceRecord
        (
            GameBiz     TEXT    NOT NULL,
            RoleId      TEXT    NOT NULL,
            PeriodType  TEXT    NOT NULL,
            PeriodIndex INTEGER NOT NULL,
            Title       TEXT,
            DataJson    TEXT,
            UpdateTime  INTEGER NOT NULL,
            PRIMARY KEY (GameBiz, RoleId, PeriodType, PeriodIndex)
        );

        PRAGMA USER_VERSION = 5;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v6 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS TowerFloorRecord
        (
            Mode       TEXT    NOT NULL,
            RoleId     TEXT    NOT NULL,
            PeriodEnd  INTEGER NOT NULL,
            Difficulty INTEGER NOT NULL,
            AreaId     INTEGER NOT NULL,
            AreaName   TEXT,
            Floor      INTEGER NOT NULL,
            Star       INTEGER NOT NULL DEFAULT 0,
            Roles      TEXT,
            UpdateTime INTEGER NOT NULL,
            PRIMARY KEY (Mode, RoleId, PeriodEnd, Difficulty, AreaId, Floor)
        );
        CREATE INDEX IF NOT EXISTS IX_TowerFloorRecord_Mode_Role ON TowerFloorRecord (Mode, RoleId);

        PRAGMA USER_VERSION = 6;
        COMMIT TRANSACTION;
        """;
}
