namespace GoldbergGUI.Core.Models;

/// <summary>
///     Complete application configuration (stored in app_config.json)
/// </summary>
public sealed record AppConfiguration
{
    /// <summary>
    ///     Default settings for game configurations
    /// </summary>
    public GuiDefaults GuiDefaults { get; init; } = new();

    /// <summary>
    ///     Goldberg emulator installation state
    /// </summary>
    public GoldbergState GoldbergState { get; init; } = new();

    /// <summary>
    ///     Steam database state
    /// </summary>
    public DatabaseState DatabaseState { get; init; } = new();
}

/// <summary>
///     Default settings that apply to new game configurations
/// </summary>
public sealed record GuiDefaults
{
    /// <summary>
    ///     Default account name to use for new game configurations
    /// </summary>
    public string AccountName { get; init; } = "Goldberg";

    /// <summary>
    ///     Default Steam64ID to use for new game configurations
    /// </summary>
    public long SteamId { get; init; } = 76561197960287930;

    /// <summary>
    ///     Default language to use for new game configurations
    /// </summary>
    public string Language { get; init; } = "english";

    /// <summary>
    ///     Default custom broadcast IPs to use for new game configurations
    /// </summary>
    public List<string>? CustomBroadcastIps { get; init; }

    /// <summary>
    ///     Use experimental build of Goldberg emulator
    /// </summary>
    public bool UseExperimental { get; init; }

    /// <summary>
    ///     Goldberg emulator update check frequency in hours. -1 = disabled, 0 = always check
    /// </summary>
    public int GoldbergUpdateCheckHours { get; init; } = 24;

    /// <summary>
    ///     Steam database update check frequency in hours. -1 = disabled, 0 = always check
    /// </summary>
    public int DatabaseUpdateCheckHours { get; init; } = 24;
}

/// <summary>
///     Goldberg emulator installation state
/// </summary>
public sealed record GoldbergState
{
    /// <summary>
    ///     Currently installed Goldberg version (release tag)
    /// </summary>
    public string? InstalledVersion { get; init; }

    /// <summary>
    ///     Last time we checked for Goldberg updates (UTC)
    /// </summary>
    public DateTime? LastUpdateCheck { get; init; }
}

/// <summary>
///     Steam database state
/// </summary>
public sealed record DatabaseState
{
    /// <summary>
    ///     Last time the Steam database was updated (UTC)
    /// </summary>
    public DateTime? LastUpdate { get; init; }
}