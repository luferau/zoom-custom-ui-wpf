using System.Windows;
using System.Windows.Interop;
using zoom_custom_ui_wpf.ViewModels;

namespace zoom_custom_ui_wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Window _window;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region UI Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _window = GetWindow(this);

            SetWindowHandle();
        }

        private void VideoContainerBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetVideoContainerPosition();
        }

        #endregion

        private void SetVideoContainerPosition()
        {
            var videoContainerRect = BoundsRelativeTo(VideoContainerBorder, _window);
            ((MainViewModel) DataContext).ZoomHost.SetVideoContainerPosition(videoContainerRect);
        }

        private Rect BoundsRelativeTo(UIElement element, UIElement relativeTo)
        {
            var topLeft = element.TranslatePoint(new Point(0, 0), relativeTo);
            var bottomRight = element.TranslatePoint(new Point(element.RenderSize.Width, element.RenderSize.Height),
                relativeTo);

            return new Rect(topLeft, bottomRight);
        }

        private void SetWindowHandle()
        {
            if (_window != null)
            {
                var windowHandle = new WindowInteropHelper(_window).EnsureHandle();
                ((MainViewModel) DataContext).ZoomHost.SetWindowHandle(windowHandle);
            }
        }
    }
}
