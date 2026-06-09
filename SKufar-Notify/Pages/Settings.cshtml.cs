using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SKufar;

public class SettingsModel : PageModel
{
    private readonly AppConfigService _cfg;
    public SettingsModel(AppConfigService cfg) => _cfg = cfg;

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string TelegramBotToken { get; set; } = "";
    [BindProperty] public string TelegramChatId { get; set; } = "";
    [BindProperty] public int CheckIntervalSeconds { get; set; } = 10;
    [BindProperty] public int AdsLimit { get; set; } = 20;
    [BindProperty] public string TimeZoneId { get; set; } = "Europe/Minsk";

    public IReadOnlyList<TimeZoneInfo> AvailableTimeZones { get; } =
        TimeZoneInfo.GetSystemTimeZones();

    public bool Saved { get; set; }

    public void OnGet()
    {
        var cfg = _cfg.Get();
        Username = cfg.Username;
        Password = cfg.Password;
        TelegramBotToken = cfg.TelegramBotToken;
        TelegramChatId = cfg.TelegramChatId;
        CheckIntervalSeconds = cfg.CheckIntervalSeconds;
        AdsLimit = cfg.AdsLimit;
        TimeZoneId = cfg.TimeZoneId;
    }

    public IActionResult OnPost()
    {
        _cfg.Save(new AppConfiguration
        {
            Username = Username,
            Password = string.IsNullOrWhiteSpace(Password) ? _cfg.Get().Password : Password,
            TelegramBotToken = TelegramBotToken,
            TelegramChatId = TelegramChatId,
            CheckIntervalSeconds = CheckIntervalSeconds < 1 ? 1 : CheckIntervalSeconds,
            AdsLimit = AdsLimit < 1 ? 1 : AdsLimit,
            TimeZoneId = TimeZoneId
        });
        Saved = true;
        return Page();
    }
}
