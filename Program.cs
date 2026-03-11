using System;
using System.Windows.Forms;

namespace Risk_Manager
{
    internal static class Program
    {
        // Flag to allow controlled shutdown via the shutdown button
        // Volatile ensures proper memory visibility across threads
        private static volatile bool _allowClose = false;
        
        public static bool AllowClose
        {
            get => _allowClose;
            set => _allowClose = value;
        }

        [STAThread]
        static void Main()
        {
            // Initialize WinForms in a way compatible with all project types
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form
            {
                Text = "QuantGuard v1.1.0 - Risk Manager",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1000,
                Height = 700,
                BackColor = System.Drawing.Color.FromArgb(45, 62, 80) // Match the dark theme
            };

            var control = new RiskManagerControl
            {
                Dock = DockStyle.Fill
            };

            form.Controls.Add(control);
            
            // Prevent form from being closed via X button or Alt+F4
            form.FormClosing += (s, e) =>
            {
                if (!AllowClose && e.CloseReason != CloseReason.WindowsShutDown)
                {
                    // Cancel the close event
                    e.Cancel = true;
                    
                    // Notify the user
                    MessageBox.Show(
                        "This application cannot be closed directly.\n\n" +
                        "Use the 🚪 Shutdown button in the top-right corner to lock accounts and close the application safely.",
                        "Cannot Close Application",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };
            
            Application.Run(form);
        }
    }
}