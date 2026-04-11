using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ======================
    // REGISTER
    // ======================
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email y contrase�a son obligatorios");
            return View();
        }

        var client = _httpClientFactory.CreateClient("Api");

        var dto = new RegisterDto
        {
            Email = email,
            Password = password
        };

        var content = new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("api/auth/register", content);

        if (response.IsSuccessStatusCode)
            return RedirectToAction("Login");

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError("", $"Error al registrarse: {error}");
        return View();
    }

    // ======================
    // LOGIN
    // ======================
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email y contrase�a son obligatorios");
            return View();
        }

        var client = _httpClientFactory.CreateClient("Api");

        var dto = new RegisterDto
        {
            Email = email,
            Password = password
        };

        var content = new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("api/auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Credenciales inv�lidas");
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<LoginResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null || string.IsNullOrEmpty(result.Token))
        {
            ModelState.AddModelError("", "Error procesando el token");
            return View();
        }

        // ======================
        // LEER JWT
        // ======================
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        var claims = new List<Claim>();

        foreach (var claim in jwtToken.Claims)
        {
            if (claim.Type == "role")
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
            else
                claims.Add(claim);
        }

        // Guardamos token en sesión (evita problemas de tamaño en cookie)
        HttpContext.Session.SetString("JWToken", result.Token);

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        await HttpContext.Session.CommitAsync();

        return RedirectToAction("Index", "Home");
    }

    // ======================
    // LOGOUT
    // ======================
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }
}

// ======================
// DTOs
// ======================
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}