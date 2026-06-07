using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
namespace SKufar;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AppConfigService _cfg;
    public LoginModel(AppConfigService cfg) => _cfg = cfg;

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var cfg = _cfg.Get();
        if (Username == cfg.Username && Password == cfg.Password)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, Username) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToPage("/Index");
        }
        Error = "Неверный логин или пароль";
        return Page();
    }
}
