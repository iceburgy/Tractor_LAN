namespace Duan.Xiugang.Tractor
{
    partial class FormSettings
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
            this.lblHostName = new System.Windows.Forms.Label();
            this.tbxHostName = new System.Windows.Forms.TextBox();
            this.lblNickName = new System.Windows.Forms.Label();
            this.tbxNickName = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbxUpdateOnLoad = new System.Windows.Forms.CheckBox();
            this.cbxEnableSound = new System.Windows.Forms.CheckBox();
            this.cbxShowSuitSeq = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblHostName
            // 
            this.lblHostName.AutoSize = true;
            this.lblHostName.Location = new System.Drawing.Point(66, 78);
            this.lblHostName.Name = "lblHostName";
            this.lblHostName.Size = new System.Drawing.Size(89, 20);
            this.lblHostName.TabIndex = 0;
            this.lblHostName.Text = "服务器地址";
            // 
            // tbxHostName
            // 
            this.tbxHostName.Location = new System.Drawing.Point(173, 78);
            this.tbxHostName.Name = "tbxHostName";
            this.tbxHostName.Size = new System.Drawing.Size(157, 26);
            this.tbxHostName.TabIndex = 1;
            // 
            // lblNickName
            // 
            this.lblNickName.AutoSize = true;
            this.lblNickName.Location = new System.Drawing.Point(66, 135);
            this.lblNickName.Name = "lblNickName";
            this.lblNickName.Size = new System.Drawing.Size(73, 20);
            this.lblNickName.TabIndex = 2;
            this.lblNickName.Text = "玩家昵称";
            // 
            // tbxNickName
            // 
            this.tbxNickName.Location = new System.Drawing.Point(173, 129);
            this.tbxNickName.Name = "tbxNickName";
            this.tbxNickName.Size = new System.Drawing.Size(157, 26);
            this.tbxNickName.TabIndex = 3;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(70, 350);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(85, 34);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "保存";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(173, 350);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(82, 34);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cbxUpdateOnLoad
            // 
            this.cbxUpdateOnLoad.AutoSize = true;
            this.cbxUpdateOnLoad.Location = new System.Drawing.Point(70, 194);
            this.cbxUpdateOnLoad.Name = "cbxUpdateOnLoad";
            this.cbxUpdateOnLoad.Size = new System.Drawing.Size(147, 24);
            this.cbxUpdateOnLoad.TabIndex = 6;
            this.cbxUpdateOnLoad.Text = "启动时检查更新";
            this.cbxUpdateOnLoad.UseVisualStyleBackColor = true;
            // 
            // cbxEnableSound
            // 
            this.cbxEnableSound.AutoSize = true;
            this.cbxEnableSound.Location = new System.Drawing.Point(70, 247);
            this.cbxEnableSound.Name = "cbxEnableSound";
            this.cbxEnableSound.Size = new System.Drawing.Size(99, 24);
            this.cbxEnableSound.TabIndex = 7;
            this.cbxEnableSound.Text = "开启音效";
            this.cbxEnableSound.UseVisualStyleBackColor = true;
            // 
            // cbxShowSuitSeq
            // 
            this.cbxShowSuitSeq.AutoSize = true;
            this.cbxShowSuitSeq.Location = new System.Drawing.Point(70, 295);
            this.cbxShowSuitSeq.Name = "cbxShowSuitSeq";
            this.cbxShowSuitSeq.Size = new System.Drawing.Size(131, 24);
            this.cbxShowSuitSeq.TabIndex = 8;
            this.cbxShowSuitSeq.Text = "显示手牌张数";
            this.cbxShowSuitSeq.UseVisualStyleBackColor = true;
            // 
            // FormSettings
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(422, 417);
            this.Controls.Add(this.cbxShowSuitSeq);
            this.Controls.Add(this.cbxEnableSound);
            this.Controls.Add(this.cbxUpdateOnLoad);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tbxNickName);
            this.Controls.Add(this.lblNickName);
            this.Controls.Add(this.tbxHostName);
            this.Controls.Add(this.lblHostName);
            this.Name = "FormSettings";
            this.Text = "FormSettings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblHostName;
        private System.Windows.Forms.TextBox tbxHostName;
        private System.Windows.Forms.Label lblNickName;
        private System.Windows.Forms.TextBox tbxNickName;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbxUpdateOnLoad;
        private System.Windows.Forms.CheckBox cbxEnableSound;
        private System.Windows.Forms.CheckBox cbxShowSuitSeq;
    }
}