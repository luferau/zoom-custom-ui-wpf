using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using zoom_custom_ui_wpf.Models;
using zoom_custom_ui_wpf.Services.Log;
using ZOOM_SDK_DOTNET_WRAP;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public class ZoomService : IZoomService
    {
        private readonly ILogService _logService;

        private const int AuthenticationTimeout_s = 10;
        private const int MeetingJoiningTimeout_s = 60;

        private TaskCompletionSource<bool> _authTaskCompletion;
        private TaskCompletionSource<bool> _joinTaskCompletion;

        public ZoomService(ILogService logService)
        {
            _logService = logService;
        }

        #region Events

        /// <summary>
        /// Zoom SDK initialization status changed event
        /// </summary>
        public event EventHandler<bool> InitializedChanged;

        protected virtual void OnInitializedChanged(bool state)
        {
            InitializedChanged?.Invoke(this, state);
        }

        /// <summary>
        /// Meeting status changed event
        /// </summary>
        public event EventHandler<string> MeetingStatusChanged;

        protected virtual void OnMeetingStatusChanged(ZOOM_SDK_DOTNET_WRAP.MeetingStatus status)
        {
            MeetingStatusChanged?.Invoke(this, MeetingStatusDecoder(status));
        }

        #endregion

        #region Initialization

        private bool _initialized;
        /// <summary>
        /// Shows whether Zoom SDK is initialized
        /// </summary>
        public bool Initialized
        {
            get => _initialized;
            private set
            {
                _initialized = value;
                OnInitializedChanged(Initialized);
            }
        }

        /// <summary>
        /// Performs Zoom SDK initialization.
        /// You should register your app at https://marketplace.zoom.us and generate SDK Key & Secret.
        /// See for help: https://marketplace.zoom.us/docs/guides/build/sdk-app
        /// </summary>
        /// <param name="appKey">SDK Key app credential</param>
        /// <param name="appSecret">SDK Secret app credential</param>
        /// <returns>SDK Initialization result</returns>
        public async Task<bool> InitializationAsync(string appKey, string appSecret)
        {
            try
            {
                var sdkInitialized = SdkInitialize();
                if (!sdkInitialized) return false;

                Initialized = await SdkAuthenticationAsync(appKey, appSecret);
            }
            catch (Exception)
            {
                Initialized = false;
            }
            
            return Initialized;
        }

        private bool SdkInitialize()
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

        private Task<bool> SdkAuthenticationAsync(string appKey, string appSecret)
        {
            RegisterAuthenticationCallbacks();

            ZOOM_SDK_DOTNET_WRAP.AuthParam authParam = new AuthParam
            {
                appKey = appKey,
                appSecret = appSecret
            };

            ZOOM_SDK_DOTNET_WRAP.SDKError error = CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().SDKAuth(authParam);

            _logService.Log($"ZOOM SDKAuth request result: {error}");

            if (error == ZOOM_SDK_DOTNET_WRAP.SDKError.SDKERR_SUCCESS)
            {
                _authTaskCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _authTaskCompletion.Task.TimeoutAfter(TimeSpan.FromSeconds(AuthenticationTimeout_s));
            }

            return Task.FromResult(false);
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

            _authTaskCompletion.SetResult(result == ZOOM_SDK_DOTNET_WRAP.AuthResult.AUTHRET_SUCCESS);
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

        #region Meeting control

        /// <summary>
        /// Joins to already exists meeting created by some host.
        /// </summary>
        /// <param name="userName">Name under which user is joining to meeting</param>
        /// <param name="meetingNumber">Meeting number</param>
        /// <param name="password">Meeting password</param>
        /// <returns>Meeting joining result</returns>
        public Task<bool> JoinMeetingAsync(string userName, ulong meetingNumber, string password)
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

            var error = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Join(joinParam);

            _logService.Log(text: $"ZOOM Join request result: {error}");

            if (error == ZOOM_SDK_DOTNET_WRAP.SDKError.SDKERR_SUCCESS)
            {
                _joinTaskCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _joinTaskCompletion.Task.TimeoutAfter(TimeSpan.FromSeconds(MeetingJoiningTimeout_s));
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Leave current meeting
        /// </summary>
        public void LeaveMeeting()
        {
            ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Leave(LeaveMeetingCmd.LEAVE_MEETING);

            CleanupVideoRenderers();
            UnRegisterMeetingCallBacks();
            CleanupVideoContainer();
        }

        /// <summary>
        /// Unmute meeting video (start showing for other meeting participants)
        /// </summary>
        public void UnmuteVideo()
        {
            var video = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingVideoController();
            video.UnmuteVideo();
        }

        /// <summary>
        /// Mute meeting video (stop showing for other meeting participants)
        /// </summary>
        public void MuteVideo()
        {
            var video = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingVideoController();
            video.MuteVideo();
        }

        /// <summary>
        /// Clean up ZOOM SDK
        /// </summary>
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

        #endregion

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

        public void OnMeetingStatusChanged(ZOOM_SDK_DOTNET_WRAP.MeetingStatus status, int iResult)
        {
            _logService.Log(text: $"ZOOM OnMeetingStatusChanged Callback Status: {status}");

            OnMeetingStatusChanged(status);

            switch (status)
            {
                #region MEETING_STATUS_CONNECTING

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

                #endregion

                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_DISCONNECTING:
                    LeaveMeeting();
                    break;
                case ZOOM_SDK_DOTNET_WRAP.MeetingStatus.MEETING_STATUS_INMEETING:
                    _joinTaskCompletion?.SetResult(true);
                    break;
            }
        }

        private string MeetingStatusDecoder(ZOOM_SDK_DOTNET_WRAP.MeetingStatus status)
        {
            string statusString;

            switch (status)
            {
                case MeetingStatus.MEETING_STATUS_IDLE:
                    statusString = "No meeting is running.";
                    break;
                case MeetingStatus.MEETING_STATUS_CONNECTING:
                    statusString = "Connect to the meeting server status.";
                    break;
                case MeetingStatus.MEETING_STATUS_WAITINGFORHOST:
                    statusString = "Waiting for the host to start the meeting.";
                    break;
                case MeetingStatus.MEETING_STATUS_INMEETING:
                    statusString = "Meeting is ready, in meeting status.";
                    break;
                case MeetingStatus.MEETING_STATUS_DISCONNECTING:
                    statusString = "Disconnect the meeting server, leave meeting status.";
                    break;
                case MeetingStatus.MEETING_STATUS_RECONNECTING:
                    statusString = "Reconnecting meeting server status.";
                    break;
                case MeetingStatus.MEETING_STATUS_FAILED:
                    statusString = "Failed to connect the meeting server.";
                    break;
                case MeetingStatus.MEETING_STATUS_ENDED:
                    statusString = "Meeting ends.";
                    break;
                case MeetingStatus.MEETING_STATUS_UNKNOW:
                    statusString = "Unknown status.";
                    break;
                case MeetingStatus.MEETING_STATUS_LOCKED:
                    statusString = "Meeting is locked to prevent the further participants to join the meeting.";
                    break;
                case MeetingStatus.MEETING_STATUS_UNLOCKED:
                    statusString = "Meeting is open and participants can join the meeting.";
                    break;
                case MeetingStatus.MEETING_STATUS_IN_WAITING_ROOM:
                    statusString = "Participants who join the meeting before the start are in the waiting room.";
                    break;
                case MeetingStatus.MEETING_STATUS_WEBINAR_PROMOTE:
                    statusString = "Upgrade the attendees to panelist in webinar.";
                    break;
                case MeetingStatus.MEETING_STATUS_WEBINAR_DEPROMOTE:
                    statusString = "Downgrade the attendees from the panelist.";
                    break;
                case MeetingStatus.MEETING_STATUS_JOIN_BREAKOUT_ROOM:
                    statusString = "Join the breakout room.";
                    break;
                case MeetingStatus.MEETING_STATUS_LEAVE_BREAKOUT_ROOM:
                    statusString = "Leave the breakout room.";
                    break;
                case MeetingStatus.MEETING_STATUS_WAITING_EXTERNAL_SESSION_KEY:
                    statusString = "Waiting for the additional secret key.";
                    break;
                default:
                    statusString = "Unknown status.";
                    break;
            }

            return statusString;
        }

        public void OnUserJoin(Array lstUserIds)
        {
            if (lstUserIds == null)
                return;

            _logService.Log($"ZOOM {lstUserIds.Length} users joined");

            if (NormalVideoRenderers == null)
                NormalVideoRenderers = new List<INormalVideoRenderElementDotNetWrap>();

            foreach (uint userId in lstUserIds)
            {
                var user = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().GetMeetingParticipantsController().GetUserByUserID(userId: userId);

                if (user != null)
                {
                    if (VideoContainer.CreateVideoElement(elementType: VideoRenderElementType.VideoRenderElement_NORMAL) is INormalVideoRenderElementDotNetWrap vidRenderer)
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

                        _logService.Log("Zoom Render user: " + vidRenderer.GetCurrentRenderUserId());
                    }

                    var name = user.GetUserNameW();
                    var isMySelf = user.IsMySelf();

                    _logService.Log($"Zoom User name: {name} IsMySelf: {isMySelf}");
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


        /// <summary>
        /// Apply meeting settings
        /// </summary>
        /// <param name="settings"></param>
        public void ApplySettings(ZoomSettings settings)
        {
            // Video
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            videoSettings.EnableHardwareEncode(settings.HardwareEncode);
            videoSettings.EnableVideoMirrorEffect(settings.VideoMirrorEffect);
            
            // Audio
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            audioSettings.EnableAutoJoinAudio(settings.AutoJoinAudio);
        }
        
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
                    _logService.Log(text: $"ZOOM VideoContainerPosition changed: {r.Left},{r.Top} - {r.Right},{r.Bottom}");
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

        /// <summary>
        /// Get camera device list. 
        /// </summary>
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

        /// <summary>
        /// Get the microphone device list.
        /// </summary>
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

        /// <summary>
        /// Get the speaker device list.
        /// </summary>
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

        /// <summary>
        /// Set video device to use in meeting.
        /// </summary>
        /// <param name="device">Video device</param>
        /// <returns>Operation result</returns>
        public bool SetVideoDevice(ZoomDevice device)
        {
            var videoSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetVideoSettings();
            var error = videoSettings.SelectCamera(device.Id);

            return error == SDKError.SDKERR_SUCCESS;
        }

        /// <summary>
        /// Set microphone device to use in meeting.
        /// </summary>
        /// <param name="device">Microphone device</param>
        /// <returns>Operation result</returns>
        public bool SetMicDevice(ZoomDevice device)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var error = audioSettings.SelectMic(device.Id, device.Name);

            return error == SDKError.SDKERR_SUCCESS;
        }

        /// <summary>
        /// Set speaker device to use in meeting.
        /// </summary>
        /// <param name="device">Speaker device</param>
        /// <returns>Operation result</returns>
        public bool SetSpeakerDevice(ZoomDevice device)
        {
            var audioSettings = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap().GetAudioSettings();
            var error = audioSettings.SelectSpeaker(device.Id, device.Name);

            return error == SDKError.SDKERR_SUCCESS;
        }

        #endregion
    }
}
