namespace PathSmootherTest {
	partial class Form1 {
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
			this.picEntry = new System.Windows.Forms.PictureBox();
			this.optPath = new System.Windows.Forms.RadioButton();
			this.optUB = new System.Windows.Forms.RadioButton();
			this.optLB = new System.Windows.Forms.RadioButton();
			this.buttonClear = new System.Windows.Forms.Button();
			this.buttonSmooth = new System.Windows.Forms.Button();
			this.labelLoc = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.optIPOPT = new System.Windows.Forms.RadioButton();
			this.optLOQO = new System.Windows.Forms.RadioButton();
			this.trackW = new System.Windows.Forms.TrackBar();
			this.labelw = new System.Windows.Forms.Label();
			this.trackV = new System.Windows.Forms.TrackBar();
			this.labelV = new System.Windows.Forms.Label();
			this.trackD1 = new System.Windows.Forms.TrackBar();
			this.labelD1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.picEntry)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackW)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackV)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackD1)).BeginInit();
			this.SuspendLayout();
			// 
			// picEntry
			// 
			this.picEntry.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.picEntry.BackColor = System.Drawing.Color.White;
			this.picEntry.Location = new System.Drawing.Point(14, 12);
			this.picEntry.Name = "picEntry";
			this.picEntry.Size = new System.Drawing.Size(1372, 378);
			this.picEntry.TabIndex = 0;
			this.picEntry.TabStop = false;
			this.picEntry.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picEntry_MouseDown);
			this.picEntry.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picEntry_MouseMove);
			this.picEntry.Paint += new System.Windows.Forms.PaintEventHandler(this.picEntry_Paint);
			this.picEntry.Resize += new System.EventHandler(this.picEntry_Resize);
			// 
			// optPath
			// 
			this.optPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.optPath.AutoSize = true;
			this.optPath.Checked = true;
			this.optPath.Location = new System.Drawing.Point(14, 396);
			this.optPath.Name = "optPath";
			this.optPath.Size = new System.Drawing.Size(50, 17);
			this.optPath.TabIndex = 1;
			this.optPath.TabStop = true;
			this.optPath.Text = "Path";
			this.optPath.UseVisualStyleBackColor = true;
			// 
			// optUB
			// 
			this.optUB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.optUB.AutoSize = true;
			this.optUB.Location = new System.Drawing.Point(14, 419);
			this.optUB.Name = "optUB";
			this.optUB.Size = new System.Drawing.Size(99, 17);
			this.optUB.TabIndex = 1;
			this.optUB.TabStop = true;
			this.optUB.Text = "Upper Bound";
			this.optUB.UseVisualStyleBackColor = true;
			// 
			// optLB
			// 
			this.optLB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.optLB.AutoSize = true;
			this.optLB.Location = new System.Drawing.Point(14, 442);
			this.optLB.Name = "optLB";
			this.optLB.Size = new System.Drawing.Size(99, 17);
			this.optLB.TabIndex = 1;
			this.optLB.TabStop = true;
			this.optLB.Text = "Lower Bound";
			this.optLB.UseVisualStyleBackColor = true;
			// 
			// buttonClear
			// 
			this.buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonClear.Location = new System.Drawing.Point(185, 396);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(105, 28);
			this.buttonClear.TabIndex = 2;
			this.buttonClear.Text = "Clear";
			this.buttonClear.UseVisualStyleBackColor = true;
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			// 
			// buttonSmooth
			// 
			this.buttonSmooth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonSmooth.Location = new System.Drawing.Point(185, 430);
			this.buttonSmooth.Name = "buttonSmooth";
			this.buttonSmooth.Size = new System.Drawing.Size(105, 28);
			this.buttonSmooth.TabIndex = 2;
			this.buttonSmooth.Text = "Smooth";
			this.buttonSmooth.UseVisualStyleBackColor = true;
			this.buttonSmooth.Click += new System.EventHandler(this.buttonSmooth_Click);
			// 
			// labelLoc
			// 
			this.labelLoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelLoc.AutoSize = true;
			this.labelLoc.Location = new System.Drawing.Point(334, 396);
			this.labelLoc.Name = "labelLoc";
			this.labelLoc.Size = new System.Drawing.Size(29, 13);
			this.labelLoc.TabIndex = 3;
			this.labelLoc.Text = "0, 0";
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.optIPOPT);
			this.groupBox1.Controls.Add(this.optLOQO);
			this.groupBox1.Location = new System.Drawing.Point(420, 396);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(199, 69);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Algorithm";
			// 
			// optIPOPT
			// 
			this.optIPOPT.AutoSize = true;
			this.optIPOPT.Checked = true;
			this.optIPOPT.Location = new System.Drawing.Point(6, 43);
			this.optIPOPT.Name = "optIPOPT";
			this.optIPOPT.Size = new System.Drawing.Size(60, 17);
			this.optIPOPT.TabIndex = 0;
			this.optIPOPT.TabStop = true;
			this.optIPOPT.Text = "IPOPT";
			this.optIPOPT.UseVisualStyleBackColor = true;
			// 
			// optLOQO
			// 
			this.optLOQO.AutoSize = true;
			this.optLOQO.Location = new System.Drawing.Point(6, 20);
			this.optLOQO.Name = "optLOQO";
			this.optLOQO.Size = new System.Drawing.Size(58, 17);
			this.optLOQO.TabIndex = 0;
			this.optLOQO.Text = "LOQO";
			this.optLOQO.UseVisualStyleBackColor = true;
			// 
			// trackW
			// 
			this.trackW.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.trackW.Location = new System.Drawing.Point(625, 420);
			this.trackW.Maximum = 100;
			this.trackW.Name = "trackW";
			this.trackW.Size = new System.Drawing.Size(231, 45);
			this.trackW.TabIndex = 5;
			this.trackW.TickFrequency = 10;
			this.trackW.Scroll += new System.EventHandler(this.trackW_Scroll);
			// 
			// labelw
			// 
			this.labelw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelw.AutoSize = true;
			this.labelw.Location = new System.Drawing.Point(625, 400);
			this.labelw.Name = "labelw";
			this.labelw.Size = new System.Drawing.Size(21, 13);
			this.labelw.TabIndex = 6;
			this.labelw.Text = "w:";
			// 
			// trackV
			// 
			this.trackV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.trackV.Location = new System.Drawing.Point(895, 420);
			this.trackV.Maximum = 133;
			this.trackV.Minimum = 10;
			this.trackV.Name = "trackV";
			this.trackV.Size = new System.Drawing.Size(231, 45);
			this.trackV.TabIndex = 5;
			this.trackV.TickFrequency = 10;
			this.trackV.Value = 10;
			this.trackV.Scroll += new System.EventHandler(this.trackV_Scroll);
			// 
			// labelV
			// 
			this.labelV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelV.AutoSize = true;
			this.labelV.Location = new System.Drawing.Point(895, 400);
			this.labelV.Name = "labelV";
			this.labelV.Size = new System.Drawing.Size(19, 13);
			this.labelV.TabIndex = 6;
			this.labelV.Text = "v:";
			// 
			// trackD1
			// 
			this.trackD1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.trackD1.Location = new System.Drawing.Point(1132, 420);
			this.trackD1.Maximum = 100;
			this.trackD1.Name = "trackD1";
			this.trackD1.Size = new System.Drawing.Size(231, 45);
			this.trackD1.TabIndex = 5;
			this.trackD1.TickFrequency = 10;
			this.trackD1.Value = 1;
			this.trackD1.Scroll += new System.EventHandler(this.trackD1_Scroll);
			// 
			// labelD1
			// 
			this.labelD1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelD1.AutoSize = true;
			this.labelD1.Location = new System.Drawing.Point(1132, 400);
			this.labelD1.Name = "labelD1";
			this.labelD1.Size = new System.Drawing.Size(26, 13);
			this.labelD1.TabIndex = 6;
			this.labelD1.Text = "d1:";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1400, 474);
			this.Controls.Add(this.labelD1);
			this.Controls.Add(this.labelV);
			this.Controls.Add(this.labelw);
			this.Controls.Add(this.trackD1);
			this.Controls.Add(this.trackV);
			this.Controls.Add(this.trackW);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.labelLoc);
			this.Controls.Add(this.buttonSmooth);
			this.Controls.Add(this.buttonClear);
			this.Controls.Add(this.optLB);
			this.Controls.Add(this.optUB);
			this.Controls.Add(this.optPath);
			this.Controls.Add(this.picEntry);
			this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "Form1";
			this.Text = "Form1";
			((System.ComponentModel.ISupportInitialize)(this.picEntry)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackW)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackV)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackD1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox picEntry;
		private System.Windows.Forms.RadioButton optPath;
		private System.Windows.Forms.RadioButton optUB;
		private System.Windows.Forms.RadioButton optLB;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonSmooth;
		private System.Windows.Forms.Label labelLoc;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton optIPOPT;
		private System.Windows.Forms.RadioButton optLOQO;
		private System.Windows.Forms.TrackBar trackW;
		private System.Windows.Forms.Label labelw;
		private System.Windows.Forms.TrackBar trackV;
		private System.Windows.Forms.Label labelV;
		private System.Windows.Forms.TrackBar trackD1;
		private System.Windows.Forms.Label labelD1;
	}
}

