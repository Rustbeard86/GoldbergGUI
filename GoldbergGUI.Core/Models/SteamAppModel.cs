using System.Text.Json.Serialization;

namespace GoldbergGUI.Core.Models;

/// <summary>
///     Represents a Steam application (game or DLC)
/// </summary>
public class SteamApp
{
    [JsonPropertyName("appid")] public int AppId { get; init; }

    [JsonPropertyName("name")] public required string Name { get; set; }

    public string ComparableName { get; set; } = string.Empty;

    public string AppType { get; set; } = string.Empty;

    [JsonPropertyName("last_modified")] public long LastModified { get; init; }

    [JsonPropertyName("price_change_number")]
    public long PriceChangeNumber { get; init; }

    public override string ToString()
    {
        return $"{AppId}={Name}";
    }
}

/// <summary>
///     Response container for paginated Steam app list
/// </summary>
public sealed record AppList
{
    [JsonPropertyName("apps")] public required List<SteamApp> Apps { get; init; }

    [JsonPropertyName("have_more_results")]
    public bool HaveMoreResults { get; init; }

    [JsonPropertyName("last_appid")] public long LastAppid { get; init; }
}

/// <summary>
///     Base class for Steam API response wrapper
/// </summary>
public abstract class SteamApps
{
    public abstract AppList? AppList { get; init; }
}

/// <summary>
///     Steam API v2 response format
/// </summary>
public sealed class SteamAppsV2 : SteamApps
{
    [JsonPropertyName("applist")] public override required AppList? AppList { get; init; }
}

/// <summary>
///     Steam API v1 response format
/// </summary>
public sealed class SteamAppsV1 : SteamApps
{
    [JsonPropertyName("response")] public override required AppList? AppList { get; init; }
}