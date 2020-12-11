namespace zoom_custom_ui_wpf.Services.Credentials
{
    public interface ICredentialsService
    {
        string GetAppKey();
        string GetAppSecret();
        string GetUserName();
        string GetMeetingNumber();
        string GetPassword();
    }
}