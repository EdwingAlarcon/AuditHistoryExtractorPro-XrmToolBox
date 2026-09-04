using System;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using AuditHistoryExtractorPro.XrmToolBox.Core.Queries;
using FluentAssertions;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Tests
{
    public class AuditQueryBuilderTests
    {
        [Fact]
        public void Build_SinEntidad_LanzaExcepcion()
        {
            var filtros = new AuditQueryFilters { EntityLogicalName = "" };

            Action act = () => AuditQueryBuilder.Build(filtros);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Build_ConEntidadYFechas_ArmaCriteriosCorrectos()
        {
            var filtros = new AuditQueryFilters
            {
                EntityLogicalName = "account",
                FechaDesde = new DateTime(2026, 1, 1),
                FechaHasta = new DateTime(2026, 3, 31),
                Operaciones = new[] { AuditAction.Update, AuditAction.Delete }
            };

            var query = AuditQueryBuilder.Build(filtros);

            query.EntityName.Should().Be("audit");
            query.Criteria.Conditions.Should().Contain(c => c.AttributeName == "objecttypecode" && (string)c.Values[0] == "account");
            query.Criteria.Conditions.Should().Contain(c => c.AttributeName == "createdon" && c.Operator == ConditionOperator.OnOrAfter);
            query.Criteria.Conditions.Should().Contain(c => c.AttributeName == "action" && c.Operator == ConditionOperator.In);
        }
    }
}
