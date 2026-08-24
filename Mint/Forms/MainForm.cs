using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using File = System.IO.File;

namespace Mint
{
    public partial class MainForm : Form
    {
        internal AppsStructure _AppsStructure;
        private Point dragStartPoint = Point.Empty;
		
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem)sender;

            if (clickedItem.Tag is Guid idToDelete)
            {
                DeleteAppById(idToDelete);
            }
        }
		
        private string cachePath = Path.Combine(Application.StartupPath, "icon_cache");
        
        private Image GetAppIcon(string filePath, string customIconPath, string appTitle)
        {
            if (!string.IsNullOrEmpty(customIconPath) && File.Exists(customIconPath))
            {
                try
                {
                    string ext = Path.GetExtension(customIconPath).ToLower();
                    // Grab icon directly if pointing to an executable, dll, shortcut, or icon file
                    if (ext == ".exe" || ext == ".dll" || ext == ".lnk" || ext == ".ico")
                    {
                        using (Icon icon = Icon.ExtractAssociatedIcon(customIconPath))
                        {
                            if (icon != null) return icon.ToBitmap();
                        }
                    }
                    else
                    {
                        return Image.FromFile(customIconPath);
                    }
                }
                catch 
                {
                    try { return Image.FromFile(customIconPath); } catch { }
                }
            }
        
            if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
        
            string safeTitle = string.Join("_", appTitle.Split(Path.GetInvalidFileNameChars()));
            string cacheFile = Path.Combine(cachePath, safeTitle + ".png");
        
            if (File.Exists(cacheFile))
            {
                try
                {
                    return Image.FromFile(cacheFile);
                }
                catch { }
            }
        
            try
            {
                if (File.Exists(filePath))
                {
                    using (Icon icon = Icon.ExtractAssociatedIcon(filePath))
                    {
                        if (icon != null)
                        {
                            Bitmap bmp = icon.ToBitmap();
                            bmp.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Png);
                            return bmp;
                        }
                    }
                }
            }
            catch { }
        
            return null;
        }

        readonly string _deleteAppMessage = "Are you sure you want to delete the following app?\n\n";
        readonly string _deleteAllAppsMessage = "Are you sure you want to delete all apps?";

        bool _allowExit = false;

        ToolStripMenuItem _ExitItem;

        public MainForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Options.ApplyTheme(this);
            launcherMenu.Renderer = new MoonMenuRenderer();
            helperMenu.Renderer = new MoonMenuRenderer();

            LoadAppsStructure();
			CleanupOrphanedIcons();
            LoadAppsList();
            BuildExitItem();
            BuildLauncherMenu();

            LoadOptions();
            lblversion.Text += Program.GetCurrentVersionToString();
        }

        private void BuildExitItem()
        {
            _ExitItem = new ToolStripMenuItem();
            _ExitItem.ForeColor = Color.GhostWhite;
            _ExitItem.Font = new Font("Segoe UI Semibold", 10f);
            _ExitItem.Text = "Exit";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
		
        private void DeleteAppById(Guid appId)
        {
            var appToRemove = _AppsStructure.Apps.FirstOrDefault(x => x.Id == appId);
            
            if (appToRemove != null)
            {
                string safeTitle = string.Join("_", appToRemove.AppTitle.Split(Path.GetInvalidFileNameChars()));
                string cacheFile = Path.Combine(cachePath, safeTitle + ".png");
        
                try 
                {
                    if (File.Exists(cacheFile)) 
                    {
                        File.Delete(cacheFile);
                    }
                } 
                catch {  }
        
                _AppsStructure.Apps.Remove(appToRemove);
                
                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();
            }
        }

        private void LoadAppsStructure()
        {
            if (System.IO.File.Exists(Options.AppsStructureFile))
            {
                _AppsStructure = JsonConvert.DeserializeObject<AppsStructure>(System.IO.File.ReadAllText(Options.AppsStructureFile));
            }
            else
            {
                _AppsStructure = new AppsStructure();
                _AppsStructure.Apps = new List<App>();
                _AppsStructure.Groups = new List<string>();

                using (FileStream fs = System.IO.File.Open(Options.AppsStructureFile, FileMode.CreateNew))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;

                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(jw, _AppsStructure);
                }
            }
        }

        public void SaveAppsStructure()
        {
            File.WriteAllText(Options.AppsStructureFile, string.Empty);

            using (FileStream fs = System.IO.File.Open(Options.AppsStructureFile, FileMode.OpenOrCreate))
            using (StreamWriter sw = new StreamWriter(fs))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;

                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(jw, _AppsStructure);
            }
        }

        public void LoadAppsList()
        {
            listApps.Items.Clear();
            groupBox.Items.Clear();
        
            if (_AppsStructure != null)
            {
                if (_AppsStructure.Groups != null) 
                {
                    groupBox.Items.AddRange(_AppsStructure.Groups.ToArray());
                }
        
                if (_AppsStructure.Apps != null)
                {
                    foreach (App x in _AppsStructure.Apps)
                    {
                        listApps.Items.Add(x.AppTitle);
                    }
                }
                
                label3.Text = string.Format("Apps ({0})", _AppsStructure.Apps.Count);
            }
        }

        private void LoadOptions()
        {
            switch (Options.CurrentOptions.Theme)
            {
                case Theme.Amber:
                    radioCaramel.Checked = true;
                    break;
                case Theme.Jade:
                    radioLime.Checked = true;
                    break;
                case Theme.Ruby:
                    radioMagma.Checked = true;
                    break;
                case Theme.Silver:
                    radioMinimal.Checked = true;
                    break;
                case Theme.Azurite:
                    radioOcean.Checked = true;
                    break;
                case Theme.Amethyst:
                    radioZerg.Checked = true;
                    break;
            }

            checkAutoStart.Checked = Options.CurrentOptions.AutoStart;
        }

        public void BuildLauncherMenu()
        {
            launcherMenu.Items.Clear();

            if (_AppsStructure.Apps != null)
            {
                ToolStripMenuItem i;
                ToolStripMenuItem subItem;

                if (_AppsStructure.Groups != null)
                {
                    foreach (string group in _AppsStructure.Groups)
                    {
                        if (_AppsStructure.Apps.Find(a => a.AppGroup == group) == null) continue;

                        i = new ToolStripMenuItem(group, null);
                        i.Name = $"gi_{group}";
                        i.ForeColor = Color.GhostWhite;
                        i.Tag = "GroupItem";
                        launcherMenu.Items.Add(i);
                    }

                    if (_AppsStructure.Groups.Count > 0) launcherMenu.Items.Add("-");
                }

                foreach (App x in _AppsStructure.Apps)
                {
                    bool isDeadItem = false;
                    if (!string.IsNullOrEmpty(x.AppLink))
                    {
                        bool isLocalPath = Path.IsPathRooted(x.AppLink) || x.AppLink.Contains(":\\") || x.AppLink.Contains("\\\\");
                        if (isLocalPath && !File.Exists(x.AppLink) && !Directory.Exists(x.AppLink))
                        {
                            isDeadItem = true;
                        }
                    }
                    
                    Image appIcon = !isDeadItem ? GetAppIcon(x.AppLink, x.CustomIconPath, x.AppTitle) : null;
                
                    if (!string.IsNullOrEmpty(x.AppGroup))
                    {
                        subItem = new ToolStripMenuItem(x.AppTitle, appIcon);
                        subItem.Click += subItem_Click;
                        
                        if (!isDeadItem)
                        {
                            subItem.ForeColor = Color.GhostWhite;
                        }
                        else
                        {
                            subItem.ForeColor = Color.DimGray;
                            subItem.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Strikeout);
                        }
                
                        ((ToolStripMenuItem)(launcherMenu.Items[$"gi_{x.AppGroup}"])).DropDownItems.Add(subItem);
                    }
                    else
                    {
                        i = new ToolStripMenuItem(x.AppTitle, appIcon);
                        
                        if (!isDeadItem)
                        {
                            i.ForeColor = Color.GhostWhite;
                        }
                        else
                        {
                            i.ForeColor = Color.DimGray;
                            i.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Strikeout);
                        }
                
                        launcherMenu.Items.Add(i);
                    }
                }
            }

            launcherMenu.Items.Add("-");
            launcherMenu.Items.Add(_ExitItem);
        }

        private void subItem_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var app = _AppsStructure.Apps.FirstOrDefault(x => x.AppTitle == item.Text);
        
            if (app != null)
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = app.AppLink,
                        Arguments = app.AppParams,
                        UseShellExecute = true 
                    };
                    if (!string.IsNullOrEmpty(app.AppLink) && (File.Exists(app.AppLink) || Directory.Exists(app.AppLink)))
                    {
                        startInfo.WorkingDirectory = Path.GetDirectoryName(app.AppLink);
                    }
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddApp()
        {
            if (!string.IsNullOrEmpty(txtAppLink.Text) && !string.IsNullOrEmpty(txtAppTitle.Text))
            {
                if (_AppsStructure.Apps.Find(x => x.AppTitle == txtAppTitle.Text) != null)
                {
                    MessageBox.Show("An app with this title already exists!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
    
                App app = new App();
                app.Id = Guid.NewGuid();
                app.AppLink = txtAppLink.Text;
                app.AppTitle = txtAppTitle.Text;
                app.AppParams = txtParams.Text;
                app.AppGroup = groupBox.Text;
                app.CustomIconPath = txtCustomIcon.Text;
    
                _AppsStructure.Apps.Add(app);
                SaveAppsStructure();
    
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();
    
                txtAppLink.Clear();
                txtAppTitle.Clear();
                txtParams.Clear();
                txtCustomIcon.Clear();
            }
            else
            {
                MessageBox.Show("Please fill both title & location!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteAppItem(string appTitle, int appIndex)
        {
            if (appIndex < 0 || appIndex >= _AppsStructure.Apps.Count) return;
        
            if (MessageBox.Show(_deleteAppMessage + appTitle, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _AppsStructure.Apps.RemoveAt(appIndex);
        
                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();
            }
        }

        private void DeleteAllAppItems()
        {
            if (MessageBox.Show(_deleteAllAppsMessage, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _AppsStructure.Apps.Clear();

                SaveAppsStructure();
                LoadAppsStructure();

                LoadAppsList();
                BuildLauncherMenu();
            }
        }
		
        private void CleanupOrphanedIcons()
        {
            try
            {
                if (!Directory.Exists(cachePath)) return;
        
                // Get all files in the cache
                string[] cachedFiles = Directory.GetFiles(cachePath, "*.png");
        
                // Create a list of titles that SHOULD exist
                var validFilenames = _AppsStructure.Apps
                    .Select(app => string.Join("_", app.AppTitle.Split(Path.GetInvalidFileNameChars())) + ".png")
                    .ToList();
        
                // Delete anything that isn't in our valid list
                foreach (string filePath in cachedFiles)
                {
                    if (!validFilenames.Contains(Path.GetFileName(filePath)))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch { /* Locked files will be caught on the next launch */ }
        }

        private void LaunchApp(string app)
        {
            try
            {
                App appX = _AppsStructure.Apps.Find(x => x.AppTitle == app);

                if (appX == null) return;

                Process p = new Process();
                if (!string.IsNullOrEmpty(appX.AppLink) && (File.Exists(appX.AppLink) || Directory.Exists(appX.AppLink)))
                {
                    p.StartInfo.WorkingDirectory = Path.GetDirectoryName(appX.AppLink);
                }
                p.StartInfo.Arguments = appX.AppParams;
                p.StartInfo.FileName = appX.AppLink;
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void checkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.AutoStart = checkAutoStart.Checked;
            Utilities.RegisterAutoStart(!Options.CurrentOptions.AutoStart);
        }

        private void radioOcean_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Azurite;
            Options.ApplyTheme(this);
        }

        private void radioMagma_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Ruby;
            Options.ApplyTheme(this);
        }

        private void radioZerg_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Amethyst;
            Options.ApplyTheme(this);
        }

        private void radioCaramel_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Amber;
            Options.ApplyTheme(this);
        }

        private void radioLime_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Jade;
            Options.ApplyTheme(this);
        }

        private void radioMinimal_CheckedChanged(object sender, EventArgs e)
        {
            Options.CurrentOptions.Theme = Theme.Silver;
            Options.ApplyTheme(this);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_allowExit)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                SaveAppsStructure();
                Options.SaveSettings();
            }
        }

        private void launcherMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Text == "Exit")
            {
                _allowExit = true;

                SaveAppsStructure();
                Options.SaveSettings();

                Application.Exit();
            }
            else
            {
                _allowExit = false;
                LaunchApp(e.ClickedItem.Text);
            }
        }

        private void launcherIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized || !this.Visible)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
                this.Focus();
            }
            else
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void LoadFile(string file)
        {
            if (file.EndsWith(".lnk"))
            {
                WshShell shell = new WshShell();
                IWshShortcut link = (IWshShortcut)shell.CreateShortcut(file);

                txtAppLink.Text = link.TargetPath;
                txtAppTitle.Text = Path.GetFileNameWithoutExtension(file);
                txtParams.Text = link.Arguments;
            }
            else
            {
                txtAppLink.Text = file;
                txtAppTitle.Text = Path.GetFileNameWithoutExtension(file);
            }
        }

        private void btnLocate_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "Mint | Select an application or file...";
            dialog.Filter = "All Files (*.*)|*.*|Applications (*.exe;*.lnk)|*.exe;*.lnk";

            if (dialog.ShowDialog() == DialogResult.OK) LoadFile(dialog.FileName);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddApp();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            AboutForm f = new AboutForm();
            f.ShowDialog(this);
        }

        private void SortByAZ()
        {
            if (MessageBox.Show("Are you sure you want to sort all apps alphabetically from A to Z?", "Confirm Sort", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _AppsStructure.Apps = _AppsStructure.Apps.OrderBy(x => x.AppTitle).ToList();

                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();
            }
        }

        private void listApps_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listApps.SelectedIndex > -1)
            {
                ModifyForm f = new ModifyForm(listApps.SelectedIndex, this);
                f.ShowDialog();

                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();
            }
        }

        private void btnGroups_Click(object sender, EventArgs e)
        {
            GroupsForm gf = new GroupsForm(_AppsStructure);
            gf.ShowDialog(this);

            groupBox.Items.Clear();
            if (_AppsStructure.Groups != null) groupBox.Items.AddRange(_AppsStructure.Groups.ToArray());
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            try
            {
                LoadFile(files[0]);
            }
            catch { }
        }

        private void listApps_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listApps.IndexFromPoint(e.Location) != ListBox.NoMatches)
                {
                    listApps.SelectedIndex = listApps.IndexFromPoint(e.Location);
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                int index = listApps.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    listApps.SelectedIndex = index;
                    dragStartPoint = e.Location; // Capture starting coordinates for mouse dragging
                }
                else
                {
                    dragStartPoint = Point.Empty;
                }
            }
        }

        private void listApps_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragStartPoint != Point.Empty)
            {
                // Measure if drag passes safety threshold to allow normal double click triggers
                if (Math.Abs(e.X - dragStartPoint.X) > SystemInformation.DragSize.Width ||
                    Math.Abs(e.Y - dragStartPoint.Y) > SystemInformation.DragSize.Height)
                {
                    if (listApps.SelectedIndex != -1)
                    {
                        listApps.DoDragDrop(listApps.SelectedItem, DragDropEffects.Move);
                        dragStartPoint = Point.Empty;
                    }
                }
            }
        }

        private void sortByAZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortByAZ();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listApps.SelectedIndex > -1)
            {
                DeleteAppItem(listApps.SelectedItem.ToString(), listApps.SelectedIndex);
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listApps.SelectedIndex > -1)
            {
                int i = listApps.SelectedIndex;
                ModifyForm f = new ModifyForm(listApps.SelectedIndex, this);
                f.ShowDialog(this);

                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();

                listApps.SelectedIndex = i;
            }
        }

        private void deleteAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listApps.Items.Count > 0)
            {
                DeleteAllAppItems();
            }
        }

        private void locateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listApps.SelectedIndex > -1)
            {
                App file = _AppsStructure.Apps.Find(x => x.AppTitle == listApps.SelectedItem.ToString());
                if (file != null)
                {
                    if (File.Exists(file.AppLink)) Process.Start("explorer.exe", "/select, " + file.AppLink);
                } 
            }
        }

        private void btnLocateIcon_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Mint | Select a custom icon...";
                dialog.Filter = "Icon Sources (*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll;*.lnk)|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll;*.lnk|Images (*.ico;*.png;*.jpg;*.bmp)|*.ico;*.png;*.jpg;*.bmp|Programs & Shortcuts (*.exe;*.dll;*.lnk)|*.exe;*.dll;*.lnk|All Files (*.*)|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtCustomIcon.Text = dialog.FileName;
                }
            }
        }

        private void listApps_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void listApps_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                try
                {
                    LoadFile(files[0]);
                }
                catch { }
            }
            else
            {
                Point point = listApps.PointToClient(new Point(e.X, e.Y));
                int targetIndex = listApps.IndexFromPoint(point);
                int sourceIndex = listApps.SelectedIndex;

                if (targetIndex != ListBox.NoMatches && targetIndex != sourceIndex && sourceIndex >= 0)
                {
                    var app = _AppsStructure.Apps[sourceIndex];
                    _AppsStructure.Apps.RemoveAt(sourceIndex);
                    _AppsStructure.Apps.Insert(targetIndex, app);

                    SaveAppsStructure();
                    LoadAppsStructure();
                    LoadAppsList();
                    BuildLauncherMenu();

                    listApps.SelectedIndex = targetIndex;
                }
            }
        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = listApps.SelectedIndex;
            if (index > 0)
            {
                var app = _AppsStructure.Apps[index];
                _AppsStructure.Apps.RemoveAt(index);
                _AppsStructure.Apps.Insert(index - 1, app);

                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();

                listApps.SelectedIndex = index - 1;
            }
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = listApps.SelectedIndex;
            if (index >= 0 && index < _AppsStructure.Apps.Count - 1)
            {
                var app = _AppsStructure.Apps[index];
                _AppsStructure.Apps.RemoveAt(index);
                _AppsStructure.Apps.Insert(index + 1, app);

                SaveAppsStructure();
                LoadAppsStructure();
                LoadAppsList();
                BuildLauncherMenu();

                listApps.SelectedIndex = index + 1;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtAppLink.Clear();
            txtAppTitle.Clear();
            txtParams.Clear();
            txtCustomIcon.Clear();
            try { groupBox.SelectedIndex = -1; } catch { }
        }
    }
}
