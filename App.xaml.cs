using System.Windows;
using zoom_custom_ui_wpf.Services.Credentials;
using zoom_custom_ui_wpf.Services.Log;
using zoom_custom_ui_wpf.Services.Settings;
using zoom_custom_ui_wpf.Services.Zoom;
using zoom_custom_ui_wpf.ViewModels;
using zoom_custom_ui_wpf.Views;

namespace zoom_custom_ui_wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Create app services
            IApplicationSettings appSettings = new ApplicationSettings();
            ILogService logService = new DebugLogService();
            ICredentialsService credentialsService = new CredentialsService();

            IZoomService zoomService = new ZoomService(logService);

            // Create and show MainWindow
            var mainWindow = new MainWindow();
            var mainViewModel = new MainViewModel(zoomService, credentialsService);

            mainWindow.DataContext = mainViewModel;
            mainWindow.ShowDialog();
        }

    }
}
