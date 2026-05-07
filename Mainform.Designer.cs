namespace SoulsModInstaller
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            Button_Install = new Button();
            FileDialog_Browse = new OpenFileDialog();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            b_editBonfireWarps = new Button();
            b_openBackups = new Button();
            Button_Browse = new Button();
            toolTip1 = new ToolTip(components);
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // Button_Install
            // 
            Button_Install.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Button_Install.Font = new Font("Segoe UI", 9F);
            Button_Install.Location = new Point(345, 163);
            Button_Install.Name = "Button_Install";
            Button_Install.Size = new Size(86, 24);
            Button_Install.TabIndex = 83;
            Button_Install.Text = "Install";
            Button_Install.UseVisualStyleBackColor = true;
            Button_Install.Click += Button_Install_Click;
            // 
            // FileDialog_Browse
            // 
            FileDialog_Browse.Filter = "Dark Souls executable|DarkSoulsRemastered.exe; DARKSOULS.exe|All Files|*.*";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(445, 223);
            tabControl1.TabIndex = 89;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(b_editBonfireWarps);
            tabPage1.Controls.Add(b_openBackups);
            tabPage1.Controls.Add(Button_Install);
            tabPage1.Controls.Add(Button_Browse);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(437, 195);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Installer";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // b_editBonfireWarps
            // 
            b_editBonfireWarps.AllowDrop = true;
            b_editBonfireWarps.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            b_editBonfireWarps.Font = new Font("Segoe UI", 9F);
            b_editBonfireWarps.Location = new Point(8, 163);
            b_editBonfireWarps.Name = "b_editBonfireWarps";
            b_editBonfireWarps.Size = new Size(127, 24);
            b_editBonfireWarps.TabIndex = 89;
            b_editBonfireWarps.Text = "Edit BonfireWarps.txt";
            b_editBonfireWarps.UseVisualStyleBackColor = true;
            b_editBonfireWarps.Click += b_editBonfireWarps_Click;
            // 
            // b_openBackups
            // 
            b_openBackups.AllowDrop = true;
            b_openBackups.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            b_openBackups.Font = new Font("Segoe UI", 9F);
            b_openBackups.Location = new Point(8, 133);
            b_openBackups.Name = "b_openBackups";
            b_openBackups.Size = new Size(127, 24);
            b_openBackups.TabIndex = 88;
            b_openBackups.Text = "Open Backups Folder";
            b_openBackups.UseVisualStyleBackColor = true;
            b_openBackups.Click += b_openBackups_Click;
            // 
            // Button_Browse
            // 
            Button_Browse.AllowDrop = true;
            Button_Browse.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Button_Browse.Font = new Font("Segoe UI", 9F);
            Button_Browse.Location = new Point(345, 133);
            Button_Browse.Name = "Button_Browse";
            Button_Browse.Size = new Size(86, 24);
            Button_Browse.TabIndex = 87;
            Button_Browse.Text = "Browse";
            Button_Browse.UseVisualStyleBackColor = true;
            Button_Browse.Click += Button_Browse_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(445, 223);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            Load += MainForm_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion
        private Button Button_Install;
        private OpenFileDialog FileDialog_Browse;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Button Button_Browse;
        private ToolTip toolTip1;
        private Button b_openBackups;
        private Button b_editBonfireWarps;
    }
}