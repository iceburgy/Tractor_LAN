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
            this.ToolStripMenuItemEnterHall = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RebootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemInRoom = new System.Windows.Forms.ToolStripMenuItem();
            this.BeginRankToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRank10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRankJ = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRankQ = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRankK = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemBeginRankA = new System.Windows.Forms.ToolStripMenuItem();
            this.RestoreGameStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MoveToNextPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TeamUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemGetReady = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemRobot = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemObserve = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemObserverNextPlayer = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripMenuItemUserManual = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemShowVersion = new System.Windows.Forms.ToolStripMenuItem();
            this.AutoUpdaterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FeatureOverviewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.lblSouthNickName = new System.Windows.Forms.Label();
            this.lblEastNickName = new System.Windows.Forms.Label();
            this.lblNorthNickName = new System.Windows.Forms.Label();
            this.lblWestNickName = new System.Windows.Forms.Label();
            this.btnReady = new System.Windows.Forms.Button();
            this.lblSouthStarter = new System.Windows.Forms.Label();
            this.lblWestStarter = new System.Windows.Forms.Label();
            this.lblNorthStarter = new System.Windows.Forms.Label();
            this.lblEastStarter = new System.Windows.Forms.Label();
            this.lblTheTimer = new System.Windows.Forms.Label();
            this.theTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnEnterHall = new System.Windows.Forms.Button();
            this.btnRobot = new System.Windows.Forms.Button();
            this.btnObserveNext = new System.Windows.Forms.Button();
            this.pnlGameRooms = new System.Windows.Forms.Panel();
            this.btnExitRoom = new System.Windows.Forms.Button();
            this.menuStrip.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GameToolStripMenuItem,
            this.ToolStripMenuItemInRoom,
            this.ToolStripMenuItemObserve,
            this.HelpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Padding = new System.Windows.Forms.Padding(9, 3, 0, 3);
            this.menuStrip.Size = new System.Drawing.Size(820, 35);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip1";
            // 
            // GameToolStripMenuItem
            // 
            this.GameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemEnterHall,
            this.SettingToolStripMenuItem,
            this.RebootToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.GameToolStripMenuItem.Name = "GameToolStripMenuItem";
            this.GameToolStripMenuItem.Size = new System.Drawing.Size(62, 29);
            this.GameToolStripMenuItem.Text = "游戏";
            // 
            // ToolStripMenuItemEnterHall
            // 
            this.ToolStripMenuItemEnterHall.Name = "ToolStripMenuItemEnterHall";
            this.ToolStripMenuItemEnterHall.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.ToolStripMenuItemEnterHall.Size = new System.Drawing.Size(191, 30);
            this.ToolStripMenuItemEnterHall.Text = "进入大厅";
            this.ToolStripMenuItemEnterHall.Click += new System.EventHandler(this.ToolStripMenuItemEnterHall_Click);
            // 
            // SettingToolStripMenuItem
            // 
            this.SettingToolStripMenuItem.Name = "SettingToolStripMenuItem";
            this.SettingToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.SettingToolStripMenuItem.Text = "设置";
            this.SettingToolStripMenuItem.Click += new System.EventHandler(this.SettingToolStripMenuItem_Click);
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
            // ToolStripMenuItemInRoom
            // 
            this.ToolStripMenuItemInRoom.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BeginRankToolStripMenuItem,
            this.RestoreGameStateToolStripMenuItem,
            this.MoveToNextPositionToolStripMenuItem,
            this.TeamUpToolStripMenuItem,
            this.ToolStripMenuItemGetReady,
            this.ToolStripMenuItemRobot});
            this.ToolStripMenuItemInRoom.Name = "ToolStripMenuItemInRoom";
            this.ToolStripMenuItemInRoom.Size = new System.Drawing.Size(62, 29);
            this.ToolStripMenuItemInRoom.Text = "牌局";
            this.ToolStripMenuItemInRoom.Visible = false;
            // 
            // BeginRankToolStripMenuItem
            // 
            this.BeginRankToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemBeginRank2,
            this.toolStripMenuItemBeginRank3,
            this.toolStripMenuItemBeginRank4,
            this.toolStripMenuItemBeginRank5,
            this.toolStripMenuItemBeginRank6,
            this.toolStripMenuItemBeginRank7,
            this.toolStripMenuItemBeginRank8,
            this.toolStripMenuItemBeginRank9,
            this.toolStripMenuItemBeginRank10,
            this.toolStripMenuItemBeginRankJ,
            this.toolStripMenuItemBeginRankQ,
            this.toolStripMenuItemBeginRankK,
            this.toolStripMenuItemBeginRankA});
            this.BeginRankToolStripMenuItem.Name = "BeginRankToolStripMenuItem";
            this.BeginRankToolStripMenuItem.Size = new System.Drawing.Size(217, 30);
            this.BeginRankToolStripMenuItem.Text = "从几打起";
            // 
            // toolStripMenuItemBeginRank2
            // 
            this.toolStripMenuItemBeginRank2.Name = "toolStripMenuItemBeginRank2";
            this.toolStripMenuItemBeginRank2.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank2.Text = "2";
            this.toolStripMenuItemBeginRank2.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank3
            // 
            this.toolStripMenuItemBeginRank3.Name = "toolStripMenuItemBeginRank3";
            this.toolStripMenuItemBeginRank3.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank3.Text = "3";
            this.toolStripMenuItemBeginRank3.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank4
            // 
            this.toolStripMenuItemBeginRank4.Name = "toolStripMenuItemBeginRank4";
            this.toolStripMenuItemBeginRank4.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank4.Text = "4";
            this.toolStripMenuItemBeginRank4.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank5
            // 
            this.toolStripMenuItemBeginRank5.Name = "toolStripMenuItemBeginRank5";
            this.toolStripMenuItemBeginRank5.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank5.Text = "5";
            this.toolStripMenuItemBeginRank5.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank6
            // 
            this.toolStripMenuItemBeginRank6.Name = "toolStripMenuItemBeginRank6";
            this.toolStripMenuItemBeginRank6.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank6.Text = "6";
            this.toolStripMenuItemBeginRank6.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank7
            // 
            this.toolStripMenuItemBeginRank7.Name = "toolStripMenuItemBeginRank7";
            this.toolStripMenuItemBeginRank7.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank7.Text = "7";
            this.toolStripMenuItemBeginRank7.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank8
            // 
            this.toolStripMenuItemBeginRank8.Name = "toolStripMenuItemBeginRank8";
            this.toolStripMenuItemBeginRank8.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank8.Text = "8";
            this.toolStripMenuItemBeginRank8.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank9
            // 
            this.toolStripMenuItemBeginRank9.Name = "toolStripMenuItemBeginRank9";
            this.toolStripMenuItemBeginRank9.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank9.Text = "9";
            this.toolStripMenuItemBeginRank9.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRank10
            // 
            this.toolStripMenuItemBeginRank10.Name = "toolStripMenuItemBeginRank10";
            this.toolStripMenuItemBeginRank10.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRank10.Text = "10";
            this.toolStripMenuItemBeginRank10.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRankJ
            // 
            this.toolStripMenuItemBeginRankJ.Name = "toolStripMenuItemBeginRankJ";
            this.toolStripMenuItemBeginRankJ.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRankJ.Text = "J";
            this.toolStripMenuItemBeginRankJ.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRankQ
            // 
            this.toolStripMenuItemBeginRankQ.Name = "toolStripMenuItemBeginRankQ";
            this.toolStripMenuItemBeginRankQ.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRankQ.Text = "Q";
            this.toolStripMenuItemBeginRankQ.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRankK
            // 
            this.toolStripMenuItemBeginRankK.Name = "toolStripMenuItemBeginRankK";
            this.toolStripMenuItemBeginRankK.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRankK.Text = "K";
            this.toolStripMenuItemBeginRankK.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // toolStripMenuItemBeginRankA
            // 
            this.toolStripMenuItemBeginRankA.Name = "toolStripMenuItemBeginRankA";
            this.toolStripMenuItemBeginRankA.Size = new System.Drawing.Size(104, 30);
            this.toolStripMenuItemBeginRankA.Text = "A";
            this.toolStripMenuItemBeginRankA.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // RestoreGameStateToolStripMenuItem
            // 
            this.RestoreGameStateToolStripMenuItem.Name = "RestoreGameStateToolStripMenuItem";
            this.RestoreGameStateToolStripMenuItem.Size = new System.Drawing.Size(217, 30);
            this.RestoreGameStateToolStripMenuItem.Text = "读取上盘牌局";
            this.RestoreGameStateToolStripMenuItem.Click += new System.EventHandler(this.RestoreGameStateToolStripMenuItem_Click);
            // 
            // MoveToNextPositionToolStripMenuItem
            // 
            this.MoveToNextPositionToolStripMenuItem.Name = "MoveToNextPositionToolStripMenuItem";
            this.MoveToNextPositionToolStripMenuItem.Size = new System.Drawing.Size(217, 30);
            this.MoveToNextPositionToolStripMenuItem.Text = "和下家互换座位";
            this.MoveToNextPositionToolStripMenuItem.Click += new System.EventHandler(this.MoveToNextPositionToolStripMenuItem_Click);
            // 
            // TeamUpToolStripMenuItem
            // 
            this.TeamUpToolStripMenuItem.Name = "TeamUpToolStripMenuItem";
            this.TeamUpToolStripMenuItem.Size = new System.Drawing.Size(217, 30);
            this.TeamUpToolStripMenuItem.Text = "随机组队";
            this.TeamUpToolStripMenuItem.Click += new System.EventHandler(this.TeamUpToolStripMenuItem_Click);
            // 
            // ToolStripMenuItemGetReady
            // 
            this.ToolStripMenuItemGetReady.Name = "ToolStripMenuItemGetReady";
            this.ToolStripMenuItemGetReady.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.ToolStripMenuItemGetReady.Size = new System.Drawing.Size(217, 30);
            this.ToolStripMenuItemGetReady.Text = "就绪";
            this.ToolStripMenuItemGetReady.Click += new System.EventHandler(this.ToolStripMenuItemGetReady_Click);
            // 
            // ToolStripMenuItemRobot
            // 
            this.ToolStripMenuItemRobot.Name = "ToolStripMenuItemRobot";
            this.ToolStripMenuItemRobot.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.ToolStripMenuItemRobot.Size = new System.Drawing.Size(217, 30);
            this.ToolStripMenuItemRobot.Text = "托管代打";
            this.ToolStripMenuItemRobot.Click += new System.EventHandler(this.ToolStripMenuItemRobot_Click);
            // 
            // ToolStripMenuItemObserve
            // 
            this.ToolStripMenuItemObserve.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemObserverNextPlayer});
            this.ToolStripMenuItemObserve.Name = "ToolStripMenuItemObserve";
            this.ToolStripMenuItemObserve.Size = new System.Drawing.Size(62, 29);
            this.ToolStripMenuItemObserve.Text = "旁观";
            this.ToolStripMenuItemObserve.Visible = false;
            // 
            // ToolStripMenuItemObserverNextPlayer
            // 
            this.ToolStripMenuItemObserverNextPlayer.Name = "ToolStripMenuItemObserverNextPlayer";
            this.ToolStripMenuItemObserverNextPlayer.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.ToolStripMenuItemObserverNextPlayer.Size = new System.Drawing.Size(191, 30);
            this.ToolStripMenuItemObserverNextPlayer.Text = "旁观下家";
            this.ToolStripMenuItemObserverNextPlayer.Click += new System.EventHandler(this.ToolStripMenuItemObserverNextPlayer_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator5,
            this.ToolStripMenuItemUserManual,
            this.ToolStripMenuItemShowVersion,
            this.AutoUpdaterToolStripMenuItem,
            this.FeatureOverviewToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(62, 29);
            this.HelpToolStripMenuItem.Text = "帮助";
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(157, 6);
            // 
            // ToolStripMenuItemUserManual
            // 
            this.ToolStripMenuItemUserManual.Name = "ToolStripMenuItemUserManual";
            this.ToolStripMenuItemUserManual.Size = new System.Drawing.Size(160, 30);
            this.ToolStripMenuItemUserManual.Text = "使用说明";
            this.ToolStripMenuItemUserManual.Click += new System.EventHandler(this.ToolStripMenuItemUserManual_Click);
            // 
            // ToolStripMenuItemShowVersion
            // 
            this.ToolStripMenuItemShowVersion.Name = "ToolStripMenuItemShowVersion";
            this.ToolStripMenuItemShowVersion.Size = new System.Drawing.Size(160, 30);
            this.ToolStripMenuItemShowVersion.Text = "当前版本";
            this.ToolStripMenuItemShowVersion.Click += new System.EventHandler(this.ToolStripMenuItemShowVersion_Click);
            // 
            // AutoUpdaterToolStripMenuItem
            // 
            this.AutoUpdaterToolStripMenuItem.Name = "AutoUpdaterToolStripMenuItem";
            this.AutoUpdaterToolStripMenuItem.Size = new System.Drawing.Size(160, 30);
            this.AutoUpdaterToolStripMenuItem.Text = "检查更新";
            this.AutoUpdaterToolStripMenuItem.Click += new System.EventHandler(this.AutoUpdaterToolStripMenuItem_Click);
            // 
            // FeatureOverviewToolStripMenuItem
            // 
            this.FeatureOverviewToolStripMenuItem.Name = "FeatureOverviewToolStripMenuItem";
            this.FeatureOverviewToolStripMenuItem.Size = new System.Drawing.Size(160, 30);
            this.FeatureOverviewToolStripMenuItem.Text = "功能一览";
            this.FeatureOverviewToolStripMenuItem.Click += new System.EventHandler(this.FeatureOverviewToolStripMenuItem_Click);
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
            // lblSouthNickName
            // 
            this.lblSouthNickName.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblSouthNickName.AutoSize = true;
            this.lblSouthNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblSouthNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSouthNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblSouthNickName.Location = new System.Drawing.Point(372, 784);
            this.lblSouthNickName.Name = "lblSouthNickName";
            this.lblSouthNickName.Size = new System.Drawing.Size(0, 37);
            this.lblSouthNickName.TabIndex = 5;
            this.lblSouthNickName.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblEastNickName
            // 
            this.lblEastNickName.AutoSize = true;
            this.lblEastNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblEastNickName.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblEastNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblEastNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblEastNickName.Location = new System.Drawing.Point(4, 0);
            this.lblEastNickName.Name = "lblEastNickName";
            this.lblEastNickName.Size = new System.Drawing.Size(0, 37);
            this.lblEastNickName.TabIndex = 6;
            this.lblEastNickName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNorthNickName
            // 
            this.lblNorthNickName.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblNorthNickName.AutoSize = true;
            this.lblNorthNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblNorthNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNorthNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblNorthNickName.Location = new System.Drawing.Point(372, 44);
            this.lblNorthNickName.Name = "lblNorthNickName";
            this.lblNorthNickName.Size = new System.Drawing.Size(0, 37);
            this.lblNorthNickName.TabIndex = 7;
            // 
            // lblWestNickName
            // 
            this.lblWestNickName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblWestNickName.AutoSize = true;
            this.lblWestNickName.BackColor = System.Drawing.Color.Transparent;
            this.lblWestNickName.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblWestNickName.ForeColor = System.Drawing.SystemColors.Control;
            this.lblWestNickName.Location = new System.Drawing.Point(9, 401);
            this.lblWestNickName.Name = "lblWestNickName";
            this.lblWestNickName.Size = new System.Drawing.Size(0, 37);
            this.lblWestNickName.TabIndex = 8;
            this.lblWestNickName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnReady
            // 
            this.btnReady.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReady.Enabled = false;
            this.btnReady.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnReady.Location = new System.Drawing.Point(13, 776);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(72, 45);
            this.btnReady.TabIndex = 10;
            this.btnReady.Text = "就绪";
            this.btnReady.UseVisualStyleBackColor = true;
            this.btnReady.Visible = false;
            this.btnReady.Click += new System.EventHandler(this.btnReady_Click);
            // 
            // lblSouthStarter
            // 
            this.lblSouthStarter.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblSouthStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblSouthStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSouthStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblSouthStarter.Location = new System.Drawing.Point(251, 782);
            this.lblSouthStarter.Name = "lblSouthStarter";
            this.lblSouthStarter.Size = new System.Drawing.Size(121, 39);
            this.lblSouthStarter.TabIndex = 11;
            this.lblSouthStarter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblSouthStarter.Click += new System.EventHandler(this.lblSouthStarter_Click);
            // 
            // lblWestStarter
            // 
            this.lblWestStarter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblWestStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblWestStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblWestStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblWestStarter.Location = new System.Drawing.Point(8, 359);
            this.lblWestStarter.Name = "lblWestStarter";
            this.lblWestStarter.Size = new System.Drawing.Size(124, 42);
            this.lblWestStarter.TabIndex = 12;
            this.lblWestStarter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNorthStarter
            // 
            this.lblNorthStarter.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblNorthStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblNorthStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblNorthStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblNorthStarter.Location = new System.Drawing.Point(244, 44);
            this.lblNorthStarter.Name = "lblNorthStarter";
            this.lblNorthStarter.Size = new System.Drawing.Size(128, 37);
            this.lblNorthStarter.TabIndex = 13;
            this.lblNorthStarter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblEastStarter
            // 
            this.lblEastStarter.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblEastStarter.BackColor = System.Drawing.Color.Transparent;
            this.lblEastStarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblEastStarter.ForeColor = System.Drawing.Color.Orange;
            this.lblEastStarter.Location = new System.Drawing.Point(694, 359);
            this.lblEastStarter.Name = "lblEastStarter";
            this.lblEastStarter.Size = new System.Drawing.Size(126, 42);
            this.lblEastStarter.TabIndex = 14;
            this.lblEastStarter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTheTimer
            // 
            this.lblTheTimer.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTheTimer.BackColor = System.Drawing.Color.Transparent;
            this.lblTheTimer.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTheTimer.ForeColor = System.Drawing.Color.Yellow;
            this.lblTheTimer.Location = new System.Drawing.Point(726, 541);
            this.lblTheTimer.Name = "lblTheTimer";
            this.lblTheTimer.Size = new System.Drawing.Size(82, 57);
            this.lblTheTimer.TabIndex = 15;
            this.lblTheTimer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // theTimer
            // 
            this.theTimer.Interval = 1000;
            this.theTimer.Tag = "";
            this.theTimer.Tick += new System.EventHandler(this.theTimer_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.lblEastNickName, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(814, 404);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(6, 37);
            this.tableLayoutPanel1.TabIndex = 16;
            // 
            // btnEnterHall
            // 
            this.btnEnterHall.Location = new System.Drawing.Point(263, 386);
            this.btnEnterHall.Name = "btnEnterHall";
            this.btnEnterHall.Size = new System.Drawing.Size(109, 55);
            this.btnEnterHall.TabIndex = 21;
            this.btnEnterHall.Text = "进入大厅";
            this.btnEnterHall.UseVisualStyleBackColor = true;
            this.btnEnterHall.Click += new System.EventHandler(this.btnEnterHall_Click);
            // 
            // btnRobot
            // 
            this.btnRobot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRobot.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRobot.Location = new System.Drawing.Point(91, 776);
            this.btnRobot.Name = "btnRobot";
            this.btnRobot.Size = new System.Drawing.Size(72, 45);
            this.btnRobot.TabIndex = 22;
            this.btnRobot.Text = "托管";
            this.btnRobot.UseVisualStyleBackColor = true;
            this.btnRobot.Visible = false;
            this.btnRobot.Click += new System.EventHandler(this.btnRobot_Click);
            // 
            // btnObserveNext
            // 
            this.btnObserveNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnObserveNext.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnObserveNext.Location = new System.Drawing.Point(13, 776);
            this.btnObserveNext.Name = "btnObserveNext";
            this.btnObserveNext.Size = new System.Drawing.Size(119, 45);
            this.btnObserveNext.TabIndex = 23;
            this.btnObserveNext.Text = "旁观下家";
            this.btnObserveNext.UseVisualStyleBackColor = true;
            this.btnObserveNext.Visible = false;
            this.btnObserveNext.Click += new System.EventHandler(this.btnObserveNext_Click);
            // 
            // pnlGameRooms
            // 
            this.pnlGameRooms.BackColor = System.Drawing.Color.Transparent;
            this.pnlGameRooms.Location = new System.Drawing.Point(44, 139);
            this.pnlGameRooms.Name = "pnlGameRooms";
            this.pnlGameRooms.Size = new System.Drawing.Size(734, 533);
            this.pnlGameRooms.TabIndex = 24;
            this.pnlGameRooms.Visible = false;
            // 
            // btnExitRoom
            // 
            this.btnExitRoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExitRoom.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnExitRoom.Location = new System.Drawing.Point(736, 776);
            this.btnExitRoom.Name = "btnExitRoom";
            this.btnExitRoom.Size = new System.Drawing.Size(72, 45);
            this.btnExitRoom.TabIndex = 25;
            this.btnExitRoom.Text = "退出";
            this.btnExitRoom.UseVisualStyleBackColor = true;
            this.btnExitRoom.Visible = false;
            this.btnExitRoom.Click += new System.EventHandler(this.btnExitRoom_Click);
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImage = global::Duan.Xiugang.Tractor.Properties.Resources.Backgroud;
            this.ClientSize = new System.Drawing.Size(820, 830);
            this.Controls.Add(this.btnExitRoom);
            this.Controls.Add(this.pnlGameRooms);
            this.Controls.Add(this.btnObserveNext);
            this.Controls.Add(this.btnRobot);
            this.Controls.Add(this.btnEnterHall);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.lblTheTimer);
            this.Controls.Add(this.lblEastStarter);
            this.Controls.Add(this.lblNorthStarter);
            this.Controls.Add(this.lblWestStarter);
            this.Controls.Add(this.lblSouthStarter);
            this.Controls.Add(this.btnReady);
            this.Controls.Add(this.lblWestNickName);
            this.Controls.Add(this.lblNorthNickName);
            this.Controls.Add(this.lblSouthNickName);
            this.Controls.Add(this.menuStrip);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDoubleClick);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem GameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
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
        private System.Windows.Forms.ToolStripMenuItem RestoreGameStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem BeginRankToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRank10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRankJ;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRankQ;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRankK;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemBeginRankA;
        private System.Windows.Forms.Label lblTheTimer;
        private System.Windows.Forms.Timer theTimer;
        private System.Windows.Forms.ToolStripMenuItem AutoUpdaterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FeatureOverviewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem TeamUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MoveToNextPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemShowVersion;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemGetReady;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemObserverNextPlayer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemRobot;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemEnterHall;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemInRoom;
        private System.Windows.Forms.Button btnEnterHall;
        private System.Windows.Forms.Button btnRobot;
        private System.Windows.Forms.Button btnObserveNext;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemObserve;
        private System.Windows.Forms.Panel pnlGameRooms;
        private System.Windows.Forms.Button btnExitRoom;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemUserManual;
    }
}

