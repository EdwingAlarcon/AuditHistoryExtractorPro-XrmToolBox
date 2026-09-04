namespace AuditHistoryExtractorPro.XrmToolBox.Plugin.Settings
{
    /// <summary>
    /// Preferencias del plugin persistidas por XrmToolBox vía SettingsHelper
    /// (JSON en %APPDATA%\MscrmTools\XrmToolBox\Settings). No confundir con
    /// "historial de extracciones": esto es solo configuración, no datos de auditoría.
    /// </summary>
    public class PluginSettings
    {
        public string CarpetaExportacionPorDefecto { get; set; } = string.Empty;
        public int MaxRegistrosPorDefecto { get; set; } = 50_000;
        public string FormatoExportacionPorDefecto { get; set; } = "xlsx";
    }
}
