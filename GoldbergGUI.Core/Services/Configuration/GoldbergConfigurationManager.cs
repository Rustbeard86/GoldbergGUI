using System.Text.Encodings.Web;
using System.Text.Json;
using GoldbergGUI.Core.Models;
using Microsoft.Extensions.Logging;

namespace GoldbergGUI.Core.Services.Configuration;

/// <summary>
///     Manages saving and loading Goldberg emulator configuration files
/// </summary>
public sealed class GoldbergConfigurationManager(
    ILogger<GoldbergConfigurationManager> log,
    MainConfigWriter mainConfigWriter,
    UserConfigWriter userConfigWriter,
    AppConfigWriter appConfigWriter)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    ///     Saves complete Goldberg configuration to the specified game path
    /// </summary>
    public async Task SaveConfiguration(
        string gamePath,
        GoldbergConfiguration config,
        GoldbergGlobalConfiguration globalConfig)
    {
        var steamSettingsPath = Path.Combine(gamePath, "steam_settings");
        Directory.CreateDirectory(steamSettingsPath);

        // Save steam_appid.txt
        await SaveAppId(gamePath, config.AppId).ConfigureAwait(false);

        // Save configs.main.ini
        await SaveMainConfig(steamSettingsPath, config).ConfigureAwait(false);

        // Save configs.user.ini (per-game overrides or global defaults)
        await SaveUserConfig(steamSettingsPath, globalConfig, config.OverwrittenGlobalConfiguration)
            .ConfigureAwait(false);

        // Save configs.app.ini
        await SaveAppConfig(steamSettingsPath, config).ConfigureAwait(false);

        // Save achievements.json
        await SaveAchievements(steamSettingsPath, config.Achievements).ConfigureAwait(false);

        // Save stats.json
        if (config.Stats?.Count > 0) await SaveStats(steamSettingsPath, config.Stats).ConfigureAwait(false);

        // Save custom_broadcasts.txt (if overwritten or using global)
        var activeConfig = config.OverwrittenGlobalConfiguration ?? globalConfig;
        if (activeConfig.CustomBroadcastIps?.Count > 0)
            await SaveCustomBroadcasts(steamSettingsPath, activeConfig.CustomBroadcastIps).ConfigureAwait(false);
    }

    private async Task SaveAppId(string gamePath, int appId)
    {
        log.LogInformation("Saving steam_appid.txt");
        await File.WriteAllTextAsync(
            Path.Combine(gamePath, "steam_appid.txt"),
            appId.ToString()
        ).ConfigureAwait(false);
    }

    private async Task SaveMainConfig(string steamSettingsPath, GoldbergConfiguration config)
    {
        log.LogInformation("Saving configs.main.ini");
        var content = mainConfigWriter.Generate(config);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "configs.main.ini"),
            content
        ).ConfigureAwait(false);
    }

    private async Task SaveUserConfig(
        string steamSettingsPath,
        GoldbergGlobalConfiguration globalConfig,
        GoldbergGlobalConfiguration? overwrittenConfig)
    {
        log.LogInformation("Saving configs.user.ini");
        var content = userConfigWriter.Generate(globalConfig, overwrittenConfig);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "configs.user.ini"),
            content
        ).ConfigureAwait(false);
    }

    private async Task SaveAppConfig(string steamSettingsPath, GoldbergConfiguration config)
    {
        log.LogInformation("Saving configs.app.ini");
        var content = appConfigWriter.Generate(config);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "configs.app.ini"),
            content
        ).ConfigureAwait(false);
    }

    private async Task SaveAchievements(string steamSettingsPath, List<Achievement> achievements)
    {
        if (achievements.Count == 0)
        {
            log.LogInformation("No achievements to save");
            return;
        }

        log.LogInformation("Saving achievements.json");
        var json = JsonSerializer.Serialize(achievements, JsonOptions);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "achievements.json"),
            json
        ).ConfigureAwait(false);
    }

    private async Task SaveStats(string steamSettingsPath, List<Stat> stats)
    {
        log.LogInformation("Saving stats.json");
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "stats.json"),
            json
        ).ConfigureAwait(false);
    }

    private async Task SaveCustomBroadcasts(string steamSettingsPath, List<string> broadcasts)
    {
        log.LogInformation("Saving custom_broadcasts.txt");
        var content = string.Join(Environment.NewLine, broadcasts);
        await File.WriteAllTextAsync(
            Path.Combine(steamSettingsPath, "custom_broadcasts.txt"),
            content
        ).ConfigureAwait(false);
    }
}