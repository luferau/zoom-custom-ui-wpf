namespace zoom_custom_ui_wpf.Services.Credentials
{
    public class CredentialsService : ICredentialsService
    {
        public string GetAppKey() => "<Your App (SDK) Key here>";

        public string GetAppSecret() => "<Your App (SDK) Secret here>";

        public string GetUserName() => "zoom-custom-ui-wpf";
        public string GetMeetingNumber() => $"<Your Meeting Number here>";
        public string GetPassword() => "<Your Meeting Password here>";
    }
}
