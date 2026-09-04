using System;
using System.Collections.Generic;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Models
{
    /// <summary>
    /// Filtros de extracción que el usuario define en la pantalla "Extraer" del plugin.
    /// Se traduce 1:1 a un FetchXML/QueryExpression contra la entidad audit.
    /// </summary>
    public class AuditQueryFilters
    {
        /// <summary>Nombre lógico de la entidad auditada (ej. "account", "opportunity").</summary>
        public string EntityLogicalName { get; set; }

        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        /// <summary>Vacío = todas las operaciones.</summary>
        public IList<AuditAction> Operaciones { get; set; } = new List<AuditAction>();

        /// <summary>
        /// true (default) = traer todos los registros que haya para el rango de fechas, sin
        /// cortar — la extracción es para auditoría/compliance, no debe faltar nada en silencio.
        /// En false, se aplica <see cref="MaxRegistros"/> como límite explícito.
        /// </summary>
        public bool SinLimite { get; set; } = true;

        /// <summary>Límite de filas, solo aplica cuando <see cref="SinLimite"/> es false.</summary>
        public int MaxRegistros { get; set; } = 50_000;
    }
}
