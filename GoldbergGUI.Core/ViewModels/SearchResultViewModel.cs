using GoldbergGUI.Core.Models;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core.ViewModels;

public class SearchResultViewModel(
    ILogger<SearchResultViewModel> log,
    ILoggerFactory loggerFactory,
    IMvxNavigationService navigationService)
    : MvxNavigationViewModel<IEnumerable<SteamApp>>(loggerFactory, navigationService)
{
    private readonly IMvxNavigationService _navigationService = navigationService;

    public IEnumerable<SteamApp> Apps
    {
        get => field;
        set
        {
            field = value;
            RaisePropertyChanged(() => Apps);
        }
    } = [];

    public SteamApp? Selected { get; set; }

    public IMvxCommand SaveCommand => new MvxAsyncCommand(Save);

    public IMvxCommand CloseCommand => new MvxAsyncCommand(Close);

    public TaskCompletionSource<object?>? CloseCompletionSource { get; set; }

    public override void Prepare(IEnumerable<SteamApp> parameter)
    {
        Apps = parameter;
    }

    public override void ViewDestroy(bool viewFinishing = true)
    {
        if (viewFinishing && CloseCompletionSource is { Task.IsCompleted: false, Task.IsFaulted: false })
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