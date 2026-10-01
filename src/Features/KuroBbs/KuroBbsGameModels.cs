using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Arietta.Features.KuroBbs;

#region 签到（initSignInV2 / signIn/v2）


public sealed class KuroSignGood
{
    [JsonPropertyName("goodsId")] public long? GoodsId { get; set; }
    [JsonPropertyName("goodsName")] public string? GoodsName { get; set; }
    [JsonPropertyName("goodsNum")] public int? GoodsNum { get; set; }
    [JsonPropertyName("goodsUrl")] public string? GoodsUrl { get; set; }
    [JsonPropertyName("id")] public int? Id { get; set; }
    [JsonPropertyName("isGain")] public bool? IsGain { get; set; }
    [JsonPropertyName("serialNum")] public int? SerialNum { get; set; }
    [JsonPropertyName("signId")] public int? SignId { get; set; }

    
    [JsonIgnore] public bool IsSign { get; set; }
}


public sealed class KuroSignInitData
{
    [JsonPropertyName("isSigIn")] public bool IsSignIn { get; set; }
    [JsonPropertyName("sigInNum")] public int SignInNum { get; set; }
    [JsonPropertyName("signInGoodsConfigs")] public List<KuroSignGood>? SignInGoodsConfigs { get; set; }
}

#endregion

#region 逆境深塔（towerDataDetail）

public sealed class KuroTowerSimpleRole
{
    [JsonPropertyName("roleId")] public int? RoleId { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    
    
    [JsonPropertyName("skillBranchIndex")] public int? SkillBranchIndex { get; set; }
}

public sealed class KuroTowerFloor
{
    [JsonPropertyName("floor")] public int? Floor { get; set; }
    [JsonPropertyName("picUrl")] public string? PicUrl { get; set; }
    [JsonPropertyName("roleList")] public List<KuroTowerSimpleRole>? RoleList { get; set; }
    [JsonPropertyName("star")] public int Star { get; set; }
}

public sealed class KuroTowerArea
{
    [JsonPropertyName("areaId")] public int? AreaId { get; set; }
    [JsonPropertyName("areaName")] public string? AreaName { get; set; }
    [JsonPropertyName("floorList")] public List<KuroTowerFloor>? FloorList { get; set; }
    [JsonPropertyName("maxStar")] public int MaxStar { get; set; }
    [JsonPropertyName("star")] public int Star { get; set; }
}

public sealed class KuroTowerDifficulty
{
    [JsonPropertyName("difficulty")] public int Difficulty { get; set; }
    [JsonPropertyName("difficultyName")] public string? DifficultyName { get; set; }
    [JsonPropertyName("towerAreaList")] public List<KuroTowerArea>? TowerAreaList { get; set; }
}


public sealed class KuroTowerData
{
    [JsonPropertyName("difficultyList")] public List<KuroTowerDifficulty>? DifficultyList { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("seasonEndTime")] public long SeasonEndTime { get; set; }
}

#endregion

#region 终焉矩阵（newTowerDetail）

public sealed class KuroNewTowerBuff
{
    [JsonPropertyName("buffIcon")] public string? BuffIcon { get; set; }
    [JsonPropertyName("buffId")] public int BuffId { get; set; }
    [JsonPropertyName("buffName")] public string? BuffName { get; set; }
    [JsonPropertyName("desc")] public string? Desc { get; set; }
}

public sealed class KuroNewTowerRole
{
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("roleId")] public int RoleId { get; set; }
    [JsonPropertyName("skillBranchIndex")] public int? SkillBranchIndex { get; set; }
}

public sealed class KuroNewTowerTeam
{
    [JsonPropertyName("bossCount")] public int BossCount { get; set; }
    [JsonPropertyName("buffs")] public List<KuroNewTowerBuff>? Buffs { get; set; }
    [JsonPropertyName("passBoss")] public int PassBoss { get; set; }
    [JsonPropertyName("roleIcons")] public List<string>? RoleIcons { get; set; }
    [JsonPropertyName("roleList")] public List<KuroNewTowerRole>? RoleList { get; set; }
    [JsonPropertyName("round")] public int Round { get; set; }
    [JsonPropertyName("score")] public int Score { get; set; }
}

public sealed class KuroNewTowerModeDetail
{
    [JsonPropertyName("bossCount")] public int BossCount { get; set; }
    [JsonPropertyName("hasRecord")] public bool HasRecord { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("modeId")] public int ModeId { get; set; }
    [JsonPropertyName("passBoss")] public int PassBoss { get; set; }
    [JsonPropertyName("rank")] public int Rank { get; set; }
    [JsonPropertyName("round")] public int Round { get; set; }
    [JsonPropertyName("score")] public int Score { get; set; }
    [JsonPropertyName("teams")] public List<KuroNewTowerTeam>? Teams { get; set; }
}


public sealed class KuroNewTowerData
{
    [JsonPropertyName("endTime")] public long EndTime { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("modeDetails")] public List<KuroNewTowerModeDetail>? ModeDetails { get; set; }
    [JsonPropertyName("reward")] public int Reward { get; set; }
    [JsonPropertyName("totalReward")] public int TotalReward { get; set; }
}

#endregion

#region 冥歌海墟（slashDetail）

public sealed class KuroSlashHalf
{
    [JsonPropertyName("buffDescription")] public string? BuffDescription { get; set; }
    [JsonPropertyName("buffIcon")] public string? BuffIcon { get; set; }
    [JsonPropertyName("buffName")] public string? BuffName { get; set; }
    [JsonPropertyName("buffQuality")] public int BuffQuality { get; set; }
    [JsonPropertyName("roleList")] public List<KuroTowerSimpleRole>? RoleList { get; set; }
    [JsonPropertyName("score")] public int Score { get; set; }
}

public sealed class KuroSlashChallenge
{
    [JsonPropertyName("challengeId")] public int ChallengeId { get; set; }
    [JsonPropertyName("challengeName")] public string? ChallengeName { get; set; }
    [JsonPropertyName("halfList")] public List<KuroSlashHalf>? HalfList { get; set; }
    [JsonPropertyName("rank")] public string? Rank { get; set; }
    [JsonPropertyName("score")] public int Score { get; set; }
}

public sealed class KuroSlashDifficulty
{
    [JsonPropertyName("allScore")] public int AllScore { get; set; }
    [JsonPropertyName("challengeList")] public List<KuroSlashChallenge>? ChallengeList { get; set; }
    [JsonPropertyName("difficulty")] public int Difficulty { get; set; }
    [JsonPropertyName("difficultyName")] public string? DifficultyName { get; set; }
    [JsonPropertyName("maxScore")] public int MaxScore { get; set; }
}


public sealed class KuroSlashData
{
    [JsonPropertyName("difficultyList")] public List<KuroSlashDifficulty>? DifficultyList { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("seasonEndTime")] public long SeasonEndTime { get; set; }
}

#endregion
