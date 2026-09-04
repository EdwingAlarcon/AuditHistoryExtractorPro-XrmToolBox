using System.Windows.Forms;

namespace AuditHistoryExtractorPro.XrmToolBox.Plugin
{
    partial class PluginControl
    {
        private System.ComponentModel.IContainer components = null;
        private TabControl tabPrincipal;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabPrincipal = new TabControl();
            this.SuspendLayout();

            this.tabPrincipal.Dock = DockStyle.Fill;
            this.tabPrincipal.Name = "tabPrincipal";

            this.Controls.Add(this.tabPrincipal);
            this.Name = "PluginControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.PluginControl_Load);

            this.ResumeLayout(false);
        }
    }
}
