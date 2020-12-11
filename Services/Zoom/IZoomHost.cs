using System;
using System.Windows;

namespace zoom_custom_ui_wpf.Services.Zoom
{
    public interface IZoomHost
    {
        void SetVideoContainerPosition(Rect position);

        void SetWindowHandle(IntPtr handle);
    }
}