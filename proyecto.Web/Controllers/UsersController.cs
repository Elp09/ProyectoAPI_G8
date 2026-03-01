using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

//[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // =========================
    // LISTAR USUARIOS
    // =========================
    public async Task<IActionResult> Index()
    {
        var client = CreateClientWithToken();

        var response = await client.GetAsync("api/users");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            ViewBag.Error = $"Error API: {response.StatusCode} - {error}";
            return View(new List<UserViewModel>());
        }

        var json = await response.Content.ReadAsStringAsync();

        var users = JsonSerializer.Deserialize<List<UserViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<UserViewModel>();

        return View(users);
    }

    // =========================
    // ACTUALIZAR ROL
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string id, string role)
    {
        // 🔥 Evitar que un admin se quite su propio rol
        var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (id == currentUserId && role != "Admin")
        {
            TempData["Error"] = "No puedes quitarte tu propio rol de Admin.";
            return RedirectToAction("Index");
        }

        var client = CreateClientWithToken();

        var content = new StringContent(
            JsonSerializer.Serialize(new { Role = role }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PutAsync($"api/users/{id}/role", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error actualizando rol: {error}";
        }
        else
        {
            TempData["Success"] = "Rol actualizado correctamente.";
        }

        return RedirectToAction("Index");
    }

    // =========================
    // CLIENTE CON TOKEN
    // =========================
    private HttpClient CreateClientWithToken()
    {
        var client = _httpClientFactory.CreateClient("Api");

        var token = User.Claims.FirstOrDefault(c => c.Type == "JWToken")?.Value;

        if (string.IsNullOrEmpty(token))
            throw new Exception("Token no encontrado en los claims.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}