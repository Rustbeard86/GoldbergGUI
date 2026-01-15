namespace GoldbergGUI.Core.Models;

/// <summary>
///     GoldbergGUI application settings (stored locally, not used by Goldberg emulator)
/// </summary>
public sealed record GuiSettings
{
    /// <summary>
    ///     Default account name to use for new game configurations
    /// </summary>
    public string DefaultAccountName { get; init; } = "Mr_Goldberg";

    /// <summary>
    ///     Default Steam64ID to use for new game configurations
    /// </summary>
    public long DefaultUserSteamId { get; init; } = 76561197960287930;

    /// <summary>
    ///     Default language to use for new game configurations
    /// </summary>
    public string DefaultLanguage { get; init; } = "english";

    /// <summary>
    ///     Default custom broadcast IPs to use for new game configurations
    /// </summary>
    public List<string>? DefaultCustomBroadcastIps { get; init; }

    /// <summary>
    ///     Use experimental build of Goldberg emulator (application setting)
    /// </summary>
    public bool UseExperimental { get; init; }
}