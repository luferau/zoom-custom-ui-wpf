using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using zoom_custom_ui_wpf.Models;
using zoom_custom_ui_wpf.Services.Credentials;
using zoom_custom_ui_wpf.Services.Log;
using zoom_custom_ui_wpf.Services.Settings;
using ZOOM_SDK_DOTNET_WRAP;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public class ZoomService : IZoomService
    {
        private readonly IApplicationSettings _appSettingsService;
        private readonly ICredentialsService _credentialsService;
        private readonly ILogService _logService;

        private TaskCompletionSource<bool> _authTaskCompletion;

        public ZoomService(
            IApplicationSettings appSettingsService,
            ICredentialsService credentialsService, 
            ILogService logService)
        {
            _appSettingsService = appSettingsService;
            _credentialsService = credentialsService;
            _logService = logService;
        }

        private bool _initialized;
        public bool Initialized
        {
            get => _initialized;
            private set
            {
                _initialized = value;
                OnInitializedChanged(Initialized);
            }
        }

        public async Task<bool> InitializationAsync()
        {
            var sdkInitialized = InitializeSdk();
            if (!sdkInitialized) return false;

            Initialized = await SdkAuthenticationAsync();
            return Initialized;
        }

        public void Cleanup()
        {
            if (Initialized)
            {
                Initialized = false;

                // Zoom SDK clean up
                ZOOM_SDK_DOTNET_WRAP.SDKError error = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.CleanUp();

                _logService.Log($"ZOOM cleanup result: {error}");
            }
        }

        private bool InitializeSdk()
        {
            ZOOM_SDK_DOTNET_WRAP.InitParam param = new ZOOM_SDK_DOTNET_WRAP.InitParam
            {
                web_domain = "https://zoom.us",
                config_opts = { optionalFeatures = 1 << 5 }   // Using Zoom Customized UI
            };
            ZOOM_SDK_DOTNET_WRAP.SDKError error = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.Initialize(param);

            _logService.Log($"ZOOM InitSDK request result: {error}");

            return error == ZOOM_SDK_DOTNET_WRAP.SDKError.SDKERR_SUCCESS;
        }

        private Task<bool> SdkAuthenticationAsync()
        {
            // Register callbacks
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onAuthenticationReturn(OnAuthenticationReturn);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLoginRet(OnLoginRet);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLogout(OnLogout);

            ZOOM_SDK_DOTNET_WRAP.AuthParam authParam = new AuthParam
            {
                appKey = _credentialsService.GetAppKey(),
                appSecret = _credentialsService.GetAppSecret()
            };

            ZOOM_SDK_DOTNET_WRAP.SDKError error = CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().SDKAuth(authParam);

            _logService.Log($"ZOOM SDKAuth request result: {error}");

            _authTaskCompletion = new TaskCompletionSource<bool>();
            return _authTaskCompletion.Task;
        }

        #region Zoom wrapper callbacks

        private void OnAuthenticationReturn(AuthResult result)
        {
            _logService.Log($"ZOOM OnAuthenticationReturn callback AuthResult: {result}");

            if (result == ZOOM_SDK_DOTNET_WRAP.AuthResult.AUTHRET_SUCCESS)
            {
                _authTaskCompletion.SetResult(true);
            }
            else
            {
                _authTaskCompletion.SetResult(false);
            }
        }

        private void OnLoginRet(LOGINSTATUS status, IAccountInfo pAccountInfo)
        {
            _logService.Log($"ZOOM OnLoginRet callback LoginStatus: {status}");
        }

        private void OnLogout()
        {
            _logService.Log("ZOOM OnLogout callback");
        }

        #endregion

        #region Events

        public event EventHandler<bool> InitializedChanged;

        protected virtual void OnInitializedChanged(bool state)
        {
            InitializedChanged?.Invoke(this, state);
        }

        #endregion

        private void SetDevices()
        {
            var devices = EnumerateVideoDevices()?.ToList();
            var videoDeviceId = _appSettingsService.GetVideoDeviceId();

            if (videoDeviceId != null && devices != null)
            {
                var i = devices.FindIndex(device => device.Id == videoDeviceId);
                if (i != -1)
                    SelectVideoDevice(devices[i].Id);
            }

            devices = EnumerateMicDevices()?.ToList();
            var micDeviceId = _appSettingsService.GetMicDeviceId();
            if (micDeviceId != null && devices != null)
            {
                var i = devices.FindIndex(d => d.Id == micDeviceId);
                if (i != -1)
                    SelectMicDevice(devices[i].Id, devices[i].Name);
            }

            devices = EnumerateSpeakerDevices()?.ToList();
            var speakerDeviceId = _appSettingsService.GetSpeakerDeviceId();
            if (speakerDeviceId != null && devices != null)
            {
                var i = devices.FindIndex(d => d.Id == speakerDeviceId);
                if (i != -1)
                    SelectSpeakerDevice(devices[i].Id, devices[i].Name);
            }
        }

        #region Enumerate PC audio/video devices

        public IEnumerable<ZoomDevice> EnumerateVideoDevices()
        {
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            var camList = videoSettings.GetCameraList();
            var devicesList = new List<ZoomDevice>();
            if (camList != null)
            {
                devicesList.AddRange(
                    from d in camList
                    select new ZoomDevice
                    {
                        Id = d.GetDeviceId(),
                        Name = d.GetDeviceName()
                    }
                );
            }
            return devicesList;
        }

        public IEnumerable<ZoomDevice> EnumerateMicDevices()
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var micList = audioSettings.GetMicList();
            var devicesList = new List<ZoomDevice>();
            if (micList != null)
            {
                devicesList.AddRange(
                    from d in micList
                    select new ZoomDevice
                    {
                        Id = d.GetDeviceId(),
                        Name = d.GetDeviceName()
                    }
                );
            }
            return devicesList;
        }

        public IEnumerable<ZoomDevice> EnumerateSpeakerDevices()
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var speakerList = audioSettings.GetSpeakerList();
            var devicesList = new List<ZoomDevice>();
            if (speakerList != null)
            {
                devicesList.AddRange(
                    from d in speakerList
                    select new ZoomDevice
                    {
                        Id = d.GetDeviceId(),
                        Name = d.GetDeviceName()
                    }
                );
            }
            return devicesList;
        }

        #endregion

        #region Select PC audio/video devices for use in Zoom

        public void SelectVideoDevice(string deviceId)
        {
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            videoSettings.SelectCamera(deviceId);
        }

        public void SelectMicDevice(string deviceId, string deviceName)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            audioSettings.SelectMic(deviceId, deviceName);
        }

        public void SelectSpeakerDevice(string deviceId, string deviceName)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            audioSettings.SelectSpeaker(deviceId, deviceName);
        }

        #endregion
    }
}
