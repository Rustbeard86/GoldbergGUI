using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;

namespace GoldbergGUI.Core.Services.Configuration;

/// <summary>
///     Writes configs.app.ini configuration file
/// </summary>
public sealed class AppConfigWriter
{
    /// <summary>
    ///     Generates configs.app.ini content from configuration
    /// </summary>
    public string Generate(GoldbergConfiguration config)
    {
        var writer = new IniFileWriter();

        // General app settings
        writer.WriteSection("app::general", new Dictionary<string, string>
        {
            ["is_beta_branch"] = "0",
            ["branch_name"] = "public"
        });

        // DLC settings - only list individual DLCs
        var dlcSection = new Dictionary<string, string>();

        // Add individual DLCs (never use unlock_all)
        foreach (var dlc in config.DlcList) dlcSection[dlc.AppId.ToString()] = dlc.Name;

        writer.WriteSection("app::dlcs", dlcSection);

        // App paths (if any DLC has custom paths)
        var dlcsWithPaths = config.DlcList.Where(d => !string.IsNullOrEmpty(d.AppPath)).ToList();
        if (dlcsWithPaths.Count > 0)
        {
            var pathsSection = new Dictionary<string, string>();
            foreach (var dlc in dlcsWithPaths) pathsSection[dlc.AppId.ToString()] = dlc.AppPath!;
            writer.WriteSection("app::paths", pathsSection);
        }

        return writer.Generate();
    }
}