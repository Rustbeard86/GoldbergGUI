using GoldbergGUI.Core.Models;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoldbergGUI.Core.ViewModels
{
    public class SearchResultViewModel : MvxNavigationViewModel<IEnumerable<SteamApp>>
    {
        private readonly IMvxNavigationService _navigationService;
        private readonly ILogger<SearchResultViewModel> _log;
        private IEnumerable<SteamApp> _apps;

        public SearchResultViewModel(ILogger<SearchResultViewModel> log, ILoggerFactory loggerFactory, 
            IMvxNavigationService navigationService) : base(loggerFactory, navigationService)
        {
            _log = log;
            _navigationService = navigationService;
        }

        public override void Prepare(IEnumerable<SteamApp> parameter)
        {
            Apps = parameter;
        }

        public IEnumerable<SteamApp> Apps
        {
            get => _apps;
            set
            {
                _apps = value;
                RaisePropertyChanged(() => Apps);
            }
        }

        public SteamApp Selected
        {
            get;
            set;
        }

        public IMvxCommand SaveCommand => new MvxAsyncCommand(Save);

        public IMvxCommand CloseCommand => new MvxAsyncCommand(Close);

        public TaskCompletionSource<object> CloseCompletionSource { get; set; }

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
                _log.LogInformation("Successfully got app {Selected}", Selected);
                await _navigationService.Close(this).ConfigureAwait(false);
            }
        }

        private async Task Close()
        {
            await _navigationService.Close(this).ConfigureAwait(false);
        }
    }
}