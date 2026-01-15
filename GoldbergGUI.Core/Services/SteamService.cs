using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using GoldbergGUI.Core.Data;
using GoldbergGUI.Core.Models;
using GoldbergGUI.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NinjaNye.SearchExtensions;
using SteamStorefrontAPI;

namespace GoldbergGUI.Core.Services;

/// <summary>
///     Service for interacting with Steam API and managing app data
/// </summary>
public interface ISteamService
{
    Task Initialize();
    bool IsInitialized();
    Task InitializeInBackground(Action<string> statusCallback);
    IAsyncEnumerable<SteamApp> StreamAppsByName(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<SteamApp>> GetListOfAppsByName(string name);
    Task<SteamApp?> GetAppByName(string name);
    Task<SteamApp?> GetAppById(int appid);
    Task<List<Achievement>> GetListOfAchievements(SteamApp? steamApp);
    Task<List<Stat>> GetListOfStats(SteamApp? steamApp);
    Task<List<DlcApp>> GetListOfDlc(SteamApp? steamApp, bool useSteamDb);
}

internal sealed record SteamCache(string SteamUri, Type ApiVersion, string SteamAppType);

/// <summary>
///     Steam API stat format (for deserialization from GetSchemaForGame API)
/// </summary>
internal sealed record SteamStat
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("defaultvalue")] public double DefaultValue { get; init; }

    [JsonPropertyName("displayName")] public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
