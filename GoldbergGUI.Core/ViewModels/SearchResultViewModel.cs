using System.Collections.Generic;
using System.Threading.Tasks;
using GoldbergGUI.Core.Models;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

#pragma warning disable CA1873

namespace GoldbergGUI.Core.ViewModels;

public class SearchResultViewModel(
    ILogger<SearchResultViewModel> log,
    ILoggerFactory loggerFactory,
    IMvxNavigationService navigationService)
    : MvxNavigationViewModel<IEnumerable<SteamApp>>(loggerFactory, navigationService)
{
    private readonly IMvxNavigationService _navigationService = navigationService;
    private IEnumerable<SteamApp> _apps;

    public IEnumerable<SteamApp> Apps
    {
        get => _apps;
        set
        {
            _apps = value;
            RaisePropertyChanged(() => Apps);
        }
    }

    public SteamApp Selected { get; set; }

    public IMvxCommand SaveCommand => new MvxAsyncCommand(Save);

    public IMvxCommand CloseCommand => new MvxAsyncCommand(Close);

    public TaskCompletionSource<object> CloseCompletionSource { get; set; }

    public override void Prepare(IEnumerable<SteamApp> parameter)
    {
        Apps = parameter;
    }

    public override void ViewDestroy(bool viewFinishing = true)
    {
        if (viewFinishing && CloseCompletionSource != null && !CloseCompletionSource.Task.IsCompleted &&
            !CloseCompletionSource.Task.IsFaulted)
            CloseCompletionSource?.TrySetCanceled();

        base.ViewDestroy(viewFinishing);
    }

    private async Task Save()
    {
        if (Selected != null)
        {
            log.LogInformation("Successfully got app {Selected}", Selected);
            await _navigationService.Close(this).ConfigureAwait(false);
        }
    }

    private async Task Close()
    {
        await _navigationService.Close(this).ConfigureAwait(false);
    }
}