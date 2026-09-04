using System;
using System.ComponentModel;
using System.Windows.Forms;
using AuditHistoryExtractorPro.XrmToolBox.Core.Comparison;
using AuditHistoryExtractorPro.XrmToolBox.Core.Models;
using Microsoft.Xrm.Sdk;

namespace AuditHistoryExtractorPro.XrmToolBox.Plugin.Views
{
    /// <summary>
    /// Pantalla "Validar": spot-check de un AuditId puntual contra el estado actual
    /// del registro en Dataverse. Equivalente reducido a /validar.
    /// </summary>
    public class ValidarView : UserControl
    {
        public IOrganizationService Service { get; set; }

        /// <summary>(entidad, auditId) — el trabajo contra Dataverse lo ejecuta PluginControl vía WorkAsync.</summary>
        public event Action<string, Guid> SolicitarValidacion;

        private TextBox txtAuditId;
        private TextBox txtEntidad;
        private Button btnValidar;
        private DataGridView grid;

        public ValidarView()
        {
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var panelSuperior = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true, Padding = new Padding(4) };

            panelSuperior.Controls.Add(new System.Windows.Forms.Label { Text = "Entidad:", AutoSize = true }, 0, 0);
            txtEntidad = new TextBox { Width = 150 };
            panelSuperior.Controls.Add(txtEntidad, 1, 0);

            panelSuperior.Controls.Add(new System.Windows.Forms.Label { Text = "AuditId:", AutoSize = true }, 2, 0);
            txtAuditId = new TextBox { Width = 260 };
            panelSuperior.Controls.Add(txtAuditId, 3, 0);

            btnValidar = new Button { Text = "Validar contra Dynamics", AutoSize = true };
            btnValidar.Click += BtnValidar_Click;
            panelSuperior.Controls.Add(btnValidar, 3, 1);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            grid.Columns.Add("NombreCampo", "Campo");
            grid.Columns.Add("ValorEnAuditoria", "Valor en auditoría");
            grid.Columns.Add("ValorActualEnDynamics", "Valor actual en Dynamics");
            grid.Columns["NombreCampo"].DataPropertyName = "NombreCampo";
            grid.Columns["ValorEnAuditoria"].DataPropertyName = "ValorEnAuditoria";
            grid.Columns["ValorActualEnDynamics"].DataPropertyName = "ValorActualEnDynamics";

            Controls.Add(grid);
            Controls.Add(panelSuperior);
        }

        private void BtnValidar_Click(object sender, EventArgs e)
        {
            if (!Guid.TryParse(txtAuditId.Text.Trim(), out var auditId) || string.IsNullOrWhiteSpace(txtEntidad.Text))
            {
                MessageBox.Show(this, "Indique la entidad y un AuditId válido.", "Falta información",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SolicitarValidacion?.Invoke(txtEntidad.Text.Trim(), auditId);
        }

        /// <summary>Vuelca el resultado de la comparación en la grilla. Llamado por PluginControl tras WorkAsync.</summary>
        public void MostrarResultado(AuditComparisonResult resultado)
        {
            grid.DataSource = new BindingList<CampoDiferencia>(resultado.Diferencias);

            if (!resultado.TodoCoincide)
            {
                MessageBox.Show(this,
                    "Se encontraron diferencias entre la auditoría y el estado actual del registro.",
                    "Diferencias detectadas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void MostrarError(string mensaje)
        {
            MessageBox.Show(this, mensaje, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
