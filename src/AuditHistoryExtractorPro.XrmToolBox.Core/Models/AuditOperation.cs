namespace AuditHistoryExtractorPro.XrmToolBox.Core.Models
{
    /// <summary>
    /// Valores del campo "operation" de la entidad audit de Dataverse — la operación CRUD base,
    /// más gruesa que "action" (que distingue variantes como Merge, Associate, etc. dentro de
    /// una misma operación). Ver:
    /// https://learn.microsoft.com/power-apps/developer/data-platform/auditing/entity-metadata#audit-operation-values
    /// </summary>
    public enum AuditOperation
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        Other = 0
    }
}
