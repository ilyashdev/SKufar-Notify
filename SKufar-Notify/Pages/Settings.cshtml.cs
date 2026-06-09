using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SKufar;

public class SettingsModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly AppConfigService _cfg;
    private readonly FilterStorageService _filters;

    public SettingsModel(AppConfigService cfg, FilterStorageService filters)
    {
        _cfg = cfg;
        _filters = filters;
    }

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
    public bool Imported { get; set; }
    public string? ImportError { get; set; }

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

    public IActionResult OnGetExport()
    {
        var backup = new BackupData
        {
            Config = _cfg.Get(),
            Filters = _filters.GetAll()
        };
        var json = JsonSerializer.Serialize(backup, JsonOpts);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var filename = $"skufar-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", filename);
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? file)
    {
        OnGet();
        if (file == null || file.Length == 0)
        {
            ImportError = "Файл не выбран.";
            return Page();
        }

        try
        {
            using var stream = file.OpenReadStream();
            var backup = await JsonSerializer.DeserializeAsync<BackupData>(stream, JsonOpts);
            if (backup == null) throw new InvalidDataException("Пустой файл.");

            if (backup.Config != null)
            {
                var current = _cfg.Get();
                backup.Config.Password = string.IsNullOrWhiteSpace(backup.Config.Password)
                    ? current.Password
                    : backup.Config.Password;
                _cfg.Save(backup.Config);
            }

            if (backup.Filters != null)
            {
                foreach (var f in backup.Filters)
                    _filters.Upsert(f);
            }

            Imported = true;
            OnGet();
        }
        catch (Exception ex)
        {
            ImportError = $"Ошибка при импорте: {ex.Message}";
        }

        return Page();
    }
}
