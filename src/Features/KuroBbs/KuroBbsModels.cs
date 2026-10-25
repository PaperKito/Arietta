using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Arietta.Features.KuroBbs;

#region 通用返回包装（移植自 WutheringWavesTool wwt-kuro-api 的 ResponseBody）

public sealed class KuroBbsResponse<T>
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }

    [JsonIgnore] public bool IsSuccess => Code is 200 or 10902;

    [JsonIgnore] public bool IsTokenExpired => Code == 220
        || (Msg?.Contains("登录已过期") ?? false) || (Msg?.Contains("Token") ?? false);
}

#endregion

#region 我的角色 · 角色列表（roleData 接口 data.roleList）

public sealed class KuroRole
{
    [JsonPropertyName("roleId")] public int RoleId { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("chainUnlockNum")] public int ChainUnlockNum { get; set; }
    [JsonPropertyName("roleName")] public string? RoleName { get; set; }
    [JsonPropertyName("roleIconUrl")] public string? RoleIconUrl { get; set; }
    [JsonPropertyName("rolePicUrl")] public string? RolePicUrl { get; set; }
    [JsonPropertyName("starLevel")] public int StarLevel { get; set; }
}

#endregion

#region 我的角色 · 角色详情（getRoleDetail 接口 data）

public sealed class KuroRoleDetail
{
    [JsonPropertyName("role")] public KuroRole? Role { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("chainList")] public List<KuroChain>? ChainList { get; set; }
    [JsonPropertyName("weaponData")] public KuroWeaponData? WeaponData { get; set; }
    [JsonPropertyName("phantomData")] public KuroPhantomData? PhantomData { get; set; }
    [JsonPropertyName("skillList")] public List<KuroSkillData>? SkillList { get; set; }
    [JsonPropertyName("roleAttributeList")] public List<KuroRoleAttribute>? RoleAttributeList { get; set; }
}

public sealed class KuroChain
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    [JsonPropertyName("unlocked")] public bool Unlocked { get; set; }
}

public sealed class KuroWeaponData
{
    [JsonPropertyName("weapon")] public KuroWeapon? Weapon { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("breach")] public int Breach { get; set; }
}

public sealed class KuroWeapon
{
    [JsonPropertyName("weaponName")] public string? WeaponName { get; set; }
    [JsonPropertyName("weaponStarLevel")] public int WeaponStarLevel { get; set; }
    [JsonPropertyName("weaponIcon")] public string? WeaponIcon { get; set; }
    [JsonPropertyName("weaponEffectName")] public string? WeaponEffectName { get; set; }
    [JsonPropertyName("effectDescription")] public string? EffectDescription { get; set; }
}

public sealed class KuroPhantomData
{
    [JsonPropertyName("cost")] public int Cost { get; set; }
    [JsonPropertyName("equipPhantomList")] public List<KuroPhantom>? EquipPhantomList { get; set; }
}

public sealed class KuroPhantom
{
    [JsonPropertyName("phantomProp")] public KuroPhantomProp? PhantomProp { get; set; }
    [JsonPropertyName("cost")] public int Cost { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("fetterDetail")] public KuroFetterDetail? FetterDetail { get; set; }
    [JsonPropertyName("mainProps")] public List<KuroPhantomPropEntry>? MainProps { get; set; }
    [JsonPropertyName("subProps")] public List<KuroPhantomPropEntry>? SubProps { get; set; }
}

public sealed class KuroPhantomProp
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
}

public sealed class KuroPhantomPropEntry
{
    [JsonPropertyName("attributeName")] public string? AttributeName { get; set; }
    [JsonPropertyName("attributeValue")] public string? AttributeValue { get; set; }
    [JsonPropertyName("attributeMaxValue")] public double AttributeMaxValue { get; set; }
}

public sealed class KuroFetterDetail
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed class KuroSkillData
{
    [JsonPropertyName("skill")] public KuroSkill? Skill { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
}

public sealed class KuroSkill
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
}

