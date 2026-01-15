using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;

namespace GoldbergGUI.Core.Services.Configuration;

/// <summary>
///     Writes configs.main.ini configuration file
/// </summary>
public sealed class MainConfigWriter
{
    /// <summary>
    ///     Generates configs.main.ini content from configuration
    /// </summary>
    public string Generate(GoldbergConfiguration config)
    {
        var writer = new IniFileWriter();

        // Connectivity settings
        writer.WriteSection("main::connectivity", new Dictionary<string, string>
        {
            ["offline"] = config.Offline ? "1" : "0",
            ["disable_networking"] = config.DisableNetworking ? "1" : "0",
            ["listen_port"] = "47584"
        });

        // Misc settings
        writer.WriteSection("main::misc", new Dictionary<string, string>
        {
            ["disable_overlay"] = config.DisableOverlay ? "1" : "0"
        });

        return writer.Generate();
    }
}