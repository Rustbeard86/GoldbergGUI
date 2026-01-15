using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace GoldbergGUI.Core.Models;

public sealed record GoldbergGlobalConfiguration
{
    /// <summary>
    ///     Name of the user
    /// </summary>
    public required string AccountName { get; init; }

    /// <summary>
    ///     Steam64ID of the user
    /// </summary>
    public required long UserSteamId { get; init; }

    /// <summary>
    ///     language to be used
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    ///     Custom broadcast addresses (IPv4 or domain addresses)
    /// </summary>
    public List<string>? CustomBroadcastIps { get; init; }

    /// <summary>
    ///     Use experimental build of Goldberg emulator (GoldbergGUI application setting, not a Goldberg setting)
    /// </summary>
    public bool UseExperimental { get; init; }
}

public sealed record GoldbergConfiguration
{
    /// <summary>
    ///     App ID of the game
    /// </summary>
    public required int AppId { get; init; }

    /// <summary>
    ///     List of DLC
    /// </summary>
    public required List<DlcApp> DlcList { get; init; }

    public List<int>? Depots { get; init; }

    public List<Group>? SubscribedGroups { get; init; }

    //public List<AppPath> AppPaths { get; init; }

    public required List<Achievement> Achievements { get; init; }

    public List<Item>? Items { get; init; }

    public List<Leaderboard>? Leaderboards { get; init; }

    public List<Stat>? Stats { get; init; }

    // Add controller setting here!
    /// <summary>
    ///     Set offline mode.
    /// </summary>
    public required bool Offline { get; init; }

    /// <summary>
    ///     Disable networking (game is set to online, however all outgoing network connectivity will be disabled).
    /// </summary>
    public required bool DisableNetworking { get; init; }

    /// <summary>
    ///     Disable overlay (experimental only).
    /// </summary>
    public required bool DisableOverlay { get; init; }

    public GoldbergGlobalConfiguration? OverwrittenGlobalConfiguration { get; init; }
}

public class DlcApp : SteamApp
{
    public DlcApp()
    {
    }

    [SetsRequiredMembers]
    public DlcApp(SteamApp steamApp)
    {
        AppId = steamApp.AppId;
        Name = steamApp.Name;
        ComparableName = steamApp.ComparableName;
        AppType = steamApp.AppType;
        LastModified = steamApp.LastModified;
        PriceChangeNumber = steamApp.PriceChangeNumber;
    }

    /// <summary>
    ///     Path to DLC (relative to Steam API DLL) (optional)
    /// </summary>
    public string? AppPath { get; set; }
}

public sealed record Group
{
    /// <summary>
    ///     ID of group (https://steamcommunity.com/gid/103582791433980119/memberslistxml/?xml=1).
    /// </summary>
    public required int GroupId { get; init; }

    /// <summary>
    ///     Name of group.
    /// </summary>
    public required string GroupName { get; init; }

    /// <summary>
    ///     App ID of game associated with group (https://steamcommunity.com/games/218620/memberslistxml/?xml=1).
    /// </summary>
    public required int AppId { get; init; }
}

public sealed record Achievement
{
    /// <summary>
    ///     Achievement description (optional, some achievements don't have descriptions).
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Human readable name, as shown on webpage, game library, overlay, etc.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }

    /// <summary>
    ///     Is achievement hidden? 0 = false, else true.
    /// </summary>
    [JsonPropertyName("hidden")]
    public required int Hidden { get; init; }

    /// <summary>
    ///     Path to icon when unlocked (colored).
    /// </summary>
    [JsonPropertyName("icon")]
    public required string Icon { get; set; }

    /// <summary>
    ///     Path to icon when locked (grayed out).
    /// </summary>
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("icongray")]
    public required string IconGray { get; set; }

    /// <summary>
    ///     Internal name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public sealed record Item
{
    [JsonPropertyName("Timestamp")] public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("modified")] public required string Modified { get; init; }

    [JsonPropertyName("date_created")] public required string DateCreated { get; init; }

    [JsonPropertyName("type")] public required string Type { get; init; }

    [JsonPropertyName("display_type")] public required string DisplayType { get; init; }

    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("bundle")] public string? Bundle { get; init; }

    [JsonPropertyName("description")] public required string Description { get; init; }

    [JsonPropertyName("background_color")] public required string BackgroundColor { get; init; }

    [JsonPropertyName("icon_url")] public required Uri IconUrl { get; init; }

    [JsonPropertyName("icon_url_large")] public required Uri IconUrlLarge { get; init; }

    [JsonPropertyName("name_color")] public required string NameColor { get; init; }

    [JsonPropertyName("tradable")]
    // [JsonConverter(typeof(PurpleParseStringConverter))]
    public required bool Tradable { get; init; }

    [JsonPropertyName("marketable")]
    // [JsonConverter(typeof(PurpleParseStringConverter))]
    public required bool Marketable { get; init; }

    [JsonPropertyName("commodity")]
    // [JsonConverter(typeof(PurpleParseStringConverter))]
    public required bool Commodity { get; init; }

    [JsonPropertyName("drop_interval")]
    // [JsonConverter(typeof(FluffyParseStringConverter))]
    public required long DropInterval { get; init; }

    [JsonPropertyName("drop_max_per_window")]
    // [JsonConverter(typeof(FluffyParseStringConverter))]
    public required long DropMaxPerWindow { get; init; }

    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("workshopid")]
    // [JsonConverter(typeof(FluffyParseStringConverter))]
    public required long WorkshopId { get; init; }

    [JsonPropertyName("tw_unique_to_own")]
    // [JsonConverter(typeof(PurpleParseStringConverter))]
    public required bool TwUniqueToOwn { get; init; }

    [JsonPropertyName("item_quality")]
    // [JsonConverter(typeof(FluffyParseStringConverter))]
    public required long ItemQuality { get; init; }

    [JsonPropertyName("tw_price")] public string? TwPrice { get; init; }

    [JsonPropertyName("tw_type")] public string? TwType { get; init; }

    [JsonPropertyName("tw_client_visible")]
    // [JsonConverter(typeof(FluffyParseStringConverter))]
    public required long TwClientVisible { get; init; }

    [JsonPropertyName("tw_icon_small")] public string? TwIconSmall { get; init; }

    [JsonPropertyName("tw_icon_large")] public string? TwIconLarge { get; init; }

    [JsonPropertyName("tw_description")] public string? TwDescription { get; init; }

    [JsonPropertyName("tw_client_name")] public string? TwClientName { get; init; }

    [JsonPropertyName("tw_client_type")] public string? TwClientType { get; init; }

    [JsonPropertyName("tw_rarity")] public string? TwRarity { get; init; }
}

public sealed record Leaderboard
{
    public enum DisplayType
    {
        None,
        Numeric,
        TimeSeconds,
        TimeMilliseconds
    }

    public enum SortMethod
    {
        None,
        Ascending,
        Descending
    }

    public required string Name { get; init; }
    public required SortMethod SortMethodSetting { get; init; }
    public required DisplayType DisplayTypeSetting { get; init; }
}

public sealed record Stat
{
    public enum StatType
    {
        Int,
        Float,
        AvgRate
    }

    public required string Name { get; init; }
    public required StatType StatTypeSetting { get; init; }
    public required string Value { get; init; }
}