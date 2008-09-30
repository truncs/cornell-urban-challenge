namespace Publisher
{
	partial class frmNewPublish
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
			this.label3 = new System.Windows.Forms.Label();
			this.lstPublishFiles = new System.Windows.Forms.ListBox();
			this.txtPublishName = new System.Windows.Forms.TextBox();
			this.btnAddFiles = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtRelativeLocation = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.openFD = new System.Windows.Forms.OpenFileDialog();
			this.btnRemoveFiles = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.txtWatchdogName = new System.Windows.Forms.TextBox();
			this.chkEnableWatchdog = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.cmbWatchdogPeriod = new System.Windows.Forms.ComboBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.btnEditCommand = new System.Windows.Forms.Button();
			this.lstCommands = new System.Windows.Forms.ListBox();
			this.txtNewCommand = new System.Windows.Forms.TextBox();
			this.btnRemoveCommand = new System.Windows.Forms.Button();
			this.btnAddCommand = new System.Windows.Forms.Button();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.label7 = new System.Windows.Forms.Label();
			this.lstWatchdogs = new System.Windows.Forms.ListBox();
			this.btnEditWatchdog = new System.Windows.Forms.Button();
			this.btnAddWatchdog = new System.Windows.Forms.Button();
			this.btnRemoveWatchdog = new System.Windows.Forms.Button();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(65, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Publish Files";
			// 
			// lstPublishFiles
			// 
			this.lstPublishFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.lstPublishFiles.FormattingEnabled = true;
			this.lstPublishFiles.HorizontalScrollbar = true;
			this.lstPublishFiles.Location = new System.Drawing.Point(3, 16);
			this.lstPublishFiles.Name = "lstPublishFiles";
			this.lstPublishFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lstPublishFiles.Size = new System.Drawing.Size(264, 56);
			this.lstPublishFiles.TabIndex = 1;
			this.lstPublishFiles.DoubleClick += new System.EventHandler(this.lstPublishFiles_DoubleClick);
			this.lstPublishFiles.SelectedIndexChanged += new System.EventHandler(this.lstPublishFiles_SelectedIndexChanged);
			// 
			// txtPublishName
			// 
			this.txtPublishName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtPublishName.Location = new System.Drawing.Point(12, 23);
			this.txtPublishName.Name = "txtPublishName";
			this.txtPublishName.Size = new System.Drawing.Size(268, 20);
			this.txtPublishName.TabIndex = 0;
			this.txtPublishName.Text = "My Publish 1";
			this.txtPublishName.TextChanged += new System.EventHandler(this.txtPublishName_TextChanged);
			// 
			// btnAddFiles
			// 
			this.btnAddFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddFiles.Location = new System.Drawing.Point(156, 89);
			this.btnAddFiles.Name = "btnAddFiles";
			this.btnAddFiles.Size = new System.Drawing.Size(108, 25);
			this.btnAddFiles.TabIndex = 2;
			this.btnAddFiles.Text = "Add Files...";
			this.btnAddFiles.UseVisualStyleBackColor = true;
			this.btnAddFiles.Click += new System.EventHandler(this.btnAddFiles_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 7);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Publish Name";
			// 
			// txtRelativeLocation
			// 
			this.txtRelativeLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtRelativeLocation.Location = new System.Drawing.Point(18, 519);
			this.txtRelativeLocation.Name = "txtRelativeLocation";
			this.txtRelativeLocation.Size = new System.Drawing.Size(267, 20);
			this.txtRelativeLocation.TabIndex = 3;
			this.txtRelativeLocation.Text = "\\";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(15, 503);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(89, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "Remote Directory";
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.Location = new System.Drawing.Point(172, 545);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(108, 25);
			this.btnOK.TabIndex = 4;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(58, 545);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(108, 25);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// openFD
			// 
			this.openFD.DefaultExt = "*.exe";
			this.openFD.Multiselect = true;
			this.openFD.Title = "Select Files to Publish....";
			// 
			// btnRemoveFiles
			// 
			this.btnRemoveFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRemoveFiles.Location = new System.Drawing.Point(42, 89);
			this.btnRemoveFiles.Name = "btnRemoveFiles";
			this.btnRemoveFiles.Size = new System.Drawing.Size(108, 25);
			this.btnRemoveFiles.TabIndex = 9;
			this.btnRemoveFiles.Text = "Remove Selected";
			this.btnRemoveFiles.UseVisualStyleBackColor = true;
			this.btnRemoveFiles.Click += new System.EventHandler(this.btnRemoveFiles_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 9);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(59, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "Commands";
			// 
			// txtWatchdogName
			// 
			this.txtWatchdogName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtWatchdogName.Location = new System.Drawing.Point(9, 79);
			this.txtWatchdogName.Name = "txtWatchdogName";
			this.txtWatchdogName.Size = new System.Drawing.Size(192, 20);
			this.txtWatchdogName.TabIndex = 13;
			this.txtWatchdogName.Text = "watchdog";
			// 
			// chkEnableWatchdog
			// 
			this.chkEnableWatchdog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.chkEnableWatchdog.AutoSize = true;
			this.chkEnableWatchdog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.chkEnableWatchdog.Location = new System.Drawing.Point(147, 135);
			this.chkEnableWatchdog.Name = "chkEnableWatchdog";
			this.chkEnableWatchdog.Size = new System.Drawing.Size(115, 17);
			this.chkEnableWatchdog.TabIndex = 14;
			this.chkEnableWatchdog.Text = "Watchdog Enabled";
			this.chkEnableWatchdog.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 137);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(37, 13);
			this.label6.TabIndex = 15;
			this.label6.Text = "Period";
			// 
			// cmbWatchdogPeriod
			// 
			this.cmbWatchdogPeriod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cmbWatchdogPeriod.FormattingEnabled = true;
			this.cmbWatchdogPeriod.Items.AddRange(new object[] {
            "100",
            "500",
            "1000",
            "2000",
            "5000"});
			this.cmbWatchdogPeriod.Location = new System.Drawing.Point(50, 133);
			this.cmbWatchdogPeriod.Name = "cmbWatchdogPeriod";
			this.cmbWatchdogPeriod.Size = new System.Drawing.Size(94, 21);
			this.cmbWatchdogPeriod.TabIndex = 16;
			this.cmbWatchdogPeriod.Text = "500";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(11, 49);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.label3);
			this.splitContainer1.Panel1.Controls.Add(this.lstPublishFiles);
			this.splitContainer1.Panel1.Controls.Add(this.btnRemoveFiles);
			this.splitContainer1.Panel1.Controls.Add(this.btnAddFiles);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(270, 451);
			this.splitContainer1.SplitterDistance = 119;
			this.splitContainer1.TabIndex = 18;
			// 
			// btnEditCommand
			// 
			this.btnEditCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnEditCommand.Location = new System.Drawing.Point(225, 104);
			this.btnEditCommand.Name = "btnEditCommand";
			this.btnEditCommand.Size = new System.Drawing.Size(40, 20);
			this.btnEditCommand.TabIndex = 10;
			this.btnEditCommand.Text = "Edit";
			this.btnEditCommand.UseVisualStyleBackColor = true;
			this.btnEditCommand.Click += new System.EventHandler(this.btnEditCommand_Click);
			// 
			// lstCommands
			// 
			this.lstCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.lstCommands.FormattingEnabled = true;
			this.lstCommands.HorizontalScrollbar = true;
			this.lstCommands.Location = new System.Drawing.Point(10, 25);
			this.lstCommands.Name = "lstCommands";
			this.lstCommands.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lstCommands.Size = new System.Drawing.Size(255, 69);
			this.lstCommands.TabIndex = 10;
			this.lstCommands.SelectedIndexChanged += new System.EventHandler(this.lstCommands_SelectedIndexChanged);
			// 
			// txtNewCommand
			// 
			this.txtNewCommand.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtNewCommand.Location = new System.Drawing.Point(10, 104);
			this.txtNewCommand.Name = "txtNewCommand";
			this.txtNewCommand.Size = new System.Drawing.Size(211, 20);
			this.txtNewCommand.TabIndex = 12;
			this.txtNewCommand.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtNewCommand_KeyUp);
			this.txtNewCommand.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtNewCommand_KeyPress);
			this.txtNewCommand.TextChanged += new System.EventHandler(this.txtNewCommand_TextChanged);
			// 
			// btnRemoveCommand
			// 
			this.btnRemoveCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRemoveCommand.Location = new System.Drawing.Point(43, 130);
			this.btnRemoveCommand.Name = "btnRemoveCommand";
			this.btnRemoveCommand.Size = new System.Drawing.Size(108, 25);
			this.btnRemoveCommand.TabIndex = 11;
			this.btnRemoveCommand.Text = "Remove Selected";
			this.btnRemoveCommand.UseVisualStyleBackColor = true;
			this.btnRemoveCommand.Click += new System.EventHandler(this.btnRemoveCommand_Click);
			// 
			// btnAddCommand
			// 
			this.btnAddCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddCommand.Location = new System.Drawing.Point(157, 130);
			this.btnAddCommand.Name = "btnAddCommand";
			this.btnAddCommand.Size = new System.Drawing.Size(108, 25);
			this.btnAddCommand.TabIndex = 10;
			this.btnAddCommand.Text = "Add Command";
			this.btnAddCommand.UseVisualStyleBackColor = true;
			this.btnAddCommand.Click += new System.EventHandler(this.btnAddCommand_Click);
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.lstCommands);
			this.splitContainer2.Panel1.Controls.Add(this.btnEditCommand);
			this.splitContainer2.Panel1.Controls.Add(this.btnAddCommand);
			this.splitContainer2.Panel1.Controls.Add(this.label4);
			this.splitContainer2.Panel1.Controls.Add(this.txtNewCommand);
			this.splitContainer2.Panel1.Controls.Add(this.btnRemoveCommand);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.btnAddWatchdog);
			this.splitContainer2.Panel2.Controls.Add(this.btnRemoveWatchdog);
			this.splitContainer2.Panel2.Controls.Add(this.btnEditWatchdog);
			this.splitContainer2.Panel2.Controls.Add(this.lstWatchdogs);
			this.splitContainer2.Panel2.Controls.Add(this.chkEnableWatchdog);
			this.splitContainer2.Panel2.Controls.Add(this.label7);
			this.splitContainer2.Panel2.Controls.Add(this.cmbWatchdogPeriod);
			this.splitContainer2.Panel2.Controls.Add(this.txtWatchdogName);
			this.splitContainer2.Panel2.Controls.Add(this.label6);
			this.splitContainer2.Size = new System.Drawing.Size(270, 328);
			this.splitContainer2.SplitterDistance = 164;
			this.splitContainer2.TabIndex = 13;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(4, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(57, 13);
			this.label7.TabIndex = 14;
			this.label7.Text = "Watchdog";
			// 
			// lstWatchdogs
			// 
			this.lstWatchdogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.lstWatchdogs.FormattingEnabled = true;
			this.lstWatchdogs.HorizontalScrollbar = true;
			this.lstWatchdogs.Location = new System.Drawing.Point(9, 13);
			this.lstWatchdogs.Name = "lstWatchdogs";
			this.lstWatchdogs.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lstWatchdogs.Size = new System.Drawing.Size(255, 56);
			this.lstWatchdogs.TabIndex = 13;
			this.lstWatchdogs.SelectedIndexChanged += new System.EventHandler(this.lstWatchdogs_SelectedIndexChanged);
			// 
			// btnEditWatchdog
			// 
			this.btnEditWatchdog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnEditWatchdog.Location = new System.Drawing.Point(207, 78);
			this.btnEditWatchdog.Name = "btnEditWatchdog";
			this.btnEditWatchdog.Size = new System.Drawing.Size(57, 20);
			this.btnEditWatchdog.TabIndex = 17;
			this.btnEditWatchdog.Text = "Edit";
			this.btnEditWatchdog.UseVisualStyleBackColor = true;
			this.btnEditWatchdog.Click += new System.EventHandler(this.btnEditWatchdog_Click);
			// 
			// btnAddWatchdog
			// 
			this.btnAddWatchdog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddWatchdog.Location = new System.Drawing.Point(156, 104);
			this.btnAddWatchdog.Name = "btnAddWatchdog";
			this.btnAddWatchdog.Size = new System.Drawing.Size(108, 25);
			this.btnAddWatchdog.TabIndex = 13;
			this.btnAddWatchdog.Text = "Add Watchdog";
			this.btnAddWatchdog.UseVisualStyleBackColor = true;
			this.btnAddWatchdog.Click += new System.EventHandler(this.btnAddWatchdog_Click);
			// 
			// btnRemoveWatchdog
			// 
			this.btnRemoveWatchdog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRemoveWatchdog.Location = new System.Drawing.Point(42, 104);
			this.btnRemoveWatchdog.Name = "btnRemoveWatchdog";
			this.btnRemoveWatchdog.Size = new System.Drawing.Size(108, 25);
			this.btnRemoveWatchdog.TabIndex = 14;
			this.btnRemoveWatchdog.Text = "Remove Selected";
			this.btnRemoveWatchdog.UseVisualStyleBackColor = true;
			this.btnRemoveWatchdog.Click += new System.EventHandler(this.btnRemoveWatchdog_Click);
			// 
			// frmNewPublish
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(286, 576);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.txtRelativeLocation);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtPublishName);
			this.MinimumSize = new System.Drawing.Size(217, 228);
			this.Name = "frmNewPublish";
			this.Text = "Create New Publish";
			this.Load += new System.EventHandler(this.frmNewPublish_Load);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel1.PerformLayout();
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.Panel2.PerformLayout();
			this.splitContainer2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListBox lstPublishFiles;
		private System.Windows.Forms.TextBox txtPublishName;
		private System.Windows.Forms.Button btnAddFiles;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtRelativeLocation;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.OpenFileDialog openFD;
		private System.Windows.Forms.Button btnRemoveFiles;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtWatchdogName;
		private System.Windows.Forms.CheckBox chkEnableWatchdog;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox cmbWatchdogPeriod;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListBox lstCommands;
		private System.Windows.Forms.TextBox txtNewCommand;
		private System.Windows.Forms.Button btnRemoveCommand;
		private System.Windows.Forms.Button btnAddCommand;
		private System.Windows.Forms.Button btnEditCommand;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button btnAddWatchdog;
		private System.Windows.Forms.Button btnRemoveWatchdog;
		private System.Windows.Forms.Button btnEditWatchdog;
		private System.Windows.Forms.ListBox lstWatchdogs;
	}
}