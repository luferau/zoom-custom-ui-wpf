using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using zoom_custom_ui_wpf.Models;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public interface IZoomService
    {
        event EventHandler<bool> InitializedChanged;

        bool Initialized { get; }

        Task<bool> InitializationAsync();
        void Cleanup();

        IEnumerable<ZoomDevice> EnumerateVideoDevices();
        IEnumerable<ZoomDevice> EnumerateMicDevices();
        IEnumerable<ZoomDevice> EnumerateSpeakerDevices();

        void SelectVideoDevice(string deviceId);
        void SelectMicDevice(string deviceId, string deviceName);
        void SelectSpeakerDevice(string deviceId, string deviceName);
    }
}