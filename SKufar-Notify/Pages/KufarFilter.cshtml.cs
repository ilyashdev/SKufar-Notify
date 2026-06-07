using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SKufar;

public class SkufarFilterModel : PageModel
{
    private readonly SKufarQueryService _query;
    private readonly FilterStorageService _filters;

    public SkufarFilterModel(SKufarQueryService query, FilterStorageService filters)
    {
        _query = query;
        _filters = filters;
    }

    // ── Identity ───────────────────────────────────────────────
    [BindProperty] public string? FilterId { get; set; }
    [BindProperty] public string FilterName { get; set; } = "";

    // ── Kufar config ───────────────────────────────────────────
    [BindProperty] public string? Tag { get; set; }
    [BindProperty] public bool OnlyTitleSearch { get; set; }
    [BindProperty] public int? PriceMin { get; set; }
    [BindProperty] public int? PriceMax { get; set; }
    [BindProperty] public int? Limit { get; set; }
    [BindProperty] public string? Currency { get; set; }
    [BindProperty] public int? Condition { get; set; }
    [BindProperty] public int? SellerType { get; set; }
    [BindProperty] public bool KufarDelivery { get; set; }
    [BindProperty] public bool KufarPayment { get; set; }
    [BindProperty] public bool KufarHalva { get; set; }
    [BindProperty] public bool OnlyWithPhotos { get; set; }
    [BindProperty] public bool OnlyWithVideos { get; set; }
    [BindProperty] public bool OnlyWithExchange { get; set; }
    [BindProperty] public int? SortType { get; set; }
    [BindProperty] public int? Category { get; set; }
    [BindProperty] public int? SubCategory { get; set; }
    [BindProperty] public int? Region { get; set; }
    [BindProperty] public List<int>? Areas { get; set; }

    // ── Results ────────────────────────────────────────────────
    public List<SkufarAd> Results { get; set; } = new();
    public string? GeneratedUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsEdit => !string.IsNullOrEmpty(FilterId);

    // ── GET ────────────────────────────────────────────────────
    public async Task OnGetAsync(string? id, bool run = false)
    {
        if (id == null) return;
        var f = _filters.GetById(id);
        if (f == null) return;

        FilterId   = f.Id;
        FilterName = f.Name;
        Tag        = f.Tag;
        OnlyTitleSearch = f.OnlyTitleSearch;
        PriceMin   = f.PriceMin;
        PriceMax   = f.PriceMax;
        Limit      = f.Limit;
        Currency   = f.Currency;
        Condition  = f.Condition;
        SellerType = f.SellerType;
        KufarDelivery   = f.KufarDelivery;
        KufarPayment    = f.KufarPayment;
        KufarHalva      = f.KufarHalva;
        OnlyWithPhotos  = f.OnlyWithPhotos;
        OnlyWithVideos  = f.OnlyWithVideos;
        OnlyWithExchange = f.OnlyWithExchange;
        SortType    = f.SortType;
        Category    = f.Category;
        SubCategory = f.SubCategory;
        Region      = f.Region;
        Areas       = f.Areas;

        if (run) await RunSearchAsync();
    }

    // ── POST: Save ─────────────────────────────────────────────
    public IActionResult OnPostSave()
    {
        _filters.Upsert(BuildFilter());
        return RedirectToPage("/Index");
    }

    // ── POST: Search ───────────────────────────────────────────
    public async Task OnPostSearchAsync() => await RunSearchAsync();

    // ── POST: SaveAndSearch ────────────────────────────────────
    public async Task OnPostSaveSearchAsync()
    {
        var filter = BuildFilter();
        _filters.Upsert(filter);
        FilterId = filter.Id;
        await RunSearchAsync();
    }

    // ── Helpers ────────────────────────────────────────────────
    private SavedFilter BuildFilter() => new()
    {
        Id   = string.IsNullOrEmpty(FilterId) ? Guid.NewGuid().ToString() : FilterId,
        Name = string.IsNullOrWhiteSpace(FilterName) ? "Без названия" : FilterName,
        Tag  = Tag,
        OnlyTitleSearch  = OnlyTitleSearch,
        PriceMin  = PriceMin,
        PriceMax  = PriceMax,
        Limit     = Limit,
        Currency  = Currency,
        Condition = Condition,
        SellerType = SellerType,
        KufarDelivery    = KufarDelivery,
        KufarPayment     = KufarPayment,
        KufarHalva       = KufarHalva,
        OnlyWithPhotos   = OnlyWithPhotos,
        OnlyWithVideos   = OnlyWithVideos,
        OnlyWithExchange = OnlyWithExchange,
        SortType    = SortType,
        Category    = Category,
        SubCategory = SubCategory,
        Region      = Region,
        Areas       = Areas
    };

    private async Task RunSearchAsync()
    {
        var filter = BuildFilter();
        GeneratedUrl = _query.BuildUrl(filter);
        try
        {
            Results = await _query.SearchAsync(filter);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
