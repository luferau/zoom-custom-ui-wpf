using System.Diagnostics;

namespace zoom_custom_ui_wpf.Services.Log
{
    public class DebugLogService : ILogService
    {
        public void Log(string text)
        {
            Debug.WriteLine(text);
        }
    }
}