///     Implementation of Steam service using Entity Framework Core for caching
/// </summary>
public sealed partial class SteamService(
    ILogger<SteamService> log,
    IDbContextFactory<SteamDbContext> contextFactory,
    ICacheService cacheService) : ISteamService
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleSharp/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";

    private const string AppTypeGame = "game";
    private const string AppTypeDlc = "dlc";
    private const string GameSchemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/";

    private static readonly Secrets Secrets = new();

    private readonly Dictionary<string, SteamCache> _caches = new()
    {
        {
            AppTypeGame,
            new SteamCache(
                $"https://api.steampowered.com/IStoreService/GetAppList/v1/?max_results=50000&include_games=1&key={Secrets.SteamWebApiKey()}",
                typeof(SteamAppsV1),
                AppTypeGame
            )
        },
        {
            AppTypeDlc,
            new SteamCache(
                $"https://api.steampowered.com/IStoreService/GetAppList/v1/?max_results=50000&include_games=0&include_dlc=1&key={Secrets.SteamWebApiKey()}",
                typeof(SteamAppsV1),
                AppTypeDlc
            )
        }
    };

    public bool IsInitialized()
    {
        try
        {
            using var context = contextFactory.CreateDbContext();
            var count = context.SteamApps.Count();
            var dbPath = context.Database.GetDbConnection().DataSource;

            // Check if database exists and has data
            return count > 0 && !string.IsNullOrEmpty(dbPath) && File.Exists(dbPath);
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeInBackground(Action<string> statusCallback)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var count = await context.SteamApps.CountAsync().ConfigureAwait(false);
        var dbPath = context.Database.GetDbConnection().DataSource;
        var needsUpdate = count == 0 ||
                          (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath) &&
                           DateTime.Now.Subtract(File.GetLastWriteTimeUtc(dbPath)).TotalDays >= 1);

        if (!needsUpdate)
        {
            statusCallback("Database is up to date.");
            return;
        }

        foreach (var (appType, steamCache) in _caches)
        {
            statusCallback($"Updating {appType} cache in background...");
            log.LogInformation("Updating cache ({AppType}) in background...", appType);

            using var client = new HttpClient();
            var cacheRaw = new HashSet<SteamApp>();
            bool haveMoreResults;
            var lastAppId = 0L;

            do
            {
                var uri = lastAppId > 0
                    ? $"{steamCache.SteamUri}&last_appid={lastAppId}"
                    : steamCache.SteamUri;

                var response = await client.GetAsync(uri).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var steamApps = DeserializeSteamApps(steamCache.ApiVersion, responseBody);

                if (steamApps?.AppList?.Apps is not null)
                {
                    foreach (var app in steamApps.AppList.Apps) cacheRaw.Add(app);
                    haveMoreResults = steamApps.AppList.HaveMoreResults;
                    lastAppId = steamApps.AppList.LastAppid;
                }
                else
                {
                    break;
                }
            } while (haveMoreResults);

            // Prepare apps for insertion
            var cache = cacheRaw.Select(app => new SteamApp
            {
                AppId = app.AppId,
                Name = app.Name,
                ComparableName = PrepareStringToCompare(app.Name),
                AppType = steamCache.SteamAppType,
                LastModified = app.LastModified,
                PriceChangeNumber = app.PriceChangeNumber
            }).ToList();

            // Clear existing entries and add new ones
            await context.SteamApps
                .Where(x => x.AppType == steamCache.SteamAppType)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.SteamApps.AddRangeAsync(cache).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            statusCallback($"Updated {appType}: {cache.Count} entries");
            log.LogInformation("Cache updated for {AppType}: {Count} entries", appType, cache.Count);
        }
    }

    public async Task Initialize()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // Apply migrations
        await context.Database.MigrateAsync().ConfigureAwait(false);

        var count = await context.SteamApps.CountAsync().ConfigureAwait(false);
        var dbPath = context.Database.GetDbConnection().DataSource;
        var needsUpdate = count == 0 ||
                          (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath) &&
                           DateTime.Now.Subtract(File.GetLastWriteTimeUtc(dbPath)).TotalDays >= 1);

        if (!needsUpdate) return;

        foreach (var (appType, steamCache) in _caches)
        {
            log.LogInformation("Updating cache ({AppType})...", appType);

            using var client = new HttpClient();
            var cacheRaw = new HashSet<SteamApp>();
            bool haveMoreResults;
            var lastAppId = 0L;

            do
            {
                var uri = lastAppId > 0
                    ? $"{steamCache.SteamUri}&last_appid={lastAppId}"
                    : steamCache.SteamUri;

                var response = await client.GetAsync(uri).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var steamApps = DeserializeSteamApps(steamCache.ApiVersion, responseBody);

                if (steamApps?.AppList?.Apps is not null)
                {
                    foreach (var app in steamApps.AppList.Apps) cacheRaw.Add(app);
                    haveMoreResults = steamApps.AppList.HaveMoreResults;
                    lastAppId = steamApps.AppList.LastAppid;
                }
                else
                {
                    break;
                }
            } while (haveMoreResults);

            // Prepare apps for insertion
            var cache = cacheRaw.Select(app => new SteamApp
            {
                AppId = app.AppId,
                Name = app.Name,
                ComparableName = PrepareStringToCompare(app.Name),
                AppType = steamCache.SteamAppType,
                LastModified = app.LastModified,
                PriceChangeNumber = app.PriceChangeNumber
            }).ToList();

            // Clear existing entries and add new ones
            await context.SteamApps
                .Where(x => x.AppType == steamCache.SteamAppType)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.SteamApps.AddRangeAsync(cache).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            log.LogInformation("Cache updated for {AppType}: {Count} entries", appType, cache.Count);
        }
    }

    /// <summary>
    ///     Streams apps by name using async enumerable for efficient memory usage
    /// </summary>
    public async IAsyncEnumerable<SteamApp> StreamAppsByName(string name,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var searchTerms = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var query = context.SteamApps
            .AsNoTracking()
            .Where(x => x.AppType == AppTypeGame)
            .AsAsyncEnumerable();

        await foreach (var app in query.WithCancellation(cancellationToken).ConfigureAwait(false))
            // Filter by search terms
            if (searchTerms.All(term => app.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                yield return app;
    }

    public async Task<IEnumerable<SteamApp>> GetListOfAppsByName(string name)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var query = await context.SteamApps
            .AsNoTracking()
            .Where(x => x.AppType == AppTypeGame)
            .ToListAsync()
            .ConfigureAwait(false);

        return query.Search(x => x.Name)
            .SetCulture(StringComparison.OrdinalIgnoreCase)
            .ContainingAll(name.Split(' '));
    }

    public async Task<SteamApp?> GetAppByName(string name)
    {
        log.LogInformation("Trying to get app {Name}", name);

        var comparableName = PrepareStringToCompare(name);
        var cacheKey = $"app:name:{comparableName}";

        return await cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var app = await context.SteamApps
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppType == AppTypeGame && x.ComparableName == comparableName)
                .ConfigureAwait(false);

            if (app is not null) log.LogInformation("Successfully got app {App}", app);

            return app;
        }, TimeSpan.FromHours(2)).ConfigureAwait(false);
    }

    public async Task<SteamApp?> GetAppById(int appid)
    {
        log.LogInformation("Trying to get app with ID {AppId}", appid);

        var cacheKey = $"app:id:{appid}";

        return await cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var app = await context.SteamApps
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppType == AppTypeGame && x.AppId == appid)
                .ConfigureAwait(false);

            if (app is not null) log.LogInformation("Successfully got app {App}", app);

            return app;
        }, TimeSpan.FromHours(2)).ConfigureAwait(false);
    }

    public async Task<List<Achievement>> GetListOfAchievements(SteamApp? steamApp)
    {
        if (steamApp is null) return [];

        log.LogInformation("Getting achievements for App {SteamApp}", steamApp);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        var apiUrl = $"{GameSchemaUrl}?key={Secrets.SteamWebApiKey()}&appid={steamApp.AppId}&l=en";

        try
        {
            var response = await client.GetAsync(apiUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var jsonResponse = JsonDocument.Parse(responseBody);

            if (jsonResponse.RootElement.TryGetProperty("game", out var game) &&
                game.TryGetProperty("availableGameStats", out var stats) &&
                stats.TryGetProperty("achievements", out var achievementData))
                return JsonSerializer.Deserialize<List<Achievement>>(achievementData.GetRawText()) ?? [];
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to get achievements for app {AppId}", steamApp.AppId);
        }

        return [];
    }

    public async Task<List<Stat>> GetListOfStats(SteamApp? steamApp)
    {
        if (steamApp is null) return [];

        log.LogInformation("Getting stats for App {SteamApp}", steamApp);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        var apiUrl = $"{GameSchemaUrl}?key={Secrets.SteamWebApiKey()}&appid={steamApp.AppId}&l=en";

        try
        {
            var response = await client.GetAsync(apiUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var jsonResponse = JsonDocument.Parse(responseBody);

            if (jsonResponse.RootElement.TryGetProperty("game", out var game) &&
                game.TryGetProperty("availableGameStats", out var stats) &&
                stats.TryGetProperty("stats", out var statsData))
            {
                // Deserialize Steam API stats format
                var steamStats = JsonSerializer.Deserialize<List<SteamStat>>(statsData.GetRawText());
                if (steamStats == null) return [];

                // Convert to Goldberg format
                return steamStats.Select(s => new Stat
                {
                    Name = s.Name,
                    Default = s.DefaultValue.ToString(CultureInfo.InvariantCulture),
                    Global = "0",
                    Type = InferStatType(s.DefaultValue)
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to get stats for app {AppId}", steamApp.AppId);
        }

        return [];
    }

    public async Task<List<DlcApp>> GetListOfDlc(SteamApp? steamApp, bool useSteamDb)
    {
        if (steamApp is null)
        {
            log.LogError("Could not get DLC: Invalid Steam App");
            return [];
        }

        log.LogInformation("Get DLC for App {SteamApp}", steamApp);

        var dlcList = new List<DlcApp>();

        try
        {
            var steamAppDetails = await AppDetails.GetAsync(steamApp.AppId).ConfigureAwait(false);

            if (steamAppDetails.Type != AppTypeGame)
            {
                log.LogError("Could not get DLC: Steam App is not of type \"game\"");
                return dlcList;
            }

            await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            foreach (var dlcId in steamAppDetails.DLC)
            {
                var result = await context.SteamApps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AppType == AppTypeDlc && x.AppId == dlcId)
                    .ConfigureAwait(false);

                var dlcApp = result is not null
                    ? new DlcApp(result)
                    : new DlcApp
                    {
                        AppId = dlcId,
                        Name = $"Unknown DLC {dlcId}",
                        ComparableName = $"unknownDlc{dlcId}",
                        AppType = AppTypeDlc
                    };

                dlcList.Add(dlcApp);
                log.LogDebug("{AppId}={Name}", dlcApp.AppId, dlcApp.Name);
            }

            log.LogInformation("Got DLC successfully...");

            if (!useSteamDb) return dlcList;

            // Get additional DLC from SteamDB
            await GetDlcFromSteamDb(steamApp, dlcList).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error getting DLC list for app {AppId}", steamApp.AppId);
        }

        return dlcList;
    }

    private static string InferStatType(double value)
    {
        // If value has decimals, it's float; otherwise int
        // avgrate type needs game-specific knowledge, default to float for decimals
        return value % 1 == 0 ? "int" : "float";
    }

    private async Task GetDlcFromSteamStore(SteamApp steamApp, List<DlcApp> dlcList)
    {
        log.LogInformation("Getting DLC from Steam Store API for {SteamApp}", steamApp);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var steamStoreUrl = $"https://store.steampowered.com/dlc/{steamApp.AppId}/ajaxgetdlclist";
        var response = await client.GetAsync(steamStoreUrl).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var jsonDoc = JsonDocument.Parse(responseBody);

        if (!jsonDoc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("dlcs", out var dlcs))
        {
            log.LogWarning("No DLC data found in Steam Store API response");
            return;
        }

        foreach (var dlc in dlcs.EnumerateArray())
        {
            if (!dlc.TryGetProperty("id", out var idElement) ||
                !int.TryParse(idElement.GetString(), out var dlcId))
                continue;

            var dlcName = dlc.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? $"Unknown DLC {dlcId}"
                : $"Unknown DLC {dlcId}";

            var dlcApp = new DlcApp
            {
                AppId = dlcId,
                Name = dlcName,
                ComparableName = PrepareStringToCompare(dlcName),
                AppType = AppTypeDlc
            };

            var existingIndex = dlcList.FindIndex(x => x.AppId == dlcApp.AppId);
            if (existingIndex > -1)
            {
                if (dlcList[existingIndex].Name.Contains("Unknown DLC"))
                    dlcList[existingIndex] = dlcApp;
            }
            else
            {
                dlcList.Add(dlcApp);
            }
        }

        dlcList.ForEach(x => log.LogDebug("{AppId}={Name}", x.AppId, x.Name));
        log.LogInformation("Got {Count} DLC from Steam Store API successfully", dlcs.GetArrayLength());
    }

    private async Task GetDlcFromSteamDb(SteamApp steamApp, List<DlcApp> dlcList)
    {
        // Try Steam Store API first (more reliable)
        try
        {
            await GetDlcFromSteamStore(steamApp, dlcList).ConfigureAwait(false);
            return; // If successful, we're done
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not get DLC from Steam Store API, falling back to SteamDB scraping...");
        }

        // Fallback to SteamDB scraping
        try
        {
            var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            log.LogInformation("Get SteamDB App {SteamApp}", steamApp);
            var response = await client.GetAsync(steamDbUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(responseBody);

            var dlcSection = doc.QuerySelector("#dlc");
            if (dlcSection is null)
            {
                log.LogError("Could not get DLC from SteamDB!");
                return;
            }

            log.LogInformation("Got list of DLC from SteamDB.");
            var appElements = dlcSection.QuerySelectorAll(".app");

            foreach (var element in appElements)
            {
                var dlcIdStr = element.GetAttribute("data-appid");
                if (string.IsNullOrEmpty(dlcIdStr) || !int.TryParse(dlcIdStr, out var dlcId))
                    continue;

                var cells = element.QuerySelectorAll("td");
                var dlcName = cells.Length > 1
                    ? cells[1].TextContent.Replace("\n", "").Trim()
                    : $"Unknown DLC {dlcId}";

                var dlcApp = new DlcApp
                {
                    AppId = dlcId,
                    Name = dlcName,
                    ComparableName = PrepareStringToCompare(dlcName),
                    AppType = AppTypeDlc
                };

                var existingIndex = dlcList.FindIndex(x => x.AppId == dlcApp.AppId);
                if (existingIndex > -1)
                {
                    if (dlcList[existingIndex].Name.Contains("Unknown DLC")) dlcList[existingIndex] = dlcApp;
                }
                else
                {
                    dlcList.Add(dlcApp);
                }
            }

            dlcList.ForEach(x => log.LogDebug("{AppId}={Name}", x.AppId, x.Name));
            log.LogInformation("Got DLC from SteamDB successfully...");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Could not get DLC from SteamDB! Skipping...");
        }
    }

    private static SteamApps? DeserializeSteamApps(Type type, string cacheString)
    {
        try
        {
            return type == typeof(SteamAppsV2)
                ? JsonSerializer.Deserialize<SteamAppsV2>(cacheString)
                : JsonSerializer.Deserialize<SteamAppsV1>(cacheString);
        }
        catch
        {
            return null;
        }
    }

    private static string PrepareStringToCompare(string name)
    {
        return AlphaNumOnlyRegex().Replace(name, "").ToLower();
    }

    [GeneratedRegex("[^0-9a-zA-Z]+")]
    private static partial Regex AlphaNumOnlyRegex();
}