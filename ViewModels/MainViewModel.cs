using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using zoom_custom_ui_wpf.Models;
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
            _zoomService.InitializedChanged += ZoomServiceOnInitializedChanged;
            _zoomService.InitializationAsync();
        }

        private void ZoomServiceOnInitializedChanged(object sender, bool state)
        {
            if (state)
            {
                VideoDevices = _zoomService.EnumerateVideoDevices()?.ToList();
                MicDevices = _zoomService.EnumerateMicDevices()?.ToList();
                SpeakerDevices = _zoomService.EnumerateSpeakerDevices()?.ToList();
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        #region Settings

        private List<ZoomDevice> _videoDevices;
        public List<ZoomDevice> VideoDevices
        {
            get => _videoDevices;
            set => SetProperty(ref _videoDevices, value);
        }

        private List<ZoomDevice> _micDevices;
        public List<ZoomDevice> MicDevices
        {
            get => _micDevices;
            set => SetProperty(ref _micDevices, value);
        }

        private List<ZoomDevice> _speakerDevices;
        public List<ZoomDevice> SpeakerDevices
        {
            get => _speakerDevices;
            set => SetProperty(ref _speakerDevices, value);
        }

        #endregion

        #region Commands

        private ICommand _SubmitCommand;
        public ICommand LoginCommand
        {
            get
            {
                if (_SubmitCommand == null)
                {
                    _SubmitCommand = new RelayCommand(param => Login(), null);
                }
                return _SubmitCommand;
            }
        }

        private async void Login()
        {
            
        }

        #endregion
    }
}
