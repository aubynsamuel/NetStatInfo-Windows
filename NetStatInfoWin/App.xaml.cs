using Microsoft.UI.Xaml;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Services;
using NetStatInfoWin.ViewModels;

namespace NetStatInfoWin;

public partial class App : Application
{
    private static IResourceService _resourceService = null!;
    private static MainViewModel _mainViewModel = null!;

    internal static MainWindow MainWindowInstance { get; private set; } = null!;

    internal static MainViewModel MainViewModel => _mainViewModel;

    internal static IResourceService ResourceService => _resourceService;

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
    }

    private static void ConfigureServices()
    {
        _resourceService = new ResourceService();
        var usageWindowProvider = new UsageWindowProvider(TimeProvider.System);
        var displayNameResolver = new AppUsageDisplayNameResolver();
        var attributedAppUsageService = new AttributedAppUsageService(displayNameResolver);
        var appUsageAggregator = new AppUsageAggregator(_resourceService);
        var delayProvider = new SystemDelayProvider();

        _mainViewModel = new MainViewModel(
            attributedAppUsageService,
            usageWindowProvider,
            appUsageAggregator,
            delayProvider,
            _resourceService);
    }
}
