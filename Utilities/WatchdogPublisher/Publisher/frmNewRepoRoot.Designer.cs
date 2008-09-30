namespace Publisher
{
	partial class frmNewRepoRoot
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
			this.label1 = new System.Windows.Forms.Label();
			this.txtRepoRoot = new System.Windows.Forms.TextBox();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.label2 = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnChangeRoot = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(336, 27);
			this.label1.TabIndex = 0;
			this.label1.Text = "It looks like you haven\'t set your repository location. You\'ll need to do this be" +
					"fore you can publish.";
			// 
			// txtRepoRoot
			// 
			this.txtRepoRoot.Location = new System.Drawing.Point(12, 57);
			this.txtRepoRoot.Name = "txtRepoRoot";
			this.txtRepoRoot.Size = new System.Drawing.Size(285, 20);
			this.txtRepoRoot.TabIndex = 1;
			this.txtRepoRoot.Text = "c:\\repo";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 41);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(175, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Currently Selected Repository Root:";
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(129, 108);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(106, 25);
			this.btnOK.TabIndex = 3;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnChangeRoot
			// 
			this.btnChangeRoot.Location = new System.Drawing.Point(303, 57);
			this.btnChangeRoot.Name = "btnChangeRoot";
			this.btnChangeRoot.Size = new System.Drawing.Size(63, 20);
			this.btnChangeRoot.TabIndex = 4;
			this.btnChangeRoot.Text = "Browse...";
			this.btnChangeRoot.UseVisualStyleBackColor = true;
			this.btnChangeRoot.Click += new System.EventHandler(this.btnChangeRoot_Click);
			// 
			// frmNewRepoRoot
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(369, 145);
			this.ControlBox = false;
			this.Controls.Add(this.btnChangeRoot);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtRepoRoot);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "frmNewRepoRoot";
			this.Text = "Where is your Repository Root?";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtRepoRoot;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnChangeRoot;
	}
}