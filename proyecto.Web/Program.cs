using Microsoft.AspNetCore.Authentication.Cookies;
using proyecto.Web.Models;
using proyecto.Web.Services;

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

// =========================
// SERVICIOS
// =========================
builder.Services.AddSingleton<InMemoryStore>();

// IMPORTANTE: Google TTS como Singleton
builder.Services.AddSingleton<GoogleTextToSpeechService>();

// =========================
// HTTP CLIENT HACIA API
// =========================
builder.Services.AddHttpClient("Api", client =>
{
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

// =========================
// ROUTES
// =========================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================
// RUN
// =========================
app.Run();