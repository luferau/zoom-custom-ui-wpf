using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        #region Events

        public event EventHandler<bool> InitializedChanged;

        protected virtual void OnInitializedChanged(bool state)
        {
            InitializedChanged?.Invoke(this, state);
        }

        #endregion

        #region Initialization

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
            RegisterAuthenticationCallbacks();

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

        #endregion

        #region Zoom authentication callbacks
        
        private void RegisterAuthenticationCallbacks()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onAuthenticationReturn(OnAuthenticationReturn);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLoginRet(OnLoginRet);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLogout(OnLogout);
        }

        private void UnRegisterAuthenticationCallBack()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Remove_CB_onAuthenticationReturn(OnAuthenticationReturn);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Remove_CB_onLoginRet(OnLoginRet);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Remove_CB_onLogout(OnLogout);
        }

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

        public void JoinMeeting(string userName, ulong meetingNumber, string password)
        {
            RegisterMeetingCallBacks();

            var joinApiParamNormal = new ZOOM_SDK_DOTNET_WRAP.JoinParam4NormalUser
            {
                meetingNumber = meetingNumber,
                userName = userName,
                psw = password
            };

            var joinParam = new ZOOM_SDK_DOTNET_WRAP.JoinParam
            {
                userType = ZOOM_SDK_DOTNET_WRAP.SDKUserType.SDK_UT_NORMALUSER,
                normaluserJoin = joinApiParamNormal
            };

            var err = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Join(joinParam);

            _logService.Log(text: $"Join Result: {err}");

            if (err == ZOOM_SDK_DOTNET_WRAP.SDKError.SDKERR_SUCCESS)
            {
                //ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingAudioController()>.EnableMuteOnEntry(false);
            }
            else // todo error handle
            {

            }
        }
        
        public void LeaveMeeting()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Leave(LeaveMeetingCmd.LEAVE_MEETING);

            CleanupVideoRenderers();
            UnRegisterMeetingCallBacks();
            CleanupVideoContainer();
        }

        public void CleanUp()
        {
            if (Initialized)
            {
                Initialized = false;

                // Zoom SDK clean up
                ZOOM_SDK_DOTNET_WRAP.SDKError error = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.CleanUp();

                _logService.Log($"ZOOM cleanup result: {error}");
            }
        }

        public void ApplySettings(ZoomSettings settings)
        {
            // Video
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            videoSettings.EnableHardwareEncode(settings.HardwareEncode);

            // Audio
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            audioSettings.EnableAutoJoinAudio(settings.AutoJoinAudio);
        }
        
        #region Zoom meeting callbacks

        private void RegisterMeetingCallBacks()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Add_CB_onMeetingStatusChanged(cb: OnMeetingStatusChanged);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Add_CB_onUserJoin(cb: OnUserJoin);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Add_CB_onUserLeft(cb: OnUserLeft);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Add_CB_onUserNameChanged(cb: OnUserNameChanged);
        }

        private void UnRegisterMeetingCallBacks()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Remove_CB_onMeetingStatusChanged(cb: OnMeetingStatusChanged);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Remove_CB_onUserJoin(cb: OnUserJoin);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Remove_CB_onUserLeft(cb: OnUserLeft);
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().Remove_CB_onUserNameChanged(cb: OnUserNameChanged);
        }

        public void OnMeetingStatusChanged(MeetingStatus status, int iResult)
        {
            _logService.Log(text: $"OnMeetingStatusChanged Callback Status: {status}");

            switch (status)
            {
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_CONNECTING:

                    var rect = new RECT
                    {
                        Right = (int)VideoContainerPosition.Right,
                        Left = (int)VideoContainerPosition.Left,
                        Bottom = (int)VideoContainerPosition.Bottom,
                        Top = (int)VideoContainerPosition.Top
                    };

                    CleanupVideoContainer();

                    var uiManager = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetCustomizedUIMgrWrap();
                    VideoContainer = uiManager.CreateVideoContainer(hParentWnd: WindowHandle, rc: rect);
                    VideoContainer.Show();

                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_DISCONNECTING:
                    LeaveMeeting();
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_ENDED:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_FAILED:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_WAITINGFORHOST:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_INMEETING:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_IDLE:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_RECONNECTING:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_UNKNOW:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_LOCKED:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_UNLOCKED:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_IN_WAITING_ROOM:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_WEBINAR_PROMOTE:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_WEBINAR_DEPROMOTE:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_JOIN_BREAKOUT_ROOM:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_LEAVE_BREAKOUT_ROOM:
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_WAITING_EXTERNAL_SESSION_KEY:
                    break;
                default:
                    break;
            }
        }

        public void OnUserJoin(Array lstUserIds)
        {
            _logService.Log("User joined...");

            if (lstUserIds == null)
                return;

            if (NormalVideoRenderers == null)
                NormalVideoRenderers = new List<INormalVideoRenderElementDotNetWrap>();

            foreach (uint userId in lstUserIds)
            {
                var user = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().GetUserByUserID(userId: userId);

                if (user != null)
                {
                    var vidRenderer = VideoContainer.CreateVideoElement(elementType: VideoRenderElementType.VideoRenderElement_NORMAL) as INormalVideoRenderElementDotNetWrap;

                    if (vidRenderer != null)
                    {
                        vidRenderer.SetPos(pos: new RECT
                        {
                            Left = 0,
                            Top = 0,
                            Right = 300,
                            Bottom = 300
                        });

                        vidRenderer.EnableShowScreenNameOnVideo(enableShow: true);
                        vidRenderer.Show();
                        vidRenderer.Subscribe(userId: userId);

                        NormalVideoRenderers.Add(item: vidRenderer);

                        _logService.Log("Session Render user: " + vidRenderer.GetCurrentRenderUserId());
                    }

                    var name = user.GetUserNameW();
                    var isMySelf = user.IsMySelf();

                    _logService.Log($"Session User name: {name} IsMySelf: {isMySelf}");
                }
            }

            UpdateNormalVideoRenderersSizes();
        }

        public void OnUserLeft(Array lstUserId)
        {
            foreach (uint userid in lstUserId)
            {
                var removed = NormalVideoRenderers.RemoveAll(match: p =>
                {
                    if (p.GetCurrentRenderUserId() == userid)
                    {
                        p.Unsubscribe(userId: userid);
                        p.Hide();
                        return true;
                    }
                    else return false;
                });
            }
        }

        public void OnUserNameChanged(uint userId, string userName)
        {

        }

        #endregion

        #region IZoomHost implementation

        public void SetVideoContainerPosition(Rect position)
        {
            VideoContainerPosition = position;
        }

        public void SetWindowHandle(IntPtr handle)
        {
            WindowHandle = handle;
        }

        #endregion

        #region Zoom SDK Custom UI

        private ICustomizedVideoContainerDotNetWrap VideoContainer { get; set; }
        private List<INormalVideoRenderElementDotNetWrap> NormalVideoRenderers { get; set; }

        private IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        private Rect _videoContainerPosition;
        private Rect VideoContainerPosition
        {
            get => _videoContainerPosition;
            set
            {
                if (value == _videoContainerPosition) return;

                _videoContainerPosition = value;

                var r = new RECT
                {
                    Right = (int)VideoContainerPosition.Right,
                    Left = (int)VideoContainerPosition.Left,
                    Bottom = (int)VideoContainerPosition.Bottom,
                    Top = (int)VideoContainerPosition.Top
                };

                if (VideoContainer != null)
                {
                    _logService.Log(text: $"VideoContainerPosition changed: {r.Left},{r.Top} - {r.Right},{r.Bottom}");
                    VideoContainer.Resize(rc: r);

                    UpdateNormalVideoRenderersSizes();
                }
            }
        }

        private void UpdateNormalVideoRenderersSizes()
        {
            if (NormalVideoRenderers == null) return;

            for (var i = 0; i < NormalVideoRenderers.Count; ++i)
            {
                var column = i % 2;
                var row = i / 2;
                var width = (int)VideoContainerPosition.Width / 2;
                var height = (int)VideoContainerPosition.Height / 2;

                var rect = new RECT
                {
                    Left = column * width,
                    Top = row * height,
                    Right = (column + 1) * width,
                    Bottom = (row + 1) * height
                };
                NormalVideoRenderers[index: i].SetPos(pos: rect);
            }
        }

        private void CleanupVideoRenderers()
        {
            if (NormalVideoRenderers != null)
            {
                NormalVideoRenderers.Clear();
                NormalVideoRenderers = null;
            }
        }

        private void CleanupVideoContainer()
        {
            if (VideoContainer == null) return;

            var uiManager = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetCustomizedUIMgrWrap();
            uiManager.DestroyAllVideoContainer();

            VideoContainer = null;
        }

        #endregion

        #region Enumerate PC audio/video devices

        public IEnumerable<ZoomDevice> EnumerateVideoDevices()
        {
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            var cameraList = videoSettings.GetCameraList();
            var devicesList = new List<ZoomDevice>();
            if (cameraList != null)
            {
                devicesList.AddRange(
                    from d in cameraList
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

        public bool SetVideoDevice(ZoomDevice device)
        {
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            var error = videoSettings.SelectCamera(device.Id);

            return error == SDKError.SDKERR_SUCCESS;
        }

        public bool SetMicDevice(ZoomDevice device)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var error = audioSettings.SelectMic(device.Id, device.Name);

            return error == SDKError.SDKERR_SUCCESS;
        }

        public bool SetSpeakerDevice(ZoomDevice device)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var error = audioSettings.SelectSpeaker(device.Id, device.Name);

            return error == SDKError.SDKERR_SUCCESS;
        }

        #endregion
    }
}
