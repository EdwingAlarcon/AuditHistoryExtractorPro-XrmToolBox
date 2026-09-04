using System.Collections.Generic;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using FluentAssertions;
using Xunit;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Tests
{
    public class AuditRecordTests
    {
        [Fact]
        public void ResumenCambios_SinNewValues_DevuelveVacio()
        {
            var registro = new AuditRecord();

            registro.ResumenCambios.Should().BeEmpty();
        }

        [Fact]
        public void ResumenCambios_SoloCamposQueCambiaron_LosLista()
        {
            var registro = new AuditRecord
            {
                OldValues = new Dictionary<string, string> { ["name"] = "Contoso SA", ["revenue"] = "1000" },
                NewValues = new Dictionary<string, string> { ["name"] = "Contoso SA de CV", ["revenue"] = "1000" }
            };

            registro.ResumenCambios.Should().Be("name: Contoso SA → Contoso SA de CV");
        }

        [Fact]
        public void ResumenCambios_CampoNuevoSinValorAnterior_MuestraVacioComoAnterior()
        {
            var registro = new AuditRecord
            {
                NewValues = new Dictionary<string, string> { ["name"] = "Contoso SA" }
            };

            registro.ResumenCambios.Should().Be("name: (vacío) → Contoso SA");
        }
    }
}
