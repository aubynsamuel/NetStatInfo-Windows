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

        var processMetadataService = new ProcessMetadataService();
        var processConnectionSummarizer = new ProcessConnectionSummarizer(processMetadataService, _resourceService);
        var connectionTableReader = new OwnedConnectionTableReader();
        var networkSnapshotService = new NetworkSnapshotService(connectionTableReader, processConnectionSummarizer);
        var sessionUsageAggregator = new SessionUsageAggregator(_resourceService);

        _mainViewModel = new MainViewModel(networkSnapshotService, sessionUsageAggregator, _resourceService);
    }
}
