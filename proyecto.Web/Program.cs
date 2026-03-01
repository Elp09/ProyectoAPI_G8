using Microsoft.AspNetCore.Authentication.Cookies;
using proyecto.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================
// AUTHENTICATION (COOKIES)
// =========================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// =========================
// MVC
// =========================
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<InMemoryStore>();

// =========================
// HTTP CLIENT HACIA API
// =========================
builder.Services.AddHttpClient("Api", client =>
{
    //  IMPORTANTE:
    // Este puerto debe ser EXACTAMENTE el de tu API
    client.BaseAddress = new Uri("https://localhost:7251/");
});

// =========================
// BUILD
// =========================
var app = builder.Build();

// =========================
// MIDDLEWARE
// =========================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();