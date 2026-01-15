using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;

namespace GoldbergGUI.Core.Services.Configuration;

/// <summary>
///     Writes configs.user.ini configuration file
/// </summary>
public sealed class UserConfigWriter
{
    /// <summary>
    ///     Generates configs.user.ini content from global or overwritten configuration
    /// </summary>
    public string Generate(GoldbergGlobalConfiguration globalConfig, GoldbergGlobalConfiguration? overwrittenConfig)
    {
        var writer = new IniFileWriter();
        var activeConfig = overwrittenConfig ?? globalConfig;

        writer.WriteSection("user::general", new Dictionary<string, string>
        {
            ["account_name"] = activeConfig.AccountName,
            ["account_steamid"] = activeConfig.UserSteamId.ToString(),
            ["language"] = activeConfig.Language,
            ["ip_country"] = "US" // Default, can be made configurable later
        });

        // Only write custom broadcasts if they exist
        if (activeConfig.CustomBroadcastIps?.Count > 0)
        {
            // Custom broadcasts go in a separate file, not in configs.user.ini
            // This is just for reference
        }

        return writer.Generate();
    }
}