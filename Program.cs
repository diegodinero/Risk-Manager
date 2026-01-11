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
            
            // Add FormClosing event handler to ensure graceful shutdown
            form.FormClosing += (sender, e) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MAIN] Form closing event triggered");
                    
                    // Dispose the control to trigger cleanup
                    control?.Dispose();
                    
                    System.Diagnostics.Debug.WriteLine("[MAIN] Control disposed successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MAIN] Error during form closing: {ex.Message}");
                    
                    // Even if there's an error, allow the form to close
                    // The shutdown mechanism will handle forceful termination if needed
                }
            };
            
            Application.Run(form);
        }
    }
}