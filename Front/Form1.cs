using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using SmartCard.Runtime.Remoting.Channels.APDU;
using Cipher.OnCardApp;
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
        private TextBox txtKeyName;
        private Label labelKeyName;
        private Button btnGenerateKeys;
        private ListBox listKeys;
        private Label labelAvailableKeys;
        private Button btnRefresh;
        private Label lblCurrentPath;
        private TextBox txtStatus;

        // Navigation
        private Stack<string> pathHistory = new Stack<string>();
        private string currentPath;

        // Smart card service
        private Service service;
        private const string URL = "apdu://selfdiscover/gemalto_dotnet_cipher.uri";

        // List of keys on card
        private string[] availableKeys = new string[0];

        public Form1(Service authenticatedService)
        {
            service = authenticatedService;
            InitializeComponents();
            LoadDrives();
        }

        private void InitializeComponents()
        {
            this.Text = "Smart Card Encryption App";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create main split container for left-right layout
            SplitContainer mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 800 // ~67% for file list, ~33% for keys
            };

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

            // Main ListView (LEFT PANEL - File Browser)
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

            // Key Panel (RIGHT PANEL - Key Management)
            Panel keyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke
            };

            // Key Name Input Section
            GroupBox keyNameGroup = new GroupBox
            {
                Text = "Key Name for Generation",
                Location = new Point(10, 10),
                Size = new Size(370, 80),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            labelKeyName = new Label
            {
                Text = "Key Name:",
                Location = new Point(10, 25),
                Size = new Size(80, 20)
            };

            txtKeyName = new TextBox
            {
                Location = new Point(95, 22),
                Size = new Size(260, 20),
                Text = "my_key"
            };

            keyNameGroup.Controls.AddRange(new Control[] { labelKeyName, txtKeyName });

            // Available Keys List Section
            GroupBox keysListGroup = new GroupBox
            {
                Text = "Available Keys on Smart Card",
                Location = new Point(10, 100),
                Size = new Size(370, 450),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            labelAvailableKeys = new Label
            {
                Text = "Select a key for operations:",
                Location = new Point(10, 25),
                Size = new Size(200, 20)
            };

            listKeys = new ListBox
            {
                Location = new Point(10, 50),
                Size = new Size(350, 390),
                SelectionMode = SelectionMode.One,
                Font = new Font("Consolas", 9)
            };
            listKeys.SelectedIndexChanged += LstKeys_SelectedIndexChanged;

            keysListGroup.Controls.AddRange(new Control[] {
                labelAvailableKeys, listKeys
            });

            keyPanel.Controls.AddRange(new Control[] { keyNameGroup, keysListGroup });

            // Add panels to split container
            mainSplitContainer.Panel1.Controls.Add(mainListView);  // Left: File browser
            mainSplitContainer.Panel2.Controls.Add(keyPanel);      // Right: Key panel

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
                Size = new Size(1170, 70),
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

            // Add panels to form in correct docking order (bottom to top)
            Controls.AddRange(new Control[] {
                mainSplitContainer,  // Main content
                statusPanel,         // Status at bottom
                toolbarPanel,        // Toolbar above status
                topPanel            // Top navigation
            });
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
            bool isConnected = service != null; //check connection before anything
            btnGenerateKeys.Enabled = isConnected;

            btnBack.Enabled = pathHistory.Count > 0;

            // Enable encrypt/decrypt based on selection
            if (mainListView.SelectedItems.Count > 0 && isConnected)
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
                APDUClientChannel channel = new APDUClientChannel();
                ChannelServices.RegisterChannel(channel);
                service = (Service)Activator.GetObject(typeof(Service), URL);

                //string[] serviceList = service.GetServices(); // Test call to verify connection
                //bool success = serviceList != null && serviceList.Length > 0 &&
                //    serviceList.Contains("gemalto_dotnet_cipher.uri");
                //if (success)
                    UpdateStatus("Connected to smart card successfully", Color.Green);

                btnGenerateKeys.Enabled = true;
                LoadAvailableKeys();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection failed: {ex.Message}", Color.Red);
            }
        }

        private void LoadAvailableKeys()
        {
            try
            {
                if (service == null) return;

                // Get keys from smart card
                availableKeys = service.GetKeys();

                listKeys.Items.Clear();
                foreach (var key in availableKeys)
                {
                    listKeys.Items.Add($"{key}");
                }

                if (listKeys.Items.Count > 0)
                {
                    listKeys.SelectedIndex = 0;
                }

                UpdateStatus($"Loaded {availableKeys.Length} keys from smart card", Color.Blue);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to load keys: {ex.Message}", Color.Red);
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
        
        private void LstKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listKeys.SelectedIndex >= 0)
            {
                string selectedKey = listKeys.SelectedItem.ToString();
                UpdateStatus($"Selected key: {selectedKey}", Color.DarkBlue);
            }
        }

        private void BtnGenerateKeys_Click(object sender, EventArgs e)
        {
            if (service == null)
            {
                UpdateStatus("Not connected to smart card", Color.Red);
                return;
            }

            string keyName = txtKeyName.Text.Trim();
            if (string.IsNullOrEmpty(keyName))
            {
                UpdateStatus("Please enter a key name", Color.Orange);
                return;
            }

            try
            {
                UpdateStatus($"Generating key '{keyName}'...", Color.Blue);
                service.GenerateAndSaveKeyIvByFileName(keyName, 256);

                UpdateStatus($"Key '{keyName}' generated and stored on smart card", Color.Green);
                LoadAvailableKeys();
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
            string keyName = listKeys.SelectedItem.ToString();

            if (!File.Exists(selectedFile))
            {
                UpdateStatus("Selected item is not a file", Color.Red);
                return;
            }

            string encryptedFile = Path.Combine(Path.GetDirectoryName(selectedFile), Path.GetFileName(selectedFile) + ".enc");

            try
            {
                FileInfo fileInfo = new FileInfo(selectedFile);
                long fileSize = fileInfo.Length;

                UpdateStatus($"Encrypting '{Path.GetFileName(selectedFile)}' ({FormatFileSize(fileSize)}) with key '{keyName}'...", Color.Blue);

                // Start encryption session on smart card
                service.StartEncryption(keyName);

                // Use smaller chunks for smart card
                const int CHUNK_SIZE = 256;
                byte[] buffer = new byte[CHUNK_SIZE];
                long totalBytesProcessed = 0;

                using (FileStream fs = new FileStream(selectedFile, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] chunk = new byte[bytesRead];
                        Array.Copy(buffer, chunk, bytesRead);

                        bool isFinal = (fs.Position == fs.Length);
                        byte[] result = service.ProcessEncryptionChunk(chunk, isFinal);

                        totalBytesProcessed += bytesRead;

                        // Update progress
                        int percent = (int)((totalBytesProcessed * 100) / fileSize);
                        UpdateStatus($"Encrypting... {percent}% ({FormatFileSize(totalBytesProcessed)}/{FormatFileSize(fileSize)})", Color.Blue);

                        // For final chunk, we get the encrypted data
                        if (isFinal && result.Length > 0)
                        {
                            // Write the encrypted bytes to file
                            File.WriteAllBytes(encryptedFile, result);

                            UpdateStatus($"Success! Encrypted file created: {Path.GetFileName(encryptedFile)}", Color.Green);

                            if (!string.IsNullOrEmpty(currentPath))
                                LoadDirectory(currentPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Encryption failed: {ex.Message}", Color.Red);

                // Try to cancel the encryption session
                try { service.CancelEncryption(); } catch { }

                // Clean up partial file
                try { if (File.Exists(encryptedFile)) File.Delete(encryptedFile); } catch { }
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
            string keyName = listKeys.SelectedItem.ToString();

            if (!File.Exists(selectedFile))
            {
                UpdateStatus("Selected item is not a file", Color.Red);
                return;
            }

            string fileNameWithoutEnc = Path.GetFileNameWithoutExtension(selectedFile);
            string extension = Path.GetExtension(fileNameWithoutEnc);

            string decryptedFile = Path.Combine(
                Path.GetDirectoryName(selectedFile),
                Path.GetFileNameWithoutExtension(fileNameWithoutEnc) + "_decrypted" +
                extension
            );

            try
            {
                FileInfo fileInfo = new FileInfo(selectedFile);
                long fileSize = fileInfo.Length;

                UpdateStatus($"Decrypting '{Path.GetFileName(selectedFile)}' ({FormatFileSize(fileSize)}) with key '{keyName}'...", Color.Blue);

                // Start decryption session on smart card
                service.StartDecryption(keyName);

                // Use smaller chunks for smart card communication
                const int CHUNK_SIZE = 256; // Same as encryption
                byte[] buffer = new byte[CHUNK_SIZE];
                long totalBytesProcessed = 0;

                using (FileStream fs = new FileStream(selectedFile, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] chunk = new byte[bytesRead];
                        Array.Copy(buffer, chunk, bytesRead);

                        bool isFinal = (fs.Position == fs.Length);
                        byte[] result = service.ProcessDecryptionChunk(chunk, isFinal);

                        totalBytesProcessed += bytesRead;

                        // Update progress
                        int percent = (int)((totalBytesProcessed * 100) / fileSize);
                        UpdateStatus($"Decrypting... {percent}% ({FormatFileSize(totalBytesProcessed)}/{FormatFileSize(fileSize)})", Color.Blue);

                        // For final chunk, we get the decrypted data
                        if (isFinal && result.Length > 0)
                        {
                            // Write the decrypted bytes to file
                            File.WriteAllBytes(decryptedFile, result);

                            UpdateStatus($"Success! Decrypted file saved: {Path.GetFileName(decryptedFile)}", Color.Green);

                            if (!string.IsNullOrEmpty(currentPath))
                                LoadDirectory(currentPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Decryption failed: {ex.Message}", Color.Red);

                // Try to cancel the decryption session
                try { service.CancelDecryption(); } catch { }

                // Clean up partial file
                try { if (File.Exists(decryptedFile)) File.Delete(decryptedFile); } catch { }
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
            ChannelServices.UnregisterChannel(
                ChannelServices.RegisteredChannels
                .FirstOrDefault(c => c is APDUClientChannel)
            );
            base.OnFormClosing(e);
        }
    }
}
