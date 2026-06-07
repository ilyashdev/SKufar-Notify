namespace SKufar;

public class SavedFilter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Без названия";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Tag { get; set; }
    public bool OnlyTitleSearch { get; set; }
    public int? PriceMin { get; set; }
    public int? PriceMax { get; set; }
    public int? Limit { get; set; }
    public string? Currency { get; set; }
    public int? Condition { get; set; }
    public int? SellerType { get; set; }
    public bool KufarDelivery { get; set; }
    public bool KufarPayment { get; set; }
    public bool KufarHalva { get; set; }
    public bool OnlyWithPhotos { get; set; }
    public bool OnlyWithVideos { get; set; }
    public bool OnlyWithExchange { get; set; }
    public int? SortType { get; set; }
    public int? Category { get; set; }
    public int? SubCategory { get; set; }
    public int? Region { get; set; }
    public List<int>? Areas { get; set; }
}
