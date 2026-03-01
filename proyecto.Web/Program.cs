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
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// =========================
// MVC
// =========================
builder.Services.AddControllersWithViews();

// =========================
// INYECCIONES
// =========================
builder.Services.AddSingleton<InMemoryStore>();

// =========================
// HTTP CLIENTS
// =========================

// HttpClient normal
builder.Services.AddHttpClient();

// HttpClient hacia tu API
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri("http://localhost:5222/");
});

// =========================
// SESSION (para guardar JWT si quieres)
// =========================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseSession(); // 🔥 Primero Session

app.UseAuthentication(); // 🔥 ESTO TE FALTABA
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();