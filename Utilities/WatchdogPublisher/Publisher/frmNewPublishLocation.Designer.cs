namespace Publisher
{
	partial class frmNewPublishLocation
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
			this.txtRemoteShare = new System.Windows.Forms.TextBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtPublishLocationName = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtUsername = new System.Windows.Forms.TextBox();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.chkRemoveShare = new System.Windows.Forms.CheckBox();
			this.cmbLocalDrive = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// txtRemoteShare
			// 
			this.txtRemoteShare.Location = new System.Drawing.Point(12, 63);
			this.txtRemoteShare.Name = "txtRemoteShare";
			this.txtRemoteShare.Size = new System.Drawing.Size(316, 20);
			this.txtRemoteShare.TabIndex = 13;
			this.txtRemoteShare.Text = "\\\\skynet\\publish";
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(107, 218);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(108, 25);
			this.btnCancel.TabIndex = 15;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 47);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Remote computer share";
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.Location = new System.Drawing.Point(221, 218);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(108, 25);
			this.btnOK.TabIndex = 14;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 7);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(116, 13);
			this.label2.TabIndex = 18;
			this.label2.Text = "Publish Location Name";
			// 
			// txtPublishLocationName
			// 
			this.txtPublishLocationName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtPublishLocationName.Location = new System.Drawing.Point(12, 23);
			this.txtPublishLocationName.Name = "txtPublishLocationName";
			this.txtPublishLocationName.Size = new System.Drawing.Size(317, 20);
			this.txtPublishLocationName.TabIndex = 10;
			this.txtPublishLocationName.Text = "skynet";
			this.txtPublishLocationName.TextChanged += new System.EventHandler(this.txtPublishLocationName_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 114);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(55, 13);
			this.label3.TabIndex = 20;
			this.label3.Text = "Username";
			// 
			// txtUsername
			// 
			this.txtUsername.Location = new System.Drawing.Point(12, 130);
			this.txtUsername.Name = "txtUsername";
			this.txtUsername.Size = new System.Drawing.Size(137, 20);
			this.txtUsername.TabIndex = 21;
			this.txtUsername.Text = "labuser";
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(192, 130);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.Size = new System.Drawing.Size(137, 20);
			this.txtPassword.TabIndex = 23;
			this.txtPassword.Text = "dgcee05";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(192, 114);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(53, 13);
			this.label4.TabIndex = 22;
			this.label4.Text = "Password";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 154);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(151, 13);
			this.label5.TabIndex = 24;
			this.label5.Text = "Preferred Local Drive Mapping";
			// 
			// chkRemoveShare
			// 
			this.chkRemoveShare.Checked = true;
			this.chkRemoveShare.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkRemoveShare.Location = new System.Drawing.Point(12, 192);
			this.chkRemoveShare.Name = "chkRemoveShare";
			this.chkRemoveShare.Size = new System.Drawing.Size(214, 22);
			this.chkRemoveShare.TabIndex = 26;
			this.chkRemoveShare.Text = "Remove Share After Program Closes";
			this.chkRemoveShare.UseVisualStyleBackColor = true;
			// 
			// cmbLocalDrive
			// 
			this.cmbLocalDrive.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbLocalDrive.FormattingEnabled = true;
			this.cmbLocalDrive.Location = new System.Drawing.Point(12, 170);
			this.cmbLocalDrive.Name = "cmbLocalDrive";
			this.cmbLocalDrive.Size = new System.Drawing.Size(137, 21);
			this.cmbLocalDrive.TabIndex = 27;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.ForeColor = System.Drawing.Color.Red;
			this.label6.Location = new System.Drawing.Point(12, 86);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(64, 13);
			this.label6.TabIndex = 28;
			this.label6.Text = "Important:";
			// 
			// label7
			// 
			this.label7.ForeColor = System.Drawing.Color.Black;
			this.label7.Location = new System.Drawing.Point(73, 86);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(256, 13);
			this.label7.TabIndex = 29;
			this.label7.Text = "The network share MUST have the path C:\\publish";
			// 
			// frmNewPublishLocation
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(341, 252);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.cmbLocalDrive);
			this.Controls.Add(this.chkRemoveShare);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.txtUsername);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtRemoteShare);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtPublishLocationName);
			this.Name = "frmNewPublishLocation";
			this.Text = "Create New Publish Location";
			this.Load += new System.EventHandler(this.frmNewPublishLocation_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox txtRemoteShare;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtPublishLocationName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtUsername;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox chkRemoveShare;
		private System.Windows.Forms.ComboBox cmbLocalDrive;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
	}
}