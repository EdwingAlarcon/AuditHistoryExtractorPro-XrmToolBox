using System.Collections.Generic;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using Microsoft.Xrm.Sdk;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Comparison
{
    /// <summary>
    /// Compara los "ValoresNuevos" de un AuditRecord contra el estado actual
    /// del registro en Dataverse. Equivalente reducido a la pantalla /validar de la app web.
    /// </summary>
    public class AuditComparisonService
    {
        private readonly IOrganizationService _service;

        public AuditComparisonService(IOrganizationService service)
        {
            _service = service;
        }

        public AuditComparisonResult Comparar(AuditRecord registroAuditoria)
        {
            var actual = _service.Retrieve(
                registroAuditoria.EntityLogicalName,
                registroAuditoria.RecordId,
                new Microsoft.Xrm.Sdk.Query.ColumnSet(registroAuditoria.NewValues.Keys.ToArray()));

            var diferencias = registroAuditoria.NewValues.Select(kv => new CampoDiferencia
            {
                NombreCampo = kv.Key,
                ValorEnAuditoria = kv.Value,
                ValorActualEnDynamics = FormatearValorActual(actual, kv.Key)
            }).ToList();

            return new AuditComparisonResult
            {
                RegistroAuditoria = registroAuditoria,
                Diferencias = diferencias
            };
        }

        private static string FormatearValorActual(Entity entidad, string atributo)
        {
            if (!entidad.Contains(atributo)) return null;

            var valor = entidad[atributo];
            switch (valor)
            {
                case EntityReference er: return er.Name ?? er.Id.ToString();
                case OptionSetValue osv: return osv.Value.ToString();
                case Money money: return money.Value.ToString("F2");
                default: return valor?.ToString();
            }
        }
    }
}
