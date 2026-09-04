using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Resumen legible de los campos que cambiaron ("campo: antes → después"), calculado
        /// comparando NewValues contra OldValues. Pensado para mostrarse en grilla sin exponer
        /// los diccionarios crudos.
        /// </summary>
        public string ResumenCambios
        {
            get
            {
                if (NewValues == null || NewValues.Count == 0) return string.Empty;

                var cambios = NewValues
                    .Where(kv => !string.Equals(ValorAnterior(kv.Key), kv.Value, StringComparison.Ordinal))
                    .Select(kv => $"{kv.Key}: {ValorAnterior(kv.Key) ?? "(vacío)"} → {kv.Value ?? "(vacío)"}");

                return string.Join("; ", cambios);
            }
        }

        private string ValorAnterior(string campo) =>
            OldValues != null && OldValues.TryGetValue(campo, out var valor) ? valor : null;
    }
}
