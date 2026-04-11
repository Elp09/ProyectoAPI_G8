using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Core.Services;
using proyecto.Models;

namespace proyecto.API.Controllers;

[ApiController]
[Route("api/secrets")]
[Authorize(Roles = "Admin")]
public class SecretsController : ControllerBase
{
    private readonly ISecretService _secretService;

    public SecretsController(ISecretService secretService)
    {
        _secretService = secretService;
    }

    // GET api/secrets
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var secrets = await _secretService.GetAllAsync();
        return Ok(secrets);
    }

    // GET api/secrets/source/{sourceId}
    [HttpGet("source/{sourceId:int}")]
    public async Task<IActionResult> GetBySource(int sourceId)
    {
        var secrets = await _secretService.GetBySourceIdAsync(sourceId);
        return Ok(secrets);
    }

    // POST api/secrets
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] Secret secret)
    {
        secret.CreatedAt = DateTime.UtcNow;
        var created = await _secretService.AddAsync(secret);
        return Ok(new { created.Id, created.SourceId, created.KeyName });
    }

    // DELETE api/secrets/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _secretService.DeleteAsync(id);
        return NoContent();
    }
}
