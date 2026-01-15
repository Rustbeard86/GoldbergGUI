using GoldbergGUI.Core.Extensions;
using GoldbergGUI.Core.Utils;
using GoldbergGUI.Core.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core;

/// <summary>
/// Main application class for GoldbergGUI
/// </summary>
public sealed class App : MvxApplication
{
    public override void Initialize()
    {
        var services = new ServiceCollection();
        
        // Register database
        services.AddSteamDatabase();
        
        // Register memory cache
        services.AddAppCache();
        
        var provider = services.BuildServiceProvider();
        
        // Register with MvvmCross IoC using generic overload
        if (Mvx.IoCProvider is null)
        {
            throw new InvalidOperationException("MvvmCross IoC Provider is not initialized");
        }
        
        Mvx.IoCProvider.RegisterSingleton(
            provider.GetRequiredService<IDbContextFactory<Data.SteamDbContext>>());
            
        Mvx.IoCProvider.RegisterSingleton(
            provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>());

        // Register services
        CreatableTypes()
            .EndingWith("Service")
            .AsInterfaces()
            .RegisterAsLazySingleton();

        // Register custom app start
        RegisterCustomAppStart<CustomMvxAppStart<MainViewModel>>();
    }
}

