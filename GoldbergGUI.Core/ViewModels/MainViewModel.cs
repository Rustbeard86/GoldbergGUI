using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Services;
using GoldbergGUI.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MainViewModel(
    ISteamService steam,
    IGoldbergService goldberg,
    IStatusMessageQueue statusQueue,
    ILogger<MainViewModel> log,
    ILoggerFactory loggerFactory,
    IMvxNavigationService navigationService)
    : MvxNavigationViewModel(loggerFactory, navigationService)
{
    private readonly IMvxNavigationService _navigationService = navigationService;

    // PROPERTIES //

    public string DllPath
    {
        get;
        private set
        {
            field = value;
            RaisePropertyChanged(() => DllPath);
            RaisePropertyChanged(() => DllSelected);
            RaisePropertyChanged(() => SteamInterfacesTxtExists);
        }
    } = "Path to game's steam_api(64).dll";

    public string GameName
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => GameName);
        }
    } = "Game name...";

    public ObservableCollection<SteamApp> SuggestedGames
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => SuggestedGames);
        }
    } = [];

    public SteamApp? SelectedSuggestedGame
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => SelectedSuggestedGame);
            if (value is not null)
            {
                GameName = value.Name;
                AppId = value.AppId;
                log.LogInformation("User selected game from suggestions: {GameName} (AppID: {AppId})", value.Name,
                    value.AppId);
            }
        }
    }

    public int AppId
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => AppId);
            Task.Run(async () => await GetNameById().ConfigureAwait(false));
        }
    }

    // ReSharper disable once InconsistentNaming
    public ObservableCollection<DlcApp> DLCs
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => DLCs);
            /*RaisePropertyChanged(() => DllSelected);
            RaisePropertyChanged(() => SteamInterfacesTxtExists);*/
        }
    } = [];

    public ObservableCollection<Achievement> Achievements
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => Achievements);
        }
    } = [];

    public string AccountName
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => AccountName);
        }
    } = string.Empty;

    public long SteamId
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => SteamId);
        }
    }

    public bool Offline
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => Offline);
        }
    }

    public bool DisableNetworking
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => DisableNetworking);
        }
    }

    public bool DisableOverlay
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => DisableOverlay);
        }
    }

    public bool MainWindowEnabled
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => MainWindowEnabled);
        }
    }

    public bool GoldbergApplied
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => GoldbergApplied);
        }
    }

    public bool SteamInterfacesTxtExists
    {
        get
        {
            var dllPathDirExists = GetDllPathDir(out var dirPath);
            return dllPathDirExists && dirPath is not null &&
                   !File.Exists(Path.Combine(dirPath, "steam_interfaces.txt"));
        }
    }

    public bool DllSelected
    {
        get
        {
            var value = !DllPath.Contains("Path to game's steam_api(64).dll");
            if (!value) log.LogDebug("No DLL selected! Skipping...");
            return value;
        }
    }

    public ObservableCollection<string> SteamLanguages
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => SteamLanguages);
        }
    } = [];

    public string SelectedLanguage
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => SelectedLanguage);
            //MyLogger.Log.Debug($"Lang: {value}");
        }
    } = string.Empty;

    public bool UseExperimental
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => UseExperimental);
        }
    }

    public int GoldbergUpdateCheckHours
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => GoldbergUpdateCheckHours);
        }
    } = 24;

    public int DatabaseUpdateCheckHours
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => DatabaseUpdateCheckHours);
        }
    } = 24;

    public string StatusText
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged(() => StatusText);
        }
    } = string.Empty;

    public static string AboutVersionText =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "Unknown";

    public static GlobalHelp G => new();

    // COMMANDS //

    public IMvxCommand OpenFileCommand => new MvxAsyncCommand(OpenFile);

    public IMvxCommand FindIdCommand => new MvxAsyncCommand(FindId);

    public IMvxCommand GetListOfAchievementsCommand => new MvxAsyncCommand(GetListOfAchievements);

    public IMvxCommand GetListOfDlcCommand => new MvxAsyncCommand(GetListOfDlc);

    public IMvxCommand SaveConfigCommand => new MvxAsyncCommand(SaveConfig);

    public IMvxCommand ResetConfigCommand => new MvxAsyncCommand(ResetConfig);

    public IMvxCommand GenerateSteamInterfacesCommand => new MvxAsyncCommand(GenerateSteamInterfaces);

    public IMvxCommand PasteDlcCommand => new MvxCommand(() =>
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        log.LogInformation("Trying to paste DLC list...");
        if (!(Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text)))
        {
            log.LogWarning("Invalid DLC list!");
        }
        else
        {
            var result = Clipboard.GetText();
            var expression = PrecompDlcExpression();
            var pastedDlc = (from line in result.Split(["\n", "\r\n"],
                    StringSplitOptions.RemoveEmptyEntries)
                select expression.Match(line)
                into match
                where match.Success
                select new DlcApp
                {
                    AppId = Convert.ToInt32(match.Groups["id"].Value),
                    Name = match.Groups["name"].Value
                }).ToList();
            if (pastedDlc.Count > 0)
            {
                DLCs.Clear();
                DLCs = new ObservableCollection<DlcApp>(pastedDlc);
                //var empty = DLCs.Count == 1 ? "" : "s";
                //StatusText = $"Successfully got {DLCs.Count} DLC{empty} from clipboard! Ready.";
                var statusTextCount = DLCs.Count == 1 ? "one DLC" : $"{DLCs.Count} DLCs";
                StatusText = $"Successfully got {statusTextCount} from clipboard! Ready.";
            }
            else
            {
                StatusText = "No DLC found in clipboard! Ready.";
            }
        }
    });

    public IMvxCommand OpenGoldbergSettingsFolderCommand => new MvxCommand(OpenGoldbergSettingsFolder);

    public override void Prepare()
    {
        base.Prepare();

        // Start the status message queue
        statusQueue.Start();

        // Subscribe to status message changes
        statusQueue.MessageChanged += (_, message) => { StatusText = message; };

        Task.Run(async () =>
        {
            try
            {
                SteamLanguages = new ObservableCollection<string>(goldberg.Languages());
                ResetForm();

                // Check if critical files exist
                var steamInitialized = steam.IsInitialized();
                var goldbergInitialized = goldberg.IsInitialized();

                if (!steamInitialized || !goldbergInitialized)
                {
                    // First run - block UI and initialize
                    MainWindowEnabled = false;
                    StatusText = "First run detected. Initializing required files...";
                    log.LogInformation("First run: Initializing required files...");

                    if (!steamInitialized)
                    {
                        StatusText = "Downloading Steam app database...";
                        await steam.Initialize().ConfigureAwait(false);
                    }

                    if (!goldbergInitialized)
                    {
                        StatusText = "Downloading Goldberg emulator...";
                        var globalConfiguration = await goldberg.Initialize().ConfigureAwait(false);
                        AccountName = globalConfiguration.AccountName;
                        SteamId = globalConfiguration.UserSteamId;
                        SelectedLanguage = globalConfiguration.Language;
                        UseExperimental = globalConfiguration.UseExperimental;
                        // Note: Update check hours are loaded separately since they're GUI-only settings
                    }
                    else
                    {
                        var globalConfiguration = await goldberg.GetGoldbergSettings().ConfigureAwait(false);
                        AccountName = globalConfiguration.AccountName;
                        SteamId = globalConfiguration.UserSteamId;
                        SelectedLanguage = globalConfiguration.Language;
                        UseExperimental = globalConfiguration.UseExperimental;
                        // Note: Update check hours are loaded separately since they're GUI-only settings
                    }

                    MainWindowEnabled = true;
                    StatusText = "Initialization complete!";
                    statusQueue.Enqueue("Initialization complete! Ready.", TimeSpan.FromSeconds(2));
                }
                else
                {
                    // Files exist - don't block UI
                    MainWindowEnabled = true;
                    StatusText = "Ready.";
                    log.LogInformation("Files exist. Loading in background...");

                    // Load global configuration
                    var globalConfiguration = await goldberg.GetGoldbergSettings().ConfigureAwait(false);
                    AccountName = globalConfiguration.AccountName;
                    SteamId = globalConfiguration.UserSteamId;
                    SelectedLanguage = globalConfiguration.Language;
                    UseExperimental = globalConfiguration.UseExperimental;

                    LoadGuiOnlySettings();

                    // Run updates in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await steam.InitializeInBackground(msg =>
                            {
                                statusQueue.Enqueue(msg, TimeSpan.FromSeconds(3));
                            }).ConfigureAwait(false);

                            await goldberg.InitializeInBackground(msg =>
                            {
                                statusQueue.Enqueue(msg, TimeSpan.FromSeconds(3));
                            }).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, "Error during background update");
                            statusQueue.Enqueue("Background update failed. Check logs.", TimeSpan.FromSeconds(5));
                        }
                    });
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Error during initialization");
                MainWindowEnabled = true;
                StatusText = "Initialization error! Check logs.";
                statusQueue.Enqueue("Critical error during initialization. Some features may not work.",
                    TimeSpan.FromSeconds(5));
            }
        });
    }

    public override async Task Initialize()
    {
        await base.Initialize().ConfigureAwait(false);
    }

    private async Task OpenFile()
    {
        MainWindowEnabled = false;
        StatusText = "Please choose a folder...";

        // Use OpenFolderDialog (available in .NET 8+)
        var dialog = new OpenFolderDialog
        {
            Title = "Select the game folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            MainWindowEnabled = true;
            log.LogWarning("Folder selection canceled.");
            StatusText = "No folder selected! Ready.";
            return;
        }

        var rootFolder = dialog.FolderName;
        log.LogInformation("Searching for steam_api DLL in: {RootFolder}", rootFolder);
        StatusText = "Searching for Steam API DLL...";

        // Recursively search for steam_api DLL files
        string? foundDllPath;
        try
        {
            var searchPatterns = new[] { "steam_api.dll", "steam_api64.dll" };
            var foundDlls = new List<string>();

            foreach (var pattern in searchPatterns)
            {
                var dlls = Directory.GetFiles(rootFolder, pattern, SearchOption.AllDirectories);
                foundDlls.AddRange(dlls);
            }

            if (foundDlls.Count == 0)
            {
                MainWindowEnabled = true;
                StatusText = "No steam_api DLL found in selected folder!";
                log.LogWarning("No steam_api DLL found in {RootFolder}", rootFolder);
                MessageBox.Show(
                    $"Could not find steam_api.dll or steam_api64.dll in the selected folder.\n\nSearched in: {rootFolder}",
                    "DLL Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // If multiple DLLs found, prefer 64-bit and shortest path (closest to root)
            // TODO: Improve selection logic to better handle games with multiple API dlls and cases of nested game server apps.
            foundDllPath = foundDlls
                .OrderBy(dll => dll.Contains("64") ? 0 : 1) // Prefer 64-bit
                .ThenBy(dll => dll.Length) // Prefer shorter paths (closer to root)
                .First();

            log.LogInformation("Found DLL: {DllPath} ({Count} total found)", foundDllPath, foundDlls.Count);
            if (foundDlls.Count > 1)
            {
                log.LogInformation("Multiple DLLs found, using: {DllPath}", foundDllPath);
                StatusText = $"Found {foundDlls.Count} DLLs, using: {Path.GetFileName(foundDllPath)}";
            }
        }
        catch (Exception ex)
        {
            MainWindowEnabled = true;
            log.LogError(ex, "Error searching for steam_api DLL");
            StatusText = "Error searching for DLL! Ready.";
            MessageBox.Show(
                $"Error searching for steam_api DLL:\n{ex.Message}",
                "Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        DllPath = foundDllPath;
        await ReadConfig().ConfigureAwait(false);

        // Auto-detect game using ROOT FOLDER context (not the DLL folder)
        if (!GoldbergApplied)
        {
            await AutoDetectGame(rootFolder).ConfigureAwait(false);
            await GetListOfDlc().ConfigureAwait(false);
        }

        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    /// <summary>
    ///     Attempts to automatically detect and set the game's AppID based on root folder name and executables
    /// </summary>
    /// <param name="rootFolder">The game's root folder</param>
    private async Task AutoDetectGame(string rootFolder)
    {
        StatusText = "Auto-detecting game...";
        log.LogInformation("Attempting to auto-detect game from root folder: {Path}", rootFolder);

        var searchCandidates = new List<string>();

        // 1. Get the root folder name (most likely the game name)
        var rootFolderName =
            Path.GetFileName(rootFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.IsNullOrWhiteSpace(rootFolderName))
        {
            searchCandidates.Add(CleanGameName(rootFolderName));
            log.LogDebug("Added root folder name candidate: {FolderName}", rootFolderName);
        }

        // 2. Look for executable files in the ROOT directory
        try
        {
            var exeFiles = Directory.GetFiles(rootFolder, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (var exeFile in exeFiles)
            {
                var exeName = Path.GetFileNameWithoutExtension(exeFile);
                // Skip common launcher/utility executables
                if (!IsUtilityExecutable(exeName))
                {
                    searchCandidates.Add(CleanGameName(exeName));
                    log.LogDebug("Added executable name candidate: {ExeName}", exeName);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to scan root directory for executables");
        }

        // 3. Try to find a match for each candidate
        foreach (var candidate in searchCandidates.Distinct())
        {
            log.LogInformation("Searching for game: {Candidate}", candidate);

            var appByName = await steam.GetAppByName(candidate).ConfigureAwait(false);
            if (appByName != null)
            {
                // Exact match found
                GameName = appByName.Name;
                AppId = appByName.AppId;
                SuggestedGames.Clear();
                StatusText = $"Auto-detected: {appByName.Name}";
                log.LogInformation("Auto-detected game: {GameName} (AppID: {AppId})", appByName.Name, appByName.AppId);
                return;
            }

            // Try fuzzy search
            var searchResults = await steam.GetListOfAppsByName(candidate).ConfigureAwait(false);
            var steamApps = searchResults.ToArray();

            if (steamApps.Length == 1)
            {
                // Single result - auto-select it
                var steamApp = steamApps[0];
                GameName = steamApp.Name;
                AppId = steamApp.AppId;
                SuggestedGames.Clear();
                StatusText = $"Auto-detected: {steamApp.Name}";
                log.LogInformation("Auto-detected game: {GameName} (AppID: {AppId})", steamApp.Name, steamApp.AppId);
                return;
            }

            if (steamApps.Length > 1)
            {
                // Multiple results - check if first result is a very close match
                var firstMatch = steamApps[0];
                if (IsCloseMatch(candidate, firstMatch.Name))
                {
                    // Very close match - auto-select but still show suggestions
                    GameName = firstMatch.Name;
                    AppId = firstMatch.AppId;
                    SuggestedGames = new ObservableCollection<SteamApp>(steamApps.Take(10)); // Limit to top 10
                    StatusText = $"Auto-detected: {firstMatch.Name} (Click dropdown for other options)";
                    log.LogInformation(
                        "Auto-detected game (close match): {GameName} (AppID: {AppId}), showing {Count} suggestions",
                        firstMatch.Name, firstMatch.AppId, SuggestedGames.Count);
                    return;
                }

                // Multiple results but no close match - populate suggestions
                SuggestedGames = new ObservableCollection<SteamApp>(steamApps.Take(10)); // Limit to top 10
                GameName = candidate; // Set the search term
                StatusText =
                    $"Found {steamApps.Length} possible matches. Please select from dropdown or search manually.";
                log.LogInformation("Found {Count} possible matches for '{Candidate}', showing suggestions",
                    steamApps.Length, candidate);
                return;
            }
        }

        log.LogInformation("Could not auto-detect game from folder/executable names");
        SuggestedGames.Clear();
        StatusText = "Could not auto-detect game. Please search manually.";
    }

    /// <summary>
    ///     Cleans up game name by removing common suffixes and special characters
    /// </summary>
    private static string CleanGameName(string name)
    {
        // Remove common version indicators, special editions, etc.
        var cleaned = name;

        // Remove patterns like (64-bit), [PLAZA], etc.
        cleaned = BracketsPattern().Replace(cleaned, "").Trim();

        // Remove common suffixes
        var suffixesToRemove = new[] { "_Data", "_x64", "_x86", "Game", "Launcher" };
        foreach (var suffix in suffixesToRemove)
            if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[..^suffix.Length].Trim();

        // Replace underscores and multiple spaces with single space
        cleaned = UnderscoreDashPattern().Replace(cleaned, " ");
        cleaned = MultipleSpacesPattern().Replace(cleaned, " ").Trim();

        return cleaned;
    }

    [GeneratedRegex(@"\[.*?\]|\(.*?\)")]
    private static partial Regex BracketsPattern();

    [GeneratedRegex(@"[_-]+")]
    private static partial Regex UnderscoreDashPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesPattern();

    /// <summary>
    ///     Checks if an executable name is likely a utility rather than the main game
    /// </summary>
    private static bool IsUtilityExecutable(string name)
    {
        var utilityKeywords = new[]
        {
            "unins", "setup", "install", "update", "launcher", "crash", "report",
            "config", "settings", "tool", "editor", "uxhelper", "bootstrapper",
            "prerequisite", "redist", "vcredist", "directx", "dx"
        };

        var lowerName = name.ToLowerInvariant();
        return utilityKeywords.Any(keyword => lowerName.Contains(keyword));
    }

    /// <summary>
    ///     Checks if a directory name is a common system/generic directory
    /// </summary>
    private static bool IsCommonDirectory(string name)
    {
        var commonDirs = new[]
        {
            "bin", "binary", "binaries", "common", "steamapps", "games",
            "program files", "program files (x86)", "x64", "x86", "win64", "win32"
        };

        return commonDirs.Any(dir => name.Equals(dir, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Determines if a search result is a close match to the candidate name
    /// </summary>
    private static bool IsCloseMatch(string candidate, string result)
    {
        var cleanCandidate = candidate.ToLowerInvariant().Replace(" ", "");
        var cleanResult = result.ToLowerInvariant().Replace(" ", "");

        // Check if the candidate is a substantial substring of the result or vice versa
        if (cleanResult.Contains(cleanCandidate) || cleanCandidate.Contains(cleanResult))
        {
            // Calculate similarity ratio
            var minLength = Math.Min(cleanCandidate.Length, cleanResult.Length);
            var maxLength = Math.Max(cleanCandidate.Length, cleanResult.Length);
            var ratio = (double)minLength / maxLength;

            // If similarity is > 70%, consider it a close match
            return ratio > 0.7;
        }

        return false;
    }

    private async Task FindId()
    {
        async Task FindIdInList(SteamApp[] steamApps)
        {
            await _navigationService.Navigate<SearchResultViewModel, IEnumerable<SteamApp>>(steamApps)
                .ConfigureAwait(false);
            // Note: Navigation result handling has changed in newer MvvmCross versions
            // The result is now handled through the SearchResultViewModel's Selected property
            // This may need to be refactored to use a different pattern (e.g., Messenger or callbacks)
        }

        if (GameName.Contains("Game name..."))
        {
            log.LogError("No game name entered!");
            return;
        }

        MainWindowEnabled = false;
        StatusText = "Trying to find AppID...";
        var appByName = await steam.GetAppByName(GameName).ConfigureAwait(false);
        if (appByName != null)
        {
            GameName = appByName.Name;
            AppId = appByName.AppId;
        }
        else
        {
            var list = await steam.GetListOfAppsByName(GameName).ConfigureAwait(false);
            var steamApps = list as SteamApp[] ?? [.. list];
            if (steamApps.Length == 1)
            {
                var steamApp = steamApps[0];
                GameName = steamApp.Name;
                AppId = steamApp.AppId;
            }
            else
            {
                await FindIdInList(steamApps).ConfigureAwait(false);
            }
        }

        await GetListOfDlc().ConfigureAwait(false);
        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    //public IMvxCommand GetNameByIdCommand => new MvxAsyncCommand(GetNameById);

    private async Task GetNameById()
    {
        if (AppId <= 0)
        {
            if (AppId != -1) log.LogError("Invalid Steam App!");
            return;
        }

        var steamApp = await steam.GetAppById(AppId).ConfigureAwait(false);
        if (steamApp != null) GameName = steamApp.Name;
    }

    private async Task GetListOfAchievements()
    {
        if (AppId <= 0)
        {
            if (AppId != -1) log.LogError("Invalid Steam App!");
            return;
        }

        MainWindowEnabled = false;
        StatusText = "Trying to get list of achievements...";
        var listOfAchievements = await steam.GetListOfAchievements(new SteamApp { AppId = AppId, Name = GameName });
        Achievements = new MvxObservableCollection<Achievement>(listOfAchievements);
        MainWindowEnabled = true;

        if (Achievements.Count > 0)
        {
            var empty = Achievements.Count == 1 ? "" : "s";
            StatusText = $"Successfully got {Achievements.Count} achievement{empty}! Ready.";
        }
        else
        {
            StatusText = "No achievements found! Ready.";
        }
    }

    private async Task GetListOfDlc()
    {
        if (AppId <= 0)
        {
            if (AppId != -1) log.LogError("Invalid Steam App!");
            return;
        }

        MainWindowEnabled = false;
        StatusText = "Trying to get list of DLCs...";
        var listOfDlc = await steam.GetListOfDlc(new SteamApp { AppId = AppId, Name = GameName }, true)
            .ConfigureAwait(false);
        DLCs = new MvxObservableCollection<DlcApp>(listOfDlc);
        MainWindowEnabled = true;
        if (DLCs.Count > 0)
        {
            var empty = DLCs.Count == 1 ? "" : "s";
            StatusText = $"Successfully got {DLCs.Count} DLC{empty}! Ready.";
        }
        else
        {
            StatusText = "No DLC found! Ready.";
        }
    }

    private async Task SaveConfig()
    {
        log.LogInformation("Saving Goldeberg settings...");
        var globalConfiguration = new GoldbergGlobalConfiguration
        {
            AccountName = AccountName,
            UserSteamId = SteamId,
            Language = SelectedLanguage,
            UseExperimental = UseExperimental
        };
        await goldberg.SetGoldbergSettings(globalConfiguration).ConfigureAwait(false);

        // Save GUI-only settings (update check frequencies)
        await goldberg.SetGuiOnlySettings(GoldbergUpdateCheckHours, DatabaseUpdateCheckHours).ConfigureAwait(false);

        if (!DllSelected) return;

        log.LogInformation("Saving Goldberg settings...");
        if (!GetDllPathDir(out var dirPath) || dirPath is null) return;
        MainWindowEnabled = false;
        StatusText = "Saving...";
        await goldberg.Save(dirPath, new GoldbergConfiguration
            {
                AppId = AppId,
                Achievements = [.. Achievements],
                DlcList = [.. DLCs],
                Offline = Offline,
                DisableNetworking = DisableNetworking,
                DisableOverlay = DisableOverlay
            },
            globalConfiguration
        ).ConfigureAwait(false);
        GoldbergApplied = goldberg.GoldbergApplied(dirPath);
        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    private void LoadGuiOnlySettings()
    {
        var appConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "app_config.json");
        if (!File.Exists(appConfigPath)) return;

        try
        {
            var json = File.ReadAllText(appConfigPath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json);
            if (config?.GuiDefaults is not null)
            {
                GoldbergUpdateCheckHours = config.GuiDefaults.GoldbergUpdateCheckHours;
                DatabaseUpdateCheckHours = config.GuiDefaults.DatabaseUpdateCheckHours;
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to load GUI-only settings");
        }
    }

    private async Task ResetConfig()
    {
        var globalConfiguration = await goldberg.GetGoldbergSettings().ConfigureAwait(false);
        AccountName = globalConfiguration.AccountName;
        SteamId = globalConfiguration.UserSteamId;
        SelectedLanguage = globalConfiguration.Language;
        UseExperimental = globalConfiguration.UseExperimental;
        if (!DllSelected) return;

        log.LogInformation("Reset form...");
        MainWindowEnabled = false;
        StatusText = "Resetting...";
        await ReadConfig().ConfigureAwait(false);
        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    private async Task GenerateSteamInterfaces()
    {
        if (!DllSelected) return;

        log.LogInformation("Generate steam_interfaces.txt...");
        MainWindowEnabled = false;
        StatusText = @"Generating ""steam_interfaces.txt"".";
        if (!GetDllPathDir(out var dirPath) || dirPath is null) return;
        if (File.Exists(Path.Combine(dirPath, "steam_api_o.dll")))
            await goldberg.GenerateInterfacesFile(Path.Combine(dirPath, "steam_api_o.dll")).ConfigureAwait(false);
        else if (File.Exists(Path.Combine(dirPath, "steam_api64_o.dll")))
            await goldberg.GenerateInterfacesFile(Path.Combine(dirPath, "steam_api64_o.dll"))
                .ConfigureAwait(false);
        else await goldberg.GenerateInterfacesFile(DllPath).ConfigureAwait(false);
        await RaisePropertyChanged(() => SteamInterfacesTxtExists).ConfigureAwait(false);
        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    private void OpenGoldbergSettingsFolder()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StatusText = "Can't open folder (Windows only)! Ready.";
            return;
        }

        var path = Directory.GetCurrentDirectory();
        var start = Process.Start("explorer.exe", path);
        start.Dispose();
    }

    // OTHER METHODS //

    private void ResetForm()
    {
        DllPath = "Path to game's steam_api(64).dll...";
        GameName = "Game name...";
        AppId = -1;
        Achievements = [];
        DLCs = [];
        SuggestedGames.Clear();
        SelectedSuggestedGame = null;
        AccountName = "Account name...";
        SteamId = -1;
        Offline = false;
        DisableNetworking = false;
        DisableOverlay = false;
    }

    private async Task ReadConfig()
    {
        if (!GetDllPathDir(out var dirPath) || dirPath is null) return;
        var config = await goldberg.Read(dirPath).ConfigureAwait(false);
        SetFormFromConfig(config);
        GoldbergApplied = goldberg.GoldbergApplied(dirPath);
        await RaisePropertyChanged(() => SteamInterfacesTxtExists).ConfigureAwait(false);
    }

    private void SetFormFromConfig(GoldbergConfiguration config)
    {
        AppId = config.AppId;
        Achievements = new ObservableCollection<Achievement>(config.Achievements);
        DLCs = new ObservableCollection<DlcApp>(config.DlcList);
        Offline = config.Offline;
        DisableNetworking = config.DisableNetworking;
        DisableOverlay = config.DisableOverlay;
    }

    private bool GetDllPathDir(out string? dirPath)
    {
        if (!DllSelected)
        {
            dirPath = null;
            return false;
        }

        dirPath = Path.GetDirectoryName(DllPath);
        if (dirPath is not null) return true;

        log.LogError("Invalid directory for {DllPath}.", DllPath);
        dirPath = null;
        return false;
    }

    [GeneratedRegex("(?<id>.*) *= *(?<name>.*)")]
    private static partial Regex PrecompDlcExpression();
}