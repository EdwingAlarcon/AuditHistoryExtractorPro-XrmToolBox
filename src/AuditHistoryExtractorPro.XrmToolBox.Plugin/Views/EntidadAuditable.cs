namespace AuditHistoryExtractorPro.XrmToolBox.Plugin.Views
{
    /// <summary>Entrada del combo de entidades de "Extraer": una entidad con auditoría habilitada.</summary>
    public class EntidadAuditable
    {
        public string LogicalName { get; }
        public string DisplayName { get; }

        public EntidadAuditable(string logicalName, string displayName)
        {
            LogicalName = logicalName;
            DisplayName = displayName;
        }

        public override string ToString() => $"{DisplayName} ({LogicalName})";
    }
}
