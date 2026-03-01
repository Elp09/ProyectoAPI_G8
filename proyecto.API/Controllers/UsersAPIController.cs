using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using proyecto.Models;

[ApiController]
[Route("api/users")]
//[Authorize(Roles = "Admin")]
public class UsersAPIController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersAPIController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // =========================
    // GET: api/users
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = _userManager.Users.ToList();

        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new
            {
                user.Id,
                user.Email,
                Role = roles.FirstOrDefault()
            });
        }

        return Ok(result);
    }

    // =========================
    // PUT: api/users/{id}/role
    // =========================
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleDto model)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound("Usuario no encontrado");

        var currentRoles = await _userManager.GetRolesAsync(user);

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        return Ok(new { message = "Rol actualizado correctamente" });
    }

    // =========================
    // DTO INTERNO
    // =========================
    public class UpdateRoleDto
    {
        public string Role { get; set; }
    }
}