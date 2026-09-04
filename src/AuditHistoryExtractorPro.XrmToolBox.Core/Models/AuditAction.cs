namespace AuditHistoryExtractorPro.XrmToolBox.Core.Models
{
    /// <summary>
    /// Valores del campo "action" de la entidad audit de Dataverse.
    /// Ver: https://learn.microsoft.com/power-apps/developer/data-platform/auditing/entity-metadata#audit-action-values
    /// </summary>
    public enum AuditAction
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        Access = 4,
        AccessDenied = 32,
        Other = 0
    }
}
