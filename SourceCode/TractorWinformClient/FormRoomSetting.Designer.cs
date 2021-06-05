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
            this.gpbMustDoRank = new System.Windows.Forms.GroupBox();
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
            this.gpbMustDoRank.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAllowRiotByScore
            // 
            this.lblAllowRiotByScore.AutoSize = true;
            this.lblAllowRiotByScore.Location = new System.Drawing.Point(24, 154);
            this.lblAllowRiotByScore.Name = "lblAllowRiotByScore";
            this.lblAllowRiotByScore.Size = new System.Drawing.Size(164, 20);
            this.lblAllowRiotByScore.TabIndex = 0;
            this.lblAllowRiotByScore.Text = "允许分数低于N时革命";
            // 
            // cbbRiotByScore
            // 
            this.cbbRiotByScore.DisplayMember = "0";
            this.cbbRiotByScore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbRiotByScore.FormattingEnabled = true;
            this.cbbRiotByScore.Items.AddRange(new object[] {
            "0",
            "5",
            "10",
            "15"});
            this.cbbRiotByScore.Location = new System.Drawing.Point(391, 146);
            this.cbbRiotByScore.Name = "cbbRiotByScore";
            this.cbbRiotByScore.Size = new System.Drawing.Size(121, 28);
            this.cbbRiotByScore.TabIndex = 1;
            // 
            // lblRiotByTrump
            // 
            this.lblRiotByTrump.AutoSize = true;
            this.lblRiotByTrump.Location = new System.Drawing.Point(24, 210);
            this.lblRiotByTrump.Name = "lblRiotByTrump";
            this.lblRiotByTrump.Size = new System.Drawing.Size(276, 20);
            this.lblRiotByTrump.TabIndex = 2;
            this.lblRiotByTrump.Text = "允许主牌少于N时革命（打无主不算）";
            // 
            // cbbRiotByTrump
            // 
            this.cbbRiotByTrump.DisplayMember = "0";
            this.cbbRiotByTrump.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbRiotByTrump.FormattingEnabled = true;
            this.cbbRiotByTrump.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4"});
            this.cbbRiotByTrump.Location = new System.Drawing.Point(391, 207);
            this.cbbRiotByTrump.Name = "cbbRiotByTrump";
            this.cbbRiotByTrump.Size = new System.Drawing.Size(121, 28);
            this.cbbRiotByTrump.TabIndex = 1;
            // 
            // cbxJToBottom
            // 
            this.cbxJToBottom.AutoSize = true;
            this.cbxJToBottom.Location = new System.Drawing.Point(28, 98);
            this.cbxJToBottom.Name = "cbxJToBottom";
            this.cbxJToBottom.Size = new System.Drawing.Size(267, 24);
            this.cbxJToBottom.TabIndex = 3;
            this.cbxJToBottom.Text = "允许J到底（主J到底，副J一半）";
            this.cbxJToBottom.UseVisualStyleBackColor = true;
            // 
            // cbxAllowSurrender
            // 
            this.cbxAllowSurrender.AutoSize = true;
            this.cbxAllowSurrender.Location = new System.Drawing.Point(28, 41);
            this.cbxAllowSurrender.Name = "cbxAllowSurrender";
            this.cbxAllowSurrender.Size = new System.Drawing.Size(323, 24);
            this.cbxAllowSurrender.TabIndex = 3;
            this.cbxAllowSurrender.Text = "允许投降（投降后对方上台或者升一级）";
            this.cbxAllowSurrender.UseVisualStyleBackColor = true;
            // 
            // gpbMustDoRank
            // 
            this.gpbMustDoRank.Controls.Add(this.cbxMust_12);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_7);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_11);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_6);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_10);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_5);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_2);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_9);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_4);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_1);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_8);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_3);
            this.gpbMustDoRank.Controls.Add(this.cbxMust_0);
            this.gpbMustDoRank.Location = new System.Drawing.Point(28, 273);
            this.gpbMustDoRank.Name = "gpbMustDoRank";
            this.gpbMustDoRank.Size = new System.Drawing.Size(484, 206);
            this.gpbMustDoRank.TabIndex = 4;
            this.gpbMustDoRank.TabStop = false;
            this.gpbMustDoRank.Text = "必打";
            // 
            // cbxMust_12
            // 
            this.cbxMust_12.AutoSize = true;
            this.cbxMust_12.Location = new System.Drawing.Point(131, 157);
            this.cbxMust_12.Name = "cbxMust_12";
            this.cbxMust_12.Size = new System.Drawing.Size(46, 24);
            this.cbxMust_12.TabIndex = 0;
            this.cbxMust_12.Text = "A";
            this.cbxMust_12.UseVisualStyleBackColor = true;
            // 
            // cbxMust_7
            // 
            this.cbxMust_7.AutoSize = true;
            this.cbxMust_7.Location = new System.Drawing.Point(68, 157);
            this.cbxMust_7.Name = "cbxMust_7";
            this.cbxMust_7.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_7.TabIndex = 0;
            this.cbxMust_7.Text = "9";
            this.cbxMust_7.UseVisualStyleBackColor = true;
            // 
            // cbxMust_11
            // 
            this.cbxMust_11.AutoSize = true;
            this.cbxMust_11.Location = new System.Drawing.Point(131, 127);
            this.cbxMust_11.Name = "cbxMust_11";
            this.cbxMust_11.Size = new System.Drawing.Size(45, 24);
            this.cbxMust_11.TabIndex = 0;
            this.cbxMust_11.Text = "K";
            this.cbxMust_11.UseVisualStyleBackColor = true;
            // 
            // cbxMust_6
            // 
            this.cbxMust_6.AutoSize = true;
            this.cbxMust_6.Location = new System.Drawing.Point(68, 127);
            this.cbxMust_6.Name = "cbxMust_6";
            this.cbxMust_6.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_6.TabIndex = 0;
            this.cbxMust_6.Text = "8";
            this.cbxMust_6.UseVisualStyleBackColor = true;
            // 
            // cbxMust_10
            // 
            this.cbxMust_10.AutoSize = true;
            this.cbxMust_10.Location = new System.Drawing.Point(131, 97);
            this.cbxMust_10.Name = "cbxMust_10";
            this.cbxMust_10.Size = new System.Drawing.Size(47, 24);
            this.cbxMust_10.TabIndex = 0;
            this.cbxMust_10.Text = "Q";
            this.cbxMust_10.UseVisualStyleBackColor = true;
            // 
            // cbxMust_5
            // 
            this.cbxMust_5.AutoSize = true;
            this.cbxMust_5.Location = new System.Drawing.Point(68, 97);
            this.cbxMust_5.Name = "cbxMust_5";
            this.cbxMust_5.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_5.TabIndex = 0;
            this.cbxMust_5.Text = "7";
            this.cbxMust_5.UseVisualStyleBackColor = true;
            // 
            // cbxMust_2
            // 
            this.cbxMust_2.AutoSize = true;
            this.cbxMust_2.Location = new System.Drawing.Point(7, 97);
            this.cbxMust_2.Name = "cbxMust_2";
            this.cbxMust_2.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_2.TabIndex = 0;
            this.cbxMust_2.Text = "4";
            this.cbxMust_2.UseVisualStyleBackColor = true;
            // 
            // cbxMust_9
            // 
            this.cbxMust_9.AutoSize = true;
            this.cbxMust_9.Location = new System.Drawing.Point(131, 66);
            this.cbxMust_9.Name = "cbxMust_9";
            this.cbxMust_9.Size = new System.Drawing.Size(43, 24);
            this.cbxMust_9.TabIndex = 0;
            this.cbxMust_9.Text = "J";
            this.cbxMust_9.UseVisualStyleBackColor = true;
            // 
            // cbxMust_4
            // 
            this.cbxMust_4.AutoSize = true;
            this.cbxMust_4.Location = new System.Drawing.Point(68, 66);
            this.cbxMust_4.Name = "cbxMust_4";
            this.cbxMust_4.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_4.TabIndex = 0;
            this.cbxMust_4.Text = "6";
            this.cbxMust_4.UseVisualStyleBackColor = true;
            // 
            // cbxMust_1
            // 
            this.cbxMust_1.AutoSize = true;
            this.cbxMust_1.Location = new System.Drawing.Point(7, 66);
            this.cbxMust_1.Name = "cbxMust_1";
            this.cbxMust_1.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_1.TabIndex = 0;
            this.cbxMust_1.Text = "3";
            this.cbxMust_1.UseVisualStyleBackColor = true;
            // 
            // cbxMust_8
            // 
            this.cbxMust_8.AutoSize = true;
            this.cbxMust_8.Location = new System.Drawing.Point(131, 36);
            this.cbxMust_8.Name = "cbxMust_8";
            this.cbxMust_8.Size = new System.Drawing.Size(53, 24);
            this.cbxMust_8.TabIndex = 0;
            this.cbxMust_8.Text = "10";
            this.cbxMust_8.UseVisualStyleBackColor = true;
            // 
            // cbxMust_3
            // 
            this.cbxMust_3.AutoSize = true;
            this.cbxMust_3.Location = new System.Drawing.Point(68, 36);
            this.cbxMust_3.Name = "cbxMust_3";
            this.cbxMust_3.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_3.TabIndex = 0;
            this.cbxMust_3.Text = "5";
            this.cbxMust_3.UseVisualStyleBackColor = true;
            // 
            // cbxMust_0
            // 
            this.cbxMust_0.AutoSize = true;
            this.cbxMust_0.Location = new System.Drawing.Point(7, 36);
            this.cbxMust_0.Name = "cbxMust_0";
            this.cbxMust_0.Size = new System.Drawing.Size(44, 24);
            this.cbxMust_0.TabIndex = 0;
            this.cbxMust_0.Text = "2";
            this.cbxMust_0.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(28, 510);
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
            this.btnCancel.Location = new System.Drawing.Point(197, 510);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(98, 32);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FormRoomSetting
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(562, 579);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gpbMustDoRank);
            this.Controls.Add(this.cbxAllowSurrender);
            this.Controls.Add(this.cbxJToBottom);
            this.Controls.Add(this.lblRiotByTrump);
            this.Controls.Add(this.cbbRiotByTrump);
            this.Controls.Add(this.cbbRiotByScore);
            this.Controls.Add(this.lblAllowRiotByScore);
            this.Name = "FormRoomSetting";
            this.Text = "FormRoomSetting";
            this.gpbMustDoRank.ResumeLayout(false);
            this.gpbMustDoRank.PerformLayout();
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
        private System.Windows.Forms.GroupBox gpbMustDoRank;
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
    }
}