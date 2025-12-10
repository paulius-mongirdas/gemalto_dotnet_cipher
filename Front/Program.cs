using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (Login loginForm = new Login())
            {
                if (loginForm.ShowDialog() == DialogResult.OK && loginForm.IsAuthenticated)
                {
                    // Pass authenticated service to main form
                    Form1 mainForm = new Form1(loginForm.AuthenticatedService);
                    Application.Run(mainForm);
                }
                else
                {
                    // Login failed or was cancelled
                    MessageBox.Show("Authentication required to use the application.",
                                  "Authentication Required",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                    Application.Exit();
                }
            }
        }
    }
}
