using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Text;
using System.Text.Json;

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
        var client = _httpClientFactory.CreateClient("Api");

        // Usamos DTO idéntico al que espera el API
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

        // Capturamos el mensaje de error real para depuración
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
        var client = _httpClientFactory.CreateClient("Api");

        // Usamos DTO idéntico al que espera el API
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
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Credenciales inválidas: {error}");
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Guardamos el token en sesión
        HttpContext.Session.SetString("JWToken", result.Token);

        return RedirectToAction("Index", "Home");
    }
}

// DTO idéntico al de API
public class RegisterDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
}