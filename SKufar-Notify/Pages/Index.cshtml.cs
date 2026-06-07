using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SKufar;

public class IndexModel : PageModel
{
    private readonly FilterStorageService _filters;
    public IndexModel(FilterStorageService filters) => _filters = filters;

    public List<SavedFilter> Filters { get; set; } = new();

    public void OnGet() => Filters = _filters.GetAll();

    public IActionResult OnPostDelete(string id)
    {
        _filters.Delete(id);
        return RedirectToPage();
    }
}
