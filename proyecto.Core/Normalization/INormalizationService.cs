using proyecto.Core.Schema;
using proyecto.Models;

namespace proyecto.Core.Normalization;

public interface INormalizationService
{
    /// <summary>
    /// Normaliza un JSON crudo (array u objeto) en una lista de IngestDocuments.
    /// </summary>
    List<IngestDocument> Normalize(string rawJson, Source source);

    /// <summary>
    /// Verifica si un JSON ya sigue el esquema edu.univ.ingest.v1.
    /// </summary>
    bool IsAlreadyNormalized(string json);

    /// <summary>
    /// Deserializa un JSON que ya sigue el esquema edu.univ.ingest.v1.
    /// </summary>
    IngestDocument? Deserialize(string json);
}
