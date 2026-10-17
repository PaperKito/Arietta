using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI;
using Arietta.Localization;

namespace Arietta.Features.KuroBbs;

#region 账号概览（haru/roleBox/accountData）

public sealed class KuroPgrAccountData
{
    [JsonPropertyName("roleId")] public string? RoleId { get; set; }
    [JsonPropertyName("roleName")] public string? RoleName { get; set; }
    [JsonPropertyName("serverName")] public string? ServerName { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("headIconUrl")] public string? HeadIconUrl { get; set; }
}

#endregion

#region 角色（haru/roleBox/roleIndex）

public sealed class KuroPgrRoleIndex
{
    [JsonPropertyName("show")] public bool Show { get; set; }
    [JsonPropertyName("characterList")] public List<KuroPgrCharacter>? CharacterList { get; set; }
    [JsonPropertyName("showRoleIdList")] public List<long>? ShowRoleIdList { get; set; }
}

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class KuroPgrCharacter
{
    [JsonPropertyName("bodyId")] public long BodyId { get; set; }
    [JsonPropertyName("bodyName")] public string? BodyName { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("element")] public string? Element { get; set; }
    [JsonPropertyName("effect")] public string? Effect { get; set; }

    [JsonPropertyName("quality")] public JsonElement? Quality { get; set; }

    [JsonIgnore] public int QualityValue => Quality?.ValueKind == JsonValueKind.Number && Quality.Value.TryGetInt32(out var q) ? q : 0;
    [JsonPropertyName("grade")] public string? Grade { get; set; }
    [JsonPropertyName("fightAbility")] public int FightAbility { get; set; }
    [JsonPropertyName("level")] public int? Level { get; set; }
    [JsonPropertyName("roleRank")] public string? RoleRank { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
    [JsonPropertyName("weaponType")] public int WeaponType { get; set; }

    [JsonIgnore] public string ElementText => string.IsNullOrEmpty(Element) ? "" : Element!;
    [JsonIgnore] public string GradeText => string.IsNullOrEmpty(Grade) ? (RoleRank ?? "") : Grade!;
    [JsonIgnore] public string FightText => string.Format(Lang.Instance.PgrRole_Fight, FightAbility);

    [JsonIgnore]
    public Brush RankBrush => new SolidColorBrush(PgrRankColor(RoleRank));

    private static Color PgrRankColor(string? rank) => rank?.Trim().ToUpperInvariant() switch
    {
        "A" or "B" => Color.FromArgb(0xFF, 0xFD, 0x9C, 0x34),
        "S" => Color.FromArgb(0xFF, 0xE3, 0x3B, 0x00),
        _ => Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
    };
}

#endregion

#region 资源月报（haru/resource/halfOfYear）

public sealed class KuroPgrResourceReport
{
    [JsonPropertyName("totalBlackCard")] public int TotalBlackCard { get; set; }
    [JsonPropertyName("totalDevelopResource")] public int TotalDevelopResource { get; set; }
    [JsonPropertyName("totalTradeCredit")] public int TotalTradeCredit { get; set; }
    [JsonPropertyName("perMonthList")] public List<KuroPgrResourceMonth>? PerMonthList { get; set; }
}

public sealed class KuroPgrResourceMonth
{
    [JsonPropertyName("currentYear")] public int CurrentYear { get; set; }
    [JsonPropertyName("currentMonth")] public int CurrentMonth { get; set; }

    [JsonPropertyName("month")] public string? Month { get; set; }
    [JsonPropertyName("monthBlackCard")] public int MonthBlackCard { get; set; }
    [JsonPropertyName("monthDevelopResource")] public int MonthDevelopResource { get; set; }
    [JsonPropertyName("monthTradeCredit")] public int MonthTradeCredit { get; set; }

    [JsonPropertyName("isHighest")] public bool IsHighest { get; set; }
}

#endregion

#region 幻痛囚笼（haru/roleBox/prisonerCage）

public sealed class KuroPgrCageData
{
    [JsonPropertyName("show")] public bool Show { get; set; }
    [JsonPropertyName("isOpen")] public bool IsOpen { get; set; }
    [JsonPropertyName("isConfirm")] public bool IsConfirm { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }

    [JsonPropertyName("chooseArea")] public bool ChooseArea { get; set; }
    [JsonPropertyName("challengeArea")] public string? ChallengeArea { get; set; }
    [JsonPropertyName("challengeLevel")] public string? ChallengeLevel { get; set; }

    [JsonPropertyName("prisonerCage")] public KuroPgrCageInfo? PrisonerCage { get; set; }

    [JsonPropertyName("fightHardZone")] public JsonElement? FightHardZone { get; set; }
}

public sealed class KuroPgrCageInfo
{
    [JsonPropertyName("totalPoint")] public int TotalPoint { get; set; }
    [JsonPropertyName("totalChallengeTimes")] public int TotalChallengeTimes { get; set; }
    [JsonPropertyName("bossFightInfoList")] public List<KuroPgrCageBossFight>? BossFightInfoList { get; set; }
}

public sealed class KuroPgrCageBossFight
{
    [JsonPropertyName("totalPoint")] public int TotalPoint { get; set; }
    [JsonPropertyName("totalNum")] public int TotalNum { get; set; }
    [JsonPropertyName("boss")] public KuroPgrCageBoss? Boss { get; set; }

    [JsonPropertyName("stageInfoList")] public List<KuroPgrCageStage>? StageInfoList { get; set; }
}

public sealed class KuroPgrCageBoss
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("bossId")] public int BossId { get; set; }
    [JsonPropertyName("weaknessList")] public List<KuroPgrWeakness>? WeaknessList { get; set; }
}

