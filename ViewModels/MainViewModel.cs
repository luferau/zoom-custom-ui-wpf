using zoom_custom_ui_wpf.Services.Zoom;

namespace zoom_custom_ui_wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IZoomService _zoomService;

        public MainViewModel(IZoomService zoomService)
        {
            Title = "zoom-custom-ui-wpf";

            _zoomService = zoomService;

            var initResult = _zoomService.Init();

            Text = initResult ? "Ok":"Error";

            if (initResult)
            {
                _zoomService.Login();
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }
}
