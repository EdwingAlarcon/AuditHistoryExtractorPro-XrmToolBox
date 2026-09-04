using System;
using System.Collections.Generic;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Models
{
    /// <summary>
    /// Fila de auditoría ya aplanada, lista para exportar o mostrar en grilla.
    /// </summary>
    public class AuditRecord
    {
        public Guid AuditId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string EntityLogicalName { get; set; }
        public Guid RecordId { get; set; }
        public string RecordPrimaryName { get; set; }
        public AuditAction Action { get; set; }
        public string UserFullName { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Valores anteriores por atributo (solo aplica a Update).</summary>
        public IDictionary<string, string> OldValues { get; set; } = new Dictionary<string, string>();

        /// <summary>Valores nuevos por atributo.</summary>
        public IDictionary<string, string> NewValues { get; set; } = new Dictionary<string, string>();
    }
}
