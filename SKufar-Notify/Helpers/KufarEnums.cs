namespace SKufar;

public static class SkufarEnums
{
    public static readonly Dictionary<int, string> Categories = new()
    {
        { 1000,  "Недвижимость" },
        { 2000,  "Автомобили и транспорт" },
        { 15000, "Бытовая техника" },
        { 16000, "Компьютерная техника" },
        { 17000, "Телефоны и планшеты" },
        { 5000,  "Электроника" },
        { 8000,  "Женский гардероб" },
        { 19000, "Мужской гардероб" },
        { 18000, "Красота и здоровье" },
        { 12000, "Всё для детей и мам" },
        { 21000, "Мебель" },
        { 3000,  "Всё для дома" },
        { 14000, "Ремонт и стройка" },
        { 10000, "Сад и огород" },
        { 4000,  "Хобби, спорт и туризм" },
        { 9000,  "Свадьба и праздники" },
        { 11000, "Животные" },
        { 20000, "Готовый бизнес и оборудование" },
        { 6000,  "Работа" },
        { 13000, "Услуги" },
        { 7000,  "Прочее" },
    };

    public static readonly Dictionary<int, string> Regions = new()
    {
        { 7, "Минск" },
        { 1, "Брестская обл." },
        { 2, "Гомельская обл." },
        { 3, "Гродненская обл." },
        { 4, "Могилёвская обл." },
        { 5, "Минская обл." },
        { 6, "Витебская обл." },
    };

    public static readonly Dictionary<int, string> Currencies = new()
    {
        { 0, "BYN" }, { 1, "USD" }, { 2, "EUR" }
    };

    public static string CategoryName(int? id) =>
        id.HasValue && Categories.TryGetValue(id.Value, out var v) ? v : "";

    public static string RegionName(int? id) =>
        id.HasValue && Regions.TryGetValue(id.Value, out var v) ? v : "";
}
