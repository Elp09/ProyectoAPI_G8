using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Web.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateClientWithToken();

        var response = await client.GetAsync("api/users");

        if (!response.IsSuccessStatusCode)
            return View(new List<UserViewModel>());

        var json = await response.Content.ReadAsStringAsync();

        var users = JsonSerializer.Deserialize<List<UserViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(string id, string role)
    {
        var client = CreateClientWithToken();

        var content = new StringContent(
            JsonSerializer.Serialize(new { Role = role }),
            Encoding.UTF8,
            "application/json");

        await client.PutAsync($"api/users/{id}/role", content);

        return RedirectToAction("Index");
    }

    private HttpClient CreateClientWithToken()
    {
        var client = _httpClientFactory.CreateClient("Api");

        var token = User.Claims.FirstOrDefault(c => c.Type == "JWToken")?.Value;

        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }
}