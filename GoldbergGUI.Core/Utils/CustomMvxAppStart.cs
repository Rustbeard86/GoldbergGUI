using System;
using System.Threading.Tasks;
using MvvmCross.Exceptions;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core.Utils;

// ReSharper disable once ClassNeverInstantiated.Global
public class CustomMvxAppStart<TViewModel>(IMvxApplication application, IMvxNavigationService navigationService)
    : MvxAppStart<TViewModel>(application, navigationService)
    where TViewModel : IMvxViewModel
{
    protected override async Task NavigateToFirstViewModel(object hint = null)
    {
        //return base.NavigateToFirstViewModel(hint);
        try
        {
            await NavigationService.Navigate<TViewModel>().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw exception.MvxWrap("Problem navigating to ViewModel {0}", typeof(TViewModel).Name);
        }
    }
}