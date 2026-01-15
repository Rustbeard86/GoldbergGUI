using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    ILogger<MainViewModel> log,
    ILoggerFactory loggerFactory,
    IMvxNavigationService navigationService)
    : MvxNavigationViewModel(loggerFactory, navigationService)
{
    private readonly IMvxNavigationService _navigationService = navigationService;
    private string _accountName = string.Empty;
    private ObservableCollection<Achievement> _achievements = [];
    private int _appId;
    private bool _disableNetworking;
    private bool _disableOverlay;
    private ObservableCollection<DlcApp> _dlcs = [];
    private string _dllPath = "Path to game's steam_api(64).dll";
    private string _gameName = "Game name...";
    private bool _goldbergApplied;
    private bool _mainWindowEnabled;
    private bool _offline;
    private string _selectedLanguage = string.Empty;
    private string _statusText = string.Empty;
    private long _steamId;
    private ObservableCollection<string> _steamLanguages = [];

    // PROPERTIES //

    public string DllPath
    {
        get => _dllPath;
        private set
        {
            _dllPath = value;
            RaisePropertyChanged(() => DllPath);
            RaisePropertyChanged(() => DllSelected);
            RaisePropertyChanged(() => SteamInterfacesTxtExists);
        }
    }

    public string GameName
    {
        get => _gameName;
        set
        {
            _gameName = value;
            RaisePropertyChanged(() => GameName);
        }
    }

    public int AppId
    {
        get => _appId;
        set
        {
            _appId = value;
            RaisePropertyChanged(() => AppId);
            Task.Run(async () => await GetNameById().ConfigureAwait(false));
        }
    }

    // ReSharper disable once InconsistentNaming
    public ObservableCollection<DlcApp> DLCs
    {
        get => _dlcs;
        set
        {
            _dlcs = value;
            RaisePropertyChanged(() => DLCs);
            /*RaisePropertyChanged(() => DllSelected);
            RaisePropertyChanged(() => SteamInterfacesTxtExists);*/
        }
    }

    public ObservableCollection<Achievement> Achievements
    {
        get => _achievements;
        set
        {
            _achievements = value;
            RaisePropertyChanged(() => Achievements);
        }
    }

    public string AccountName
    {
        get => _accountName;
        set
        {
            _accountName = value;
            RaisePropertyChanged(() => AccountName);
        }
    }

    public long SteamId
    {
        get => _steamId;
        set
        {
            _steamId = value;
            RaisePropertyChanged(() => SteamId);
        }
    }

    public bool Offline
    {
        get => _offline;
        set
        {
            _offline = value;
            RaisePropertyChanged(() => Offline);
        }
    }

    public bool DisableNetworking
    {
        get => _disableNetworking;
        set
        {
            _disableNetworking = value;
            RaisePropertyChanged(() => DisableNetworking);
        }
    }

    public bool DisableOverlay
    {
        get => _disableOverlay;
        set
        {
            _disableOverlay = value;
            RaisePropertyChanged(() => DisableOverlay);
        }
    }

    public bool MainWindowEnabled
    {
        get => _mainWindowEnabled;
        set
        {
            _mainWindowEnabled = value;
            RaisePropertyChanged(() => MainWindowEnabled);
        }
    }

    public bool GoldbergApplied
    {
        get => _goldbergApplied;
        set
        {
            _goldbergApplied = value;
            RaisePropertyChanged(() => GoldbergApplied);
        }
    }

    public bool SteamInterfacesTxtExists
    {
        get
        {
            var dllPathDirExists = GetDllPathDir(out var dirPath);
            return dllPathDirExists && dirPath is not null && !File.Exists(Path.Combine(dirPath, "steam_interfaces.txt"));
        }
    }

    public bool DllSelected
    {
        get
        {
            var value = !DllPath.Contains("Path to game's steam_api(64).dll");
            if (!value) log.LogWarning("No DLL selected! Skipping...");
            return value;
        }
    }

    public ObservableCollection<string> SteamLanguages
    {
        get => _steamLanguages;
        set
        {
            _steamLanguages = value;
            RaisePropertyChanged(() => SteamLanguages);
        }
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;
            RaisePropertyChanged(() => SelectedLanguage);
            //MyLogger.Log.Debug($"Lang: {value}");
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            RaisePropertyChanged(() => StatusText);
        }
    }

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
            var expression = PrecompDLCExpression();
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

    public IMvxCommand OpenGlobalSettingsFolderCommand => new MvxCommand(OpenGlobalSettingsFolder);

    public override void Prepare()
    {
        base.Prepare();
        Task.Run(async () =>
        {
            //var errorDuringInit = false;
            MainWindowEnabled = false;
            StatusText = "Initializing! Please wait...";
            try
            {
                SteamLanguages = new ObservableCollection<string>(goldberg.Languages());
                ResetForm();
                await steam.Initialize().ConfigureAwait(false);
                var globalConfiguration =
                    await goldberg.Initialize().ConfigureAwait(false);
                AccountName = globalConfiguration.AccountName;
                SteamId = globalConfiguration.UserSteamId;
                SelectedLanguage = globalConfiguration.Language;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.LogError(e, "Error during initialization");
                throw;
            }

            MainWindowEnabled = true;
            StatusText = "Ready.";
        });
    }

    public override async Task Initialize()
    {
        await base.Initialize().ConfigureAwait(false);
    }

    private async Task OpenFile()
    {
        MainWindowEnabled = false;
        StatusText = "Please choose a file...";
        var dialog = new OpenFileDialog
        {
            Filter = "SteamAPI DLL|steam_api.dll;steam_api64.dll|" +
                     "All files (*.*)|*.*",
            Multiselect = false,
            Title = "Select SteamAPI DLL..."
        };
        if (dialog.ShowDialog() != true)
        {
            MainWindowEnabled = true;
            log.LogWarning("File selection canceled.");
            StatusText = "No file selected! Ready.";
            return;
        }

        DllPath = dialog.FileName;
        await ReadConfig().ConfigureAwait(false);
        if (!GoldbergApplied) await GetListOfDlc().ConfigureAwait(false);
        MainWindowEnabled = true;
        StatusText = "Ready.";
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
        var appByName = await steam.GetAppByName(_gameName).ConfigureAwait(false);
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
                if (steamApp != null)
                {
                    GameName = steamApp.Name;
                    AppId = steamApp.AppId;
                }
                else
                {
                    await FindIdInList(steamApps).ConfigureAwait(false);
                }
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
            log.LogError("Invalid Steam App!");
            return;
        }

        var steamApp = await steam.GetAppById(AppId).ConfigureAwait(false);
        if (steamApp != null) GameName = steamApp.Name;
    }

    private async Task GetListOfAchievements()
    {
        if (AppId <= 0)
        {
            log.LogError("Invalid Steam App!");
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
            log.LogError("Invalid Steam App!");
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
        log.LogInformation("Saving global settings...");
        var globalConfiguration = new GoldbergGlobalConfiguration
        {
            AccountName = AccountName,
            UserSteamId = SteamId,
            Language = SelectedLanguage
        };
        await goldberg.SetGlobalSettings(globalConfiguration).ConfigureAwait(false);
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
            }
        ).ConfigureAwait(false);
        GoldbergApplied = goldberg.GoldbergApplied(dirPath);
        MainWindowEnabled = true;
        StatusText = "Ready.";
    }

    private async Task ResetConfig()
    {
        var globalConfiguration = await goldberg.GetGlobalSettings().ConfigureAwait(false);
        AccountName = globalConfiguration.AccountName;
        SteamId = globalConfiguration.UserSteamId;
        SelectedLanguage = globalConfiguration.Language;
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

    private void OpenGlobalSettingsFolder()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StatusText = "Can't open folder (Windows only)! Ready.";
            return;
        }

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Goldberg SteamEmu Saves", "settings");
        var start = Process.Start("explorer.exe", path);
        start?.Dispose();
    }

    // OTHER METHODS //

    private void ResetForm()
    {
        DllPath = "Path to game's steam_api(64).dll...";
        GameName = "Game name...";
        AppId = -1;
        Achievements = [];
        DLCs = [];
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

    [GeneratedRegex(@"(?<id>.*) *= *(?<name>.*)")]
    private static partial Regex PrecompDLCExpression();
}