public sealed class KuroPgrWeakness
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("icon")] public string? Icon { get; set; }
    [JsonPropertyName("type")] public int Type { get; set; }
}

public sealed class KuroPgrCageStage
{
    [JsonPropertyName("stageName")] public string? StageName { get; set; }
    [JsonPropertyName("point")] public int Point { get; set; }

    [JsonPropertyName("fightTime")] public int FightTime { get; set; }

    [JsonPropertyName("bodyList")] public List<KuroPgrWarAreaBodyEntry>? BodyList { get; set; }
}

#endregion

#region 纷争战区（haru/roleBox/area）

public sealed class KuroPgrWarAreaData
{
    [JsonPropertyName("show")] public bool Show { get; set; }
    [JsonPropertyName("isOpen")] public bool IsOpen { get; set; }
    [JsonPropertyName("isConfirm")] public bool IsConfirm { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("groupName")] public string? GroupName { get; set; }
    [JsonPropertyName("groupLevel")] public string? GroupLevel { get; set; }
    [JsonPropertyName("areaInfo")] public KuroPgrAreaInfo? AreaInfo { get; set; }
}

public sealed class KuroPgrAreaInfo
{
    [JsonPropertyName("totalPoint")] public int? TotalPoint { get; set; }
    [JsonPropertyName("totalChallengeTimes")] public int? TotalChallengeTimes { get; set; }
    [JsonPropertyName("stageFightInfoList")] public List<KuroPgrStageFightInfo>? StageFightInfoList { get; set; }
}

public sealed class KuroPgrStageFightInfo
{
    [JsonPropertyName("stageId")] public int StageId { get; set; }
    [JsonPropertyName("stageName")] public string? StageName { get; set; }
    [JsonPropertyName("stageIconUrl")] public string? StageIconUrl { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("point")] public int? Point { get; set; }
    [JsonPropertyName("npcGroup")] public int? NpcGroup { get; set; }
    [JsonPropertyName("totalNum")] public int? TotalNum { get; set; }
    [JsonPropertyName("areaBuffFightInfoList")] public List<KuroPgrBuffFightInfo>? AreaBuffFightInfoList { get; set; }
}

public sealed class KuroPgrBuffFightInfo
{
    [JsonPropertyName("buffId")] public int BuffId { get; set; }

    [JsonPropertyName("buffName")] public string? BuffName { get; set; }
    [JsonPropertyName("buffIconUrl")] public string? BuffIconUrl { get; set; }
    [JsonPropertyName("supportBuffList")] public List<KuroPgrSupportBuff>? SupportBuffList { get; set; }
    [JsonPropertyName("point")] public int? Point { get; set; }
    [JsonPropertyName("npcGroup")] public int? NpcGroup { get; set; }
    [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
    [JsonPropertyName("bodyList")] public List<KuroPgrWarAreaBodyEntry>? BodyList { get; set; }
    [JsonPropertyName("fightTime")] public long? FightTime { get; set; }
    [JsonPropertyName("totalNum")] public int? TotalNum { get; set; }
}

public sealed class KuroPgrWarAreaBodyEntry
{
    [JsonPropertyName("auto")] public bool Auto { get; set; }
    [JsonPropertyName("bodyInfo")] public KuroPgrWarAreaBodyInfo? BodyInfo { get; set; }

