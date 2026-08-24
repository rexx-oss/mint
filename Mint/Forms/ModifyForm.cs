using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Windows.Forms;

namespace Mint
{
    public partial class ModifyForm : Form
    {
        int _appIndex;
        MainForm _main;

        // Custom UI components defined dynamically to bypass designer limits
        private Label lblCustomIcon;
        private TextBox txtCustomIcon;
        private Button btnLocateIcon;

        public ModifyForm(int appIndex, MainForm main)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            _appIndex = appIndex;
            _main = main;

            InitializeCustomIconControls();

            Options.ApplyTheme(this);

            if (_appIndex > -1)
            {
                txtAppTitle.Text = _main._AppsStructure.Apps[_appIndex].AppTitle;
                txtParams.Text = _main._AppsStructure.Apps[_appIndex].AppParams;
                txtLink.Text = _main._AppsStructure.Apps[_appIndex].AppLink;
                txtCustomIcon.Text = _main._AppsStructure.Apps[_appIndex].CustomIconPath;

                if (_main._AppsStructure.Groups != null)
                {
                    groupBox.Items.AddRange(_main._AppsStructure.Groups.ToArray());
                    groupBox.Text = _main._AppsStructure.Apps[_appIndex].AppGroup;
                }
            }
        }

        private void InitializeCustomIconControls()
        {
            lblCustomIcon = new Label();
            lblCustomIcon.AutoSize = true;
            lblCustomIcon.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            lblCustomIcon.ForeColor = System.Drawing.Color.Silver;
            lblCustomIcon.Text = "Custom Icon (optional):";

            txtCustomIcon = new TextBox();
            txtCustomIcon.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            txtCustomIcon.BorderStyle = BorderStyle.FixedSingle;
            txtCustomIcon.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            txtCustomIcon.ForeColor = System.Drawing.Color.White;
            txtCustomIcon.Size = new System.Drawing.Size(txtLink.Width - 36, txtLink.Height);

            btnLocateIcon = new Button();
            btnLocateIcon.BackColor = System.Drawing.Color.DodgerBlue;
            btnLocateIcon.FlatStyle = FlatStyle.Flat;
            btnLocateIcon.FlatAppearance.BorderSize = 0;
            btnLocateIcon.ForeColor = System.Drawing.Color.White;
            btnLocateIcon.Size = new System.Drawing.Size(31, txtLink.Height);
            btnLocateIcon.Text = "...";
            btnLocateIcon.Click += btnLocateIcon_Click;
            btnLocateIcon.Tag = "themeable";

            // Save the original anchor styles to prevent WinForms automatic push-down
            AnchorStyles saveAnchor = btnSave.Anchor;
            AnchorStyles cancelAnchor = btnCancel.Anchor;

            // Temporarily switch anchors to Top-Left
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // 75 pixels is the perfect vertical space required for the new icon row
            int verticalOffset = 75; 
            this.Height += verticalOffset;

            // Manually shift buttons down with anchoring disabled
            btnSave.Top += verticalOffset;
            btnCancel.Top += verticalOffset;

            // Restore the original designer anchors
            btnSave.Anchor = saveAnchor;
            btnCancel.Anchor = cancelAnchor;

            // Position elements cleanly below txtParams
            lblCustomIcon.Location = new System.Drawing.Point(txtParams.Left, txtParams.Bottom + 10);
            txtCustomIcon.Location = new System.Drawing.Point(txtParams.Left, lblCustomIcon.Bottom + 5);
            btnLocateIcon.Location = new System.Drawing.Point(txtCustomIcon.Right + 5, txtCustomIcon.Top);

            this.Controls.Add(lblCustomIcon);
            this.Controls.Add(txtCustomIcon);
            this.Controls.Add(btnLocateIcon);
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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ModifyForm_Load(object sender, EventArgs e)
        {

        }

        private void ModifyAppEntry()
        {
            if (!string.IsNullOrEmpty(txtAppTitle.Text))
            {
                _main._AppsStructure.Apps[_appIndex].AppTitle = txtAppTitle.Text;
                _main._AppsStructure.Apps[_appIndex].AppParams = txtParams.Text;
                _main._AppsStructure.Apps[_appIndex].AppLink = txtLink.Text;
                _main._AppsStructure.Apps[_appIndex].AppGroup = groupBox.Text;
                _main._AppsStructure.Apps[_appIndex].CustomIconPath = txtCustomIcon.Text;

                this.Close();
            }
            else
            {
                MessageBox.Show("Please fill both app title & location!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ModifyAppEntry();
        }

        private void btnLocate_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "Mint | Select an application...";
            dialog.Filter = "Applications | *.exe; *.lnk";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.FileName.EndsWith(".lnk"))
                {
                    WshShell shell = new WshShell();
                    IWshShortcut link = (IWshShortcut)shell.CreateShortcut(dialog.FileName);

                    txtLink.Text = link.TargetPath;
                    txtAppTitle.Text = Path.GetFileNameWithoutExtension(dialog.FileName).Replace(".exe", string.Empty);
                    txtParams.Text = link.Arguments;
                }
                else
                {
                    txtLink.Text = dialog.FileName;
                    if (string.IsNullOrEmpty(txtAppTitle.Text)) txtAppTitle.Text = dialog.SafeFileName.Replace(".exe", string.Empty);
                }
            }
        }
    }
}
