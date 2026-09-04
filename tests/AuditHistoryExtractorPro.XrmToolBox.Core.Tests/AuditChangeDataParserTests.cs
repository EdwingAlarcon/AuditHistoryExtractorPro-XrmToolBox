using AuditHistoryExtractorPro.XrmToolBox.Core.Parsing;
using FluentAssertions;
using Xunit;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Tests
{
    public class AuditChangeDataParserTests
    {
        [Fact]
        public void Parse_ConValoresAnterioresYNuevos_LosSepara()
        {
            const string xml = @"
                <audit>
                    <oldValues>
                        <name value=""Contoso SA"" />
                        <revenue value=""1000"" />
                    </oldValues>
                    <newValues>
                        <name value=""Contoso SA de CV"" />
                        <revenue value=""2000"" />
                    </newValues>
                </audit>";

            var resultado = AuditChangeDataParser.Parse(xml);

            resultado.OldValues.Should().Contain("name", "Contoso SA");
            resultado.OldValues.Should().Contain("revenue", "1000");
            resultado.NewValues.Should().Contain("name", "Contoso SA de CV");
            resultado.NewValues.Should().Contain("revenue", "2000");
        }

        [Fact]
        public void Parse_XmlVacioONulo_DevuelveDiccionariosVacios()
        {
            AuditChangeDataParser.Parse(null).OldValues.Should().BeEmpty();
            AuditChangeDataParser.Parse("").NewValues.Should().BeEmpty();
        }

        [Fact]
        public void Parse_XmlInvalido_NoLanzaYDevuelveVacio()
        {
            var resultado = AuditChangeDataParser.Parse("<audit><oldValues>");

            resultado.OldValues.Should().BeEmpty();
            resultado.NewValues.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SoloNewValues_OldValuesQuedaVacio()
        {
            const string xml = @"
                <audit>
                    <newValues>
                        <name value=""Contoso SA"" />
                    </newValues>
                </audit>";

            var resultado = AuditChangeDataParser.Parse(xml);

            resultado.OldValues.Should().BeEmpty();
            resultado.NewValues.Should().Contain("name", "Contoso SA");
        }

        [Fact]
        public void Parse_AtributoLookup_SeparaIdYDisplayName()
        {
            const string xml = @"
                <audit>
                    <oldValues>
                        <ownerid value=""8d4e4c2e-0000-0000-0000-000000000001"">Juan Pérez</ownerid>
                    </oldValues>
                    <newValues>
                        <ownerid value=""a1b2c3d4-0000-0000-0000-000000000002"">María Gómez</ownerid>
                    </newValues>
                </audit>";

            var resultado = AuditChangeDataParser.Parse(xml);

            resultado.OldValues.Should().Contain("ownerid", "8d4e4c2e-0000-0000-0000-000000000001");
            resultado.NewValues.Should().Contain("ownerid", "a1b2c3d4-0000-0000-0000-000000000002");
            resultado.LookupOldValues.Should().Contain("ownerid", "Juan Pérez");
            resultado.LookupNewValues.Should().Contain("ownerid", "María Gómez");
        }

        [Fact]
        public void Parse_AtributoNoLookup_NoLlenaDiccionarioDeLookup()
        {
            const string xml = @"
                <audit>
                    <newValues>
                        <revenue value=""2000"" />
                    </newValues>
                </audit>";

            var resultado = AuditChangeDataParser.Parse(xml);

            resultado.NewValues.Should().Contain("revenue", "2000");
            resultado.LookupNewValues.Should().BeEmpty();
        }
    }
}
