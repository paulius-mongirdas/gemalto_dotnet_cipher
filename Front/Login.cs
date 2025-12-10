using Cipher.OnCardApp;
using SmartCard.Runtime.Remoting.Channels.APDU;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    public partial class Login : Form
    {
        private Service service;
        private const string URL = "apdu://selfdiscover/gemalto_dotnet_cipher.uri";

        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblMessage;
        private Label lblTitle;
        private Button btnSetPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Label lblNewPassword;
        private Label lblConfirmPassword;

        public bool IsAuthenticated { get; private set; }
        public Service AuthenticatedService { get; private set; }

        public Login()
        {
            InitializeComponents();
            InitializeSmartCardConnection();
            CheckPasswordStatus();
        }

        private void InitializeComponents()
        {
            this.Text = "Smart Card Authentication";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title
            lblTitle = new Label
            {
                Text = "Smart Card Login",
                Location = new Point(120, 20),
                Size = new Size(160, 30),
                Font = new Font("Arial", 14, FontStyle.Bold)
            };

            // Message label
            lblMessage = new Label
            {
                Location = new Point(20, 60),
                Size = new Size(350, 40),
                Font = new Font("Arial", 9),
                ForeColor = Color.Red
            };

            Controls.AddRange(new Control[] { lblTitle, lblMessage });

            // Other controls will be added dynamically based on password status
        }

        private void InitializeSmartCardConnection()
        {
            try
            {
                APDUClientChannel channel = new APDUClientChannel();
                ChannelServices.RegisterChannel(channel);
                service = (Service)Activator.GetObject(typeof(Service), URL);
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Connection failed: {ex.Message}";
                btnLogin.Enabled = false;
            }
        }

        private void CheckPasswordStatus()
        {
            try
            {
                bool passwordSet = service.IsPasswordSet();

                if (passwordSet)
                {
                    ShowLoginControls();
                }
                else
                {
                    ShowSetPasswordControls();
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Error: {ex.Message}";
            }
        }

        private void ShowLoginControls()
        {
            // Clear existing controls
            ClearDynamicControls();

            lblMessage.Text = "Please enter your password";

            // Password input
            Label lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(50, 110),
                Size = new Size(80, 25)
            };

            txtPassword = new TextBox
            {
                Location = new Point(130, 110),
                Size = new Size(200, 25),
                PasswordChar = '*'
            };

            // Login button
            btnLogin = new Button
            {
                Text = "Login",
                Location = new Point(150, 160),
                Size = new Size(100, 30)
            };
            btnLogin.Click += BtnLogin_Click;

            Controls.AddRange(new Control[] { lblPassword, txtPassword, btnLogin });
        }

        private void ShowSetPasswordControls()
        {
            // Clear existing controls
            ClearDynamicControls();

            lblMessage.Text = "No password set. Please create one.";

            // New password
            lblNewPassword = new Label
            {
                Text = "New Password:",
                Location = new Point(50, 100),
                Size = new Size(100, 25)
            };

            txtNewPassword = new TextBox
            {
                Location = new Point(150, 100),
                Size = new Size(180, 25),
                PasswordChar = '*'
            };

            // Confirm password
            lblConfirmPassword = new Label
            {
                Text = "Confirm:",
                Location = new Point(50, 140),
                Size = new Size(100, 25)
            };

            txtConfirmPassword = new TextBox
            {
                Location = new Point(150, 140),
                Size = new Size(180, 25),
                PasswordChar = '*'
            };

            // Set password button
            btnSetPassword = new Button
            {
                Text = "Set Password",
                Location = new Point(150, 190),
                Size = new Size(100, 30)
            };
            btnSetPassword.Click += BtnSetPassword_Click;

            Controls.AddRange(new Control[] {
                lblNewPassword, txtNewPassword,
                lblConfirmPassword, txtConfirmPassword,
                btnSetPassword
            });
        }

        private void ClearDynamicControls()
        {
            // Remove all controls except title and message
            var controlsToRemove = new System.Collections.Generic.List<Control>();
            foreach (Control ctrl in Controls)
            {
                if (ctrl != lblTitle && ctrl != lblMessage)
                {
                    controlsToRemove.Add(ctrl);
                }
            }

            foreach (Control ctrl in controlsToRemove)
            {
                Controls.Remove(ctrl);
                ctrl.Dispose();
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "Please enter a password";
                return;
            }

            try
            {
                bool success = service.VerifyPassword(password);

                if (success)
                {
                    IsAuthenticated = true;
                    AuthenticatedService = service;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblMessage.Text = "Incorrect password. Try again.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Error: {ex.Message}";
            }
        }

        private void BtnSetPassword_Click(object sender, EventArgs e)
        {
            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(newPassword))
            {
                lblMessage.Text = "Please enter a password";
                return;
            }

            if (newPassword != confirmPassword)
            {
                lblMessage.Text = "Passwords do not match";
                return;
            }

            try
            {
                service.CreatePassword(newPassword);
                lblMessage.Text = "Password set successfully!";
                ShowLoginControls(); // Switch to login controls
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Error: {ex.Message}";
            }
        }
    }
}
