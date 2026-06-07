using System.Text.Json;

namespace SKufar;

public class FilterStorageService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public FilterStorageService(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "filters.json");
    }

    public List<SavedFilter> GetAll()
    {
        if (!File.Exists(_path)) return new();
        return JsonSerializer.Deserialize<List<SavedFilter>>(File.ReadAllText(_path)) ?? new();
    }

    public SavedFilter? GetById(string id) => GetAll().FirstOrDefault(f => f.Id == id);

    public void Upsert(SavedFilter filter)
    {
        var list = GetAll();
        var idx = list.FindIndex(f => f.Id == filter.Id);
        if (idx >= 0) list[idx] = filter; else list.Add(filter);
        File.WriteAllText(_path, JsonSerializer.Serialize(list, Opts));
    }

    public void Delete(string id)
    {
        var list = GetAll().Where(f => f.Id != id).ToList();
        File.WriteAllText(_path, JsonSerializer.Serialize(list, Opts));
    }
}
