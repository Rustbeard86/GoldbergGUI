using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using GoldbergGUI.Core.Models;
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
    public Task<GoldbergGlobalConfiguration> GetGlobalSettings();
    public Task SetGlobalSettings(GoldbergGlobalConfiguration configuration);
    public bool GoldbergApplied(string path);
    public Task GenerateInterfacesFile(string filePath);
    public List<string> Languages();
}

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public partial class GoldbergService(
    ILogger<GoldbergService> log,
    Configuration.GoldbergConfigurationManager configManager,
    Configuration.GoldbergConfigurationReader configReader) : IGoldbergService
{
    private const string DefaultAccountName = "Mr_Goldberg";
    private const long DefaultSteamId = 76561197960287930;
    private const string DefaultLanguage = "english";
    private const string GoldbergApiUrl = "https://api.github.com/repos/Detanup01/gbe_fork/releases/latest";
    private const string AssetName = "emu-win-release.7z";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private static readonly string GlobalSettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Goldberg SteamEmu Saves");

    private static readonly string AppSettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GoldbergGUI");

    private readonly string _accountNamePath = Path.Combine(GlobalSettingsPath, "settings/account_name.txt");

    private readonly string _customBroadcastIpsPath =
        Path.Combine(GlobalSettingsPath, "settings/custom_broadcasts.txt");

    private readonly string _experimentalPath = Path.Combine(AppSettingsPath, "experimental.txt");

    private readonly string _goldbergArchivePath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg.7z");
    private readonly string _goldbergPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg");
    private readonly string _languagePath = Path.Combine(GlobalSettingsPath, "settings/language.txt");
    private readonly string _userSteamIdPath = Path.Combine(GlobalSettingsPath, "settings/user_steam_id.txt");

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
    // Get global settings
    public async Task<GoldbergGlobalConfiguration> Initialize()
    {
        var download = await Download().ConfigureAwait(false);
        if (download) await Extract(_goldbergArchivePath).ConfigureAwait(false);

        return await GetGlobalSettings().ConfigureAwait(false);
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
            statusCallback("Checking for Goldberg updates...");
            var download = await Download().ConfigureAwait(false);
            
            if (download)
            {
                statusCallback("Extracting Goldberg emulator...");
                await Extract(_goldbergArchivePath).ConfigureAwait(false);
                statusCallback("Goldberg emulator updated successfully");
            }
            else
            {
                statusCallback("Goldberg emulator is up to date");
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error during background initialization");
            statusCallback("Failed to update Goldberg emulator");
        }
    }

    public async Task<GoldbergGlobalConfiguration> GetGlobalSettings()
    {
        log.LogInformation("Getting global settings...");
        var accountName = DefaultAccountName;
        var steamId = DefaultSteamId;
        var language = DefaultLanguage;
        var customBroadcastIps = new List<string>();
        var useExperimental = false;
        if (!File.Exists(GlobalSettingsPath)) Directory.CreateDirectory(Path.Join(GlobalSettingsPath, "settings"));
        await Task.Run(() =>
        {
            if (File.Exists(_accountNamePath)) accountName = File.ReadLines(_accountNamePath).First().Trim();
            if (File.Exists(_userSteamIdPath) &&
                !long.TryParse(File.ReadLines(_userSteamIdPath).First().Trim(), out steamId) &&
                steamId is < 76561197960265729 or > 76561202255233023)
            {
                log.LogError("Invalid User Steam ID! Using default Steam ID...");
                steamId = DefaultSteamId;
            }

            if (File.Exists(_languagePath)) language = File.ReadLines(_languagePath).First().Trim();
            if (File.Exists(_customBroadcastIpsPath))
                customBroadcastIps.AddRange(
                    File.ReadLines(_customBroadcastIpsPath).Select(line => line.Trim()));
            if (File.Exists(_experimentalPath)) useExperimental = true;
        }).ConfigureAwait(false);
        log.LogInformation("Got global settings.");
        return new GoldbergGlobalConfiguration
        {
            AccountName = accountName,
            UserSteamId = steamId,
            Language = language,
            CustomBroadcastIps = customBroadcastIps,
            UseExperimental = useExperimental
        };
    }

    public async Task SetGlobalSettings(GoldbergGlobalConfiguration c)
    {
        var accountName = c.AccountName;
        var userSteamId = c.UserSteamId;
        var language = c.Language;
        var customBroadcastIps = c.CustomBroadcastIps;
        log.LogInformation("Setting global settings...");
        // Account Name
        if (!string.IsNullOrEmpty(accountName))
        {
            log.LogInformation("Setting account name...");
            if (!File.Exists(_accountNamePath))
                await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_accountNamePath, accountName).ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Invalid account name! Skipping...");
            if (!File.Exists(_accountNamePath))
                await File.Create(_accountNamePath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_accountNamePath, DefaultAccountName).ConfigureAwait(false);
        }

        // User SteamID
        if (userSteamId is >= 76561197960265729 and <= 76561202255233023)
        {
            log.LogInformation("Setting user Steam ID...");
            if (!File.Exists(_userSteamIdPath))
                await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_userSteamIdPath, userSteamId.ToString()).ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Invalid user Steam ID! Skipping...");
            if (!File.Exists(_userSteamIdPath))
                await File.Create(_userSteamIdPath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_userSteamIdPath, DefaultSteamId.ToString()).ConfigureAwait(false);
        }

        // Language
        if (!string.IsNullOrEmpty(language))
        {
            log.LogInformation("Setting language...");
            if (!File.Exists(_languagePath))
                await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_languagePath, language).ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Invalid language! Skipping...");
            if (!File.Exists(_languagePath))
                await File.Create(_languagePath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_languagePath, DefaultLanguage).ConfigureAwait(false);
        }

        // Custom Broadcast IPs
        if (customBroadcastIps is { Count: > 0 })
        {
            log.LogInformation("Setting custom broadcast IPs...");
            var result =
                customBroadcastIps.Aggregate("", (current, address) => $"{current}{address}\n");
            if (!File.Exists(_customBroadcastIpsPath))
                await File.Create(_customBroadcastIpsPath).DisposeAsync().ConfigureAwait(false);
            await File.WriteAllTextAsync(_customBroadcastIpsPath, result).ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Empty list of custom broadcast IPs! Skipping...");
            await Task.Run(() => File.Delete(_customBroadcastIpsPath)).ConfigureAwait(false);
        }

        // Experimental (GoldbergGUI application setting)
        if (c.UseExperimental)
        {
            log.LogInformation("Enabling experimental build (app setting stored in {AppSettingsPath})...",
                AppSettingsPath);
            Directory.CreateDirectory(AppSettingsPath);
            if (!File.Exists(_experimentalPath))
                await File.Create(_experimentalPath).DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Using regular build...");
            if (File.Exists(_experimentalPath))
                File.Delete(_experimentalPath);
        }

        log.LogInformation("Setting global configuration finished.");
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

    private void CopyDllFiles(string path, string name)
    {
        var steamApiDll = Path.Combine(path, $"{name}.dll");
        var originalDll = Path.Combine(path, $"{name}_o.dll");
        var guiBackup = Path.Combine(path, $".{name}.dll.GOLDBERGGUIBACKUP");

        // Determine build type (experimental or regular)
        var buildType = File.Exists(_experimentalPath) ? "experimental" : "regular";

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
        // Get latest release from GitHub API
        // Compare release tag with local version
        // Download if update is available
        log.LogInformation("Initializing download...");
        if (!Directory.Exists(_goldbergPath)) Directory.CreateDirectory(_goldbergPath);

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
            return false;
        }

        var releaseTagPath = Path.Combine(_goldbergPath, "release_tag");
        if (File.Exists(releaseTagPath))
            try
            {
                log.LogInformation("Check if update is needed...");
                var releaseTagLocal = File.ReadLines(releaseTagPath).First().Trim();
                log.LogDebug("release_tag: local {ReleaseTagLocal}; remote {ReleaseTagRemote}", releaseTagLocal,
                    releaseTag);
                if (releaseTagLocal.Equals(releaseTag))
                {
                    log.LogInformation("Latest Goldberg emulator is already available! Skipping...");
                    return false;
                }
            }
            catch (Exception)
            {
                log.LogError("An error occured, local Goldberg setup might be broken!");
            }

        log.LogInformation("Starting download of release {ReleaseTag}...", releaseTag);
        await StartDownload(downloadUrl, releaseTag).ConfigureAwait(false);
        return true;
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
                // Save release tag for future comparison
                var releaseTagPath = Path.Combine(_goldbergPath, "release_tag");
                await File.WriteAllTextAsync(releaseTagPath, releaseTag).ConfigureAwait(false);
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

    [GeneratedRegex("(?<id>.*) *= *(?<name>.*)")]
    private static partial Regex PrecompDLCExpression();

    [GeneratedRegex("(?<id>.*) *= *(?<appPath>.*)")]
    private static partial Regex PrecompAppPathExpression();

    [GeneratedRegex(@"STEAMCONTROLLER_INTERFACE_VERSION\d{3}")]
    private static partial Regex PrecompSteamControllerVersionDigits();

    [GeneratedRegex("STEAMCONTROLLER_INTERFACE_VERSION")]
    private static partial Regex PrecompSteamControllerVersion();
}