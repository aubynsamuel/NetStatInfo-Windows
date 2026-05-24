using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NetStatInfoWin.Models;
using NetStatInfoWin.ViewModels;

namespace NetStatInfoWin;

public sealed partial class MainPage : Page
{
    internal MainViewModel ViewModel { get; } = App.MainViewModel;

    public MainPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        AutomationProperties.SetName(RefreshButton, ViewModel.RefreshAutomationName);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance.Activated += OnWindowActivated;
        ViewModel.StartPollingCommand.Execute(null);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance.Activated -= OnWindowActivated;
        ViewModel.StopPollingCommand.Execute(null);
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
    }

    private void OnRefreshAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ViewModel.RefreshCommand.Execute(null);
    }

    private async void OnSessionRangeClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.SelectRangeAsync(UsageRange.Session);
    }

    private async void OnLastHourRangeClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.SelectRangeAsync(UsageRange.LastHour);
    }

    private async void OnLastSixHoursRangeClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.SelectRangeAsync(UsageRange.LastSixHours);
    }

    private async void OnTodayRangeClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.SelectRangeAsync(UsageRange.Today);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            ViewModel.StopPollingCommand.Execute(null);
            return;
        }

        ViewModel.StartPollingCommand.Execute(null);
    }
}
