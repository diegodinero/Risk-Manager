using System;
using System.Windows.Forms;

namespace Risk_Manager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize WinForms in a way compatible with all project types
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form
            {
                Text = "RiskManagerControl Test",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1000,
                Height = 700
            };

            var control = new RiskManagerControl
            {
                Dock = DockStyle.Fill
            };

            form.Controls.Add(control);
            Application.Run(form);
        }
    }
}