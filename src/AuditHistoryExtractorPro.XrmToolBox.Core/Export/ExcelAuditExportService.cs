using System.Collections.Generic;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using ClosedXML.Excel;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public class ExcelAuditExportService : IAuditExportService
    {
        public string Extension => "xlsx";

        public void Export(IEnumerable<AuditRecord> registros, string rutaDestino)
        {
            var lista = registros.ToList();

            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("Auditoria");
                var encabezados = new[]
                {
                    "AuditId", "Fecha", "Entidad", "RegistroId", "RegistroNombre",
                    "Accion", "Usuario", "ValoresAnteriores", "ValoresNuevos"
                };

                for (int i = 0; i < encabezados.Length; i++)
                    hoja.Cell(1, i + 1).Value = encabezados[i];

                for (int fila = 0; fila < lista.Count; fila++)
                {
                    var r = lista[fila];
                    var f = fila + 2;
                    hoja.Cell(f, 1).Value = r.AuditId.ToString();
                    hoja.Cell(f, 2).Value = r.CreatedOn;
                    hoja.Cell(f, 3).Value = r.EntityLogicalName;
                    hoja.Cell(f, 4).Value = r.RecordId.ToString();
                    hoja.Cell(f, 5).Value = r.RecordPrimaryName;
                    hoja.Cell(f, 6).Value = r.Action.ToString();
                    hoja.Cell(f, 7).Value = r.UserFullName;
                    hoja.Cell(f, 8).Value = string.Join("; ", r.OldValues.Select(kv => $"{kv.Key}={kv.Value}"));
                    hoja.Cell(f, 9).Value = string.Join("; ", r.NewValues.Select(kv => $"{kv.Key}={kv.Value}"));
                }

                hoja.Columns().AdjustToContents();
                workbook.SaveAs(rutaDestino);
            }
        }
    }
}
