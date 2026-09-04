using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Drawing;
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
    /// Punto de entrada del plugin para el host de XrmToolBox.
    /// El host instancia este UserControl (vía MEF, por los atributos Export/ExportMetadata
    /// abajo) y le entrega la conexión ya autenticada (ConnectionDetail / IOrganizationService)
    /// — este plugin NO gestiona su propia auth.
    /// </summary>
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Audit History Extractor Pro")]
    [ExportMetadata("Description", "Extrae, exporta y valida historial de auditoría de Dataverse bajo demanda.")]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SmallImageBase64", "")]
    [ExportMetadata("BigImageBase64", "")]
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

            _extraccionView = new ExtraccionView { Dock = DockStyle.Fill };
            _extraccionView.SolicitarExtraccion += filtros => EjecutarExtraccion(filtros);

            _validarView = new ValidarView { Dock = DockStyle.Fill };
            _validarView.SolicitarValidacion += (entidad, auditId) => EjecutarValidacion(entidad, auditId);

            var tabExtraer = new TabPage("Extraer") { Padding = new Padding(8) };
            tabExtraer.Controls.Add(_extraccionView);

            var tabValidar = new TabPage("Validar") { Padding = new Padding(8) };
            tabValidar.Controls.Add(_validarView);

            tabPrincipal.TabPages.Add(tabExtraer);
            tabPrincipal.TabPages.Add(tabValidar);
        }

        private void EjecutarExtraccion(AuditQueryFilters filtros)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Extrayendo historial de auditoría...",
                Work = (worker, args) =>
                {
                    var query = AuditQueryBuilder.Build(filtros);
                    var registros = new List<AuditRecord>();

                    // Paginación estándar de Dataverse (5000 filas por página).
                    query.PageInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { Count = 5000, PageNumber = 1 };
                    while (true)
                    {
                        var resultado = Service.RetrieveMultiple(query);
                        foreach (var e in resultado.Entities)
                            registros.Add(MapearEntidadAuditRecord(e));

                        worker.ReportProgress(0, $"{registros.Count} registros extraídos...");

                        if (!resultado.MoreRecords) break;
                        query.PageInfo.PageNumber++;
                        query.PageInfo.PagingCookie = resultado.PagingCookie;
                    }

                    args.Result = registros;
                },
                ProgressChanged = args => SetWorkingMessage(args.UserState?.ToString() ?? "Procesando..."),
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this, $"Error al extraer: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _extraccionView.MostrarResultados((List<AuditRecord>)args.Result);
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
                UserFullName = e.GetAttributeValue<AliasedValue>("u.fullname")?.Value?.ToString(),
                UserId = e.GetAttributeValue<EntityReference>("userid")?.Id ?? Guid.Empty,
                OldValues = changeData.OldValues,
                NewValues = changeData.NewValues
            };
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();
    }
}
