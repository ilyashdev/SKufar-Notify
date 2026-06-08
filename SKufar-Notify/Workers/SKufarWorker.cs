using System.Net.Http.Json;
using System.Text.Json;
namespace SKufar;

public class SKufarWorker : BackgroundService
{
    private readonly SKufarQueryService _query;
    private readonly FilterStorageService _filters;
    private readonly AppConfigService _config;
    private readonly HttpClient _http;
    private readonly ILogger<SKufarWorker> _logger;
    private readonly string _cachePath;
    private readonly string _placeholderPath;

    private Dictionary<string, HashSet<int>> _seen = new();
    private bool _firstCycle = true;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    public SKufarWorker(
        SKufarQueryService query,
        FilterStorageService filters,
        AppConfigService config,
        IHttpClientFactory httpFactory,
        IWebHostEnvironment env,
        ILogger<SKufarWorker> logger)
    {
        _query = query;
        _filters = filters;
        _config = config;
        _http = httpFactory.CreateClient();
        _logger = logger;
        _cachePath = Path.Combine(env.ContentRootPath, "Data", "seen.json");
        _placeholderPath = Path.Combine(env.WebRootPath, "1080.jpg");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        LoadCache();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await RunCycleAsync(ct);
                await Task.Delay(Interval, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally { SaveCache(); }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        var filters = _filters.GetAll();
        var cfg = _config.Get();

        _logger.LogDebug("Cycle started, {Count} filter(s)", filters.Count);

        foreach (var filter in filters)
        {
            try
            {
                _logger.LogDebug("Filter '{Name}': fetching...", filter.Name);
                var ads = await _query.SearchAllTagsAsync(WorkerFilter(filter), ct);
                var currentIds = ads.Select(a => a.Id).ToHashSet();

                _logger.LogDebug("Filter '{Name}': got {Total} ad(s)", filter.Name, ads.Count);

                if (_firstCycle || !_seen.TryGetValue(filter.Id, out var prev))
                {
                    _seen[filter.Id] = currentIds;
                    _logger.LogInformation("Filter '{Name}' seeded with {Count} ads", filter.Name, currentIds.Count);
                    continue;
                }

                var blacklist = filter.BlacklistWords ?? new List<string>();
                var newAds = ads
                    .Where(a => !prev.Contains(a.Id))
                    .Where(a => !blacklist.Any(w => a.Title.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                _logger.LogInformation("Filter '{Name}': {New} new ad(s)", filter.Name, newAds.Count);

                foreach (var ad in newAds)
                {
                    _logger.LogInformation("Sending ad #{Id} '{Title}' for filter '{Name}'", ad.Id, ad.Title, filter.Name);
                    await SendAsync(cfg, filter.Name, ad, ct);
                }

                _seen[filter.Id] = currentIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Filter '{Name}' failed", filter.Name);
            }
        }

        _firstCycle = false;
        SaveCache();
        _logger.LogDebug("Cycle done");
    }

    private static SavedFilter WorkerFilter(SavedFilter f) => new()
    {
        Id = f.Id, Name = f.Name, CreatedAt = f.CreatedAt,
        Tag = f.Tag, OnlyTitleSearch = f.OnlyTitleSearch,
        PriceMin = f.PriceMin, PriceMax = f.PriceMax,
        Limit = 20, Currency = f.Currency,
        Condition = f.Condition, SellerType = f.SellerType,
        KufarDelivery = f.KufarDelivery, KufarPayment = f.KufarPayment, KufarHalva = f.KufarHalva,
        OnlyWithPhotos = f.OnlyWithPhotos, OnlyWithVideos = f.OnlyWithVideos,
        OnlyWithExchange = f.OnlyWithExchange,
        SortType = 0,
        Category = f.Category, SubCategory = f.SubCategory,
        Region = f.Region, Areas = f.Areas,
        AlternativeTags = f.AlternativeTags
    };

    private async Task SendAsync(AppConfiguration cfg, string filterName, SkufarAd ad, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(cfg.TelegramBotToken) || string.IsNullOrEmpty(cfg.TelegramChatId))
            return;

        var date = DateTime.TryParse(ad.ListTime, out var dt)
            ? dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : ad.ListTime;
        var phone = ad.PhoneHidden ? "скрыт" : "доступен";
        var seller = string.IsNullOrWhiteSpace(ad.SellerName) ? "—" : Esc(ad.SellerName);

        var caption =
            $"📌 Фильтр: <b>{Esc(filterName)}</b>\n" +
            $"🏷 Название: {Esc(ad.Title)}\n" +
            $"📅 Дата: {date}\n" +
            $"💰 Цена: {ad.PriceByn} BYN\n" +
            $"📞 Телефон: {phone}\n" +
            $"👤 Продавец: {seller}\n" +
            $"🔗 <a href=\"{ad.Link}\">Открыть объявление</a>";

        var apiUrl = $"https://api.telegram.org/bot{cfg.TelegramBotToken}";
        HttpResponseMessage resp;

        if (ad.Images.Count > 0)
        {
            resp = await _http.PostAsJsonAsync($"{apiUrl}/sendPhoto", new
            {
                chat_id = cfg.TelegramChatId,
                photo = ad.Images[0],
                caption,
                parse_mode = "HTML"
            }, cancellationToken: ct);
        }
        else
        {
            var imageBytes = await File.ReadAllBytesAsync(_placeholderPath, ct);
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(cfg.TelegramChatId), "chat_id");
            form.Add(new StringContent(caption), "caption");
            form.Add(new StringContent("HTML"), "parse_mode");
            form.Add(new ByteArrayContent(imageBytes), "photo", "1080.jpg");
            resp = await _http.PostAsync($"{apiUrl}/sendPhoto", form, ct);
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Telegram sendPhoto failed {Status}: {Body}", (int)resp.StatusCode, body);
        }
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private void LoadCache()
    {
        if (!File.Exists(_cachePath)) return;
        try
        {
            _seen = JsonSerializer.Deserialize<Dictionary<string, HashSet<int>>>(
                File.ReadAllText(_cachePath)) ?? new();
            _logger.LogInformation("Cache restored: {Count} filter(s)", _seen.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache load failed, starting fresh");
            _seen = new();
        }
    }

    private void SaveCache()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
            File.WriteAllText(_cachePath, JsonSerializer.Serialize(_seen));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache save failed");
        }
    }
}
