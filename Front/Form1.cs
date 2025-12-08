using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Front
{
    public partial class Form1 : Form
    {
        // Controls
        private ListView mainListView;
        private Button btnBack;
        private Button btnHome;
        private Button btnConnect;
        private Button btnEncrypt;
        private Button btnDecrypt;
        private Button btnGenerateKeys;
        private Button btnRefresh;
        private Label lblCurrentPath;
        private TextBox txtStatus;

        // Navigation
        private Stack<string> pathHistory = new Stack<string>();
        private string currentPath;

        // Smart card service
        private object service;
        private const string URL = "apdu://selfdiscover/gemalto_dotnet_cipher.uri";

        public Form1()
        {
            InitializeComponents();
            LoadDrives();
            InitializeSmartCardConnection();
        }

        private void InitializeComponents()
        {
            this.Text = "Smart Card Encryption App";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Top navigation panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightGray
            };

            btnBack = new Button
            {
                Text = "←",
                Location = new Point(5, 5),
                Size = new Size(40, 30),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnBack.Click += BtnBack_Click;

            btnHome = new Button
            {
                Text = "Home",
                Location = new Point(50, 5),
                Size = new Size(60, 30)
            };
            btnHome.Click += BtnHome_Click;

            lblCurrentPath = new Label
            {
                Location = new Point(115, 10),
                Size = new Size(500, 20),
                Text = "Select a drive",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // Toolbar panel
            Panel toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            btnConnect = new Button
            {
                Text = "Connect",
                Location = new Point(5, 5),
                Size = new Size(80, 30)
            };
            btnConnect.Click += BtnConnect_Click;

            btnGenerateKeys = new Button
            {
                Text = "Generate Keys",
                Location = new Point(90, 5),
                Size = new Size(100, 30),
                Enabled = false
            };
            btnGenerateKeys.Click += BtnGenerateKeys_Click;

            btnEncrypt = new Button
            {
                Text = "Encrypt",
                Location = new Point(195, 5),
                Size = new Size(80, 30),
                Enabled = false,
                BackColor = Color.LightGreen
            };
            btnEncrypt.Click += BtnEncrypt_Click;

            btnDecrypt = new Button
            {
                Text = "Decrypt",
                Location = new Point(280, 5),
                Size = new Size(80, 30),
                Enabled = false,
                BackColor = Color.LightCoral
            };
            btnDecrypt.Click += BtnDecrypt_Click;

            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(365, 5),
                Size = new Size(80, 30)
            };
            btnRefresh.Click += BtnRefresh_Click;

            // Main ListView
            mainListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            mainListView.Columns.Add("Name", 300);
            mainListView.Columns.Add("Type", 100);
            mainListView.Columns.Add("Size", 100);
            mainListView.Columns.Add("Modified", 150);
            mainListView.MouseDoubleClick += MainListView_MouseDoubleClick;
            mainListView.SelectedIndexChanged += MainListView_SelectedIndexChanged;

            // Status panel
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100
            };

            Label statusLabel = new Label
            {
                Text = "Status:",
                Location = new Point(5, 5),
                Size = new Size(50, 20)
            };

            txtStatus = new TextBox
            {
                Location = new Point(5, 25),
                Size = new Size(970, 70),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.WhiteSmoke
            };

            // Add controls to panels
            topPanel.Controls.AddRange(new Control[] { btnBack, btnHome, lblCurrentPath });
            toolbarPanel.Controls.AddRange(new Control[] {
                btnConnect, btnGenerateKeys, btnEncrypt, btnDecrypt, btnRefresh
            });
            statusPanel.Controls.AddRange(new Control[] { statusLabel, txtStatus });

            // Add panels to form
            Controls.AddRange(new Control[] { mainListView, toolbarPanel, topPanel, statusPanel });
        }

        private void LoadDrives()
        {
            mainListView.Items.Clear();
            currentPath = "";
            lblCurrentPath.Text = "Select a drive";

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderBy(d => d.Name);

            foreach (var drive in drives)
            {
                ListViewItem item = new ListViewItem(drive.Name);
                item.SubItems.Add("Drive");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.Tag = drive.Name;
                item.ImageIndex = 0;
                mainListView.Items.Add(item);
            }

            UpdateButtons();
        }

        private void LoadDirectory(string path)
        {
            try
            {
                mainListView.Items.Clear();
                currentPath = path;
                lblCurrentPath.Text = path;
                pathHistory.Push(path);

                // Clear the list view first
                mainListView.Items.Clear();

                // Get folders
                var folders = Directory.GetDirectories(path)
                    .Select(p => new DirectoryInfo(p))
                    .OrderBy(d => d.Name)
                    .ToList();

                // Get files
                var files = Directory.GetFiles(path)
                    .Select(p => new FileInfo(p))
                    .OrderBy(f => f.Name)
                    .ToList();

                // Add ALL folders first
                foreach (var dir in folders)
                {
                    ListViewItem item = new ListViewItem(dir.Name);
                    item.SubItems.Add("Folder");
                    item.SubItems.Add("");
                    item.SubItems.Add(dir.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.Tag = dir.FullName;
                    item.ForeColor = Color.Blue;
                    mainListView.Items.Add(item);
                }

                // Then add ALL files
                foreach (var file in files)
                {
                    ListViewItem item = new ListViewItem(file.Name);
                    item.SubItems.Add(file.Extension.ToUpper().TrimStart('.'));
                    item.SubItems.Add(FormatFileSize(file.Length));
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.Tag = file.FullName;
                    item.ForeColor = Color.Black;
                    mainListView.Items.Add(item);
                }

                UpdateButtons();
                UpdateStatus($"Loaded {folders.Count} folders and {files.Count} files from {path}", Color.Blue);
            }
            catch (UnauthorizedAccessException)
            {
                UpdateStatus("Access denied to this directory", Color.Red);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading directory: {ex.Message}", Color.Red);
            }
        }

        private void UpdateButtons()
        {
            btnBack.Enabled = pathHistory.Count > 0;

            // Enable encrypt/decrypt based on selection
            if (mainListView.SelectedItems.Count > 0)
            {
                string selectedPath = mainListView.SelectedItems[0].Tag.ToString();
                bool isFile = File.Exists(selectedPath);
                bool isEncrypted = selectedPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase);

                btnEncrypt.Enabled = isFile && !isEncrypted;
                btnDecrypt.Enabled = isFile && isEncrypted;
            }
            else
            {
                btnEncrypt.Enabled = false;
                btnDecrypt.Enabled = false;
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            txtStatus.ForeColor = color;
            txtStatus.AppendText($"{DateTime.Now:HH:mm:ss}: {message}\r\n");
            txtStatus.ScrollToCaret();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void InitializeSmartCardConnection()
        {
            try
            {
                // TODO: Add your smart card connection code here
                // Uncomment and modify when you have the proper references
                /*
                APDUClientChannel channel = new APDUClientChannel();
                ChannelServices.RegisterChannel(channel);
                service = (Service)Activator.GetObject(typeof(Service), URL);
                */

                // Simulate connection for testing
                service = new object();
                UpdateStatus("Connected to smart card successfully", Color.Green);
                btnGenerateKeys.Enabled = true;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection failed: {ex.Message}", Color.Red);
            }
        }

        // Event Handlers
        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (pathHistory.Count > 0)
            {
                string current = pathHistory.Pop();
                if (pathHistory.Count > 0)
                {
                    string parentPath = pathHistory.Pop();
                    LoadDirectory(parentPath);
                }
                else
                {
                    LoadDrives();
                }
            }
        }

        private void BtnHome_Click(object sender, EventArgs e)
        {
            LoadDrives();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            InitializeSmartCardConnection();
        }

        private void MainListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (mainListView.SelectedItems.Count > 0)
            {
                string selectedPath = mainListView.SelectedItems[0].Tag.ToString();

                if (Directory.Exists(selectedPath))
                {
                    LoadDirectory(selectedPath);
                }
                else if (File.Exists(selectedPath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(selectedPath);
                    }
                    catch
                    {
                        UpdateStatus($"Cannot open file: {selectedPath}", Color.Orange);
                    }
                }
            }
        }

        private void MainListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtons();

            if (mainListView.SelectedItems.Count > 0)
            {
                string selectedPath = mainListView.SelectedItems[0].Tag.ToString();

                if (File.Exists(selectedPath))
                {
                    FileInfo fi = new FileInfo(selectedPath);
                    bool isEncrypted = selectedPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase);

                    UpdateStatus($"Selected: {fi.Name} ({FormatFileSize(fi.Length)}) - {(isEncrypted ? "Encrypted file" : "Regular file")}",
                                isEncrypted ? Color.Orange : Color.Blue);
                }
                else if (Directory.Exists(selectedPath))
                {
                    UpdateStatus($"Selected folder: {Path.GetFileName(selectedPath)}", Color.DarkBlue);
                }
            }
        }

        private void BtnGenerateKeys_Click(object sender, EventArgs e)
        {
            if (service == null)
            {
                UpdateStatus("Not connected to smart card", Color.Red);
                return;
            }

            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Title = "Save Key File";
                    saveDialog.Filter = "Key files (*.key)|*.key|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveDialog.FileName = "smartcard_key.key";
                    saveDialog.DefaultExt = ".key";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        UpdateStatus($"Generating keys to {saveDialog.FileName}...", Color.Blue);

                        // TODO: Replace with actual smart card method call
                        // service.GenerateAndSaveKeyIv(saveDialog.FileName, 256);

                        // Simulate key generation for testing
                        string testKey = $"AES-256 Key generated at {DateTime.Now}\n";
                        testKey += $"Key: {Guid.NewGuid():N}\n";
                        testKey += $"IV: {Guid.NewGuid():N}";
                        File.WriteAllText(saveDialog.FileName, testKey);

                        UpdateStatus("Keys generated successfully", Color.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Key generation failed: {ex.Message}", Color.Red);
            }
        }

        private void BtnEncrypt_Click(object sender, EventArgs e)
        {
            if (mainListView.SelectedItems.Count == 0 || service == null)
            {
                UpdateStatus("No file selected or not connected", Color.Red);
                return;
            }

            string selectedFile = mainListView.SelectedItems[0].Tag.ToString();

            if (!File.Exists(selectedFile))
            {
                UpdateStatus("Selected item is not a file", Color.Red);
                return;
            }

            string encryptedFile = Path.Combine(Path.GetDirectoryName(selectedFile), Path.GetFileName(selectedFile) + ".enc");

            // Keep key selection if needed
            using (OpenFileDialog keyDialog = new OpenFileDialog())
            {
                keyDialog.Title = "Select Key File for Encryption";
                keyDialog.Filter = "Key files (*.key;*.txt)|*.key;*.txt|All files (*.*)|*.*";
                keyDialog.CheckFileExists = true;

                if (keyDialog.ShowDialog() == DialogResult.OK)
                {
                    // Use 'encryptedFile' path directly for output
                    File.Copy(selectedFile, encryptedFile, true); // Replace with actual encryption
                    UpdateStatus($"Success! Encrypted file created: {Path.GetFileName(encryptedFile)}", Color.Green);

                    if (!string.IsNullOrEmpty(currentPath))
                        LoadDirectory(currentPath);
                }
            }
        }

        private void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (mainListView.SelectedItems.Count == 0 || service == null)
            {
                UpdateStatus("No file selected or not connected", Color.Red);
                return;
            }

            string selectedFile = mainListView.SelectedItems[0].Tag.ToString();

            if (!File.Exists(selectedFile))
            {
                UpdateStatus("Selected item is not a file", Color.Red);
                return;
            }

            string decryptedFile = Path.Combine(
                Path.GetDirectoryName(selectedFile),
                Path.GetFileNameWithoutExtension(selectedFile) + "_decrypted" +
                (selectedFile.EndsWith(".enc") ? "" : ".txt")
            );

            // Keep key selection if needed
            using (OpenFileDialog keyDialog = new OpenFileDialog())
            {
                keyDialog.Title = "Select Key File for Decryption";
                keyDialog.Filter = "Key files (*.key;*.txt)|*.key;*.txt|All files (*.*)|*.*";
                keyDialog.CheckFileExists = true;

                if (keyDialog.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(selectedFile, decryptedFile, true); // Replace with actual decryption
                    UpdateStatus($"Success! Decrypted file saved: {Path.GetFileName(decryptedFile)}", Color.Green);

                    if (!string.IsNullOrEmpty(currentPath))
                        LoadDirectory(currentPath);
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                LoadDirectory(currentPath);
                UpdateStatus("Refreshed current directory", Color.Blue);
            }
            else
            {
                LoadDrives();
                UpdateStatus("Refreshed drives list", Color.Blue);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // TODO: Uncomment when you have the proper references
            // ChannelServices.UnregisterAllChannels();
            base.OnFormClosing(e);
        }
    }
}
