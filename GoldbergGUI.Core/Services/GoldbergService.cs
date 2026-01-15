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
    public Task<GoldbergConfiguration> Read(string path);
    public Task Save(string path, GoldbergConfiguration configuration);
    public Task<GoldbergGlobalConfiguration> GetGlobalSettings();
    public Task SetGlobalSettings(GoldbergGlobalConfiguration configuration);
    public bool GoldbergApplied(string path);
    public Task GenerateInterfacesFile(string filePath);
    public List<string> Languages();
}

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public partial class GoldbergService(ILogger<GoldbergService> log) : IGoldbergService
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

    // If first time, call GenerateInterfaces
    // else try to read config
    public async Task<GoldbergConfiguration> Read(string path)
    {
        log.LogInformation("Reading configuration...");
        var appId = -1;
        var achievementList = new List<Achievement>();
        var dlcList = new List<DlcApp>();
        var steamAppidTxt = Path.Combine(path, "steam_appid.txt");
        if (File.Exists(steamAppidTxt))
        {
            log.LogInformation("Getting AppID...");
            await Task.Run(() => int.TryParse(File.ReadLines(steamAppidTxt).First().Trim(), out appId))
                .ConfigureAwait(false);
        }
        else
        {
            log.LogInformation(@"""steam_appid.txt"" missing! Skipping...");
        }

        var achievementJson = Path.Combine(path, "steam_settings", "achievements.json");
        if (File.Exists(achievementJson))
        {
            log.LogInformation("Getting achievements...");
            var json = await File.ReadAllTextAsync(achievementJson)
                .ConfigureAwait(false);
            achievementList = JsonSerializer.Deserialize<List<Achievement>>(json);
        }
        else
        {
            log.LogInformation(@"""steam_settings/achievements.json"" missing! Skipping...");
        }

        var dlcTxt = Path.Combine(path, "steam_settings", "DLC.txt");
        var appPathTxt = Path.Combine(path, "steam_settings", "app_paths.txt");
        if (File.Exists(dlcTxt))
        {
            log.LogInformation("Getting DLCs...");
            var readAllLinesAsync = await File.ReadAllLinesAsync(dlcTxt).ConfigureAwait(false);
            var expression = PrecompDLCExpression();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var line in readAllLinesAsync)
            {
                var match = expression.Match(line);
                if (match.Success)
                    dlcList.Add(new DlcApp
                    {
                        AppId = Convert.ToInt32(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    });
            }

            // ReSharper disable once InvertIf
            if (File.Exists(appPathTxt))
            {
                var appPathAllLinesAsync = await File.ReadAllLinesAsync(appPathTxt).ConfigureAwait(false);
                var appPathExpression = PrecompAppPathExpression();
                foreach (var line in appPathAllLinesAsync)
                {
                    var match = appPathExpression.Match(line);
                    if (!match.Success) continue;
                    var i = dlcList.FindIndex(x =>
                        x.AppId.Equals(Convert.ToInt32(match.Groups["id"].Value)));
                    dlcList[i].AppPath = match.Groups["appPath"].Value;
                }
            }
        }
        else
        {
            log.LogInformation(@"""steam_settings/DLC.txt"" missing! Skipping...");
        }

        return new GoldbergConfiguration
        {
            AppId = appId,
            Achievements = achievementList ?? [],
            DlcList = dlcList,
            Offline = File.Exists(Path.Combine(path, "steam_settings", "offline.txt")),
            DisableNetworking = File.Exists(Path.Combine(path, "steam_settings", "disable_networking.txt")),
            DisableOverlay = File.Exists(Path.Combine(path, "steam_settings", "disable_overlay.txt"))
        };
    }

    // If first time, rename original SteamAPI DLL to steam_api(64)_o.dll
    // If not, rename current SteamAPI DLL to steam_api(64).dll.backup
    // Copy Goldberg DLL to path
    // Save configuration files
    public async Task Save(string path, GoldbergConfiguration c)
    {
        log.LogInformation("Saving configuration...");
        // DLL setup
        log.LogInformation("Running DLL setup...");
        const string x86Name = "steam_api";
        const string x64Name = "steam_api64";
        if (File.Exists(Path.Combine(path, $"{x86Name}.dll"))) CopyDllFiles(path, x86Name);

        if (File.Exists(Path.Combine(path, $"{x64Name}.dll"))) CopyDllFiles(path, x64Name);
        log.LogInformation("DLL setup finished!");

        // Create steam_settings folder if missing
        log.LogInformation("Saving settings...");
        if (!Directory.Exists(Path.Combine(path, "steam_settings")))
            Directory.CreateDirectory(Path.Combine(path, "steam_settings"));

        // create steam_appid.txt
        await File.WriteAllTextAsync(Path.Combine(path, "steam_appid.txt"), c.AppId.ToString())
            .ConfigureAwait(false);

        // Achievements + Images
        if (c.Achievements.Count > 0)
        {
            log.LogInformation("Downloading images...");
            var imagePath = Path.Combine(path, "steam_settings", "images");
            Directory.CreateDirectory(imagePath);

            foreach (var achievement in c.Achievements)
            {
                await DownloadImageAsync(imagePath, achievement.Icon);
                await DownloadImageAsync(imagePath, achievement.IconGray);

                // Update achievement list to point to local images instead
                achievement.Icon = $"images/{Path.GetFileName(achievement.Icon)}";
                achievement.IconGray = $"images/{Path.GetFileName(achievement.IconGray)}";
            }

            log.LogInformation("Saving achievements...");

            var achievementJson = JsonSerializer.Serialize(c.Achievements, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "achievements.json"), achievementJson)
                .ConfigureAwait(false);

            log.LogInformation("Finished saving achievements.");
        }
        else
        {
            log.LogInformation("No achievements set! Removing achievement files...");
            var imagePath = Path.Combine(path, "steam_settings", "images");
            if (Directory.Exists(imagePath)) Directory.Delete(imagePath);
            var achievementPath = Path.Combine(path, "steam_settings", "achievements");
            if (File.Exists(achievementPath)) File.Delete(achievementPath);
            log.LogInformation("Removed achievement files.");
        }

        // DLC + App path
        if (c.DlcList.Count > 0)
        {
            log.LogInformation("Saving DLC settings...");
            var dlcContent = "";
            //var depotContent = "";
            var appPathContent = "";
            c.DlcList.ForEach(x =>
            {
                dlcContent += $"{x}\n";
                //depotContent += $"{x.DepotId}\n";
                if (!string.IsNullOrEmpty(x.AppPath))
                    appPathContent += $"{x.AppId}={x.AppPath}\n";
            });
            await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "DLC.txt"), dlcContent)
                .ConfigureAwait(false);

            /*if (!string.IsNullOrEmpty(depotContent))
            {
                await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "depots.txt"), depotContent)
                    .ConfigureAwait(false);
            }*/


            if (!string.IsNullOrEmpty(appPathContent))
            {
                await File.WriteAllTextAsync(Path.Combine(path, "steam_settings", "app_paths.txt"), appPathContent)
                    .ConfigureAwait(false);
            }
            else
            {
                if (File.Exists(Path.Combine(path, "steam_settings", "app_paths.txt")))
                    File.Delete(Path.Combine(path, "steam_settings", "app_paths.txt"));
            }

            log.LogInformation("Saved DLC settings.");
        }
        else
        {
            log.LogInformation("No DLC set! Removing DLC configuration files...");
            if (File.Exists(Path.Combine(path, "steam_settings", "DLC.txt")))
                File.Delete(Path.Combine(path, "steam_settings", "DLC.txt"));
            if (File.Exists(Path.Combine(path, "steam_settings", "app_paths.txt")))
                File.Delete(Path.Combine(path, "steam_settings", "app_paths.txt"));
            log.LogInformation("Removed DLC configuration files.");
        }

        // Offline
        if (c.Offline)
        {
            log.LogInformation("Create offline.txt");
            await File.Create(Path.Combine(path, "steam_settings", "offline.txt")).DisposeAsync()
                .ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Delete offline.txt if it exists");
            File.Delete(Path.Combine(path, "steam_settings", "offline.txt"));
        }

        // Disable Networking
        if (c.DisableNetworking)
        {
            log.LogInformation("Create disable_networking.txt");
            await File.Create(Path.Combine(path, "steam_settings", "disable_networking.txt")).DisposeAsync()
                .ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Delete disable_networking.txt if it exists");
            File.Delete(Path.Combine(path, "steam_settings", "disable_networking.txt"));
        }

        // Disable Overlay
        if (c.DisableOverlay)
        {
            log.LogInformation("Create disable_overlay.txt");
            await File.Create(Path.Combine(path, "steam_settings", "disable_overlay.txt")).DisposeAsync()
                .ConfigureAwait(false);
        }
        else
        {
            log.LogInformation("Delete disable_overlay.txt if it exists");
            File.Delete(Path.Combine(path, "steam_settings", "disable_overlay.txt"));
        }
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