namespace zoom_custom_ui_wpf.Services.Zoom
{
    public interface IZoomService
    {
        bool Init();
        void Login();
        void Cleanup();
    }
}