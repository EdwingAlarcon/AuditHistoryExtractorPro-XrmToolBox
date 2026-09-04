using System;
using System.Collections.Generic;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Queries
{
    /// <summary>
    /// Completa OldValues/NewValues/LookupOldValues/LookupNewValues de una página de
    /// <see cref="AuditRecord"/> vía <c>RetrieveAuditDetailsRequest</c> — el mensaje SDK
    /// correcto para obtener el detalle real de un evento de auditoría (Dataverse NO garantiza
    /// que el atributo "changedata" traído por un RetrieveMultiple simple tenga el detalle
    /// completo/actualizado; en la práctica, contra un entorno real, viene vacío para la
    /// enorme mayoría de los eventos). Mismo enfoque que ya usa en producción la app web
    /// hermana (AuditHistoryExtractorPro, DataverseAuditRepository.PopulateChangesBatchAsync),
    /// portado a llamadas síncronas (acá no hay ServiceClient async, es el IOrganizationService
    /// síncrono que entrega el host de XrmToolBox).
    ///
    /// Bug real detectado 2026-09-04: con solo "changedata", el plugin traía 63.713 eventos
    /// para un trimestre pero ninguno tenía Campo/ValorAnterior/ValorNuevo — el export
    /// "aplanado" (una fila por campo cambiado) nunca tenía nada que aplanar, y por eso el CSV
    /// resultante tenía muchas menos filas que el de la app web (388.489), que sí usa este
    /// mensaje. El fallback a changedata queda como red de seguridad para cuando este mensaje
    /// falla puntualmente (throttling, AuditId purgado, etc.) — se aplica el detalle real
    /// SOLO si <c>RetrieveAuditDetailsRequest</c> devuelve algo; si no, el registro se queda
    /// con lo que ya haya cargado <c>AuditChangeDataParser</c> desde changedata.
    /// </summary>
    public static class AuditDetailPopulator
    {
        // Bien por debajo del límite de 1000 de ExecuteMultiple; mantiene la respuesta acotada
        // (cada detalle trae las entidades old/new completas).
        private const int TamanioLote = 500;

        /// <summary>
        /// <paramref name="cancelado"/> se consulta entre lotes para poder cortar una extracción
        /// larga sin esperar a que termine de pedir el detalle de toda la página.
        /// </summary>
        public static void Poblar(IOrganizationService service, IReadOnlyList<AuditRecord> pagina, Func<bool> cancelado = null)
        {
            for (var inicio = 0; inicio < pagina.Count; inicio += TamanioLote)
            {
                if (cancelado != null && cancelado()) return;

                var lote = pagina.Skip(inicio).Take(TamanioLote).Where(r => r.AuditId != Guid.Empty).ToList();
                if (lote.Count == 0) continue;

                var batch = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings { ContinueOnError = true, ReturnResponses = true },
                    Requests = new OrganizationRequestCollection()
                };
                foreach (var registro in lote)
                    batch.Requests.Add(new RetrieveAuditDetailsRequest { AuditId = registro.AuditId });

                ExecuteMultipleResponse response;
                try
                {
                    response = (ExecuteMultipleResponse)service.Execute(batch);
                }
                catch
                {
                    // Si el batch entero falla (throttling severo, mensaje no soportado en este
                    // entorno, etc.), cada registro del lote se queda con el fallback de
                    // changedata que ya trae desde el mapeo inicial — pero se marca como
                    // DetalleIncompleto: no hay forma de confirmar si ese fallback capturó todo,
                    // nada, o algo a medias, y el usuario necesita poder distinguir "no cambió"
                    // de "no se pudo verificar".
                    foreach (var registro in lote)
                        registro.DetalleIncompleto = true;
                    continue;
                }

                foreach (var item in response.Responses)
                {
                    if (item.Fault != null)
                    {
                        lote[item.RequestIndex].DetalleIncompleto = true;
                        continue; // se queda con el fallback de changedata
                    }

                    var detalle = (item.Response as RetrieveAuditDetailsResponse)?.AuditDetail;
                    AplicarDetalle(lote[item.RequestIndex], detalle);
                }
            }
        }

        private static void AplicarDetalle(AuditRecord registro, AuditDetail detalle)
        {
            if (!(detalle is AttributeAuditDetail attrDetail)) return;

            var claves = (attrDetail.NewValue?.Attributes.Keys ?? Enumerable.Empty<string>())
                .Union(attrDetail.OldValue?.Attributes.Keys ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (claves.Count == 0) return;

            var oldValues = new Dictionary<string, string>();
            var newValues = new Dictionary<string, string>();
            var lookupOld = new Dictionary<string, string>();
            var lookupNew = new Dictionary<string, string>();

            foreach (var atributo in claves)
            {
                object nuevoRaw = null, viejoRaw = null;
                attrDetail.NewValue?.Attributes.TryGetValue(atributo, out nuevoRaw);
                attrDetail.OldValue?.Attributes.TryGetValue(atributo, out viejoRaw);

                newValues[atributo] = FormatearValor(nuevoRaw, attrDetail.NewValue, atributo);
                oldValues[atributo] = FormatearValor(viejoRaw, attrDetail.OldValue, atributo);

                if (viejoRaw is EntityReference erViejo && !string.IsNullOrWhiteSpace(erViejo.Name))
                    lookupOld[atributo] = erViejo.Name;
                if (nuevoRaw is EntityReference erNuevo && !string.IsNullOrWhiteSpace(erNuevo.Name))
                    lookupNew[atributo] = erNuevo.Name;
            }

            // RetrieveAuditDetails trajo detalle real: reemplaza lo que haya quedado del
            // fallback de changedata (más confiable — incluye valores formateados y el Name
            // completo de los lookups, no solo lo que Dataverse haya escrito en el XML).
            registro.OldValues = oldValues;
            registro.NewValues = newValues;
            registro.LookupOldValues = lookupOld;
            registro.LookupNewValues = lookupNew;
        }

        private static string FormatearValor(object valorCrudo, Entity entidadOrigen, string atributo)
        {
            if (entidadOrigen?.FormattedValues != null && entidadOrigen.FormattedValues.TryGetValue(atributo, out var formateado))
                return formateado;

            switch (valorCrudo)
            {
                case null: return null;
                case EntityReference er: return string.IsNullOrWhiteSpace(er.Name) ? er.Id.ToString() : er.Name;
                case OptionSetValue osv: return osv.Value.ToString();
                case Money money: return money.Value.ToString("F2");
                case bool b: return b.ToString();
                case DateTime dt: return dt.ToString("O");
                default: return valorCrudo.ToString();
            }
        }
    }
}
