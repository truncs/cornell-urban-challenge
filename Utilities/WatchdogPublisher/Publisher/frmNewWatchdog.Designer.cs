namespace Publisher
{
	partial class frmNewWatchdog
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
			this.btnRemoveFiles = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.lstPublishFiles = new System.Windows.Forms.ListBox();
			this.btnAddFiles = new System.Windows.Forms.Button();
			this.openFD = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			// 
			// btnRemoveFiles
			// 
			this.btnRemoveFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRemoveFiles.Location = new System.Drawing.Point(146, 216);
			this.btnRemoveFiles.Name = "btnRemoveFiles";
			this.btnRemoveFiles.Size = new System.Drawing.Size(108, 25);
			this.btnRemoveFiles.TabIndex = 21;
			this.btnRemoveFiles.Text = "Remove Selected";
			this.btnRemoveFiles.UseVisualStyleBackColor = true;
			this.btnRemoveFiles.Click += new System.EventHandler(this.btnRemoveFiles_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(146, 268);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(108, 25);
			this.btnCancel.TabIndex = 17;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.Location = new System.Drawing.Point(259, 268);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(108, 25);
			this.btnOK.TabIndex = 16;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 11);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(81, 13);
			this.label3.TabIndex = 19;
			this.label3.Text = "Watchdog Files";
			// 
			// lstPublishFiles
			// 
			this.lstPublishFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.lstPublishFiles.FormattingEnabled = true;
			this.lstPublishFiles.HorizontalScrollbar = true;
			this.lstPublishFiles.Location = new System.Drawing.Point(12, 27);
			this.lstPublishFiles.Name = "lstPublishFiles";
			this.lstPublishFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lstPublishFiles.Size = new System.Drawing.Size(355, 186);
			this.lstPublishFiles.TabIndex = 13;
			// 
			// btnAddFiles
			// 
			this.btnAddFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddFiles.Location = new System.Drawing.Point(260, 216);
			this.btnAddFiles.Name = "btnAddFiles";
			this.btnAddFiles.Size = new System.Drawing.Size(108, 25);
			this.btnAddFiles.TabIndex = 14;
			this.btnAddFiles.Text = "Add Files...";
			this.btnAddFiles.UseVisualStyleBackColor = true;
			this.btnAddFiles.Click += new System.EventHandler(this.btnAddFiles_Click);
			// 
			// openFD
			// 
			this.openFD.DefaultExt = "*.exe";
			this.openFD.Multiselect = true;
			this.openFD.Title = "Select Files to Publish....";
			// 
			// frmNewWatchdog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(380, 295);
			this.Controls.Add(this.btnRemoveFiles);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.lstPublishFiles);
			this.Controls.Add(this.btnAddFiles);
			this.Name = "frmNewWatchdog";
			this.Text = "New Watchdog Deployment";
			this.Load += new System.EventHandler(this.frmNewWatchdog_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnRemoveFiles;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListBox lstPublishFiles;
		private System.Windows.Forms.Button btnAddFiles;
		private System.Windows.Forms.OpenFileDialog openFD;
	}
}