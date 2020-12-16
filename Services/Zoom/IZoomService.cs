using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using zoom_custom_ui_wpf.Models;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public interface IZoomService : IZoomHost
    {
        /// <summary>
        /// Zoom SDK initialization status changed event
        /// </summary>
        event EventHandler<bool> InitializedChanged;

        /// <summary>
        /// Meeting status changed event
        /// </summary>
        event EventHandler<string> MeetingStatusChanged; 

        /// <summary>
        /// Shows whether Zoom SDK is initialized
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// Performs Zoom SDK initialization.
        /// You should register your app at https://marketplace.zoom.us and generate SDK Key & Secret.
        /// See for help: https://marketplace.zoom.us/docs/guides/build/sdk-app
        /// </summary>
        /// <param name="appKey">SDK Key app credential</param>
        /// <param name="appSecret">SDK Secret app credential</param>
        /// <returns>SDK Initialization result</returns>
        Task<bool> InitializationAsync(string appKey, string appSecret);

        /// <summary>
        /// Joins to already exists meeting created by some host.
        /// </summary>
        /// <param name="userName">Name under which user is joining to meeting</param>
        /// <param name="meetingNumber">Meeting number</param>
        /// <param name="password">Meeting password</param>
        /// <returns>Meeting joining result</returns>
        Task<bool> JoinMeetingAsync(string userName, ulong meetingNumber, string password);

        /// <summary>
        /// Leave current meeting
        /// </summary>
        void LeaveMeeting();

        /// <summary>
        /// Unmute meeting video (start showing for other meeting participants)
        /// </summary>
        void UnmuteVideo();

        /// <summary>
        /// Mute meeting video (stop showing for other meeting participants)
        /// </summary>
        void MuteVideo();

        /// <summary>
        /// Clean up ZOOM SDK
        /// </summary>
        void CleanUp();

        /// <summary>
        /// Apply meeting settings
        /// </summary>
        /// <param name="settings"></param>
        void ApplySettings(ZoomSettings settings);

        /// <summary>
        /// Get camera device list. 
        /// </summary>
        IEnumerable<ZoomDevice> EnumerateVideoDevices();

        /// <summary>
        /// Get the microphone device list.
        /// </summary>
        IEnumerable<ZoomDevice> EnumerateMicDevices();
        
        /// <summary>
        /// Get the speaker device list.
        /// </summary>
        IEnumerable<ZoomDevice> EnumerateSpeakerDevices();

        /// <summary>
        /// Set video device to use in meeting.
        /// </summary>
        /// <param name="device">Video device</param>
        /// <returns>Operation result</returns>
        bool SetVideoDevice(ZoomDevice device);

        /// <summary>
        /// Set microphone device to use in meeting.
        /// </summary>
        /// <param name="device">Microphone device</param>
        /// <returns>Operation result</returns>
        bool SetMicDevice(ZoomDevice device);

        /// <summary>
        /// Set speaker device to use in meeting.
        /// </summary>
        /// <param name="device">Speaker device</param>
        /// <returns>Operation result</returns>
        bool SetSpeakerDevice(ZoomDevice device);
    }
}