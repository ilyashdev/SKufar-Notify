using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SKufar;

public class TelegramBotService : BackgroundService
{
    private readonly AppConfigService _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<TelegramBotService> _logger;
    private int _offset;
    private TelegramBotClient? _bot;
    private string? _botToken;

    public TelegramBotService(AppConfigService config, IHttpClientFactory httpFactory, ILogger<TelegramBotService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private TelegramBotClient? GetBot(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        if (token != _botToken)
        {
            _botToken = token;
            _bot = new TelegramBotClient(token, _httpFactory.CreateClient("telegram"));
        }
        return _bot;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram poll error");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var bot = GetBot(_config.Get().TelegramBotToken);
        if (bot == null)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return;
        }

        var updates = await bot.GetUpdates(offset: _offset, limit: 20, timeout: 30, cancellationToken: ct);

        foreach (var update in updates)
        {
            _offset = update.Id + 1;
            if (update.Message?.Text == null) continue;
            if (!update.Message.Text.StartsWith("/chatid", StringComparison.OrdinalIgnoreCase)) continue;

            var chatId = update.Message.Chat.Id;
            var chatTitle = update.Message.Chat.Title;
            var reply = chatTitle != null
                ? $"Chat ID этого чата: <code>{chatId}</code>\n📌 {chatTitle}"
                : $"Твой Chat ID: <code>{chatId}</code>";

            await bot.SendMessage(chatId, reply, parseMode: ParseMode.Html, cancellationToken: ct);
        }
    }
}
