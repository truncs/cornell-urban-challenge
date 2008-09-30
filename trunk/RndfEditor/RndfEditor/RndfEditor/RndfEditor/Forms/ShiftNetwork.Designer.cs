namespace RndfEditor.Forms
{
	partial class ShiftNetwork
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShiftNetwork));
			this.shiftNetworkEastLabel = new System.Windows.Forms.Label();
			this.ShiftNetworkEastTextBox = new System.Windows.Forms.TextBox();
			this.shiftAmountNorthLabel = new System.Windows.Forms.Label();
			this.ShiftNetworkNorthTextBox = new System.Windows.Forms.TextBox();
			this.ShiftNetworkCancelButton = new System.Windows.Forms.Button();
			this.ShiftNetworkOkButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// shiftNetworkEastLabel
			// 
			this.shiftNetworkEastLabel.AutoSize = true;
			this.shiftNetworkEastLabel.Location = new System.Drawing.Point(12, 9);
			this.shiftNetworkEastLabel.Name = "shiftNetworkEastLabel";
			this.shiftNetworkEastLabel.Size = new System.Drawing.Size(108, 13);
			this.shiftNetworkEastLabel.TabIndex = 0;
			this.shiftNetworkEastLabel.Text = "Shift Amount East (m)";
			// 
			// ShiftNetworkEastTextBox
			// 
			this.ShiftNetworkEastTextBox.Location = new System.Drawing.Point(15, 30);
			this.ShiftNetworkEastTextBox.Name = "ShiftNetworkEastTextBox";
			this.ShiftNetworkEastTextBox.Size = new System.Drawing.Size(100, 20);
			this.ShiftNetworkEastTextBox.TabIndex = 1;
			// 
			// shiftAmountNorthLabel
			// 
			this.shiftAmountNorthLabel.AutoSize = true;
			this.shiftAmountNorthLabel.Location = new System.Drawing.Point(126, 9);
			this.shiftAmountNorthLabel.Name = "shiftAmountNorthLabel";
			this.shiftAmountNorthLabel.Size = new System.Drawing.Size(113, 13);
			this.shiftAmountNorthLabel.TabIndex = 2;
			this.shiftAmountNorthLabel.Text = "Shift Amount North (m)";
			// 
			// ShiftNetworkNorthTextBox
			// 
			this.ShiftNetworkNorthTextBox.Location = new System.Drawing.Point(129, 30);
			this.ShiftNetworkNorthTextBox.Name = "ShiftNetworkNorthTextBox";
			this.ShiftNetworkNorthTextBox.Size = new System.Drawing.Size(100, 20);
			this.ShiftNetworkNorthTextBox.TabIndex = 4;
			// 
			// ShiftNetworkCancelButton
			// 
			this.ShiftNetworkCancelButton.Location = new System.Drawing.Point(15, 67);
			this.ShiftNetworkCancelButton.Name = "ShiftNetworkCancelButton";
			this.ShiftNetworkCancelButton.Size = new System.Drawing.Size(100, 23);
			this.ShiftNetworkCancelButton.TabIndex = 5;
			this.ShiftNetworkCancelButton.Text = "Cancel";
			this.ShiftNetworkCancelButton.UseVisualStyleBackColor = true;
			this.ShiftNetworkCancelButton.Click += new System.EventHandler(this.ShiftNetworkCancelButton_Click);
			// 
			// ShiftNetworkOkButton
			// 
			this.ShiftNetworkOkButton.Location = new System.Drawing.Point(129, 67);
			this.ShiftNetworkOkButton.Name = "ShiftNetworkOkButton";
			this.ShiftNetworkOkButton.Size = new System.Drawing.Size(100, 23);
			this.ShiftNetworkOkButton.TabIndex = 6;
			this.ShiftNetworkOkButton.Text = "Ok";
			this.ShiftNetworkOkButton.UseVisualStyleBackColor = true;
			this.ShiftNetworkOkButton.Click += new System.EventHandler(this.ShiftNetworkOkButton_Click);
			// 
			// ShiftNetwork
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(243, 102);
			this.Controls.Add(this.ShiftNetworkOkButton);
			this.Controls.Add(this.ShiftNetworkCancelButton);
			this.Controls.Add(this.ShiftNetworkNorthTextBox);
			this.Controls.Add(this.shiftAmountNorthLabel);
			this.Controls.Add(this.ShiftNetworkEastTextBox);
			this.Controls.Add(this.shiftNetworkEastLabel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ShiftNetwork";
			this.Text = "Shift Network";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label shiftNetworkEastLabel;
		private System.Windows.Forms.TextBox ShiftNetworkEastTextBox;
		private System.Windows.Forms.Label shiftAmountNorthLabel;
		private System.Windows.Forms.TextBox ShiftNetworkNorthTextBox;
		private System.Windows.Forms.Button ShiftNetworkCancelButton;
		private System.Windows.Forms.Button ShiftNetworkOkButton;
	}
}