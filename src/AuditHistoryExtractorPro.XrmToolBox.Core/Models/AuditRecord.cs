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

        /// <summary>Operación CRUD base (más gruesa que <see cref="Action"/>).</summary>
        public AuditOperation Operation { get; set; }

        public string UserFullName { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Valores anteriores por atributo (solo aplica a Update).</summary>
        public IDictionary<string, string> OldValues { get; set; } = new Dictionary<string, string>();

        /// <summary>Valores nuevos por atributo.</summary>
        public IDictionary<string, string> NewValues { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Nombre legible (display name) del valor anterior, solo para atributos lookup —
        /// <see cref="OldValues"/> guarda el Id crudo. Viene del texto del nodo XML de
        /// <c>changedata</c> (Dataverse lo incluye ahí para lookups, además del Id en el
        /// atributo "value"). Vacío para atributos que no son lookup.
        /// </summary>
        public IDictionary<string, string> LookupOldValues { get; set; } = new Dictionary<string, string>();

        /// <summary>Nombre legible del valor nuevo, solo para atributos lookup. Ver <see cref="LookupOldValues"/>.</summary>
        public IDictionary<string, string> LookupNewValues { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// true SOLO cuando <c>RetrieveAuditDetailsRequest</c> falló para este registro (batch
        /// completo o el registro puntual) y no se pudo confirmar su detalle real de cambios —
        /// distingue "no se pudo verificar" de "no tuvo cambios de campo" (evento con
        /// <c>OldValues</c>/<c>NewValues</c> vacíos porque genuinamente no hay nada que
        /// registrar, ej. Create sin más detalle, Access). Ver <c>Queries.AuditDetailPopulator</c>.
        /// Un registro con este flag en true puede haber quedado con el fallback (más débil) de
        /// <c>changedata</c>, o directamente sin ningún dato de campos.
        /// </summary>
        public bool DetalleIncompleto { get; set; }

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
