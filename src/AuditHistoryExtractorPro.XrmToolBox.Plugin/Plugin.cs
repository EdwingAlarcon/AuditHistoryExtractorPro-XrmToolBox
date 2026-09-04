using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace AuditHistoryExtractorPro.XrmToolBox.Plugin
{
    /// <summary>
    /// Punto de entrada MEF real del plugin para el host de XrmToolBox. El host compone el
    /// catálogo buscando exports de <see cref="IXrmToolBoxPlugin"/> — por eso el atributo va
    /// acá y no en <see cref="PluginControl"/> (el UserControl no implementa esa interfaz,
    /// implementa <c>IXrmToolBoxPluginControl</c> vía <c>PluginControlBase</c>; ponerle el
    /// Export directo al UserControl hace que el host descarte el plugin en silencio al no
    /// satisfacer el contrato exportado).
    /// </summary>
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Audit History Extractor Pro")]
    [ExportMetadata("Description", "Extrae, exporta y valida historial de auditoría de Dataverse bajo demanda.")]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SmallImageBase64", "")]
    [ExportMetadata("BigImageBase64", "")]
    public class Plugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl() => new PluginControl();
    }
}
