using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using Microsoft.Extensions.Logging;
using NinjaNye.SearchExtensions;
using SQLite;
using SteamStorefrontAPI;

#pragma warning disable CA1873

namespace GoldbergGUI.Core.Services;

// gets info from steam api
public interface ISteamService
{
    public Task Initialize();
    public Task<IEnumerable<SteamApp>> GetListOfAppsByName(string name);
    public Task<SteamApp> GetAppByName(string name);
    public Task<SteamApp> GetAppById(int appid);
    public Task<List<Achievement>> GetListOfAchievements(SteamApp steamApp);
    public Task<List<DlcApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb);
}

internal class SteamCache(string uri, Type apiVersion, string steamAppType)
{
    public string SteamUri { get; } = uri;
    public Type ApiVersion { get; } = apiVersion;
    public string SteamAppType { get; } = steamAppType;
}

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public partial class SteamService(ILogger<SteamService> log) : ISteamService
{
    // ReSharper disable StringLiteralTypo
    private readonly Dictionary<string, SteamCache> _caches =
        new()
        {
            {
                AppTypeGame,
                new SteamCache(
                    "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                    "?max_results=50000" +
                    "&include_games=1" +
                    "&key=" + Secrets.SteamWebApiKey(),
                    typeof(SteamAppsV1),
                    AppTypeGame
                )
            },
            {
                AppTypeDlc,
                new SteamCache(
                    "https://api.steampowered.com/IStoreService/GetAppList/v1/" +
                    "?max_results=50000" +
                    "&include_games=0" +
                    "&include_dlc=1" +
                    "&key=" + Secrets.SteamWebApiKey(),
                    typeof(SteamAppsV1),
                    AppTypeDlc
                )
            }
        };

    private static readonly Secrets Secrets = new();

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/87.0.4280.88 Safari/537.36";

    private const string AppTypeGame = "game";
    private const string AppTypeDlc = "dlc";
    private const string Database = "steamapps.cache";
    private const string GameSchemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/";

    private SQLiteAsyncConnection _db;

    public async Task Initialize()
    {
        static SteamApps DeserializeSteamApps(Type type, string cacheString)
        {
            return type == typeof(SteamAppsV2)
                ? JsonSerializer.Deserialize<SteamAppsV2>(cacheString)
                : JsonSerializer.Deserialize<SteamAppsV1>(cacheString);
        }

        _db = new SQLiteAsyncConnection(Database);
        //_db.CreateTable<SteamApp>();
        await _db.CreateTableAsync<SteamApp>()
            //.ContinueWith(x => _log.Debug("Table success!"))
            .ConfigureAwait(false);

        var countAsync = await _db.Table<SteamApp>().CountAsync().ConfigureAwait(false);
        if (DateTime.Now.Subtract(File.GetLastWriteTimeUtc(Database)).TotalDays >= 1 || countAsync == 0)
            foreach (var (appType, steamCache) in _caches)
            {
                log.LogInformation("Updating cache ({AppType})...", appType);
                bool haveMoreResults;
                long lastAppId = 0;
                var client = new HttpClient();
                var cacheRaw = new HashSet<SteamApp>();
                do
                {
                    var response = lastAppId > 0
                        ? await client.GetAsync($"{steamCache.SteamUri}&last_appid={lastAppId}")
                            .ConfigureAwait(false)
                        : await client.GetAsync(steamCache.SteamUri).ConfigureAwait(false);
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var steamApps = DeserializeSteamApps(steamCache.ApiVersion, responseBody);
                    foreach (var appListApp in steamApps.AppList.Apps) cacheRaw.Add(appListApp);
                    haveMoreResults = steamApps.AppList.HaveMoreResults;
                    lastAppId = steamApps.AppList.LastAppid;
                } while (haveMoreResults);

                var cache = new HashSet<SteamApp>();
                foreach (var steamApp in cacheRaw)
                {
                    steamApp.AppType = steamCache.SteamAppType;
                    steamApp.ComparableName = PrepareStringToCompare(steamApp.Name);
                    cache.Add(steamApp);
                }

                await _db.InsertAllAsync(cache, "OR IGNORE").ConfigureAwait(false);
            }
    }

    public async Task<IEnumerable<SteamApp>> GetListOfAppsByName(string name)
    {
        var query = await _db.Table<SteamApp>()
            .Where(x => x.AppType == AppTypeGame).ToListAsync().ConfigureAwait(false);
        var listOfAppsByName = query.Search(x => x.Name)
            .SetCulture(StringComparison.OrdinalIgnoreCase)
            .ContainingAll(name.Split(' '));
        return listOfAppsByName;
    }

    public async Task<SteamApp> GetAppByName(string name)
    {
        log.LogInformation("Trying to get app {Name}", name);
        var comparableName = PrepareStringToCompare(name);
        var app = await _db.Table<SteamApp>()
            .FirstOrDefaultAsync(x => x.AppType == AppTypeGame && x.ComparableName.Equals(comparableName))
            .ConfigureAwait(false);
        if (app != null) log.LogInformation("Successfully got app {App}", app);
        return app;
    }

    public async Task<SteamApp> GetAppById(int appid)
    {
        log.LogInformation("Trying to get app with ID {AppId}", appid);
        var app = await _db.Table<SteamApp>().Where(x => x.AppType == AppTypeGame)
            .FirstOrDefaultAsync(x => x.AppId.Equals(appid)).ConfigureAwait(false);
        if (app != null) log.LogInformation("Successfully got app {App}", app);
        return app;
    }

    public async Task<List<Achievement>> GetListOfAchievements(SteamApp steamApp)
    {
        var achievementList = new List<Achievement>();
        if (steamApp == null) return achievementList;

        log.LogInformation("Getting achievements for App {SteamApp}", steamApp);

        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        var apiUrl = $"{GameSchemaUrl}?key={Secrets.SteamWebApiKey()}&appid={steamApp.AppId}&l=en";

        var response = await client.GetAsync(apiUrl);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var jsonResponse = JsonDocument.Parse(responseBody);
        var achievementData = jsonResponse.RootElement.GetProperty("game")
            .GetProperty("availableGameStats")
            .GetProperty("achievements");

        achievementList = JsonSerializer.Deserialize<List<Achievement>>(achievementData.GetRawText());
        return achievementList;
    }

    public async Task<List<DlcApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb)
    {
        var dlcList = new List<DlcApp>();
        if (steamApp != null)
        {
            log.LogInformation("Get DLC for App {SteamApp}", steamApp);
            var task = AppDetails.GetAsync(steamApp.AppId);
            var steamAppDetails = await task.ConfigureAwait(true);
            if (steamAppDetails.Type == AppTypeGame)
            {
                steamAppDetails.DLC.ForEach(async x =>
                {
                    var result = await _db.Table<SteamApp>().Where(z => z.AppType == AppTypeDlc)
                                     .FirstOrDefaultAsync(y => y.AppId.Equals(x)).ConfigureAwait(true)
                                 ?? new SteamApp
                                 {
                                     AppId = x, Name = $"Unknown DLC {x}", ComparableName = $"unknownDlc{x}",
                                     AppType = AppTypeDlc
                                 };
                    dlcList.Add(new DlcApp(result));
                    log.LogDebug("{AppId}={Name}", result.AppId, result.Name);
                });

                log.LogInformation("Got DLC successfully...");

                // Get DLC from SteamDB
                // Get Cloudflare cookie (not implemented)
                // Scrape and parse HTML page
                // Add missing to DLC list

                // Return current list if we don't intend to use SteamDB
                if (!useSteamDb) return dlcList;

                try
                {
                    var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                    var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                    log.LogInformation("Get SteamDB App {SteamApp}", steamApp);
                    var httpCall = client.GetAsync(steamDbUri);
                    var response = await httpCall.ConfigureAwait(false);
                    log.LogDebug("{Status}", httpCall.Status.ToString());
                    log.LogDebug("{Response}", response.EnsureSuccessStatusCode().ToString());

                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    var responseBody = await readAsStringAsync.ConfigureAwait(false);
                    log.LogDebug("{Status}", readAsStringAsync.Status.ToString());

                    var parser = new HtmlParser();
                    var doc = parser.ParseDocument(responseBody);

                    var query1 = doc.QuerySelector("#dlc");
                    if (query1 != null)
                    {
                        log.LogInformation("Got list of DLC from SteamDB.");
                        var query2 = query1.QuerySelectorAll(".app");
                        foreach (var element in query2)
                        {
                            var dlcId = element.GetAttribute("data-appid");
                            var query3 = element.QuerySelectorAll("td");
                            var dlcName = query3 != null
                                ? query3[1].Text().Replace("\n", "").Trim()
                                : $"Unknown DLC {dlcId}";
                            var dlcApp = new DlcApp { AppId = Convert.ToInt32(dlcId), Name = dlcName };
                            var i = dlcList.FindIndex(x => x.AppId.Equals(dlcApp.AppId));
                            if (i > -1)
                            {
                                if (dlcList[i].Name.Contains("Unknown DLC")) dlcList[i] = dlcApp;
                            }
                            else
                            {
                                dlcList.Add(dlcApp);
                            }
                        }

                        dlcList.ForEach(x => log.LogDebug("{AppId}={Name}", x.AppId, x.Name));
                        log.LogInformation("Got DLC from SteamDB successfully...");
                    }
                    else
                    {
                        log.LogError("Could not get DLC from SteamDB!");
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "Could not get DLC from SteamDB! Skipping...");
                }
            }
            else
            {
                log.LogError("Could not get DLC: Steam App is not of type \"game\"");
            }
        }
        else
        {
            log.LogError("Could not get DLC: Invalid Steam App");
        }

        return dlcList;
    }

    private static string PrepareStringToCompare(string name)
    {
        return PrecompPrepareStringToCompare().Replace(name, "").ToLower();
    }

    [GeneratedRegex(Misc.AlphaNumOnlyRegex)]
    private static partial Regex PrecompPrepareStringToCompare();
}