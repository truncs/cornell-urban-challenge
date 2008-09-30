namespace RemoraAdvanced.Forms
{
	partial class PosteriorPose
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PosteriorPose));
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.yPosition = new System.Windows.Forms.Label();
			this.xPosition = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.speed = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.flowLayoutPanel1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.SuspendLayout();
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this.groupBox1);
			this.flowLayoutPanel1.Controls.Add(this.groupBox5);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(161, 118);
			this.flowLayoutPanel1.TabIndex = 0;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.yPosition);
			this.groupBox1.Controls.Add(this.xPosition);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(3, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(158, 66);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Position";
			// 
			// yPosition
			// 
			this.yPosition.AutoSize = true;
			this.yPosition.Location = new System.Drawing.Point(32, 39);
			this.yPosition.Name = "yPosition";
			this.yPosition.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.yPosition.Size = new System.Drawing.Size(22, 13);
			this.yPosition.TabIndex = 3;
			this.yPosition.Text = "   ";
			// 
			// xPosition
			// 
			this.xPosition.AutoSize = true;
			this.xPosition.Location = new System.Drawing.Point(32, 16);
			this.xPosition.Name = "xPosition";
			this.xPosition.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.xPosition.Size = new System.Drawing.Size(22, 13);
			this.xPosition.TabIndex = 2;
			this.xPosition.Text = "   ";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 39);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.label2.Size = new System.Drawing.Size(21, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "y:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 16);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.label1.Size = new System.Drawing.Size(21, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "x:";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.speed);
			this.groupBox5.Controls.Add(this.label3);
			this.groupBox5.Location = new System.Drawing.Point(3, 75);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(158, 40);
			this.groupBox5.TabIndex = 9;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Pose";
			// 
			// speed
			// 
			this.speed.AutoSize = true;
			this.speed.Location = new System.Drawing.Point(56, 16);
			this.speed.Name = "speed";
			this.speed.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.speed.Size = new System.Drawing.Size(22, 13);
			this.speed.TabIndex = 4;
			this.speed.Text = "   ";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 16);
			this.label3.Name = "label3";
			this.label3.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.label3.Size = new System.Drawing.Size(45, 13);
			this.label3.TabIndex = 1;
			this.label3.Text = "speed:";
			// 
			// PosteriorPose
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(161, 118);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PosteriorPose";
			this.ShowInTaskbar = false;
			this.Text = "Posterior Pose";
			this.flowLayoutPanel1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label yPosition;
		private System.Windows.Forms.Label xPosition;
		private System.Windows.Forms.Label speed;


	}
}