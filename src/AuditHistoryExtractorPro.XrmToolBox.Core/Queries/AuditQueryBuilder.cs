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
            var query = new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet(
                    "auditid", "createdon", "objectid", "objecttypecode",
                    "action", "operation", "userid", "changedata"),
                // Sin TopCount: Dataverse no permite combinarlo con paginación (PageInfo), que es
                // como se recorre el resultado en PluginControl.EjecutarExtraccion. El límite de
                // MaxRegistros se aplica ahí, cortando la paginación al alcanzarlo.
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                Criteria = ConstruirFiltro(filtros)
            };

            var userLink = query.AddLink("systemuser", "userid", "systemuserid", JoinOperator.LeftOuter);
            userLink.EntityAlias = "u";
            userLink.Columns = new ColumnSet("fullname");

            return query;
        }

        /// <summary>
        /// Versión liviana de <see cref="Build"/> — mismo filtro, pero sin "changedata" ni el
        /// join a systemuser, solo para contar cuántos registros hay antes de arrancar la
        /// extracción real (progreso: "X de Y", tiempo restante estimado). Barata en comparación
        /// con la extracción completa porque no trae el detalle de cada registro.
        /// </summary>
        public static QueryExpression BuildConteo(AuditQueryFilters filtros)
        {
            return new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet("auditid"),
                Criteria = ConstruirFiltro(filtros)
            };
        }

        private static FilterExpression ConstruirFiltro(AuditQueryFilters filtros)
        {
            if (filtros == null) throw new ArgumentNullException(nameof(filtros));
            if (string.IsNullOrWhiteSpace(filtros.EntityLogicalName))
                throw new ArgumentException("Debe indicar la entidad a auditar.", nameof(filtros));

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

            return filter;
        }
    }
}
