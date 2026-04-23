using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Core.Services;
using proyecto.Models;

namespace proyecto.API.Controllers;

[ApiController]
[Route("api/sources")]
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly ISourceService _sourceService;

    public SourcesController(ISourceService sourceService)
    {
        _sourceService = sourceService;
    }

    // GET api/sources
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sources = await _sourceService.GetAllAsync();
        return Ok(sources);
    }

    // GET api/sources/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var source = await _sourceService.GetByIdAsync(id);
        return source is null ? NotFound() : Ok(source);
    }

    // POST api/sources  (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add([FromBody] Source source)
    {
        source.CreatedAt = DateTime.UtcNow;
        var created = await _sourceService.AddAsync(source);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT api/sources/{id}  (Admin only)
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Source source)
    {
        if (id != source.Id) return BadRequest("El ID no coincide.");
        var existing = await _sourceService.GetByIdAsync(id);
        if (existing is null) return NotFound();
        source.CreatedAt = existing.CreatedAt;
        await _sourceService.UpdateAsync(source);
        return NoContent();
    }

    // DELETE api/sources/{id}  (Admin only)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _sourceService.DeleteAsync(id);
        return NoContent();
    }
}
