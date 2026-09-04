using System.Collections.Generic;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using ClosedXML.Excel;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public class ExcelAuditExportService : IAuditExportService
    {
        public string Extension => "xlsx";

        public void Export(IEnumerable<AuditRecord> registros, string rutaDestino)
        {
            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("Auditoria");
                var encabezados = AuditExportRowFlattener.Encabezados;

                for (int i = 0; i < encabezados.Length; i++)
                    hoja.Cell(1, i + 1).Value = encabezados[i];

                var f = 2;
                foreach (var registro in registros)
                {
                    foreach (var fila in AuditExportRowFlattener.Aplanar(registro))
                    {
                        for (int i = 0; i < fila.Length; i++)
                            hoja.Cell(f, i + 1).Value = fila[i];
                        f++;
                    }
                }

                hoja.Columns().AdjustToContents();
                workbook.SaveAs(rutaDestino);
            }
        }
    }
}
