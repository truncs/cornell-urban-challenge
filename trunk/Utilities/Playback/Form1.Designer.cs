namespace Playback
{
  partial class Form1
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.progressbar = new System.Windows.Forms.ProgressBar();
			this.btnLoad = new System.Windows.Forms.Button();
			this.lblFileName = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.lblVTS = new System.Windows.Forms.Label();
			this.txtGoToFrame = new System.Windows.Forms.TextBox();
			this.btnGoToFrame = new System.Windows.Forms.Button();
			this.lblHDDBandwith = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.lblPlaybackSpeed = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.txtRedirect = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.keyPort = new System.Windows.Forms.ComboBox();
			this.btnResendLast = new System.Windows.Forms.Button();
			this.chkRedirect = new System.Windows.Forms.CheckBox();
			this.grpPlaybackSpeed = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.trackBarSpeed = new System.Windows.Forms.TrackBar();
			this.btnStepFwd = new System.Windows.Forms.Button();
			this.btnStepBack = new System.Windows.Forms.Button();
			this.btnStopGo = new System.Windows.Forms.Button();
			this.trackBar = new System.Windows.Forms.TrackBar();
			this.tmrRun = new System.Windows.Forms.Timer(this.components);
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.statsGrid = new System.Windows.Forms.DataGridView();
			this.tmrLoop = new System.Windows.Forms.Timer(this.components);
			this.tmrStats = new System.Windows.Forms.Timer(this.components);
			this.tmrPreload = new System.Windows.Forms.Timer(this.components);
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.grpPlaybackSpeed.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSpeed)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.statsGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.splitContainer1.Location = new System.Drawing.Point(3, 3);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.label11);
			this.splitContainer1.Panel2.Controls.Add(this.lblVTS);
			this.splitContainer1.Panel2.Controls.Add(this.txtGoToFrame);
			this.splitContainer1.Panel2.Controls.Add(this.btnGoToFrame);
			this.splitContainer1.Panel2.Controls.Add(this.lblHDDBandwith);
			this.splitContainer1.Panel2.Controls.Add(this.label10);
			this.splitContainer1.Panel2.Controls.Add(this.lblPlaybackSpeed);
			this.splitContainer1.Panel2.Controls.Add(this.label9);
			this.splitContainer1.Panel2.Controls.Add(this.txtRedirect);
			this.splitContainer1.Panel2.Controls.Add(this.label6);
			this.splitContainer1.Panel2.Controls.Add(this.keyPort);
			this.splitContainer1.Panel2.Controls.Add(this.btnResendLast);
			this.splitContainer1.Panel2.Controls.Add(this.chkRedirect);
			this.splitContainer1.Panel2.Controls.Add(this.grpPlaybackSpeed);
			this.splitContainer1.Panel2.Controls.Add(this.btnStepFwd);
			this.splitContainer1.Panel2.Controls.Add(this.btnStepBack);
			this.splitContainer1.Panel2.Controls.Add(this.btnStopGo);
			this.splitContainer1.Panel2.Controls.Add(this.trackBar);
			this.splitContainer1.Size = new System.Drawing.Size(425, 242);
			this.splitContainer1.SplitterDistance = 61;
			this.splitContainer1.TabIndex = 0;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.progressbar);
			this.groupBox1.Controls.Add(this.btnLoad);
			this.groupBox1.Controls.Add(this.lblFileName);
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(423, 59);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Log File";
			// 
			// progressbar
			// 
			this.progressbar.Location = new System.Drawing.Point(87, 32);
			this.progressbar.Name = "progressbar";
			this.progressbar.Size = new System.Drawing.Size(332, 23);
			this.progressbar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressbar.TabIndex = 0;
			// 
			// btnLoad
			// 
			this.btnLoad.Location = new System.Drawing.Point(6, 32);
			this.btnLoad.Name = "btnLoad";
			this.btnLoad.Size = new System.Drawing.Size(75, 23);
			this.btnLoad.TabIndex = 2;
			this.btnLoad.Text = "Load...";
			this.btnLoad.UseVisualStyleBackColor = true;
			this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
			// 
			// lblFileName
			// 
			this.lblFileName.Location = new System.Drawing.Point(6, 16);
			this.lblFileName.Name = "lblFileName";
			this.lblFileName.Size = new System.Drawing.Size(414, 13);
			this.lblFileName.TabIndex = 1;
			this.lblFileName.Text = "File: <None>";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(295, 108);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(126, 13);
			this.label11.TabIndex = 25;
			this.label11.Text = "(press ENTER to commit)";
			// 
			// lblVTS
			// 
			this.lblVTS.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblVTS.Location = new System.Drawing.Point(200, 42);
			this.lblVTS.Name = "lblVTS";
			this.lblVTS.Size = new System.Drawing.Size(216, 16);
			this.lblVTS.TabIndex = 22;
			this.lblVTS.Text = "---";
			this.lblVTS.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.lblVTS.Click += new System.EventHandler(this.lblVTS_Click);
			// 
			// txtGoToFrame
			// 
			this.txtGoToFrame.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.txtGoToFrame.Location = new System.Drawing.Point(298, 59);
			this.txtGoToFrame.MaxLength = 10;
			this.txtGoToFrame.Name = "txtGoToFrame";
			this.txtGoToFrame.Size = new System.Drawing.Size(93, 20);
			this.txtGoToFrame.TabIndex = 23;
			this.txtGoToFrame.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// btnGoToFrame
			// 
			this.btnGoToFrame.FlatAppearance.BorderSize = 0;
			this.btnGoToFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnGoToFrame.Image = global::Playback.Properties.Resources.ArrowRightStart;
			this.btnGoToFrame.Location = new System.Drawing.Point(390, 57);
			this.btnGoToFrame.Name = "btnGoToFrame";
			this.btnGoToFrame.Size = new System.Drawing.Size(26, 23);
			this.btnGoToFrame.TabIndex = 24;
			this.btnGoToFrame.UseVisualStyleBackColor = true;
			this.btnGoToFrame.Click += new System.EventHandler(this.btnGoToFrame_Click);
			// 
			// lblHDDBandwith
			// 
			this.lblHDDBandwith.Location = new System.Drawing.Point(277, 155);
			this.lblHDDBandwith.Name = "lblHDDBandwith";
			this.lblHDDBandwith.Size = new System.Drawing.Size(68, 13);
			this.lblHDDBandwith.TabIndex = 19;
			this.lblHDDBandwith.Text = "--";
			this.lblHDDBandwith.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(184, 155);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(87, 13);
			this.label10.TabIndex = 18;
			this.label10.Text = "HDD Bandwidth:";
			// 
			// lblPlaybackSpeed
			// 
			this.lblPlaybackSpeed.Location = new System.Drawing.Point(133, 155);
			this.lblPlaybackSpeed.Name = "lblPlaybackSpeed";
			this.lblPlaybackSpeed.Size = new System.Drawing.Size(55, 13);
			this.lblPlaybackSpeed.TabIndex = 17;
			this.lblPlaybackSpeed.Text = "--";
			this.lblPlaybackSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(6, 155);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(121, 13);
			this.label9.TabIndex = 16;
			this.label9.Text = "Actual Playback Speed:";
			// 
			// txtRedirect
			// 
			this.txtRedirect.Location = new System.Drawing.Point(296, 86);
			this.txtRedirect.Name = "txtRedirect";
			this.txtRedirect.Size = new System.Drawing.Size(124, 20);
			this.txtRedirect.TabIndex = 15;
			this.txtRedirect.Text = "localhost";
			this.txtRedirect.TextChanged += new System.EventHandler(this.txtRedirect_TextChanged);
			this.txtRedirect.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtRedirect_KeyDown);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(227, 134);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(47, 13);
			this.label6.TabIndex = 14;
			this.label6.Text = "Key Port";
			// 
			// keyPort
			// 
			this.keyPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.keyPort.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.keyPort.FormattingEnabled = true;
			this.keyPort.Location = new System.Drawing.Point(295, 131);
			this.keyPort.Name = "keyPort";
			this.keyPort.Size = new System.Drawing.Size(124, 21);
			this.keyPort.TabIndex = 13;
			this.keyPort.SelectedIndexChanged += new System.EventHandler(this.keyPort_SelectedIndexChanged);
			// 
			// btnResendLast
			// 
			this.btnResendLast.BackColor = System.Drawing.Color.Gainsboro;
			this.btnResendLast.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnResendLast.Image = global::Playback.Properties.Resources.Refresh_32_n_p;
			this.btnResendLast.Location = new System.Drawing.Point(200, 61);
			this.btnResendLast.Name = "btnResendLast";
			this.btnResendLast.Size = new System.Drawing.Size(21, 21);
			this.btnResendLast.TabIndex = 12;
			this.btnResendLast.UseVisualStyleBackColor = false;
			this.btnResendLast.Visible = false;
			this.btnResendLast.Click += new System.EventHandler(this.btnResendLast_Click);
			// 
			// chkRedirect
			// 
			this.chkRedirect.Appearance = System.Windows.Forms.Appearance.Button;
			this.chkRedirect.BackColor = System.Drawing.Color.Red;
			this.chkRedirect.Checked = true;
			this.chkRedirect.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkRedirect.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
			this.chkRedirect.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Yellow;
			this.chkRedirect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.chkRedirect.Location = new System.Drawing.Point(214, 86);
			this.chkRedirect.Name = "chkRedirect";
			this.chkRedirect.Size = new System.Drawing.Size(81, 35);
			this.chkRedirect.TabIndex = 9;
			this.chkRedirect.Text = "Redirecting";
			this.chkRedirect.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.chkRedirect.UseVisualStyleBackColor = false;
			this.chkRedirect.CheckedChanged += new System.EventHandler(this.chkRedirect_CheckedChanged);
			// 
			// grpPlaybackSpeed
			// 
			this.grpPlaybackSpeed.Controls.Add(this.label8);
			this.grpPlaybackSpeed.Controls.Add(this.label7);
			this.grpPlaybackSpeed.Controls.Add(this.label5);
			this.grpPlaybackSpeed.Controls.Add(this.label4);
			this.grpPlaybackSpeed.Controls.Add(this.label3);
			this.grpPlaybackSpeed.Controls.Add(this.label2);
			this.grpPlaybackSpeed.Controls.Add(this.label1);
			this.grpPlaybackSpeed.Controls.Add(this.trackBarSpeed);
			this.grpPlaybackSpeed.Location = new System.Drawing.Point(6, 86);
			this.grpPlaybackSpeed.Name = "grpPlaybackSpeed";
			this.grpPlaybackSpeed.Size = new System.Drawing.Size(202, 66);
			this.grpPlaybackSpeed.TabIndex = 8;
			this.grpPlaybackSpeed.TabStop = false;
			this.grpPlaybackSpeed.Text = "Playback Speed";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(51, 19);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(18, 13);
			this.label8.TabIndex = 15;
			this.label8.Text = "/2";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(27, 19);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(18, 13);
			this.label7.TabIndex = 14;
			this.label7.Text = "/4";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(173, 17);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(24, 13);
			this.label5.TabIndex = 13;
			this.label5.Text = "x16";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(154, 17);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(18, 13);
			this.label4.TabIndex = 12;
			this.label4.Text = "x8";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(130, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(18, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "x4";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(106, 17);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(18, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "x2";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(18, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "/8";
			// 
			// trackBarSpeed
			// 
			this.trackBarSpeed.LargeChange = 1;
			this.trackBarSpeed.Location = new System.Drawing.Point(3, 19);
			this.trackBarSpeed.Maximum = 7;
			this.trackBarSpeed.Name = "trackBarSpeed";
			this.trackBarSpeed.Size = new System.Drawing.Size(194, 42);
			this.trackBarSpeed.TabIndex = 8;
			this.trackBarSpeed.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trackBarSpeed.Value = 3;
			this.trackBarSpeed.Scroll += new System.EventHandler(this.trackBarSpeed_Scroll);
			// 
			// btnStepFwd
			// 
			this.btnStepFwd.BackColor = System.Drawing.Color.Gainsboro;
			this.btnStepFwd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnStepFwd.Image = global::Playback.Properties.Resources.Next_Track_32_n_p8;
			this.btnStepFwd.Location = new System.Drawing.Point(139, 42);
			this.btnStepFwd.Name = "btnStepFwd";
			this.btnStepFwd.Size = new System.Drawing.Size(55, 40);
			this.btnStepFwd.TabIndex = 4;
			this.btnStepFwd.UseVisualStyleBackColor = false;
			this.btnStepFwd.Click += new System.EventHandler(this.btnStepFwd_Click);
			// 
			// btnStepBack
			// 
			this.btnStepBack.BackColor = System.Drawing.Color.Gainsboro;
			this.btnStepBack.FlatAppearance.BorderColor = System.Drawing.Color.White;
			this.btnStepBack.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			this.btnStepBack.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnStepBack.Image = global::Playback.Properties.Resources.Previous_Track_32_n_p8;
			this.btnStepBack.Location = new System.Drawing.Point(6, 42);
			this.btnStepBack.Name = "btnStepBack";
			this.btnStepBack.Size = new System.Drawing.Size(55, 40);
			this.btnStepBack.TabIndex = 3;
			this.btnStepBack.UseVisualStyleBackColor = false;
			this.btnStepBack.Click += new System.EventHandler(this.btnStepBack_Click);
			// 
			// btnStopGo
			// 
			this.btnStopGo.BackColor = System.Drawing.Color.Gainsboro;
			this.btnStopGo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnStopGo.Image = global::Playback.Properties.Resources.Forward_or_Next_32_n_p;
			this.btnStopGo.Location = new System.Drawing.Point(67, 42);
			this.btnStopGo.Name = "btnStopGo";
			this.btnStopGo.Size = new System.Drawing.Size(66, 40);
			this.btnStopGo.TabIndex = 1;
			this.btnStopGo.UseVisualStyleBackColor = false;
			this.btnStopGo.Click += new System.EventHandler(this.btnStopGo_Click);
			// 
			// trackBar
			// 
			this.trackBar.Location = new System.Drawing.Point(6, 3);
			this.trackBar.MaximumSize = new System.Drawing.Size(413, 42);
			this.trackBar.MinimumSize = new System.Drawing.Size(413, 42);
			this.trackBar.Name = "trackBar";
			this.trackBar.Size = new System.Drawing.Size(413, 42);
			this.trackBar.TabIndex = 0;
			this.trackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
			// 
			// tmrRun
			// 
			this.tmrRun.Tick += new System.EventHandler(this.tmrRun_Tick);
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer2.IsSplitterFixed = true;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.statsGrid);
			this.splitContainer2.Size = new System.Drawing.Size(726, 247);
			this.splitContainer2.SplitterDistance = 430;
			this.splitContainer2.SplitterWidth = 1;
			this.splitContainer2.TabIndex = 1;
			// 
			// statsGrid
			// 
			this.statsGrid.AllowUserToAddRows = false;
			this.statsGrid.AllowUserToDeleteRows = false;
			this.statsGrid.AllowUserToResizeRows = false;
			this.statsGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			this.statsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.statsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.statsGrid.Location = new System.Drawing.Point(0, 0);
			this.statsGrid.Name = "statsGrid";
			this.statsGrid.RowHeadersVisible = false;
			this.statsGrid.Size = new System.Drawing.Size(295, 247);
			this.statsGrid.TabIndex = 0;
			this.statsGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.statsGrid_CellValueChanged);
			this.statsGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.statsGrid_CurrentCellDirtyStateChanged);
			// 
			// tmrLoop
			// 
			this.tmrLoop.Interval = 200;
			this.tmrLoop.Tick += new System.EventHandler(this.tmrLoop_Tick);
			// 
			// tmrStats
			// 
			this.tmrStats.Enabled = true;
			this.tmrStats.Interval = 1000;
			this.tmrStats.Tick += new System.EventHandler(this.tmrStats_Tick);
			// 
			// tmrPreload
			// 
			this.tmrPreload.Enabled = true;
			this.tmrPreload.Tick += new System.EventHandler(this.tmrPreload_Tick);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(726, 247);
			this.Controls.Add(this.splitContainer2);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "Playback";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			this.splitContainer1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.grpPlaybackSpeed.ResumeLayout(false);
			this.grpPlaybackSpeed.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSpeed)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.statsGrid)).EndInit();
			this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label lblFileName;
    private System.Windows.Forms.Button btnLoad;
    private System.Windows.Forms.ProgressBar progressbar;
    private System.Windows.Forms.Button btnStopGo;
    private System.Windows.Forms.TrackBar trackBar;
    private System.Windows.Forms.Timer tmrRun;
    private System.Windows.Forms.Button btnStepFwd;
    private System.Windows.Forms.Button btnStepBack;
    private System.Windows.Forms.CheckBox chkRedirect;
    private System.Windows.Forms.SplitContainer splitContainer2;
    private System.Windows.Forms.DataGridView statsGrid;
    private System.Windows.Forms.Button btnResendLast;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.ComboBox keyPort;
    private System.Windows.Forms.Timer tmrLoop;
		private System.Windows.Forms.TextBox txtRedirect;
    private System.Windows.Forms.Label lblHDDBandwith;
    private System.Windows.Forms.Label label10;
    private System.Windows.Forms.Label lblPlaybackSpeed;
    private System.Windows.Forms.Label label9;
    private System.Windows.Forms.Timer tmrStats;
		private System.Windows.Forms.Timer tmrPreload;
		private System.Windows.Forms.Label lblVTS;
		private System.Windows.Forms.TextBox txtGoToFrame;
		private System.Windows.Forms.Button btnGoToFrame;
    private System.Windows.Forms.Label label11;
    private System.Windows.Forms.GroupBox grpPlaybackSpeed;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TrackBar trackBarSpeed;
  }
}

