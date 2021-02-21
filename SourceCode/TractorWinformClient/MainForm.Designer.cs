namespace Duan.Xiugang.Tractor
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.GameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RebootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.AboutMeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.lblSouth = new System.Windows.Forms.Label();
            this.lblEast = new System.Windows.Forms.Label();
            this.lblNorth = new System.Windows.Forms.Label();
            this.lblWest = new System.Windows.Forms.Label();
            this.lblSouthNickName = new System.Windows.Forms.Label();
            this.lblEastNickName = new System.Windows.Forms.Label();
            this.lblNorthNickName = new System.Windows.Forms.Label();
            this.lblWestNickName = new System.Windows.Forms.Label();
            this.btnReady = new System.Windows.Forms.Button();
            this.lblSouthStarter = new System.Windows.Forms.Label();
            this.lblWestStarter = new System.Windows.Forms.Label();
            this.lblNorthStarter = new System.Windows.Forms.Label();
            this.lblEastStarter = new System.Windows.Forms.Label();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GameToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Padding = new System.Windows.Forms.Padding(9, 3, 0, 3);
            this.menuStrip.Size = new System.Drawing.Size(944, 35);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip1";
            // 
            // GameToolStripMenuItem
            // 
            this.GameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SettingToolStripMenuItem,
            this.StartToolStripMenuItem,
            this.RebootToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.GameToolStripMenuItem.Name = "GameToolStripMenuItem";
            this.GameToolStripMenuItem.Size = new System.Drawing.Size(62, 29);
            this.GameToolStripMenuItem.Text = "游戏";
            // 
            // SettingToolStripMenuItem
            // 
            this.SettingToolStripMenuItem.Name = "SettingToolStripMenuItem";
            this.SettingToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.SettingToolStripMenuItem.Text = "设置";
            this.SettingToolStripMenuItem.Click += new System.EventHandler(this.SettingToolStripMenuItem_Click);
            // 
            // StartToolStripMenuItem
            // 
            this.StartToolStripMenuItem.Image = global::Duan.Xiugang.Tractor.Properties.Resources.MenuStart;
            this.StartToolStripMenuItem.Name = "StartToolStripMenuItem";
            this.StartToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.StartToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.StartToolStripMenuItem.Text = "加入房间";
            this.StartToolStripMenuItem.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // RebootToolStripMenuItem
            // 
            this.RebootToolStripMenuItem.Name = "RebootToolStripMenuItem";
            this.RebootToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.RebootToolStripMenuItem.Text = "重启游戏";
            this.RebootToolStripMenuItem.Click += new System.EventHandler(this.RebootToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.ExitToolStripMenuItem.Text = "退出";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator5,
            this.AboutMeToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(62, 29);
            this.HelpToolStripMenuItem.Text = "帮助";
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(69, 6);
            // 
            // AboutMeToolStripMenuItem
            // 
            this.AboutMeToolStripMenuItem.Name = "AboutMeToolStripMenuItem";
            this.AboutMeToolStripMenuItem.Size = new System.Drawing.Size(72, 22);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "png图片|*.PNG|jpg图片|*.JPG|bmp图片|*.BMP|gif图片|*.GIF|所有文件|*.*";
            this.openFileDialog.Title = "选择背景图片";
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "拖拉机大战";
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // lblSouth
            // 
            this.lblSouth.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblSouth.AutoSize = true;
            this.lblSouth.BackColor = System.Drawing.Color.Transparent;
            this.lblSouth.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSouth.ForeColor = System.Drawing.SystemColors.Control;
            this.lblSouth.Location = new System.Drawing.Point(402, 754);
            this.lblSouth.Name = "lblSouth";
            this.lblSouth.Size = new System.Drawing.Size(26, 20);
            this.lblSouth.TabIndex = 1;
            this.lblSouth.Text = "南";
            // 
            // lblEast
            // 
            this.lblEast.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblEast.AutoSize = true;
            this.lblEast.BackColor = System.Drawing.Color.Transparent;
            this.lblEast.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblEast.ForeColor = System.Drawing.SystemColors.Control;
            this.lblEast.Location = new System.Drawing.Point(904, 345);
            this.lblEast.Name = "lblEast";
            this.lblEast.Size = new System.Drawing.Size(26, 20);
            this.lblEast.TabIndex = 2;
            this.lblEast.Text = "东";
            // 
            // lblNorth
            // 
            this.lblNorth.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblNorth.AutoSize = true;
            this.lblNorth.BackColor = System.Drawing.Color.Transparent;
            this.lblNorth.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNorth.ForeColor = System.Drawing.SystemColors.Control;
            this.lblNorth.Location = new System.Drawing.Point(402, 44);
            this.lblNorth.Name = "lblNorth";
            this.lblNorth.Size = new System.Drawing.Size(26, 20);
            this.lblNorth.TabIndex = 3;
            this.lblNorth.Text = "北";
            // 
            // lblWest
            // 
            this.lblWest.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblWest.AutoSize = true;
            this.lblWest.BackColor = System.Drawing.Color.Transparent;
            this.lblWest.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblWest.ForeColor = System.Drawing.SystemColors.Control;
            this.lblWest.Location = new System.Drawing.Point(9, 345);
            this.lblWest.Name = "lblWest";
            this.lblWest.Size = new System.Drawing.Size(26, 20);
            this.lblWest.TabIndex = 4;
            this.lblWest.Text = "西";
            // 
            // lblSouthNickName
            // 
            this.lblSouthNickName.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblSouthNickName.AutoSize = true;
            this.lblSouthNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblSouthNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSouthNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblSouthNickName.Location = new System.Drawing.Point(434, 754);
            this.lblSouthNickName.Name = "lblSouthNickName";
            this.lblSouthNickName.Size = new System.Drawing.Size(0, 20);
            this.lblSouthNickName.TabIndex = 5;
            this.lblSouthNickName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblEastNickName
            // 
            this.lblEastNickName.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblEastNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblEastNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblEastNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblEastNickName.Location = new System.Drawing.Point(834, 377);
            this.lblEastNickName.Name = "lblEastNickName";
            this.lblEastNickName.Size = new System.Drawing.Size(96, 20);
            this.lblEastNickName.TabIndex = 6;
            this.lblEastNickName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNorthNickName
            // 
            this.lblNorthNickName.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblNorthNickName.AutoSize = true;
            this.lblNorthNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblNorthNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNorthNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblNorthNickName.Location = new System.Drawing.Point(434, 44);
            this.lblNorthNickName.Name = "lblNorthNickName";
            this.lblNorthNickName.Size = new System.Drawing.Size(0, 20);
            this.lblNorthNickName.TabIndex = 7;
            // 
            // lblWestNickName
            // 
            this.lblWestNickName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblWestNickName.AutoSize = true;
            this.lblWestNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblWestNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblWestNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblWestNickName.Location = new System.Drawing.Point(9, 377);
            this.lblWestNickName.Name = "lblWestNickName";
            this.lblWestNickName.Size = new System.Drawing.Size(0, 20);
            this.lblWestNickName.TabIndex = 8;
            // 
            // btnReady
            // 
            this.btnReady.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReady.Enabled = false;
            this.btnReady.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnReady.Location = new System.Drawing.Point(13, 729);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(72, 45);
            this.btnReady.TabIndex = 10;
            this.btnReady.Text = "就绪";
            this.btnReady.UseVisualStyleBackColor = true;
            this.btnReady.Click += new System.EventHandler(this.btnReady_Click);
            // 
            // lblSouthStarter
            // 
            this.lblSouthStarter.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblSouthStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblSouthStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSouthStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblSouthStarter.Location = new System.Drawing.Point(318, 744);
            this.lblSouthStarter.Name = "lblSouthStarter";
            this.lblSouthStarter.Size = new System.Drawing.Size(61, 30);
            this.lblSouthStarter.TabIndex = 11;
            this.lblSouthStarter.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblWestStarter
            // 
            this.lblWestStarter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblWestStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblWestStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblWestStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblWestStarter.Location = new System.Drawing.Point(8, 296);
            this.lblWestStarter.Name = "lblWestStarter";
            this.lblWestStarter.Size = new System.Drawing.Size(61, 30);
            this.lblWestStarter.TabIndex = 12;
            this.lblWestStarter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNorthStarter
            // 
            this.lblNorthStarter.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblNorthStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblNorthStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNorthStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblNorthStarter.Location = new System.Drawing.Point(318, 44);
            this.lblNorthStarter.Name = "lblNorthStarter";
            this.lblNorthStarter.Size = new System.Drawing.Size(61, 30);
            this.lblNorthStarter.TabIndex = 13;
            // 
            // lblEastStarter
            // 
            this.lblEastStarter.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblEastStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblEastStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblEastStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblEastStarter.Location = new System.Drawing.Point(869, 296);
            this.lblEastStarter.Name = "lblEastStarter";
            this.lblEastStarter.Size = new System.Drawing.Size(61, 30);
            this.lblEastStarter.TabIndex = 14;
            this.lblEastStarter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Duan.Xiugang.Tractor.Properties.Resources.Backgroud;
            this.ClientSize = new System.Drawing.Size(944, 783);
            this.Controls.Add(this.lblEastStarter);
            this.Controls.Add(this.lblNorthStarter);
            this.Controls.Add(this.lblWestStarter);
            this.Controls.Add(this.lblSouthStarter);
            this.Controls.Add(this.lblEastNickName);
            this.Controls.Add(this.btnReady);
            this.Controls.Add(this.lblWestNickName);
            this.Controls.Add(this.lblNorthNickName);
            this.Controls.Add(this.lblSouthNickName);
            this.Controls.Add(this.lblWest);
            this.Controls.Add(this.lblNorth);
            this.Controls.Add(this.lblEast);
            this.Controls.Add(this.lblSouth);
            this.Controls.Add(this.menuStrip);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "拖拉机大战";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDoubleClick);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem GameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem StartToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutMeToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.Label lblSouth;
        private System.Windows.Forms.Label lblEast;
        private System.Windows.Forms.Label lblNorth;
        private System.Windows.Forms.Label lblWest;
        private System.Windows.Forms.Label lblSouthNickName;
        private System.Windows.Forms.Label lblEastNickName;
        private System.Windows.Forms.Label lblNorthNickName;
        private System.Windows.Forms.Label lblWestNickName;
        private System.Windows.Forms.Button btnReady;
        private System.Windows.Forms.ToolStripMenuItem SettingToolStripMenuItem;
        private System.Windows.Forms.Label lblSouthStarter;
        private System.Windows.Forms.Label lblWestStarter;
        private System.Windows.Forms.Label lblNorthStarter;
        private System.Windows.Forms.Label lblEastStarter;
        private System.Windows.Forms.ToolStripMenuItem RebootToolStripMenuItem;
    }
}

