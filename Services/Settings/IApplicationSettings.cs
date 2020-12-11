namespace zoom_custom_ui_wpf.Services.Settings
{
    public interface IApplicationSettings
    {
        string GetVideoDeviceId();
        string GetMicDeviceId();
        string GetSpeakerDeviceId();
    }
}