﻿namespace Duan.Xiugang.Tractor
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
            this.btnOK.Location = new System.Drawing.Point(70, 201);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(85, 34);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "保存";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(173, 201);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(82, 34);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FormSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 298);
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
    }
}