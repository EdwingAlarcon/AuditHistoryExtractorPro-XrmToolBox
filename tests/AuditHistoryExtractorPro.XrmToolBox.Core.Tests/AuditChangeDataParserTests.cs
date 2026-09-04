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
    }
}
