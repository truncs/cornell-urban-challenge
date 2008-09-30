namespace UrbanChallenge.OperationalUI {
	partial class formArcVoting {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.comboField = new System.Windows.Forms.ComboBox();
			this.arcVotingDisplay1 = new UrbanChallenge.OperationalUI.Controls.ArcVotingDisplay();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(498, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(81, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Selected Field:";
			// 
			// comboField
			// 
			this.comboField.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.comboField.FormattingEnabled = true;
			this.comboField.Location = new System.Drawing.Point(498, 28);
			this.comboField.Name = "comboField";
			this.comboField.Size = new System.Drawing.Size(191, 21);
			this.comboField.TabIndex = 2;
			this.comboField.SelectedIndexChanged += new System.EventHandler(this.comboField_SelectedIndexChanged);
			// 
			// arcVotingDisplay1
			// 
			this.arcVotingDisplay1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.arcVotingDisplay1.DataSource = "totalUtility";
			this.arcVotingDisplay1.Location = new System.Drawing.Point(12, 12);
			this.arcVotingDisplay1.Name = "arcVotingDisplay1";
			this.arcVotingDisplay1.Size = new System.Drawing.Size(480, 348);
			this.arcVotingDisplay1.TabIndex = 0;
			// 
			// formArcVoting
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(701, 372);
			this.Controls.Add(this.comboField);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.arcVotingDisplay1);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "formArcVoting";
			this.Text = "Sparc Voting";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formArcVoting_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private UrbanChallenge.OperationalUI.Controls.ArcVotingDisplay arcVotingDisplay1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboField;
	}
}