public sealed class KuroRoleAttribute
{
    [JsonPropertyName("attributeName")] public string? AttributeName { get; set; }
    [JsonPropertyName("attributeValue")] public string? AttributeValue { get; set; }
}

#endregion

#region 资源简报（aki/resource/period/list + month|week|version）

public sealed class KuroBriefingTitle
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("index")] public int Index { get; set; }
}

public sealed class KuroBriefing
{
    [JsonPropertyName("weeks")] public List<KuroBriefingTitle>? Weeks { get; set; }
    [JsonPropertyName("months")] public List<KuroBriefingTitle>? Months { get; set; }
    [JsonPropertyName("versions")] public List<KuroBriefingTitle>? Versions { get; set; }
}

public sealed class KuroBriefingItem
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("num")] public long Num { get; set; }
}

public sealed class KuroWuwaWidgetData
{

    [JsonPropertyName("towerData")] public KuroWidgetItem? TowerData { get; set; }

    [JsonPropertyName("newTowerData")] public KuroWidgetItem? NewTowerData { get; set; }

    [JsonPropertyName("slashTowerData")] public KuroWidgetItem? SlashTowerData { get; set; }
}

public sealed class KuroWidgetItem
{
    [JsonPropertyName("refreshTimeStamp")] public long? RefreshTimeStamp { get; set; }
    [JsonPropertyName("cur")] public int Cur { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
}

public sealed class KuroBriefingRecord
{
    [JsonPropertyName("totalCoin")] public long TotalCoin { get; set; }
    [JsonPropertyName("totalStar")] public long TotalStar { get; set; }
    [JsonPropertyName("coinList")] public List<KuroBriefingItem>? CoinList { get; set; }
    [JsonPropertyName("starList")] public List<KuroBriefingItem>? StarList { get; set; }
}

#endregion

#region 养成计算器（aki/calculator/listRole | listWeapon）

public sealed class KuroCalculatorRole
{
    [JsonPropertyName("roleId")] public int RoleId { get; set; }
    [JsonPropertyName("roleName")] public string? RoleName { get; set; }
    [JsonPropertyName("roleIconUrl")] public string? RoleIconUrl { get; set; }
    [JsonPropertyName("acronym")] public string? Acronym { get; set; }
    [JsonPropertyName("isPreview")] public bool IsPreview { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
}

public sealed class KuroCalculatorRoleAim
{
    [JsonPropertyName("roleId")] public int RoleId { get; set; }
    [JsonPropertyName("roleStartLevel")] public int RoleStartLevel { get; set; } = 1;
    [JsonPropertyName("roleEndLevel")] public int RoleEndLevel { get; set; } = 90;
    [JsonPropertyName("weaponId")] public int WeaponId { get; set; }
    [JsonPropertyName("weaponStartLevel")] public int WeaponStartLevel { get; set; } = 1;
    [JsonPropertyName("weaponEndLevel")] public int WeaponEndLevel { get; set; } = 90;

    [JsonPropertyName("category")] public string Category { get; set; } = "all";
}

public sealed class KuroCalculatorResult
{
    [JsonPropertyName("costList")] public List<KuroCalculatorCostList>? CostList { get; set; }
}

public sealed class KuroCalculatorCostList
{
    [JsonPropertyName("allCost")] public List<KuroCalculatorCost>? AllCost { get; set; }
}

public sealed class KuroCalculatorCost
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
}

public sealed class KuroCalculatorWeapon
{
    [JsonPropertyName("weaponName")] public string? WeaponName { get; set; }
    [JsonPropertyName("weaponIcon")] public string? WeaponIcon { get; set; }
    [JsonPropertyName("acronym")] public string? Acronym { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
}

#endregion

#region 登录（sdkLogin / getSmsCodeForH5）

public sealed class KuroLoginResult
{
    public string Token { get; init; } = "";
    public string Did { get; init; } = "";

    public string HeadPhotoUrl { get; init; } = "";
}

#endregion
