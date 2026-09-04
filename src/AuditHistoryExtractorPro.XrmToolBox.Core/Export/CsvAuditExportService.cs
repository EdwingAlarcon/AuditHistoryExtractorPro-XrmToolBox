using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using CsvHelper;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public class CsvAuditExportService : IAuditExportService
    {
        public string Extension => "csv";

        public void Export(IEnumerable<AuditRecord> registros, string rutaDestino)
        {
            using (var writer = new StreamWriter(rutaDestino, false, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                foreach (var encabezado in AuditExportRowFlattener.Encabezados)
                    csv.WriteField(encabezado);
                csv.NextRecord();

                foreach (var registro in registros)
                {
                    foreach (var fila in AuditExportRowFlattener.Aplanar(registro))
                    {
                        foreach (var valor in fila)
                            csv.WriteField(valor);
                        csv.NextRecord();
                    }
                }
            }
        }
    }
}
