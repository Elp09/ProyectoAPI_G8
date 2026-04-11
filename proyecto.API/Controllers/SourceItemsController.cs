using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto.Core.Normalization;
using proyecto.Core.Services;

namespace proyecto.API.Controllers;

[ApiController]
[Route("api/sourceitems")]
[Authorize]
public class SourceItemsController : ControllerBase
{
    private readonly ISourceItemService _itemService;
    private readonly ISourceService _sourceService;
    private readonly INormalizationService _normalization;

    public SourceItemsController(
        ISourceItemService itemService,
        ISourceService sourceService,
        INormalizationService normalization)
    {
        _itemService = itemService;
        _sourceService = sourceService;
        _normalization = normalization;
    }

    // GET api/sourceitems
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _itemService.GetAllAsync();
        return Ok(items);
    }

    // GET api/sourceitems/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _itemService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    // GET api/sourceitems/source/{sourceId}
    [HttpGet("source/{sourceId:int}")]
    public async Task<IActionResult> GetBySource(int sourceId)
    {
        var items = await _itemService.GetBySourceIdAsync(sourceId);
        return Ok(items);
    }

    // POST api/sourceitems/normalize/{sourceId}
    // Recibe JSON crudo, lo normaliza y devuelve los documentos — sin guardar en BD
    [HttpPost("normalize/{sourceId:int}")]
    public async Task<IActionResult> Normalize(int sourceId, [FromBody] NormalizeRequest request)
    {
        var source = await _sourceService.GetByIdAsync(sourceId);
        if (source is null)
            return NotFound($"Source con Id={sourceId} no encontrada.");

        var documents = _normalization.Normalize(request.RawJson, source);
        var serialized = documents.Select(d => JsonSerializer.Serialize(d, new JsonSerializerOptions { WriteIndented = true }));
        return Ok(serialized);
    }

    // POST api/sourceitems/save/{sourceId}
    // Recibe un JSON ya normalizado y lo guarda en BD
    [HttpPost("save/{sourceId:int}")]
    public async Task<IActionResult> Save(int sourceId, [FromBody] SaveRequest request)
    {
        var source = await _sourceService.GetByIdAsync(sourceId);
        if (source is null)
            return NotFound($"Source con Id={sourceId} no encontrada.");

        var document = _normalization.Deserialize(request.NormalizedJson);
        if (document is null)
            return BadRequest("El JSON no sigue el esquema edu.univ.ingest.v1.");

        var savedBy = User.FindFirstValue(ClaimTypes.Email)
                   ?? User.FindFirstValue(ClaimTypes.Name);

        var item = await _itemService.SaveAsync(document, sourceId, request.Endpoint, request.IsLocalUpload, savedBy);
        return Ok(new { item.Id, item.SourceId, item.CreatedAt });
    }

    // DELETE api/sourceitems/{id}  (Admin only)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _itemService.DeleteAsync(id);
        return NoContent();
    }
}

public record NormalizeRequest(string RawJson);
public record SaveRequest(string NormalizedJson, string? Endpoint, bool IsLocalUpload);
