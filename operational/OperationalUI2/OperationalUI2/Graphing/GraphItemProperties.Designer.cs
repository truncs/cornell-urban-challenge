namespace UrbanChallenge.OperationalUI.Graphing {
	partial class GraphItemProperties {
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.comboSourceUnits = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.comboDestUnits = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboAxis = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(57, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Item Units:";
			// 
			// comboSourceUnits
			// 
			this.comboSourceUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboSourceUnits.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.comboSourceUnits.FormattingEnabled = true;
			this.comboSourceUnits.Location = new System.Drawing.Point(74, 3);
			this.comboSourceUnits.Name = "comboSourceUnits";
			this.comboSourceUnits.Size = new System.Drawing.Size(160, 21);
			this.comboSourceUnits.TabIndex = 1;
			this.comboSourceUnits.SelectedIndexChanged += new System.EventHandler(this.comboSourceUnits_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 33);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(63, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "Convert To:";
			// 
			// comboDestUnits
			// 
			this.comboDestUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboDestUnits.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.comboDestUnits.FormattingEnabled = true;
			this.comboDestUnits.Location = new System.Drawing.Point(74, 30);
			this.comboDestUnits.Name = "comboDestUnits";
			this.comboDestUnits.Size = new System.Drawing.Size(160, 21);
			this.comboDestUnits.TabIndex = 1;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 77);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "Axis:";
			// 
			// comboAxis
			// 
			this.comboAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboAxis.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.comboAxis.FormattingEnabled = true;
			this.comboAxis.Items.AddRange(new object[] {
            "Left Axis",
            "Right Axis"});
			this.comboAxis.Location = new System.Drawing.Point(74, 74);
			this.comboAxis.Name = "comboAxis";
			this.comboAxis.Size = new System.Drawing.Size(160, 21);
			this.comboAxis.TabIndex = 1;
			// 
			// GraphItemProperties
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.comboAxis);
			this.Controls.Add(this.comboDestUnits);
			this.Controls.Add(this.comboSourceUnits);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "GraphItemProperties";
			this.Size = new System.Drawing.Size(268, 368);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboSourceUnits;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboDestUnits;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboAxis;
	}
}
