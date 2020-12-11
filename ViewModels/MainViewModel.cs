using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using zoom_custom_ui_wpf.Models;
using zoom_custom_ui_wpf.Services.Credentials;
using zoom_custom_ui_wpf.Services.Zoom;

namespace zoom_custom_ui_wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IZoomService _zoomService;
        private readonly ICredentialsService _credentialsService;

        public MainViewModel(IZoomService zoomService, ICredentialsService credentialsService)
        {
            Title = "zoom-custom-ui-wpf";

            _credentialsService = credentialsService;

            _zoomService = zoomService;
            _zoomService.InitializedChanged += ZoomServiceOnInitializedChanged;
            _zoomService.InitializationAsync();

            UserName = _credentialsService.GetUserName();
            MeetingNumber = _credentialsService.GetMeetingNumber();
            Password = _credentialsService.GetPassword();
        }

        public IZoomHost ZoomHost => _zoomService;

        private void ZoomServiceOnInitializedChanged(object sender, bool state)
        {
            if (state)
            {
                VideoDevices = _zoomService.EnumerateVideoDevices()?.ToList();
                MicDevices = _zoomService.EnumerateMicDevices()?.ToList();
                SpeakerDevices = _zoomService.EnumerateSpeakerDevices()?.ToList();
            }
        }

        #region Commands

        private ICommand _joinCommand;
        public ICommand JoinCommand => _joinCommand ?? (_joinCommand = new RelayCommand(execute: param => Join(), canExecute: null));

        private void Join()
        {
            _zoomService.ApplySettings(settings: new ZoomSettings
            {
                // Video
                HardwareEncode = true,

                // Audio
                AutoJoinAudio = true
            });
            
            _zoomService.SetVideoDevice(SelectedVideoDevice);
            _zoomService.SetMicDevice(SelectedMicDevice);
            _zoomService.SetSpeakerDevice(SelectedSpeakerDevice);

            _zoomService.JoinMeeting(UserName, ulong.Parse(MeetingNumber), Password);
        }

        private ICommand _leaveCommand;
        public ICommand LeaveCommand => _leaveCommand ?? (_leaveCommand = new RelayCommand(execute: param => Leave(), canExecute: null));

        private void Leave()
        {
            _zoomService.LeaveMeeting();
        }

        #endregion

        #region Binding properties

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(storage: ref _title, value: value);
        }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(storage: ref _userName, value: value);
        }

        private string _meetingNumber;
        public string MeetingNumber
        {
            get => _meetingNumber;
            set => SetProperty(storage: ref _meetingNumber, value: value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(storage: ref _password, value: value);
        }

        #endregion
        
        #region Zoom hardware settings

        private List<ZoomDevice> _videoDevices;
        public List<ZoomDevice> VideoDevices
        {
            get => _videoDevices;
            set => SetProperty(storage: ref _videoDevices, value: value);
        }

        private ZoomDevice _selectedVideoDevice;
        public ZoomDevice SelectedVideoDevice
        {
            get => _selectedVideoDevice;
            set => SetProperty(ref _selectedVideoDevice, value);
        }

        private List<ZoomDevice> _micDevices;
        public List<ZoomDevice> MicDevices
        {
            get => _micDevices;
            set => SetProperty(storage: ref _micDevices, value: value);
        }

        private ZoomDevice _selectedMicDevice;
        public ZoomDevice SelectedMicDevice
        {
            get => _selectedMicDevice;
            set => SetProperty(ref _selectedMicDevice, value);
        }

        private List<ZoomDevice> _speakerDevices;
        public List<ZoomDevice> SpeakerDevices
        {
            get => _speakerDevices;
            set => SetProperty(storage: ref _speakerDevices, value: value);
        }

        private ZoomDevice _selectedSpeakerDevice;
        public ZoomDevice SelectedSpeakerDevice
        {
            get => _selectedSpeakerDevice;
            set => SetProperty(ref _selectedSpeakerDevice, value);
        }

        #endregion
    }
}
