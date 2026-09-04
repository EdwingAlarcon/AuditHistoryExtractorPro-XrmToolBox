using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using AuditHistoryExtractorPro.XrmToolBox.Core.Export;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using Microsoft.Xrm.Sdk;

namespace AuditHistoryExtractorPro.XrmToolBox.Plugin.Views
{
    /// <summary>
    /// Pantalla "Extraer": filtros + previsualización en grilla + exportación. Equivalente reducido a /archivar.
    /// La extracción (llamada a Dataverse) delega el trabajo largo a WorkAsync del host (ver PluginControl);
    /// la exportación a archivo es E/S local y se resuelve acá mismo, sin pasar por el host.
    /// </summary>
    public class ExtraccionView : UserControl
    {
        public IOrganizationService Service { get; set; }

        /// <summary>Filtros elegidos por el usuario. PluginControl ejecuta la consulta y llama a MostrarResultados.</summary>
        public event Action<AuditQueryFilters> SolicitarExtraccion;

        private static readonly IReadOnlyDictionary<string, IAuditExportService> Exportadores =
            new Dictionary<string, IAuditExportService>
            {
                ["xlsx"] = new ExcelAuditExportService(),
                ["csv"] = new CsvAuditExportService(),
                ["json"] = new JsonAuditExportService()
            };

        private TextBox txtEntidad;
        private DateTimePicker dtDesde;
        private DateTimePicker dtHasta;
        private CheckedListBox clbOperaciones;
        private ComboBox cboFormato;
        private Button btnExtraer;
        private Button btnExportar;
        private DataGridView grid;

        private List<AuditRecord> _resultados = new List<AuditRecord>();

        public ExtraccionView()
        {
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(4) };

            layout.Controls.Add(new System.Windows.Forms.Label { Text = "Entidad (nombre lógico):", AutoSize = true }, 0, 0);
            txtEntidad = new TextBox { Width = 200 };
            layout.Controls.Add(txtEntidad, 1, 0);

            layout.Controls.Add(new System.Windows.Forms.Label { Text = "Desde:", AutoSize = true }, 0, 1);
            dtDesde = new DateTimePicker { Width = 200, Value = DateTime.Today.AddMonths(-1) };
            layout.Controls.Add(dtDesde, 1, 1);

            layout.Controls.Add(new System.Windows.Forms.Label { Text = "Hasta:", AutoSize = true }, 0, 2);
            dtHasta = new DateTimePicker { Width = 200, Value = DateTime.Today };
            layout.Controls.Add(dtHasta, 1, 2);

            layout.Controls.Add(new System.Windows.Forms.Label { Text = "Operaciones:", AutoSize = true }, 0, 3);
            clbOperaciones = new CheckedListBox { Width = 200, Height = 80 };
            clbOperaciones.Items.AddRange(new object[]
            {
                AuditAction.Create, AuditAction.Update, AuditAction.Delete, AuditAction.Access
            });
            layout.Controls.Add(clbOperaciones, 1, 3);

            btnExtraer = new Button { Text = "Extraer", AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
            btnExtraer.Click += BtnExtraer_Click;
            layout.Controls.Add(btnExtraer, 1, 4);

            var panelExportar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };

            panelExportar.Controls.Add(new System.Windows.Forms.Label { Text = "Formato:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            cboFormato = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFormato.Items.AddRange(new object[] { "xlsx", "csv", "json" });
            cboFormato.SelectedIndex = 0;
            panelExportar.Controls.Add(cboFormato);

            btnExportar = new Button { Text = "Exportar...", AutoSize = true, Enabled = false, Margin = new Padding(8, 0, 0, 0) };
            btnExportar.Click += BtnExportar_Click;
            panelExportar.Controls.Add(btnExportar);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = true
            };

            Controls.Add(grid);
            Controls.Add(panelExportar);
            Controls.Add(layout);
        }

        private void BtnExtraer_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEntidad.Text))
            {
                MessageBox.Show(this, "Indique el nombre lógico de la entidad.", "Falta información",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var filtros = new AuditQueryFilters
            {
                EntityLogicalName = txtEntidad.Text.Trim(),
                FechaDesde = dtDesde.Value.Date,
                FechaHasta = dtHasta.Value.Date,
                Operaciones = clbOperaciones.CheckedItems.Cast<AuditAction>().ToList()
            };

            SolicitarExtraccion?.Invoke(filtros);
        }

        /// <summary>Llenar la grilla con los resultados. Llamado por PluginControl tras WorkAsync.</summary>
        public void MostrarResultados(List<AuditRecord> registros)
        {
            _resultados = registros ?? new List<AuditRecord>();

            grid.DataSource = new BindingList<AuditRecord>(_resultados);
            OcultarColumnasDeDetalle();

            btnExportar.Enabled = _resultados.Count > 0;
        }

        private void OcultarColumnasDeDetalle()
        {
            foreach (var nombre in new[] { "OldValues", "NewValues" })
            {
                if (grid.Columns.Contains(nombre))
                    grid.Columns[nombre].Visible = false;
            }
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (_resultados.Count == 0) return;

            var formato = cboFormato.SelectedItem.ToString();
            using (var sfd = new SaveFileDialog { Filter = $"Archivo .{formato}|*.{formato}", FileName = $"auditoria_{txtEntidad.Text.Trim()}.{formato}" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    Exportadores[formato].Export(_resultados, sfd.FileName);

                    MessageBox.Show(this,
                        $"Se exportaron {_resultados.Count} registros a:\n{sfd.FileName}",
                        "Exportación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error al exportar: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
