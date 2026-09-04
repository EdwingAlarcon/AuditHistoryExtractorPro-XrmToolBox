using System;
using System.Collections.Generic;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Export;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using FluentAssertions;
using Xunit;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Tests
{
    public class AuditExportRowFlattenerTests
    {
        [Fact]
        public void Aplanar_EventoSinCambios_DevuelveUnaFilaConColumnasDeCampoVacias()
        {
            var registro = new AuditRecord
            {
                AuditId = Guid.NewGuid(),
                EntityLogicalName = "account",
                Action = AuditAction.Create,
                Operation = AuditOperation.Create
            };

            var filas = AuditExportRowFlattener.Aplanar(registro).ToList();

            filas.Should().HaveCount(1);
            var campoIndex = Array.IndexOf(AuditExportRowFlattener.Encabezados, "Campo");
            filas[0][campoIndex].Should().BeEmpty();
        }

        [Fact]
        public void Aplanar_EventoConDosCamposCambiados_DevuelveUnaFilaPorCampo()
        {
            var registro = new AuditRecord
            {
                AuditId = Guid.NewGuid(),
                EntityLogicalName = "account",
                Action = AuditAction.Update,
                Operation = AuditOperation.Update,
                OldValues = new Dictionary<string, string> { ["name"] = "Contoso SA", ["revenue"] = "1000" },
                NewValues = new Dictionary<string, string> { ["name"] = "Contoso SA de CV", ["revenue"] = "2000" }
            };

            var filas = AuditExportRowFlattener.Aplanar(registro).ToList();

            filas.Should().HaveCount(2);
            var campoIndex = Array.IndexOf(AuditExportRowFlattener.Encabezados, "Campo");
            filas.Select(f => f[campoIndex]).Should().BeEquivalentTo(new[] { "name", "revenue" });
        }

        [Fact]
        public void Aplanar_CampoLookup_IncluyeValorAnteriorYNuevoDeLookup()
        {
            var registro = new AuditRecord
            {
                AuditId = Guid.NewGuid(),
                EntityLogicalName = "account",
                Operation = AuditOperation.Update,
                OldValues = new Dictionary<string, string> { ["ownerid"] = "guid-viejo" },
                NewValues = new Dictionary<string, string> { ["ownerid"] = "guid-nuevo" },
                LookupOldValues = new Dictionary<string, string> { ["ownerid"] = "Juan Pérez" },
                LookupNewValues = new Dictionary<string, string> { ["ownerid"] = "María Gómez" }
            };

            var fila = AuditExportRowFlattener.Aplanar(registro).Single();

            var lookupAnteriorIndex = Array.IndexOf(AuditExportRowFlattener.Encabezados, "ValorAnteriorLookup");
            var lookupNuevoIndex = Array.IndexOf(AuditExportRowFlattener.Encabezados, "ValorNuevoLookup");
            fila[lookupAnteriorIndex].Should().Be("Juan Pérez");
            fila[lookupNuevoIndex].Should().Be("María Gómez");
        }
    }
}
