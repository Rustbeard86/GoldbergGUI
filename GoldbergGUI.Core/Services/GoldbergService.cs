using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Services.Configuration;
using GoldbergGUI.Core.Utils;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace GoldbergGUI.Core.Services;

// downloads and updates goldberg emu
// sets up config files
// does file copy stuff
public interface IGoldbergService
{
    public Task<GoldbergGlobalConfiguration> Initialize();
    public bool IsInitialized();
    public Task InitializeInBackground(Action<string> statusCallback);
    public Task<GoldbergConfiguration> Read(string path);
    public Task Save(string path, GoldbergConfiguration configuration, GoldbergGlobalConfiguration globalConfiguration);
    public Task<GoldbergGlobalConfiguration> GetGoldbergSettings();
    public Task SetGoldbergSettings(GoldbergGlobalConfiguration configuration);
    public Task SetGuiOnlySettings(int goldbergUpdateCheckHours, int databaseUpdateCheckHours);
    public bool GoldbergApplied(string path);
    public Task GenerateInterfacesFile(string filePath);
    public List<string> Languages();
}

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public partial class GoldbergService(
    ILogger<GoldbergService> log,
    GoldbergConfigurationManager configManager,
    GoldbergConfigurationReader configReader) : IGoldbergService
{
    private const string DefaultAccountName = "Goldberg";
    private const long DefaultSteamId = 76561197960287930;
    private const string DefaultLanguage = "english";
    private const string GoldbergApiUrl = "https://api.github.com/repos/Detanup01/gbe_fork/releases/latest";
    private const string AssetName = "emu-win-release.7z";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // Paths - adjacent to application
    private readonly string _appConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "app_config.json");

    private readonly string _goldbergArchivePath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg.7z");
    private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");

    // ReSharper disable StringLiteralTypo
    private readonly List<string> _interfaceNames =
    [
        "SteamClient",
        "SteamGameServer",
        "SteamGameServerStats",
        "SteamUser",
        "SteamFriends",
        "SteamUtils",
        "SteamMatchMaking",
        "SteamMatchMakingServers",
        "STEAMUSERSTATS_INTERFACE_VERSION",
        "STEAMAPPS_INTERFACE_VERSION",
        "SteamNetworking",
        "STEAMREMOTESTORAGE_INTERFACE_VERSION",
        "STEAMSCREENSHOTS_INTERFACE_VERSION",
        "STEAMHTTP_INTERFACE_VERSION",
        "STEAMUNIFIEDMESSAGES_INTERFACE_VERSION",
        "STEAMUGC_INTERFACE_VERSION",
        "STEAMAPPLIST_INTERFACE_VERSION",
        "STEAMMUSIC_INTERFACE_VERSION",
        "STEAMMUSICREMOTE_INTERFACE_VERSION",
        "STEAMHTMLSURFACE_INTERFACE_VERSION_",
        "STEAMINVENTORY_INTERFACE_V",
        "SteamController",
        "SteamMasterServerUpdater",
        "STEAMVIDEO_INTERFACE_V"
    ];

    // Call Download
    // Get goldberg settings
    public async Task<GoldbergGlobalConfiguration> Initialize()
    {
        var download = await Download().ConfigureAwait(false);
        if (download) await Extract(_goldbergArchivePath).ConfigureAwait(false);

        return await GetGoldbergSettings().ConfigureAwait(false);
    }

    public bool IsInitialized()
    {
        // Check if Goldberg files exist
        var x64Exists = File.Exists(Path.Combine(_goldbergPath, "release", "regular", "x64", "steam_api64.dll"));
        var x32Exists = File.Exists(Path.Combine(_goldbergPath, "release", "regular", "x32", "steam_api.dll"));

        return x64Exists || x32Exists;
    }

    public async Task InitializeInBackground(Action<string> statusCallback)
    {
        try
        {
            var (downloaded, skipped) = await DownloadWithStatus().ConfigureAwait(false);

            if (downloaded)
            {
                statusCallback("Extracting Goldberg emulator...");
                await Extract(_goldbergArchivePath).ConfigureAwait(false);
                statusCallback("Goldberg emulator updated successfully");
            }
            else if (!skipped)
            {
                // Only show "up to date" if we actually checked (not skipped)
                statusCallback("Goldberg emulator is up to date");
            }
            // If skipped, don't show any message (silent)
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error during background initialization");
            statusCallback("Failed to update Goldberg emulator");
        }
    }

    public async Task<GoldbergGlobalConfiguration> GetGoldbergSettings()
    {
        log.LogInformation("Getting Goldeberg settings...");

        var config = await GetAppConfiguration().ConfigureAwait(false);

        log.LogInformation("Got Goldeberg settings.");
        return new GoldbergGlobalConfiguration
        {
            AccountName = config.GuiDefaults.AccountName,
            UserSteamId = config.GuiDefaults.SteamId,
            Language = config.GuiDefaults.Language,
            CustomBroadcastIps = config.GuiDefaults.CustomBroadcastIps ?? [],
            UseExperimental = config.GuiDefaults.UseExperimental
        };
    }

    public async Task SetGoldbergSettings(GoldbergGlobalConfiguration c)
    {
        log.LogInformation("Setting Goldeberg settings...");

        // Load existing configuration to preserve state
        var existingConfig = await GetAppConfiguration().ConfigureAwait(false);

        // Validate and use defaults if invalid
        var accountName = string.IsNullOrWhiteSpace(c.AccountName) ? DefaultAccountName : c.AccountName;
        var userSteamId = c.UserSteamId is >= 76561197960265729 and <= 76561202255233023
            ? c.UserSteamId
            : DefaultSteamId;
        var language = string.IsNullOrWhiteSpace(c.Language) ? DefaultLanguage : c.Language;

        if (string.IsNullOrWhiteSpace(c.AccountName))
            log.LogWarning("Invalid account name provided, using default: {DefaultAccountName}", DefaultAccountName);
        if (c.UserSteamId is < 76561197960265729 or > 76561202255233023)
            log.LogWarning("Invalid Steam ID provided, using default: {DefaultSteamId}", DefaultSteamId);
        if (string.IsNullOrWhiteSpace(c.Language))
            log.LogWarning("Invalid language provided, using default: {DefaultLanguage}", DefaultLanguage);

        var updatedConfig = existingConfig with
        {
            GuiDefaults = existingConfig.GuiDefaults with
            {
                AccountName = accountName,
                SteamId = userSteamId,
                Language = language,
                CustomBroadcastIps = c.CustomBroadcastIps?.Count > 0 ? c.CustomBroadcastIps : null,
                UseExperimental = c.UseExperimental
            }
        };

        await SaveAppConfiguration(updatedConfig).ConfigureAwait(false);

        log.LogInformation("Goldeberg settings saved to app_config.json");
    }

    /// <summary>
    ///     Updates only the GUI-specific settings (update frequencies)
    /// </summary>
    public async Task SetGuiOnlySettings(int goldbergUpdateCheckHours, int databaseUpdateCheckHours)
    {
        log.LogInformation("Setting GUI-only settings...");

        // Load existing configuration to preserve other values
        var existingConfig = await GetAppConfiguration().ConfigureAwait(false);

        var updatedConfig = existingConfig with
        {
            GuiDefaults = existingConfig.GuiDefaults with
            {
                GoldbergUpdateCheckHours = goldbergUpdateCheckHours,
                DatabaseUpdateCheckHours = databaseUpdateCheckHours
            }
        };

        await SaveAppConfiguration(updatedConfig).ConfigureAwait(false);

        log.LogInformation("GUI-only settings saved to app_config.json");
    }

    // Read modern configuration using the new format
    public async Task<GoldbergConfiguration> Read(string path)
    {
        return await configReader.ReadConfiguration(path).ConfigureAwait(false);
    }

    // Save modern configuration using the new format
    public async Task Save(string path, GoldbergConfiguration c, GoldbergGlobalConfiguration globalConfig)
    {
        log.LogInformation("Saving configuration...");

        // DLL setup
        log.LogInformation("Running DLL setup...");
        const string x86Name = "steam_api";
        const string x64Name = "steam_api64";
        if (File.Exists(Path.Combine(path, $"{x86Name}.dll"))) CopyDllFiles(path, x86Name);
        if (File.Exists(Path.Combine(path, $"{x64Name}.dll"))) CopyDllFiles(path, x64Name);
        log.LogInformation("DLL setup finished!");

        // Download achievement images if needed
        if (c.Achievements.Count > 0)
        {
            log.LogInformation("Downloading achievement images...");
            var imagePath = Path.Combine(path, "steam_settings", "images");
            Directory.CreateDirectory(imagePath);

            foreach (var achievement in c.Achievements)
            {
                await DownloadImageAsync(imagePath, achievement.Icon).ConfigureAwait(false);
                await DownloadImageAsync(imagePath, achievement.IconGray).ConfigureAwait(false);

                // Update achievement list to point to local images
                achievement.Icon = $"images/{Path.GetFileName(achievement.Icon)}";
                achievement.IconGray = $"images/{Path.GetFileName(achievement.IconGray)}";
            }
        }

        // Use the modern configuration manager to save
        await configManager.SaveConfiguration(path, c, globalConfig).ConfigureAwait(false);

        log.LogInformation("Configuration saved successfully!");
    }

    private static AppConfiguration CreateDefaultConfiguration()
    {
        return new AppConfiguration
        {
            GuiDefaults = new GuiDefaults
            {
                AccountName = DefaultAccountName,
                SteamId = DefaultSteamId,
                Language = DefaultLanguage,
                CustomBroadcastIps = null,
                UseExperimental = false,
                GoldbergUpdateCheckHours = 24,
                DatabaseUpdateCheckHours = 24
            },
            GoldbergState = new GoldbergState(),
            DatabaseState = new DatabaseState()
        };
    }

    private async Task<AppConfiguration> GetAppConfiguration()
    {
        if (!File.Exists(_appConfigPath)) return CreateDefaultConfiguration();

        try
        {
            var json = await File.ReadAllTextAsync(_appConfigPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions) ?? CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to read app_config.json");
            return CreateDefaultConfiguration();
        }
    }

    private async Task SaveAppConfiguration(AppConfiguration config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_appConfigPath, json).ConfigureAwait(false);
    }

    private void CopyDllFiles(string path, string name)
    {
        var steamApiDll = Path.Combine(path, $"{name}.dll");
        var originalDll = Path.Combine(path, $"{name}_o.dll");
        var guiBackup = Path.Combine(path, $".{name}.dll.GOLDBERGGUIBACKUP");

        // Get experimental setting from app configuration
        var config = GetAppConfiguration().GetAwaiter().GetResult();
        var buildType = config.GuiDefaults.UseExperimental ? "experimental" : "regular";

        // Determine architecture (x32 or x64)
        var architecture = name.Contains("64") ? "x64" : "x32";

        var goldbergDll = Path.Combine(_goldbergPath, "release", buildType, architecture, $"{name}.dll");

        if (!File.Exists(goldbergDll))
        {
            log.LogError("Goldberg DLL not found at {GoldbergDll}! Make sure Goldberg is properly downloaded.",
                goldbergDll);
            throw new FileNotFoundException($"Goldberg DLL not found: {goldbergDll}");
        }

        if (!File.Exists(originalDll))
        {
            log.LogInformation("Back up original Steam API DLL...");
            File.Move(steamApiDll, originalDll);
        }
        else
        {
            File.Move(steamApiDll, guiBackup, true);
            File.SetAttributes(guiBackup, FileAttributes.Hidden);
        }

        log.LogInformation("Copy Goldberg DLL ({BuildType}/{Architecture}) to target path...", buildType, architecture);
        File.Copy(goldbergDll, steamApiDll);
    }

    public bool GoldbergApplied(string path)
    {
        var steamSettingsDirExists = Directory.Exists(Path.Combine(path, "steam_settings"));
        var steamAppIdTxtExists = File.Exists(Path.Combine(path, "steam_appid.txt"));
        log.LogDebug("Goldberg applied? {ToString}", (steamSettingsDirExists && steamAppIdTxtExists).ToString());
        return steamSettingsDirExists && steamAppIdTxtExists;
    }

    private async Task<bool> Download()
    {
        var (downloaded, _) = await DownloadWithStatus().ConfigureAwait(false);
        return downloaded;
    }

    private async Task<(bool Downloaded, bool Skipped)> DownloadWithStatus()
    {
        // Get configuration
        var config = await GetAppConfiguration().ConfigureAwait(false);

        if (config.GuiDefaults.GoldbergUpdateCheckHours == -1)
        {
            log.LogDebug("Goldberg update checks are disabled");
            return (false, true); // Skipped
        }

        // Get latest release from GitHub API
        // Compare release tag with local version
        // Download if update is available
        log.LogInformation("Initializing download...");
        if (!Directory.Exists(_goldbergPath)) Directory.CreateDirectory(_goldbergPath);

        // Check if we should skip based on time since last check
        if (config.GuiDefaults.GoldbergUpdateCheckHours > 0 && config.GoldbergState.LastUpdateCheck.HasValue)
        {
            var hoursSinceLastCheck = (DateTime.UtcNow - config.GoldbergState.LastUpdateCheck.Value).TotalHours;

            if (hoursSinceLastCheck < config.GuiDefaults.GoldbergUpdateCheckHours)
            {
                log.LogDebug(
                    "Skipping update check (last checked {Hours:F1} hours ago, frequency set to {Frequency} hours)",
                    hoursSinceLastCheck, config.GuiDefaults.GoldbergUpdateCheckHours);
                return (false, true); // Skipped
            }
        }

        log.LogInformation("Checking for Goldberg updates...");
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "GoldbergGUI");

        var response = await client.GetAsync(GoldbergApiUrl).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        using var jsonDoc = JsonDocument.Parse(body);
        var root = jsonDoc.RootElement;

        var releaseTag = root.GetProperty("tag_name").GetString();
        var assets = root.GetProperty("assets");

        string? downloadUrl = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var assetName = asset.GetProperty("name").GetString();
            if (assetName == AssetName)
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(releaseTag))
        {
            log.LogError("Could not find {AssetName} in latest release or release tag is missing!", AssetName);
            return (false, false); // Not downloaded, not skipped (error)
        }

        // Save last check time
        var updatedConfig = config with
        {
            GoldbergState = config.GoldbergState with
            {
                LastUpdateCheck = DateTime.UtcNow
            }
        };
        await SaveAppConfiguration(updatedConfig).ConfigureAwait(false);

        // Check if we already have this version
        if (config.GoldbergState.InstalledVersion == releaseTag)
        {
            log.LogInformation("Latest Goldberg emulator is already available! Skipping...");
            return (false, false); // Not downloaded, but checked
        }

        log.LogInformation("Starting download of release {ReleaseTag}...", releaseTag);
        await StartDownload(downloadUrl, releaseTag).ConfigureAwait(false);
        return (true, false); // Downloaded
    }

    private async Task StartDownload(string downloadUrl, string releaseTag)
    {
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "GoldbergGUI");
            log.LogDebug("Download URL: {DownloadUrl}", downloadUrl);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
            var headResponse = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
            var contentLength = headResponse.Content.Headers.ContentLength;

            await using (var fileStream = File.OpenWrite(_goldbergArchivePath))
            {
                await client.GetFileAsync(downloadUrl, fileStream).ConfigureAwait(false);
                await fileStream.FlushAsync().ConfigureAwait(false);
            } // Stream is disposed here

            var fileLength = new FileInfo(_goldbergArchivePath).Length;

            if (contentLength == fileLength)
            {
                log.LogInformation("Download finished!");
                // Save installed version to config
                var config = await GetAppConfiguration().ConfigureAwait(false);
                var updatedConfig = config with
                {
                    GoldbergState = config.GoldbergState with
                    {
                        InstalledVersion = releaseTag
                    }
                };
                await SaveAppConfiguration(updatedConfig).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("File size does not match!");
            }
        }
        catch (Exception e)
        {
            ShowErrorMessage();
            log.LogError(e, "Error during download");
            Environment.Exit(1);
        }
    }

    // Empty subfolder ./goldberg/
    // Extract all from archive to subfolder ./goldberg/
    private async Task Extract(string archivePath)
    {
        var errorOccured = false;
        log.LogDebug("Start extraction...");

        // No need to preserve release_tag anymore - it's in app_config.json
        Directory.Delete(_goldbergPath, true);
        Directory.CreateDirectory(_goldbergPath);

        await Task.Run(() =>
        {
            try
            {
                using var archive = SevenZipArchive.Open(archivePath);
                var reader = archive.ExtractAllEntries();
                while (reader.MoveToNextEntry())
                    if (!reader.Entry.IsDirectory)
                        try
                        {
                            reader.WriteEntryToDirectory(_goldbergPath, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                        catch (Exception e)
                        {
                            errorOccured = true;
                            log.LogError(e, "Error while trying to extract {Key}", reader.Entry.Key);
                        }
            }
            catch (Exception e)
            {
                errorOccured = true;
                log.LogError(e, "Error while opening archive");
            }
        }).ConfigureAwait(false);

        if (errorOccured)
        {
            ShowErrorMessage();
            log.LogWarning("Error occured while extraction! Please setup Goldberg manually");
        }
        else
        {
            log.LogInformation("Extraction was successful!");
        }
    }

    private void ShowErrorMessage()
    {
        if (Directory.Exists(_goldbergPath)) Directory.Delete(_goldbergPath, true);

        Directory.CreateDirectory(_goldbergPath);
        MessageBox.Show("Could not setup Goldberg Emulator!\n" +
                        "Please download it manually and extract its content into the \"goldberg\" subfolder!");
    }

    // https://gitlab.com/Mr_Goldberg/goldberg_emulator/-/blob/master/generate_interfaces_file.cpp
    // (maybe) check DLL date first
    public async Task GenerateInterfacesFile(string filePath)
    {
        log.LogDebug("GenerateInterfacesFile {FilePath}", filePath);
        //throw new NotImplementedException();
        // Get DLL content
        var result = new HashSet<string>();
        var dllContent = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        // find interfaces
        foreach (var name in _interfaceNames)
        {
            FindInterfaces(ref result, dllContent, new Regex($"{name}\\d{{3}}"));
            if (!FindInterfaces(ref result, dllContent, PrecompSteamControllerVersionDigits()))
                FindInterfaces(ref result, dllContent, PrecompSteamControllerVersion());
        }

        var dirPath = Path.GetDirectoryName(filePath);
        if (dirPath == null) return;
        await using var destination = File.CreateText(dirPath + "/steam_interfaces.txt");
        foreach (var s in result) await destination.WriteLineAsync(s).ConfigureAwait(false);
    }

    public List<string> Languages()
    {
        return
        [
            DefaultLanguage,
            "arabic",
            "bulgarian",
            "schinese",
            "tchinese",
            "czech",
            "danish",
            "dutch",
            "finnish",
            "french",
            "german",
            "greek",
            "hungarian",
            "italian",
            "japanese",
            "koreana",
            "norwegian",
            "polish",
            "portuguese",
            "brazilian",
            "romanian",
            "russian",
            "spanish",
            "swedish",
            "thai",
            "turkish",
            "ukrainian"
        ];
    }

    private static bool FindInterfaces(ref HashSet<string> result, string dllContent, Regex regex)
    {
        var success = false;
        var matches = regex.Matches(dllContent);
        foreach (Match match in matches)
        {
            success = true;
            //result += $@"{match.Value}\n";
            result.Add(match.Value);
        }

        return success;
    }

    private async Task DownloadImageAsync(string imageFolder, string imageUrl)
    {
        var fileName = Path.GetFileName(imageUrl);
        var targetPath = Path.Combine(imageFolder, fileName);
        if (File.Exists(targetPath)) return;

        if (imageUrl.StartsWith("images/"))
            log.LogWarning("Previously downloaded image '{ImageUrl}' is now missing!", imageUrl);

        using var httpClient = new HttpClient();
        var imageBytes = await httpClient.GetByteArrayAsync(new Uri(imageUrl, UriKind.Absolute));
        await File.WriteAllBytesAsync(targetPath, imageBytes);
    }

    [GeneratedRegex(@"STEAMCONTROLLER_INTERFACE_VERSION\d{3}")]
    private static partial Regex PrecompSteamControllerVersionDigits();

    [GeneratedRegex("STEAMCONTROLLER_INTERFACE_VERSION")]
    private static partial Regex PrecompSteamControllerVersion();
}