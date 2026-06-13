using Telegram.Bot;

namespace SKufar;

public class TelegramClientProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private TelegramBotClient? _bot;
    private string? _currentToken;
    private readonly object _lock = new();

    public TelegramClientProvider(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public TelegramBotClient? Get(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        lock (_lock)
        {
            if (token != _currentToken)
            {
                _currentToken = token;
                _bot = new TelegramBotClient(token, _httpFactory.CreateClient("telegram"));
            }
            return _bot;
        }
    }
}