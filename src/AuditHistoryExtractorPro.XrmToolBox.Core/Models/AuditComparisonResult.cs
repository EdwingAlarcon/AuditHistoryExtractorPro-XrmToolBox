using System.Collections.Generic;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Models
{
    public class CampoDiferencia
    {
        public string NombreCampo { get; set; }
        public string ValorEnAuditoria { get; set; }
        public string ValorActualEnDynamics { get; set; }
        public bool Coincide => string.Equals(ValorEnAuditoria, ValorActualEnDynamics);
    }

    /// <summary>
    /// Resultado de comparar un registro de auditoría puntual (por AuditId)
    /// contra el estado actual del registro en Dataverse. Equivalente reducido a /validar.
    /// </summary>
    public class AuditComparisonResult
    {
        public AuditRecord RegistroAuditoria { get; set; }
        public IList<CampoDiferencia> Diferencias { get; set; } = new List<CampoDiferencia>();
        public bool TodoCoincide => System.Linq.Enumerable.All(Diferencias, d => d.Coincide);
    }
}
