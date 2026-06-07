namespace SKufar;

public class SkufarAd
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ListTime { get; set; } = "";
    public int PriceByn { get; set; }
    public string SellerName { get; set; } = "";
    public bool PhoneHidden { get; set; }
    public string Link { get; set; } = "";
    public List<string> Images { get; set; } = new();
}
