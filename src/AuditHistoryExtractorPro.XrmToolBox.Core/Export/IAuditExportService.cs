using System.Collections.Generic;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Export
{
    public interface IAuditExportService
    {
        /// <summary>Extensión de archivo que produce este exportador, sin punto (ej. "xlsx").</summary>
        string Extension { get; }

        void Export(IEnumerable<AuditRecord> registros, string rutaDestino);
    }
}
