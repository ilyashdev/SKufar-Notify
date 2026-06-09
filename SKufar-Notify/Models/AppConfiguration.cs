namespace SKufar;

public class AppConfiguration
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string TelegramBotToken { get; set; } = "";
    public string TelegramChatId { get; set; } = "";
    public int CheckIntervalSeconds { get; set; } = 10;
    public int AdsLimit { get; set; } = 20;
    public string TimeZoneId { get; set; } = "Europe/Minsk";
}