    [JsonIgnore] public string? Grade => BodyInfo?.Grade;
    [JsonIgnore] public string? IconUrl => BodyInfo?.Body?.IconUrl;
    [JsonIgnore] public string? BodyName => BodyInfo?.Body?.BodyName;
}

public sealed class KuroPgrWarAreaBodyInfo
{
    [JsonPropertyName("grade")] public string? Grade { get; set; }
    [JsonPropertyName("body")] public KuroPgrDetailBody? Body { get; set; }
}

public sealed class KuroPgrSupportBuff
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("supportBuffId")] public int SupportBuffId { get; set; }
}

#endregion

#region 历战映射（haru/roleBox/transfinite）

public sealed class KuroPgrTransfiniteData
{
    [JsonPropertyName("show")] public bool Show { get; set; }
    [JsonPropertyName("isOpen")] public bool IsOpen { get; set; }
    [JsonPropertyName("isConfirm")] public bool IsConfirm { get; set; }
    [JsonPropertyName("isUnlock")] public bool IsUnlock { get; set; }
    [JsonPropertyName("challengeArea")] public string? ChallengeArea { get; set; }
    [JsonPropertyName("challengeLevel")] public string? ChallengeLevel { get; set; }
    [JsonPropertyName("bossIconUrl")] public string? BossIconUrl { get; set; }

    [JsonPropertyName("operatorCount")] public int OperatorCount { get; set; }

    [JsonPropertyName("process")] public int Process { get; set; }
    [JsonPropertyName("fightTime")] public long FightTime { get; set; }
    [JsonPropertyName("characterList")] public List<KuroPgrCharacter>? CharacterList { get; set; }
}

#endregion


#region 角色深层详情（haru/roleBox/roleDetail）

public sealed class KuroPgrRoleDetailData
{
    [JsonPropertyName("character")] public KuroPgrCharacterDetail? Character { get; set; }
    [JsonPropertyName("show")] public bool Show { get; set; }
}

public sealed class KuroPgrCharacterDetail
{
    [JsonPropertyName("body")] public KuroPgrDetailBody? Body { get; set; }

    [JsonPropertyName("quality")] public int Quality { get; set; }

    [JsonPropertyName("grade")] public string? Grade { get; set; }
    [JsonPropertyName("fightAbility")] public int FightAbility { get; set; }
    [JsonPropertyName("weaponInfo")] public KuroPgrWeaponInfo? WeaponInfo { get; set; }

    [JsonPropertyName("partner")] public KuroPgrPartnerInfo? Partner { get; set; }

    [JsonPropertyName("chipSuitList")] public List<KuroPgrChipSuit>? ChipSuitList { get; set; }

    [JsonPropertyName("chipResonanceList")] public List<KuroPgrChipResonance>? ChipResonanceList { get; set; }

    [JsonPropertyName("chipExDamage")] public string? ChipExDamage { get; set; }
}

public sealed class KuroPgrDetailBody
{
    [JsonPropertyName("bodyId")] public long BodyId { get; set; }

    [JsonPropertyName("roleName")] public string? RoleName { get; set; }

    [JsonPropertyName("bodyName")] public string? BodyName { get; set; }
    [JsonPropertyName("careerId")] public int? CareerId { get; set; }

    [JsonPropertyName("career")] public string? Career { get; set; }
    [JsonPropertyName("isNewRole")] public int? IsNewRole { get; set; }

    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }

    [JsonPropertyName("imgUrl")] public string? ImgUrl { get; set; }

    [JsonPropertyName("element")] public string? Element { get; set; }

    [JsonPropertyName("elementDetail")] public string? ElementDetail { get; set; }

    [JsonPropertyName("effect")] public string? Effect { get; set; }

    [JsonPropertyName("wikiLink")] public string? WikiLink { get; set; }
    [JsonPropertyName("roleRank")] public string? RoleRank { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
    [JsonPropertyName("weaponType")] public int WeaponType { get; set; }

    [JsonPropertyName("overrunState")] public int? OverrunState { get; set; }
}

