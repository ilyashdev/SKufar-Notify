using System.Text;
using System.Text.Json;
using System.Web;

namespace SKufar;

public class SKufarQueryService
{
    private readonly HttpClient _http;

    public SKufarQueryService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public string BuildUrl(SavedFilter f)
    {
        const string baseUrl = "https://searchapi.kufar.by/v1/search/rendered-paginated?";
        const string maxPrice = "1000000000";
        var sb = new StringBuilder(baseUrl);

        void Add(string key, string? val) { if (val != null) sb.Append($"{key}={val}&"); }

        if (!string.IsNullOrWhiteSpace(f.Tag)) sb.Append($"query={HttpUtility.UrlEncode(f.Tag)}&");
        if (f.Limit.HasValue) Add("size", f.Limit.Value.ToString());

        if (f.PriceMin.HasValue || f.PriceMax.HasValue)
        {
            var lo = f.PriceMin.HasValue ? (f.PriceMin.Value * 100).ToString() : "0";
            var hi = f.PriceMax.HasValue ? (f.PriceMax.Value * 100).ToString() : maxPrice;
            Add("prc", $"r:{lo},{hi}");
        }

        Add("cur", string.IsNullOrEmpty(f.Currency) ? null : f.Currency);
        if (f.SubCategory.HasValue) Add("cat", f.SubCategory.Value.ToString());
        if (f.Category.HasValue) Add("prn", f.Category.Value.ToString());
        if (f.OnlyTitleSearch) Add("ot", "true");
        if (f.KufarDelivery) Add("dle", "true");
        if (f.KufarPayment) Add("sde", "true");
        if (f.KufarHalva) Add("hlv", "true");
        if (f.OnlyWithPhotos) Add("oph", "true");
        if (f.OnlyWithVideos) Add("ovi", "true");
        if (f.OnlyWithExchange) Add("pse", "true");
        var sort = f.SortType switch { 1 => "prc.d", 2 => "prc.a", _ => "lst.d" };
        Add("sort", sort);
        if (f.Condition.HasValue) Add("cnd", f.Condition.Value.ToString());
        if (f.SellerType.HasValue) Add("cmp", f.SellerType.Value.ToString());
        if (f.Region.HasValue) Add("rgn", f.Region.Value.ToString());
        if (f.Areas is { Count: > 0 }) Add("ar", $"v.or:{string.Join(",", f.Areas)}");

        return sb.ToString().TrimEnd('&');
    }

    public async Task<List<SkufarAd>> SearchAsync(SavedFilter filter, CancellationToken ct = default)
    {
        var url = BuildUrl(filter);
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        return ParseResponse(await resp.Content.ReadAsStringAsync(ct));
    }

    public async Task<List<SkufarAd>> SearchAllTagsAsync(SavedFilter filter, CancellationToken ct = default)
    {
        var tags = new List<string?> { filter.Tag };
        if (filter.AlternativeTags?.Count > 0)
            tags.AddRange(filter.AlternativeTags.Where(t => !string.IsNullOrWhiteSpace(t)));

        if (tags.Count == 1) return await SearchAsync(filter, ct);

        var seenIds = new HashSet<int>();
        var results = new List<SkufarAd>();
        foreach (var tag in tags)
        {
            var ads = await SearchAsync(WithTag(filter, tag), ct);
            foreach (var ad in ads.Where(a => seenIds.Add(a.Id)))
                results.Add(ad);
        }
        return results;
    }

    private static SavedFilter WithTag(SavedFilter f, string? tag) => new()
    {
        Id = f.Id, Name = f.Name, CreatedAt = f.CreatedAt,
        Tag = tag, OnlyTitleSearch = f.OnlyTitleSearch,
        PriceMin = f.PriceMin, PriceMax = f.PriceMax,
        Limit = f.Limit, Currency = f.Currency,
        Condition = f.Condition, SellerType = f.SellerType,
        KufarDelivery = f.KufarDelivery, KufarPayment = f.KufarPayment, KufarHalva = f.KufarHalva,
        OnlyWithPhotos = f.OnlyWithPhotos, OnlyWithVideos = f.OnlyWithVideos,
        OnlyWithExchange = f.OnlyWithExchange,
        SortType = f.SortType, Category = f.Category, SubCategory = f.SubCategory,
        Region = f.Region, Areas = f.Areas
    };

    public static List<SkufarAd> ParseResponse(string json)
    {
        var result = new List<SkufarAd>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("ads", out var ads)) return result;

        foreach (var ad in ads.EnumerateArray())
        {
            var item = new SkufarAd
            {
                Id          = ad.GetProperty("ad_id").GetInt32(),
                Title       = ad.GetProperty("subject").GetString() ?? "",
                Description = ad.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                PriceByn    = int.TryParse(ad.GetProperty("price_byn").GetString(), out var p) ? p / 100 : 0,
                PhoneHidden = ad.GetProperty("phone_hidden").GetBoolean(),
                Link        = ad.GetProperty("ad_link").GetString() ?? "",
                ListTime    = ad.GetProperty("list_time").GetString() ?? ""
            };

            if (ad.TryGetProperty("account_parameters", out var prms))
                foreach (var pm in prms.EnumerateArray())
                    if (pm.TryGetProperty("p", out var pk) && pk.GetString() == "name")
                    { item.SellerName = pm.GetProperty("v").GetString() ?? ""; break; }

            if (ad.TryGetProperty("images", out var images))
                foreach (var img in images.EnumerateArray())
                {
                    if (img.TryGetProperty("yams_storage", out var yams) && yams.GetBoolean())
                    {
                        var imgId = img.GetProperty("id").GetString() ?? "";
                        if (imgId.Length >= 2)
                            item.Images.Add($"https://yams.kufar.by/api/v1/kufar-ads/images/{imgId[..2]}/{imgId}.jpg?rule=pictures");
                    }
                    else if (img.TryGetProperty("media_storage", out var ms) && img.TryGetProperty("path", out var path))
                        item.Images.Add($"https://{ms.GetString()}.kufar.by/v1/gallery/{path.GetString()}");
                }

            result.Add(item);
        }

        return result;
    }
}
