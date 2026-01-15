using System.Text.Json;
using GoldbergGUI.Core.Models;
using Microsoft.Extensions.Logging;

namespace GoldbergGUI.Core.Services.Configuration;

/// <summary>
///     Reads Goldberg emulator modern configuration files (.ini format)
/// </summary>
public sealed class GoldbergConfigurationReader(ILogger<GoldbergConfigurationReader> log)
{
    /// <summary>
    ///     Reads complete Goldberg configuration from the specified game path
    /// </summary>
    public async Task<GoldbergConfiguration> ReadConfiguration(string gamePath)
    {
        log.LogInformation("Reading configuration from {GamePath}", gamePath);

        var steamSettingsPath = Path.Combine(gamePath, "steam_settings");

        // Read steam_appid.txt
        var appId = await ReadAppId(gamePath).ConfigureAwait(false);

        // Read configs.main.ini
        var (offline, disableNetworking, disableOverlay) =
            await ReadMainConfig(steamSettingsPath).ConfigureAwait(false);

        // Read configs.app.ini
        var (unlockAllDlc, dlcList) = await ReadAppConfig(steamSettingsPath).ConfigureAwait(false);

        // Read achievements.json
        var achievements = await ReadAchievements(steamSettingsPath).ConfigureAwait(false);

        // Read stats.json
        var stats = await ReadStats(steamSettingsPath).ConfigureAwait(false);

        return new GoldbergConfiguration
        {
            AppId = appId,
            DlcList = dlcList,
            UnlockAllDlc = unlockAllDlc,
            Achievements = achievements,
            Stats = stats,
            Offline = offline,
            DisableNetworking = disableNetworking,
            DisableOverlay = disableOverlay
        };
    }

    private async Task<int> ReadAppId(string gamePath)
    {
        var steamAppidTxt = Path.Combine(gamePath, "steam_appid.txt");
        if (File.Exists(steamAppidTxt))
        {
            log.LogInformation("Getting AppID...");
            var content = await File.ReadAllTextAsync(steamAppidTxt).ConfigureAwait(false);
            if (int.TryParse(content.Trim(), out var appId)) return appId;
        }

        log.LogWarning("steam_appid.txt missing or invalid!");
        return -1;
    }

    private async Task<(bool offline, bool disableNetworking, bool disableOverlay)> ReadMainConfig(
        string steamSettingsPath)
    {
        var mainConfigPath = Path.Combine(steamSettingsPath, "configs.main.ini");

        if (!File.Exists(mainConfigPath))
        {
            log.LogInformation("configs.main.ini not found, checking legacy files...");
            // Fallback to legacy .txt files
            return (
                File.Exists(Path.Combine(steamSettingsPath, "offline.txt")),
                File.Exists(Path.Combine(steamSettingsPath, "disable_networking.txt")),
                File.Exists(Path.Combine(steamSettingsPath, "disable_overlay.txt"))
            );
        }

        var ini = await File.ReadAllLinesAsync(mainConfigPath).ConfigureAwait(false);
        var offline = false;
        var disableNetworking = false;
        var disableOverlay = false;

        foreach (var line in ini)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("offline=")) offline = trimmed.EndsWith("=1");
            else if (trimmed.StartsWith("disable_networking=")) disableNetworking = trimmed.EndsWith("=1");
            else if (trimmed.StartsWith("disable_overlay=")) disableOverlay = trimmed.EndsWith("=1");
        }

        return (offline, disableNetworking, disableOverlay);
    }

    private async Task<(bool unlockAll, List<DlcApp> dlcList)> ReadAppConfig(string steamSettingsPath)
    {
        var appConfigPath = Path.Combine(steamSettingsPath, "configs.app.ini");
        var dlcList = new List<DlcApp>();
        var unlockAll = true; // Default

        if (!File.Exists(appConfigPath))
        {
            log.LogInformation("configs.app.ini not found, checking legacy DLC.txt...");
            // Fallback to legacy DLC.txt
            var dlcTxtPath = Path.Combine(steamSettingsPath, "DLC.txt");
            if (File.Exists(dlcTxtPath))
            {
                var lines = await File.ReadAllLinesAsync(dlcTxtPath).ConfigureAwait(false);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var appId))
                        dlcList.Add(new DlcApp
                        {
                            AppId = appId,
                            Name = parts[1].Trim(),
                            ComparableName = parts[1].Trim().ToLowerInvariant(),
                            AppType = "dlc"
                        });
                }
            }

            return (unlockAll, dlcList);
        }

        var ini = await File.ReadAllLinesAsync(appConfigPath).ConfigureAwait(false);
        var inDlcSection = false;
        var inPathsSection = false;

        foreach (var line in ini)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("[app::dlcs]"))
            {
                inDlcSection = true;
                inPathsSection = false;
                continue;
            }

            if (trimmed.StartsWith("[app::paths]"))
            {
                inPathsSection = true;
                inDlcSection = false;
                continue;
            }

            if (trimmed.StartsWith("["))
            {
                inDlcSection = false;
                inPathsSection = false;
                continue;
            }

            if (inDlcSection && trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                if (parts[0] == "unlock_all")
                    unlockAll = parts[1] == "1";
                else if (int.TryParse(parts[0], out var appId))
                    dlcList.Add(new DlcApp
                    {
                        AppId = appId,
                        Name = parts[1],
                        ComparableName = parts[1].ToLowerInvariant(),
                        AppType = "dlc"
                    });
            }
            else if (inPathsSection && trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                if (int.TryParse(parts[0], out var appId))
                {
                    var dlc = dlcList.FirstOrDefault(d => d.AppId == appId);
                    if (dlc != null) dlc.AppPath = parts[1];
                }
            }
        }

        return (unlockAll, dlcList);
    }

    private async Task<List<Achievement>> ReadAchievements(string steamSettingsPath)
    {
        var achievementPath = Path.Combine(steamSettingsPath, "achievements.json");
        if (!File.Exists(achievementPath))
        {
            log.LogInformation("achievements.json not found");
            return [];
        }

        var json = await File.ReadAllTextAsync(achievementPath).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<Achievement>>(json) ?? [];
    }

    private async Task<List<Stat>?> ReadStats(string steamSettingsPath)
    {
        var statsPath = Path.Combine(steamSettingsPath, "stats.json");
        if (!File.Exists(statsPath))
        {
            log.LogInformation("stats.json not found");
            return null;
        }

        var json = await File.ReadAllTextAsync(statsPath).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<Stat>>(json);
    }
}