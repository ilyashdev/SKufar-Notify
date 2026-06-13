using Microsoft.AspNetCore.Authentication.Cookies;
using SKufar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("telegram")
    .ConfigureHttpClient(c => c.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddSingleton<FilterStorageService>();
builder.Services.AddSingleton<AppConfigService>();
builder.Services.AddSingleton<TelegramClientProvider>();
builder.Services.AddTransient<SKufarQueryService>();
builder.Services.AddHostedService<SKufarWorker>();
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
