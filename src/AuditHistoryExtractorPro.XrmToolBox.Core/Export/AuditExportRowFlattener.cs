using System.Collections.Generic;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    /// <summary>
    /// Aplana un <see cref="AuditRecord"/> (un evento de auditoría, con todos sus campos
    /// cambiados agrupados) a una o más filas — una por campo cambiado, igual que el CSV de
    /// la app web hermana (AuditHistoryExtractorPro, esquema de 17 columnas). Compartido por
    /// los exportadores CSV y Excel para no duplicar el criterio de aplanado.
    /// </summary>
    public static class AuditExportRowFlattener
    {
        public static readonly string[] Encabezados =
        {
            "AuditId", "Fecha", "Entidad", "RegistroId", "RegistroNombre",
            "Accion", "OperacionId", "Operacion", "Usuario", "DetalleIncompleto",
            "Campo", "ValorAnterior", "ValorNuevo", "ValorAnteriorLookup", "ValorNuevoLookup"
        };

        /// <summary>
        /// Una fila por campo cambiado. Si el evento no tiene campos registrados (Create/Delete/
        /// Access, o un Update cuyo detalle no se pudo parsear), devuelve una única fila con las
        /// columnas de campo/valor vacías — así el evento sigue apareciendo en el export aunque
        /// no tenga detalle de campos.
        /// </summary>
        public static IEnumerable<string[]> Aplanar(AuditRecord r)
        {
            var comunes = new[]
            {
                r.AuditId.ToString(),
                r.CreatedOn.ToString("O"),
                r.EntityLogicalName,
                r.RecordId.ToString(),
                r.RecordPrimaryName,
                r.Action.ToString(),
                ((int)r.Operation).ToString(),
                r.Operation.ToString(),
                r.UserFullName,
                r.DetalleIncompleto ? "Sí" : "No"
            };

            var campos = r.NewValues?.Keys
                .Union(r.OldValues?.Keys ?? Enumerable.Empty<string>())
                .ToList() ?? new List<string>();

            if (campos.Count == 0)
            {
                yield return comunes.Concat(new[] { "", "", "", "", "" }).ToArray();
                yield break;
            }

            foreach (var campo in campos)
            {
                r.OldValues.TryGetValue(campo, out var valorAnterior);
                r.NewValues.TryGetValue(campo, out var valorNuevo);
                r.LookupOldValues.TryGetValue(campo, out var lookupAnterior);
                r.LookupNewValues.TryGetValue(campo, out var lookupNuevo);

                yield return comunes.Concat(new[]
                {
                    campo, valorAnterior, valorNuevo, lookupAnterior, lookupNuevo
                }).ToArray();
            }
        }
    }
}