public sealed class KuroPgrWeaponInfo
{
    [JsonPropertyName("weapon")] public KuroPgrWeapon? Weapon { get; set; }
    [JsonPropertyName("overRunLevel")] public int? OverRunLevel { get; set; }
    [JsonPropertyName("maxOverRunLevel")] public int? MaxOverRunLevel { get; set; }
    [JsonPropertyName("quality")] public int? Quality { get; set; }

    [JsonPropertyName("suit")] public KuroPgrWeaponSuit? Suit { get; set; }

    [JsonPropertyName("weaponOverrun2")] public KuroPgrWeaponOverrun2? WeaponOverrun2 { get; set; }
    [JsonPropertyName("resonanceList")] public List<KuroPgrResonance>? ResonanceList { get; set; }
}

public sealed class KuroPgrWeapon
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("weaponId")] public long WeaponId { get; set; }
    [JsonPropertyName("skillName")] public string? SkillName { get; set; }
    [JsonPropertyName("skillDescription")] public string? SkillDescription { get; set; }
}

public sealed class KuroPgrWeaponSuit
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("suitId")] public long SuitId { get; set; }
    [JsonPropertyName("skillDescriptionTwo")] public string? SkillDescriptionTwo { get; set; }
    [JsonPropertyName("skillDescriptionFour")] public string? SkillDescriptionFour { get; set; }
    [JsonPropertyName("skillDescriptionSix")] public string? SkillDescriptionSix { get; set; }
}

public sealed class KuroPgrWeaponOverrun2
{
    [JsonPropertyName("characterId")] public long? CharacterId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("desc")] public string? Desc { get; set; }
}

public sealed class KuroPgrResonance
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("skillId")] public long SkillId { get; set; }
    [JsonPropertyName("skillDescription")] public string? SkillDescription { get; set; }
}

public sealed class KuroPgrPartnerInfo
{
    [JsonPropertyName("partner")] public KuroPgrPartner? Partner { get; set; }
    [JsonPropertyName("level")] public int? Level { get; set; }
    [JsonPropertyName("breakThrough")] public int? BreakThrough { get; set; }

    [JsonPropertyName("grade")] public string? Grade { get; set; }
    [JsonPropertyName("quality")] public int? Quality { get; set; }
    [JsonPropertyName("skillList")] public List<KuroPgrPartnerSkill>? SkillList { get; set; }
}

public sealed class KuroPgrPartner
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("partnerId")] public long PartnerId { get; set; }
    [JsonPropertyName("grade")] public int? Grade { get; set; }
    [JsonPropertyName("gradeStr")] public string? GradeStr { get; set; }
}

public sealed class KuroPgrPartnerSkill
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("level")] public int? Level { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
}

public sealed class KuroPgrChipSuit
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("suitId")] public long SuitId { get; set; }

    [JsonPropertyName("num")] public int? Num { get; set; }
    [JsonPropertyName("descriptionTwo")] public string? DescriptionTwo { get; set; }
    [JsonPropertyName("descriptionFour")] public string? DescriptionFour { get; set; }
    [JsonPropertyName("descriptionSix")] public string? DescriptionSix { get; set; }
}

public sealed class KuroPgrChipResonance
{
    [JsonPropertyName("chipIconUrl")] public string? ChipIconUrl { get; set; }

    [JsonPropertyName("site")] public int? Site { get; set; }
    [JsonPropertyName("chipName")] public string? ChipName { get; set; }
    [JsonPropertyName("defend")] public bool? Defend { get; set; }
    [JsonPropertyName("superSlotIconUrl")] public string? SuperSlotIconUrl { get; set; }

    [JsonPropertyName("superAwake")] public bool? SuperAwake { get; set; }

    [JsonPropertyName("superDescription")] public string? SuperDescription { get; set; }
    [JsonPropertyName("subSlotIconUrl")] public string? SubSlotIconUrl { get; set; }

    [JsonPropertyName("subAwake")] public bool? SubAwake { get; set; }

    [JsonPropertyName("subDescription")] public string? SubDescription { get; set; }
}

#endregion
