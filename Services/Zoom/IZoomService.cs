using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using zoom_custom_ui_wpf.Models;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public interface IZoomService : IZoomHost
    {
        event EventHandler<bool> InitializedChanged;

        bool Initialized { get; }

        Task<bool> InitializationAsync();
        Task<bool> JoinMeetingAsync(string userName, ulong meetingNumber, string password);
        void UnmuteVideo();
        void MuteVideo();
        void LeaveMeeting();
        void CleanUp();

        void ApplySettings(ZoomSettings settings);

        IEnumerable<ZoomDevice> EnumerateVideoDevices();
        IEnumerable<ZoomDevice> EnumerateMicDevices();
        IEnumerable<ZoomDevice> EnumerateSpeakerDevices();

        bool SetVideoDevice(ZoomDevice device);
        bool SetMicDevice(ZoomDevice device);
        bool SetSpeakerDevice(ZoomDevice device);
    }
}