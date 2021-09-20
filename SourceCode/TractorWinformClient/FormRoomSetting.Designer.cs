namespace Duan.Xiugang.Tractor
{
    partial class FormRoomSetting
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
            this.lblAllowRiotByScore = new System.Windows.Forms.Label();
            this.cbbRiotByScore = new System.Windows.Forms.ComboBox();
            this.lblRiotByTrump = new System.Windows.Forms.Label();
            this.cbbRiotByTrump = new System.Windows.Forms.ComboBox();
            this.cbxJToBottom = new System.Windows.Forms.CheckBox();
            this.cbxAllowSurrender = new System.Windows.Forms.CheckBox();
            this.cbxMust_12 = new System.Windows.Forms.CheckBox();
            this.cbxMust_7 = new System.Windows.Forms.CheckBox();
            this.cbxMust_11 = new System.Windows.Forms.CheckBox();
            this.cbxMust_6 = new System.Windows.Forms.CheckBox();
            this.cbxMust_10 = new System.Windows.Forms.CheckBox();
            this.cbxMust_5 = new System.Windows.Forms.CheckBox();
            this.cbxMust_2 = new System.Windows.Forms.CheckBox();
            this.cbxMust_9 = new System.Windows.Forms.CheckBox();
            this.cbxMust_4 = new System.Windows.Forms.CheckBox();
            this.cbxMust_1 = new System.Windows.Forms.CheckBox();
            this.cbxMust_8 = new System.Windows.Forms.CheckBox();
            this.cbxMust_3 = new System.Windows.Forms.CheckBox();
            this.cbxMust_0 = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblRoomName = new System.Windows.Forms.Label();
            this.lblRoomOwnerLabel = new System.Windows.Forms.Label();
            this.lblRoomOwner = new System.Windows.Forms.Label();
            this.lblRoomNameLabel = new System.Windows.Forms.Label();
            this.lblMustDoRanksLabel = new System.Windows.Forms.Label();
            this.lblKickPlayer = new System.Windows.Forms.Label();
            this.cbbKickPlayer = new System.Windows.Forms.ComboBox();
            this.btnKickPlayer = new System.Windows.Forms.Button();
            this.btnKickObserver = new System.Windows.Forms.Button();
            this.cbbKickObserver = new System.Windows.Forms.ComboBox();
            this.lblKickObserver = new System.Windows.Forms.Label();
            this.cbxAllowRobotMakeTrump = new System.Windows.Forms.CheckBox();
            this.lblReenterWaitSeconds = new System.Windows.Forms.Label();
            this.nudReenterWaitSeconds = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.nudReenterWaitSeconds)).BeginInit();
            this.SuspendLayout();
            // 
            // lblAllowRiotByScore
            // 
            this.lblAllowRiotByScore.AutoSize = true;
            this.lblAllowRiotByScore.Location = new System.Drawing.Point(27, 243);
            this.lblAllowRiotByScore.Name = "lblAllowRiotByScore";
            this.lblAllowRiotByScore.Size = new System.Drawing.Size(196, 20);
            this.lblAllowRiotByScore.TabIndex = 0;
            this.lblAllowRiotByScore.Text = "允许分数小于等于N时革命";
            // 
            // cbbRiotByScore
            // 
            this.cbbRiotByScore.DisplayMember = "0";
            this.cbbRiotByScore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbRiotByScore.Enabled = false;
            this.cbbRiotByScore.FormattingEnabled = true;
            this.cbbRiotByScore.Items.AddRange(new object[] {
            "不允许",
            "0",
            "5",
            "10",
            "15"});
            this.cbbRiotByScore.Location = new System.Drawing.Point(394, 235);
            this.cbbRiotByScore.Name = "cbbRiotByScore";
            this.cbbRiotByScore.Size = new System.Drawing.Size(121, 28);
            this.cbbRiotByScore.TabIndex = 1;
            // 
            // lblRiotByTrump
            // 
            this.lblRiotByTrump.AutoSize = true;
            this.lblRiotByTrump.Location = new System.Drawing.Point(27, 291);
            this.lblRiotByTrump.Name = "lblRiotByTrump";
            this.lblRiotByTrump.Size = new System.Drawing.Size(324, 20);
            this.lblRiotByTrump.TabIndex = 2;
            this.lblRiotByTrump.Text = "允许主牌小于等于N张时革命（打无主不算）";
            // 
            // cbbRiotByTrump
            // 
            this.cbbRiotByTrump.DisplayMember = "0";
            this.cbbRiotByTrump.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbRiotByTrump.Enabled = false;
            this.cbbRiotByTrump.FormattingEnabled = true;
            this.cbbRiotByTrump.Items.AddRange(new object[] {
            "不允许",
            "0",
            "1",
            "2",
            "3",
            "4"});
            this.cbbRiotByTrump.Location = new System.Drawing.Point(394, 288);
            this.cbbRiotByTrump.Name = "cbbRiotByTrump";
            this.cbbRiotByTrump.Size = new System.Drawing.Size(121, 28);
            this.cbbRiotByTrump.TabIndex = 1;
            // 
            // cbxJToBottom
            // 
            this.cbxJToBottom.AutoSize = true;
            this.cbxJToBottom.Enabled = false;
            this.cbxJToBottom.Location = new System.Drawing.Point(31, 159);
            this.cbxJToBottom.Name = "cbxJToBottom";
            this.cbxJToBottom.Size = new System.Drawing.Size(267, 24);
            this.cbxJToBottom.TabIndex = 3;
            this.cbxJToBottom.Text = "允许J到底（主J到底，副J一半）";
            this.cbxJToBottom.UseVisualStyleBackColor = true;
            // 
            // cbxAllowSurrender
            // 
            this.cbxAllowSurrender.AutoSize = true;
            this.cbxAllowSurrender.Enabled = false;
            this.cbxAllowSurrender.Location = new System.Drawing.Point(31, 116);
            this.cbxAllowSurrender.Name = "cbxAllowSurrender";
            this.cbxAllowSurrender.Size = new System.Drawing.Size(323, 24);
            this.cbxAllowSurrender.TabIndex = 3;
            this.cbxAllowSurrender.Text = "允许投降（投降后对方上台或者升一级）";
            this.cbxAllowSurrender.UseVisualStyleBackColor = true;
            // 
            // cbxMust_12
            // 
            this.cbxMust_12.AutoSize = true;
            this.cbxMust_12.Enabled = false;
            this.cbxMust_12.Location = new System.Drawing.Point(155, 560);
            this.cbxMust_12.Name = "cbxMust_12";
            this.cbxMust_12.Size = new System.Drawing.Size(46, 24);
            this.cbxMust_12.TabIndex = 0;
            this.cbxMust_12.Text = "A";
            this.cbxMust_12.UseVisualStyleBackColor = true;
            // 
            // cbxMust_7
            // 
            this.cbxMust_7.AutoSize = true;
            this.cbxMust_7.Enabled = false;
            this.cbxMust_7.Location = new System.Drawing.Point(92, 560);
            this.cbxMust_7.Name = "cbxMust_7";
            this.cbxMust_7.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_7.TabIndex = 0;
            this.cbxMust_7.Text = "9";
            this.cbxMust_7.UseVisualStyleBackColor = true;
            // 
            // cbxMust_11
            // 
            this.cbxMust_11.AutoSize = true;
            this.cbxMust_11.Enabled = false;
            this.cbxMust_11.Location = new System.Drawing.Point(155, 530);
            this.cbxMust_11.Name = "cbxMust_11";
            this.cbxMust_11.Size = new System.Drawing.Size(45, 24);
            this.cbxMust_11.TabIndex = 0;
            this.cbxMust_11.Text = "K";
            this.cbxMust_11.UseVisualStyleBackColor = true;
            // 
            // cbxMust_6
            // 
            this.cbxMust_6.AutoSize = true;
            this.cbxMust_6.Enabled = false;
            this.cbxMust_6.Location = new System.Drawing.Point(92, 530);
            this.cbxMust_6.Name = "cbxMust_6";
            this.cbxMust_6.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_6.TabIndex = 0;
            this.cbxMust_6.Text = "8";
            this.cbxMust_6.UseVisualStyleBackColor = true;
            // 
            // cbxMust_10
            // 
            this.cbxMust_10.AutoSize = true;
            this.cbxMust_10.Enabled = false;
            this.cbxMust_10.Location = new System.Drawing.Point(155, 500);
            this.cbxMust_10.Name = "cbxMust_10";
            this.cbxMust_10.Size = new System.Drawing.Size(47, 24);
            this.cbxMust_10.TabIndex = 0;
            this.cbxMust_10.Text = "Q";
            this.cbxMust_10.UseVisualStyleBackColor = true;
            // 
            // cbxMust_5
            // 
            this.cbxMust_5.AutoSize = true;
            this.cbxMust_5.Enabled = false;
            this.cbxMust_5.Location = new System.Drawing.Point(92, 500);
            this.cbxMust_5.Name = "cbxMust_5";
            this.cbxMust_5.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_5.TabIndex = 0;
            this.cbxMust_5.Text = "7";
            this.cbxMust_5.UseVisualStyleBackColor = true;
            // 
            // cbxMust_2
            // 
            this.cbxMust_2.AutoSize = true;
            this.cbxMust_2.Enabled = false;
            this.cbxMust_2.Location = new System.Drawing.Point(31, 500);
            this.cbxMust_2.Name = "cbxMust_2";
            this.cbxMust_2.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_2.TabIndex = 0;
            this.cbxMust_2.Text = "4";
            this.cbxMust_2.UseVisualStyleBackColor = true;
            // 
            // cbxMust_9
            // 
            this.cbxMust_9.AutoSize = true;
            this.cbxMust_9.Enabled = false;
            this.cbxMust_9.Location = new System.Drawing.Point(155, 469);
            this.cbxMust_9.Name = "cbxMust_9";
            this.cbxMust_9.Size = new System.Drawing.Size(43, 24);
            this.cbxMust_9.TabIndex = 0;
            this.cbxMust_9.Text = "J";
            this.cbxMust_9.UseVisualStyleBackColor = true;
            // 
            // cbxMust_4
            // 
            this.cbxMust_4.AutoSize = true;
            this.cbxMust_4.Enabled = false;
            this.cbxMust_4.Location = new System.Drawing.Point(92, 469);
            this.cbxMust_4.Name = "cbxMust_4";
            this.cbxMust_4.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_4.TabIndex = 0;
            this.cbxMust_4.Text = "6";
            this.cbxMust_4.UseVisualStyleBackColor = true;
            // 
            // cbxMust_1
            // 
            this.cbxMust_1.AutoSize = true;
            this.cbxMust_1.Enabled = false;
            this.cbxMust_1.Location = new System.Drawing.Point(31, 469);
            this.cbxMust_1.Name = "cbxMust_1";
            this.cbxMust_1.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_1.TabIndex = 0;
            this.cbxMust_1.Text = "3";
            this.cbxMust_1.UseVisualStyleBackColor = true;
            // 
            // cbxMust_8
            // 
            this.cbxMust_8.AutoSize = true;
            this.cbxMust_8.Enabled = false;
            this.cbxMust_8.Location = new System.Drawing.Point(155, 439);
            this.cbxMust_8.Name = "cbxMust_8";
            this.cbxMust_8.Size = new System.Drawing.Size(53, 24);
            this.cbxMust_8.TabIndex = 0;
            this.cbxMust_8.Text = "10";
            this.cbxMust_8.UseVisualStyleBackColor = true;
            // 
            // cbxMust_3
            // 
            this.cbxMust_3.AutoSize = true;
            this.cbxMust_3.Enabled = false;
            this.cbxMust_3.Location = new System.Drawing.Point(92, 439);
            this.cbxMust_3.Name = "cbxMust_3";
            this.cbxMust_3.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_3.TabIndex = 0;
            this.cbxMust_3.Text = "5";
            this.cbxMust_3.UseVisualStyleBackColor = true;
            // 
            // cbxMust_0
            // 
            this.cbxMust_0.AutoSize = true;
            this.cbxMust_0.Enabled = false;
            this.cbxMust_0.Location = new System.Drawing.Point(31, 439);
            this.cbxMust_0.Name = "cbxMust_0";
            this.cbxMust_0.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_0.TabIndex = 0;
            this.cbxMust_0.Text = "2";
            this.cbxMust_0.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(31, 730);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(92, 32);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(200, 730);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(98, 32);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblRoomName
            // 
            this.lblRoomName.AutoSize = true;
            this.lblRoomName.Location = new System.Drawing.Point(136, 36);
            this.lblRoomName.Name = "lblRoomName";
            this.lblRoomName.Size = new System.Drawing.Size(0, 20);
            this.lblRoomName.TabIndex = 6;
            // 
            // lblRoomOwnerLabel
            // 
            this.lblRoomOwnerLabel.AutoSize = true;
            this.lblRoomOwnerLabel.Location = new System.Drawing.Point(31, 78);
            this.lblRoomOwnerLabel.Name = "lblRoomOwnerLabel";
            this.lblRoomOwnerLabel.Size = new System.Drawing.Size(57, 20);
            this.lblRoomOwnerLabel.TabIndex = 7;
            this.lblRoomOwnerLabel.Text = "房主：";
            // 
            // lblRoomOwner
            // 
            this.lblRoomOwner.AutoSize = true;
            this.lblRoomOwner.Location = new System.Drawing.Point(136, 78);
            this.lblRoomOwner.Name = "lblRoomOwner";
            this.lblRoomOwner.Size = new System.Drawing.Size(0, 20);
            this.lblRoomOwner.TabIndex = 8;
            // 
            // lblRoomNameLabel
            // 
            this.lblRoomNameLabel.AutoSize = true;
            this.lblRoomNameLabel.Location = new System.Drawing.Point(31, 36);
            this.lblRoomNameLabel.Name = "lblRoomNameLabel";
            this.lblRoomNameLabel.Size = new System.Drawing.Size(73, 20);
            this.lblRoomNameLabel.TabIndex = 9;
            this.lblRoomNameLabel.Text = "房间名：";
            // 
            // lblMustDoRanksLabel
            // 
            this.lblMustDoRanksLabel.AutoSize = true;
            this.lblMustDoRanksLabel.Location = new System.Drawing.Point(31, 393);
            this.lblMustDoRanksLabel.Name = "lblMustDoRanksLabel";
            this.lblMustDoRanksLabel.Size = new System.Drawing.Size(41, 20);
            this.lblMustDoRanksLabel.TabIndex = 10;
            this.lblMustDoRanksLabel.Text = "必打";
            // 
            // lblKickPlayer
            // 
            this.lblKickPlayer.AutoSize = true;
            this.lblKickPlayer.Location = new System.Drawing.Point(27, 622);
            this.lblKickPlayer.Name = "lblKickPlayer";
            this.lblKickPlayer.Size = new System.Drawing.Size(137, 20);
            this.lblKickPlayer.TabIndex = 11;
            this.lblKickPlayer.Text = "将玩家请出房间：";
            // 
            // cbbKickPlayer
            // 
            this.cbbKickPlayer.DisplayMember = "0";
            this.cbbKickPlayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbKickPlayer.FormattingEnabled = true;
            this.cbbKickPlayer.Location = new System.Drawing.Point(177, 619);
            this.cbbKickPlayer.Name = "cbbKickPlayer";
            this.cbbKickPlayer.Size = new System.Drawing.Size(121, 28);
            this.cbbKickPlayer.TabIndex = 12;
            // 
            // btnKickPlayer
            // 
            this.btnKickPlayer.Enabled = false;
            this.btnKickPlayer.Location = new System.Drawing.Point(336, 616);
            this.btnKickPlayer.Name = "btnKickPlayer";
            this.btnKickPlayer.Size = new System.Drawing.Size(106, 32);
            this.btnKickPlayer.TabIndex = 13;
            this.btnKickPlayer.Text = "请出房间";
            this.btnKickPlayer.UseVisualStyleBackColor = true;
            this.btnKickPlayer.Click += new System.EventHandler(this.btnKickPlayer_Click);
            // 
            // btnKickObserver
            // 
            this.btnKickObserver.Enabled = false;
            this.btnKickObserver.Location = new System.Drawing.Point(336, 664);
            this.btnKickObserver.Name = "btnKickObserver";
            this.btnKickObserver.Size = new System.Drawing.Size(106, 32);
            this.btnKickObserver.TabIndex = 16;
            this.btnKickObserver.Text = "请出房间";
            this.btnKickObserver.UseVisualStyleBackColor = true;
            this.btnKickObserver.Click += new System.EventHandler(this.btnKickObserver_Click);
            // 
            // cbbKickObserver
            // 
            this.cbbKickObserver.DisplayMember = "0";
            this.cbbKickObserver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbKickObserver.FormattingEnabled = true;
            this.cbbKickObserver.Location = new System.Drawing.Point(177, 667);
            this.cbbKickObserver.Name = "cbbKickObserver";
            this.cbbKickObserver.Size = new System.Drawing.Size(121, 28);
            this.cbbKickObserver.TabIndex = 15;
            // 
            // lblKickObserver
            // 
            this.lblKickObserver.AutoSize = true;
            this.lblKickObserver.Location = new System.Drawing.Point(27, 670);
            this.lblKickObserver.Name = "lblKickObserver";
            this.lblKickObserver.Size = new System.Drawing.Size(137, 20);
            this.lblKickObserver.TabIndex = 14;
            this.lblKickObserver.Text = "将旁观请出房间：";
            // 
            // cbxAllowRobotMakeTrump
            // 
            this.cbxAllowRobotMakeTrump.AutoSize = true;
            this.cbxAllowRobotMakeTrump.Enabled = false;
            this.cbxAllowRobotMakeTrump.Location = new System.Drawing.Point(31, 199);
            this.cbxAllowRobotMakeTrump.Name = "cbxAllowRobotMakeTrump";
            this.cbxAllowRobotMakeTrump.Size = new System.Drawing.Size(163, 24);
            this.cbxAllowRobotMakeTrump.TabIndex = 17;
            this.cbxAllowRobotMakeTrump.Text = "允许托管自动亮牌";
            this.cbxAllowRobotMakeTrump.UseVisualStyleBackColor = true;
            // 
            // lblReenterWaitSeconds
            // 
            this.lblReenterWaitSeconds.AutoSize = true;
            this.lblReenterWaitSeconds.Location = new System.Drawing.Point(27, 343);
            this.lblReenterWaitSeconds.Name = "lblReenterWaitSeconds";
            this.lblReenterWaitSeconds.Size = new System.Drawing.Size(226, 20);
            this.lblReenterWaitSeconds.TabIndex = 2;
            this.lblReenterWaitSeconds.Text = "断线重连等待时长（5-300秒）";
            // 
            // nudReenterWaitSeconds
            // 
            this.nudReenterWaitSeconds.Location = new System.Drawing.Point(394, 343);
            this.nudReenterWaitSeconds.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.nudReenterWaitSeconds.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudReenterWaitSeconds.Name = "nudReenterWaitSeconds";
            this.nudReenterWaitSeconds.Size = new System.Drawing.Size(121, 26);
            this.nudReenterWaitSeconds.TabIndex = 19;
            this.nudReenterWaitSeconds.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // FormRoomSetting
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(562, 797);
            this.Controls.Add(this.nudReenterWaitSeconds);
            this.Controls.Add(this.cbxAllowRobotMakeTrump);
            this.Controls.Add(this.btnKickObserver);
            this.Controls.Add(this.cbbKickObserver);
            this.Controls.Add(this.lblKickObserver);
            this.Controls.Add(this.btnKickPlayer);
            this.Controls.Add(this.cbbKickPlayer);
            this.Controls.Add(this.lblKickPlayer);
            this.Controls.Add(this.lblMustDoRanksLabel);
            this.Controls.Add(this.cbxMust_12);
            this.Controls.Add(this.lblRoomNameLabel);
            this.Controls.Add(this.cbxMust_7);
            this.Controls.Add(this.lblRoomOwner);
            this.Controls.Add(this.cbxMust_11);
            this.Controls.Add(this.lblRoomOwnerLabel);
            this.Controls.Add(this.cbxMust_6);
            this.Controls.Add(this.lblRoomName);
            this.Controls.Add(this.cbxMust_10);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.cbxMust_5);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbxMust_2);
            this.Controls.Add(this.cbxMust_9);
            this.Controls.Add(this.cbxAllowSurrender);
            this.Controls.Add(this.cbxMust_4);
            this.Controls.Add(this.cbxJToBottom);
            this.Controls.Add(this.cbxMust_1);
            this.Controls.Add(this.lblReenterWaitSeconds);
            this.Controls.Add(this.lblRiotByTrump);
            this.Controls.Add(this.cbxMust_8);
            this.Controls.Add(this.cbbRiotByTrump);
            this.Controls.Add(this.cbxMust_3);
            this.Controls.Add(this.cbbRiotByScore);
            this.Controls.Add(this.cbxMust_0);
            this.Controls.Add(this.lblAllowRiotByScore);
            this.Name = "FormRoomSetting";
            this.Text = "房间设置";
            ((System.ComponentModel.ISupportInitialize)(this.nudReenterWaitSeconds)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAllowRiotByScore;
        private System.Windows.Forms.ComboBox cbbRiotByScore;
        private System.Windows.Forms.Label lblRiotByTrump;
        private System.Windows.Forms.ComboBox cbbRiotByTrump;
        private System.Windows.Forms.CheckBox cbxJToBottom;
        private System.Windows.Forms.CheckBox cbxAllowSurrender;
        private System.Windows.Forms.CheckBox cbxMust_12;
        private System.Windows.Forms.CheckBox cbxMust_7;
        private System.Windows.Forms.CheckBox cbxMust_11;
        private System.Windows.Forms.CheckBox cbxMust_6;
        private System.Windows.Forms.CheckBox cbxMust_10;
        private System.Windows.Forms.CheckBox cbxMust_5;
        private System.Windows.Forms.CheckBox cbxMust_2;
        private System.Windows.Forms.CheckBox cbxMust_9;
        private System.Windows.Forms.CheckBox cbxMust_4;
        private System.Windows.Forms.CheckBox cbxMust_1;
        private System.Windows.Forms.CheckBox cbxMust_8;
        private System.Windows.Forms.CheckBox cbxMust_3;
        private System.Windows.Forms.CheckBox cbxMust_0;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblRoomName;
        private System.Windows.Forms.Label lblRoomOwnerLabel;
        private System.Windows.Forms.Label lblRoomOwner;
        private System.Windows.Forms.Label lblRoomNameLabel;
        private System.Windows.Forms.Label lblMustDoRanksLabel;
        private System.Windows.Forms.Label lblKickPlayer;
        private System.Windows.Forms.ComboBox cbbKickPlayer;
        private System.Windows.Forms.Button btnKickPlayer;
        private System.Windows.Forms.Button btnKickObserver;
        private System.Windows.Forms.ComboBox cbbKickObserver;
        private System.Windows.Forms.Label lblKickObserver;
        private System.Windows.Forms.CheckBox cbxAllowRobotMakeTrump;
        private System.Windows.Forms.Label lblReenterWaitSeconds;
        private System.Windows.Forms.NumericUpDown nudReenterWaitSeconds;
    }
}