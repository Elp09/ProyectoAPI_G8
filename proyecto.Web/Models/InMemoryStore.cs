namespace proyecto.Web.Models;

/// <summary>
/// Almacén temporal en memoria para ítems ingestados aún no guardados en BD.
/// Las fuentes (Sources) ya viven en la base de datos.
/// </summary>
public class InMemoryStore
{
    public List<IngestedItem> Items { get; } = new();
}
