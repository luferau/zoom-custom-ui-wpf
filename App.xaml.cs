using System.Windows;
using zoom_custom_ui_wpf.ViewModels;
using zoom_custom_ui_wpf.Views;

namespace zoom_custom_ui_wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            var mainViewModel = new MainViewModel();

            mainWindow.DataContext = mainViewModel;
            mainWindow.ShowDialog();
        }

    }
}
