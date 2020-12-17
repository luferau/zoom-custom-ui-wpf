using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Nito.Mvvm;
using zoom_custom_ui_wpf.Helpers;
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
            UserName = _credentialsService.GetUserName();
            MeetingNumber = _credentialsService.GetMeetingNumber();
            Password = _credentialsService.GetPassword();

            _zoomService = zoomService;
            _zoomService.InitializedChanged += ZoomServiceOnInitializedChanged;
            _zoomService.MeetingStatusChanged += ZoomServiceOnMeetingStatusChanged;

            // Running async code from constructor
            // Thanks Stephen Cleary and his Nito.Mvvm.Async library
            // https://blog.stephencleary.com/2013/01/async-oop-2-constructors.html
            InitializationNotifier = NotifyTask.Create(InitializationAsync());
        }

        public NotifyTask InitializationNotifier { get; private set; }

        private async Task InitializationAsync()
        {
            // Busy indicator on
            BusyText = "Zoom SDK initialization...";
            IsBusy = true;

            await _zoomService.InitializationAsync(_credentialsService.GetAppKey(), _credentialsService.GetAppSecret());

            // Busy indicator off
            IsBusy = false;
        }

        public IZoomHost ZoomHost => _zoomService;

        private void ZoomServiceOnInitializedChanged(object sender, bool initialized)
        {
            if (initialized)
            {
                VideoDevices = _zoomService.EnumerateVideoDevices()?.ToList();
                if (VideoDevices != null && VideoDevices.Count > 0)
                    SelectedVideoDevice = VideoDevices.First();
                
                MicDevices = _zoomService.EnumerateMicDevices()?.ToList();
                if (MicDevices != null && MicDevices.Count > 0)
                    SelectedMicDevice = MicDevices.First();
                
                SpeakerDevices = _zoomService.EnumerateSpeakerDevices()?.ToList();
                if (SpeakerDevices != null && SpeakerDevices.Count > 0)
                    SelectedSpeakerDevice = SpeakerDevices.First();
            }
        }

        private void ZoomServiceOnMeetingStatusChanged(object sender, string statusString)
        {
            Status = statusString;
        }

        #region Commands

        private ICommand _joinCommand;
        public ICommand JoinCommand => _joinCommand ?? (_joinCommand = new RelayCommand(execute: param => Join(), canExecute: null));

        private async void Join()
        {
            // Busy indicator on
            BusyText = "Joining...";
            IsBusy = true;

            _zoomService.ApplySettings(settings: new ZoomSettings
            {
                // Video
                HardwareEncode = true,
                VideoMirrorEffect = false,

                // Audio
                AutoJoinAudio = true
            });
            
            _zoomService.SetVideoDevice(SelectedVideoDevice);
            _zoomService.SetMicDevice(SelectedMicDevice);
            _zoomService.SetSpeakerDevice(SelectedSpeakerDevice);

            var joinResult = await _zoomService.JoinMeetingAsync(UserName, ulong.Parse(MeetingNumber), Password);

            _zoomService.UnmuteVideo();

            // Busy indicator off
            IsBusy = false;
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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyText;
        public string BusyText
        {
            get => _busyText;
            set => SetProperty(ref _busyText, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value.SplitByCharacters(40, Environment.NewLine));
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
