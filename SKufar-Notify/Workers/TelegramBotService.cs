using System.Net.Http.Json;
using System.Text.Json;

namespace SKufar;

public class TelegramBotService : BackgroundService
{
    private readonly AppConfigService _config;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly HttpClient _http = new();
    private int _offset;

    public TelegramBotService(AppConfigService config, ILogger<TelegramBotService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Telegram poll error"); }

            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var token = _config.Get().TelegramBotToken;
        if (string.IsNullOrEmpty(token)) return;

        var url = $"https://api.telegram.org/bot{token}/getUpdates?offset={_offset}&limit=20&timeout=0";
        var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.GetProperty("ok").GetBoolean()) return;

        foreach (var update in doc.RootElement.GetProperty("result").EnumerateArray())
        {
            _offset = update.GetProperty("update_id").GetInt32() + 1;

            if (!update.TryGetProperty("message", out var msg)) continue;
            if (!msg.TryGetProperty("text", out var textProp)) continue;

            var text = textProp.GetString() ?? "";
            if (!text.StartsWith("/chatid", StringComparison.OrdinalIgnoreCase)) continue;

            var chatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
            var chatTitle = msg.GetProperty("chat").TryGetProperty("title", out var t)
                ? t.GetString() : null;

            var reply = chatTitle != null
                ? $"Chat ID этого чата: <code>{chatId}</code>\n📌 {chatTitle}"
                : $"Твой Chat ID: <code>{chatId}</code>";

            await _http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/sendMessage", new
            {
                chat_id = chatId,
                text = reply,
                parse_mode = "HTML"
            }, ct);
        }
    }
}
