using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public class JsonAuditExportService : IAuditExportService
    {
        public string Extension => "json";

        public void Export(IEnumerable<Models.AuditRecord> registros, string rutaDestino)
        {
            var json = JsonConvert.SerializeObject(registros, Formatting.Indented);
            File.WriteAllText(rutaDestino, json, System.Text.Encoding.UTF8);
        }
    }
}
