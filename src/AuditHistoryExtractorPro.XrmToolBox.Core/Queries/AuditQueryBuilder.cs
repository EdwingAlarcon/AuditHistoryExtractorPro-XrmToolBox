using System;
using System.Linq;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using Microsoft.Xrm.Sdk.Query;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Queries
{
    /// <summary>
    /// Construye la QueryExpression contra la entidad "audit" a partir de los filtros
    /// elegidos en la UI. No depende de la conexión: solo arma la consulta.
    /// </summary>
    public static class AuditQueryBuilder
    {
        public static QueryExpression Build(AuditQueryFilters filtros)
        {
            if (filtros == null) throw new ArgumentNullException(nameof(filtros));
            if (string.IsNullOrWhiteSpace(filtros.EntityLogicalName))
                throw new ArgumentException("Debe indicar la entidad a auditar.", nameof(filtros));

            var query = new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet(
                    "auditid", "createdon", "objectid", "objecttypecode",
                    "action", "operation", "userid", "changedata"),
                TopCount = filtros.MaxRegistros,
                Orders = { new OrderExpression("createdon", OrderType.Descending) }
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition("objecttypecode", ConditionOperator.Equal, filtros.EntityLogicalName);

            if (filtros.FechaDesde.HasValue)
                filter.AddCondition("createdon", ConditionOperator.OnOrAfter, filtros.FechaDesde.Value);

            if (filtros.FechaHasta.HasValue)
                filter.AddCondition("createdon", ConditionOperator.OnOrBefore, filtros.FechaHasta.Value);

            if (filtros.Operaciones != null && filtros.Operaciones.Any())
            {
                var valores = filtros.Operaciones.Cast<int>().Cast<object>().ToArray();
                filter.AddCondition("action", ConditionOperator.In, valores);
            }

            query.Criteria = filter;

            var userLink = query.AddLink("systemuser", "userid", "systemuserid", JoinOperator.LeftOuter);
            userLink.EntityAlias = "u";
            userLink.Columns = new ColumnSet("fullname");

            return query;
        }
    }
}
