using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using CsvHelper;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public class CsvAuditExportService : IAuditExportService
    {
        public string Extension => "csv";

        private class FilaCsv
        {
            public string AuditId { get; set; }
            public string Fecha { get; set; }
            public string Entidad { get; set; }
            public string RegistroId { get; set; }
            public string RegistroNombre { get; set; }
            public string Accion { get; set; }
            public string Usuario { get; set; }
            public string ValoresAnteriores { get; set; }
            public string ValoresNuevos { get; set; }
        }

        public void Export(IEnumerable<AuditRecord> registros, string rutaDestino)
        {
            var filas = registros.Select(r => new FilaCsv
            {
                AuditId = r.AuditId.ToString(),
                Fecha = r.CreatedOn.ToString("O"),
                Entidad = r.EntityLogicalName,
                RegistroId = r.RecordId.ToString(),
                RegistroNombre = r.RecordPrimaryName,
                Accion = r.Action.ToString(),
                Usuario = r.UserFullName,
                ValoresAnteriores = string.Join("; ", r.OldValues.Select(kv => $"{kv.Key}={kv.Value}")),
                ValoresNuevos = string.Join("; ", r.NewValues.Select(kv => $"{kv.Key}={kv.Value}"))
            });

            using (var writer = new StreamWriter(rutaDestino, false, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(filas);
            }
        }
    }
}
