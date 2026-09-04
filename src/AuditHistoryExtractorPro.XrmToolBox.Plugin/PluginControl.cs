using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuditHistoryExtractorPro.XrmToolBox.Core.Comparison;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using AuditHistoryExtractorPro.XrmToolBox.Core.Parsing;
using AuditHistoryExtractorPro.XrmToolBox.Core.Queries;
using AuditHistoryExtractorPro.XrmToolBox.Plugin.Settings;
using AuditHistoryExtractorPro.XrmToolBox.Plugin.Views;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace AuditHistoryExtractorPro.XrmToolBox.Plugin
{
    /// <summary>
    /// UserControl que el host instancia (vía <see cref="Plugin.GetControl"/>) y al que le
    /// entrega la conexión ya autenticada (ConnectionDetail / IOrganizationService) — este
    /// plugin NO gestiona su propia auth. El punto de entrada MEF real es <see cref="Plugin"/>
    /// (ver Plugin.cs): esta clase NO lleva atributos Export/ExportMetadata — el host la
    /// descubre a través de <c>Plugin.GetControl()</c>, no por composición MEF directa.
    /// </summary>
    public partial class PluginControl : PluginControlBase, IGitHubPlugin, IHelpPlugin
    {
        private PluginSettings _settings;
        private ExtraccionView _extraccionView;
        private ValidarView _validarView;

        public string RepositoryName => "AuditHistoryExtractorPro-XrmToolBox";
        public string UserName => "EdwingAlarcon";
        public string HelpUrl => "https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox";

        public PluginControl()
        {
            InitializeComponent();

            // Las vistas se crean acá y no en el evento Load: el host llama a UpdateConnection
            // apenas instancia el control (antes de que se dispare Load, que solo ocurre cuando
            // el control ya tiene handle de ventana) — con las vistas creadas recién en Load,
            // UpdateConnection las encontraba en null y tiraba NullReferenceException al abrir
            // la herramienta ("Referencia a objeto no establecida como instancia de un objeto").
            _extraccionView = new ExtraccionView { Dock = DockStyle.Fill };
            _extraccionView.SolicitarExtraccion += filtros => EjecutarExtraccion(filtros);
            _extraccionView.SolicitarEntidades += EjecutarCargaEntidades;

            _validarView = new ValidarView { Dock = DockStyle.Fill };
            _validarView.SolicitarValidacion += (entidad, auditId) => EjecutarValidacion(entidad, auditId);

            var tabExtraer = new TabPage("Extraer") { Padding = new Padding(8) };
            tabExtraer.Controls.Add(_extraccionView);

            var tabValidar = new TabPage("Validar") { Padding = new Padding(8) };
            tabValidar.Controls.Add(_validarView);

            tabPrincipal.TabPages.Add(tabExtraer);
            tabPrincipal.TabPages.Add(tabValidar);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            _extraccionView.Service = newService;
            _validarView.Service = newService;
        }

        private void PluginControl_Load(object sender, EventArgs e)
        {
            if (!SettingsManager.Instance.TryLoad(GetType(), out _settings))
            {
                _settings = new PluginSettings();
            }
        }

        private void EjecutarExtraccion(AuditQueryFilters filtros)
        {
            // Se acumula en una variable de método (no en args.Result) para poder mostrar los
            // registros ya extraídos incluso si el usuario cancela a mitad de camino: al cancelar,
            // RunWorkerCompletedEventArgs.Result no es accesible, pero esta lista sí.
            var registros = new List<AuditRecord>();
            // true si se cortó por MaxRegistros habiendo más disponibles (filtros.SinLimite=false)
            // — a diferencia de terminar porque Dataverse ya no tiene más páginas. Distinguir esto
            // es necesario para poder avisarle al usuario que el resultado está incompleto: antes
            // el corte era silencioso y no había forma de saber si faltaban registros.
            var seCortoPorLimite = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Extrayendo historial de auditoría...",
                IsCancelable = true,
                Work = (worker, args) =>
                {
                    var query = AuditQueryBuilder.Build(filtros);

                    // Paginación estándar de Dataverse (5000 filas por página), acotada por
                    // filtros.MaxRegistros solo si filtros.SinLimite es false (no se puede
                    // combinar TopCount con PageInfo, por eso el límite se aplica acá).
                    query.PageInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { Count = 5000, PageNumber = 1 };
                    while (true)
                    {
                        if (worker.CancellationPending)
                        {
                            args.Cancel = true;
                            return;
                        }

                        var resultado = Service.RetrieveMultiple(query);
                        foreach (var e in resultado.Entities)
                            registros.Add(MapearEntidadAuditRecord(e));

                        worker.ReportProgress(0, $"{registros.Count} registros extraídos...");

                        if (!resultado.MoreRecords) break;
                        if (!filtros.SinLimite && registros.Count >= filtros.MaxRegistros)
                        {
                            seCortoPorLimite = true;
                            break;
                        }
                        query.PageInfo.PageNumber++;
                        query.PageInfo.PagingCookie = resultado.PagingCookie;
                    }
                },
                ProgressChanged = args => SetWorkingMessage(args.UserState?.ToString() ?? "Procesando..."),
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this, $"Error al extraer: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _extraccionView.MostrarResultados(registros);
                        return;
                    }

                    if (args.Cancelled)
                    {
                        MessageBox.Show(this,
                            $"Extracción cancelada. Se muestran los {registros.Count} registros obtenidos hasta el momento.",
                            "Extracción cancelada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (seCortoPorLimite)
                    {
                        MessageBox.Show(this,
                            $"Se alcanzó el límite configurado de {filtros.MaxRegistros:N0} registros, pero hay más " +
                            "disponibles para estos filtros — el resultado NO está completo. Tildá \"Sin límite\" o " +
                            "acotá el rango de fechas para traerlos todos.",
                            "Resultado incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    _extraccionView.MostrarResultados(registros);
                }
            });
        }

        private void EjecutarCargaEntidades()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Cargando entidades con auditoría habilitada...",
                Work = (worker, args) =>
                {
                    var request = new Microsoft.Xrm.Sdk.Messages.RetrieveAllEntitiesRequest
                    {
                        EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity,
                        RetrieveAsIfPublished = false
                    };

                    var response = (Microsoft.Xrm.Sdk.Messages.RetrieveAllEntitiesResponse)Service.Execute(request);

                    args.Result = response.EntityMetadata
                        .Where(m => m.IsAuditEnabled?.Value == true && !string.IsNullOrWhiteSpace(m.LogicalName))
                        .Select(m => new EntidadAuditable(
                            m.LogicalName,
                            m.DisplayName?.UserLocalizedLabel?.Label ?? m.LogicalName))
                        .OrderBy(x => x.DisplayName)
                        .ToList();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this, $"Error al cargar entidades: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _extraccionView.CargarEntidades((List<EntidadAuditable>)args.Result);
                }
            });
        }

        private void EjecutarValidacion(string entidad, Guid auditId)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Validando registro de auditoría...",
                Work = (worker, args) =>
                {
                    var auditEntity = Service.Retrieve("audit", auditId,
                        new Microsoft.Xrm.Sdk.Query.ColumnSet(
                            "createdon", "objectid", "objecttypecode", "action", "userid", "changedata"));

                    var registro = MapearEntidadAuditRecord(auditEntity);
                    if (string.IsNullOrWhiteSpace(registro.EntityLogicalName))
                        registro.EntityLogicalName = entidad;

                    args.Result = new AuditComparisonService(Service).Comparar(registro);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        _validarView.MostrarError($"Error al validar: {args.Error.Message}");
                        return;
                    }

                    _validarView.MostrarResultado((AuditComparisonResult)args.Result);
                }
            });
        }

        private static AuditRecord MapearEntidadAuditRecord(Entity e)
        {
            var changeData = AuditChangeDataParser.Parse(e.GetAttributeValue<string>("changedata"));

            return new AuditRecord
            {
                AuditId = e.Id,
                CreatedOn = e.GetAttributeValue<DateTime>("createdon"),
                EntityLogicalName = e.GetAttributeValue<string>("objecttypecode"),
                RecordId = e.GetAttributeValue<EntityReference>("objectid")?.Id ?? Guid.Empty,
                RecordPrimaryName = e.GetAttributeValue<EntityReference>("objectid")?.Name,
                Action = (AuditAction)(e.GetAttributeValue<OptionSetValue>("action")?.Value ?? 0),
                Operation = (AuditOperation)(e.GetAttributeValue<OptionSetValue>("operation")?.Value ?? 0),
                UserFullName = e.GetAttributeValue<AliasedValue>("u.fullname")?.Value?.ToString(),
                UserId = e.GetAttributeValue<EntityReference>("userid")?.Id ?? Guid.Empty,
                OldValues = changeData.OldValues,
                NewValues = changeData.NewValues,
                LookupOldValues = changeData.LookupOldValues,
                LookupNewValues = changeData.LookupNewValues
            };
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();
    }
}
