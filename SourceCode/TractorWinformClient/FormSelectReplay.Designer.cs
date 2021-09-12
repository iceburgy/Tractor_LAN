namespace Duan.Xiugang.Tractor
{
    partial class FormSelectReplay
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
            this.lblSelectReplay = new System.Windows.Forms.Label();
            this.lblReplayDate = new System.Windows.Forms.Label();
            this.lblReplayFile = new System.Windows.Forms.Label();
            this.cbbReplayDate = new System.Windows.Forms.ComboBox();
            this.cbbReplayName = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblSelectReplay
            // 
            this.lblSelectReplay.AutoSize = true;
            this.lblSelectReplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSelectReplay.Location = new System.Drawing.Point(36, 33);
            this.lblSelectReplay.Name = "lblSelectReplay";
            this.lblSelectReplay.Size = new System.Drawing.Size(163, 29);
            this.lblSelectReplay.TabIndex = 0;
            this.lblSelectReplay.Text = "选择录像回放";
            // 
            // lblReplayDate
            // 
            this.lblReplayDate.AutoSize = true;
            this.lblReplayDate.Location = new System.Drawing.Point(40, 82);
            this.lblReplayDate.Name = "lblReplayDate";
            this.lblReplayDate.Size = new System.Drawing.Size(73, 20);
            this.lblReplayDate.TabIndex = 1;
            this.lblReplayDate.Text = "录像日期";
            // 
            // lblReplayFile
            // 
            this.lblReplayFile.AutoSize = true;
            this.lblReplayFile.Location = new System.Drawing.Point(287, 82);
            this.lblReplayFile.Name = "lblReplayFile";
            this.lblReplayFile.Size = new System.Drawing.Size(73, 20);
            this.lblReplayFile.TabIndex = 2;
            this.lblReplayFile.Text = "录像文件";
            // 
            // cbbReplayDate
            // 
            this.cbbReplayDate.FormattingEnabled = true;
            this.cbbReplayDate.Location = new System.Drawing.Point(40, 138);
            this.cbbReplayDate.Name = "cbbReplayDate";
            this.cbbReplayDate.Size = new System.Drawing.Size(194, 28);
            this.cbbReplayDate.TabIndex = 3;
            // 
            // cbbReplayName
            // 
            this.cbbReplayName.FormattingEnabled = true;
            this.cbbReplayName.Location = new System.Drawing.Point(291, 138);
            this.cbbReplayName.Name = "cbbReplayName";
            this.cbbReplayName.Size = new System.Drawing.Size(312, 28);
            this.cbbReplayName.TabIndex = 4;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(40, 205);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(86, 37);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(291, 205);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(86, 37);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // FormSelectReplay
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(661, 277);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbbReplayName);
            this.Controls.Add(this.cbbReplayDate);
            this.Controls.Add(this.lblReplayFile);
            this.Controls.Add(this.lblReplayDate);
            this.Controls.Add(this.lblSelectReplay);
            this.Name = "FormSelectReplay";
            this.Text = "SelectReplay";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSelectReplay;
        private System.Windows.Forms.Label lblReplayDate;
        private System.Windows.Forms.Label lblReplayFile;
        internal System.Windows.Forms.ComboBox cbbReplayDate;
        internal System.Windows.Forms.ComboBox cbbReplayName;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}