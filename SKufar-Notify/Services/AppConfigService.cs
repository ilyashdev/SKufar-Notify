using System.Text.Json;

namespace SKufar;

public class AppConfigService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public AppConfigService(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "config.json");
    }

    public AppConfiguration Get()
    {
        if (File.Exists(_path))
            return JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText(_path)) ?? new();

        var cfg = new AppConfiguration
        {
            Username         = Environment.GetEnvironmentVariable("SKUFAR_USERNAME") ?? "admin",
            Password         = Environment.GetEnvironmentVariable("SKUFAR_PASSWORD") ?? "admin",
            TelegramBotToken = Environment.GetEnvironmentVariable("SKUFAR_TG_TOKEN") ?? "",
            TelegramChatId   = Environment.GetEnvironmentVariable("SKUFAR_TG_CHAT")  ?? ""
        };
        Save(cfg);
        return cfg;
    }

    public void Save(AppConfiguration cfg) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(cfg, Opts));
}
