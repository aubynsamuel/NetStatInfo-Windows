using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace NetStatInfoWin;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new SizeInt32(1320, 900));

        RootFrame.Navigate(typeof(MainPage));
    }
}
