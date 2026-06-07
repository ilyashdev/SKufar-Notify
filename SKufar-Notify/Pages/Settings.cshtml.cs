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

    public bool Saved { get; set; }

    public void OnGet()
    {
        var cfg = _cfg.Get();
        Username = cfg.Username;
        Password = cfg.Password;
        TelegramBotToken = cfg.TelegramBotToken;
        TelegramChatId = cfg.TelegramChatId;
    }

    public IActionResult OnPost()
    {
        _cfg.Save(new AppConfiguration
        {
            Username = Username,
            Password = string.IsNullOrWhiteSpace(Password) ? _cfg.Get().Password : Password,
            TelegramBotToken = TelegramBotToken,
            TelegramChatId = TelegramChatId
        });
        Saved = true;
        return Page();
    }
}
