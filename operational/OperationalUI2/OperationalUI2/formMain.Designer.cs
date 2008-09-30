namespace UrbanChallenge.OperationalUI {
	partial class formMain {
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formMain));
			this.toolStripMain = new System.Windows.Forms.ToolStrip();
			this.buttonMap = new System.Windows.Forms.ToolStripButton();
			this.buttonDataset = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.buttonAllToFront = new System.Windows.Forms.ToolStripButton();
			this.buttonConnectToSim = new System.Windows.Forms.ToolStripButton();
			this.imageListTile = new System.Windows.Forms.ImageList(this.components);
			this.listViewInstances = new UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView();
			this.columName = new System.Windows.Forms.ColumnHeader();
			this.columnMode = new System.Windows.Forms.ColumnHeader();
			this.columnGay = new System.Windows.Forms.ColumnHeader();
			this.timerRender = new System.Windows.Forms.Timer(this.components);
			this.toolStripMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripMain
			// 
			this.toolStripMain.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buttonMap,
            this.buttonDataset,
            this.toolStripSeparator1,
            this.buttonAllToFront,
            this.buttonConnectToSim});
			this.toolStripMain.Location = new System.Drawing.Point(0, 0);
			this.toolStripMain.Name = "toolStripMain";
			this.toolStripMain.Size = new System.Drawing.Size(594, 31);
			this.toolStripMain.TabIndex = 0;
			this.toolStripMain.Text = "toolStrip1";
			// 
			// buttonMap
			// 
			this.buttonMap.Image = ((System.Drawing.Image)(resources.GetObject("buttonMap.Image")));
			this.buttonMap.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonMap.Name = "buttonMap";
			this.buttonMap.Size = new System.Drawing.Size(59, 28);
			this.buttonMap.Text = "Map";
			this.buttonMap.Click += new System.EventHandler(this.buttonMap_Click);
			// 
			// buttonDataset
			// 
			this.buttonDataset.Image = ((System.Drawing.Image)(resources.GetObject("buttonDataset.Image")));
			this.buttonDataset.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonDataset.Name = "buttonDataset";
			this.buttonDataset.Size = new System.Drawing.Size(74, 28);
			this.buttonDataset.Text = "Dataset";
			this.buttonDataset.Click += new System.EventHandler(this.buttonDataset_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
			// 
			// buttonAllToFront
			// 
			this.buttonAllToFront.Image = ((System.Drawing.Image)(resources.GetObject("buttonAllToFront.Image")));
			this.buttonAllToFront.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonAllToFront.Name = "buttonAllToFront";
			this.buttonAllToFront.Size = new System.Drawing.Size(81, 28);
			this.buttonAllToFront.Text = "Show All";
			this.buttonAllToFront.ToolTipText = "Brings all open windows to the foreground";
			// 
			// buttonConnectToSim
			// 
			this.buttonConnectToSim.Image = ((System.Drawing.Image)(resources.GetObject("buttonConnectToSim.Image")));
			this.buttonConnectToSim.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonConnectToSim.Name = "buttonConnectToSim";
			this.buttonConnectToSim.Size = new System.Drawing.Size(120, 28);
			this.buttonConnectToSim.Text = "Connect To Sim";
			this.buttonConnectToSim.Click += new System.EventHandler(this.buttonConnectToSim_Click);
			// 
			// imageListTile
			// 
			this.imageListTile.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTile.ImageStream")));
			this.imageListTile.TransparentColor = System.Drawing.Color.Transparent;
			this.imageListTile.Images.SetKeyName(0, "dead");
			this.imageListTile.Images.SetKeyName(1, "unknown");
			this.imageListTile.Images.SetKeyName(2, "active");
			// 
			// listViewInstances
			// 
			this.listViewInstances.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewInstances.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columName,
            this.columnMode,
            this.columnGay});
			this.listViewInstances.FullRowSelect = true;
			this.listViewInstances.LargeImageList = this.imageListTile;
			this.listViewInstances.Location = new System.Drawing.Point(12, 34);
			this.listViewInstances.MultiSelect = false;
			this.listViewInstances.Name = "listViewInstances";
			this.listViewInstances.Size = new System.Drawing.Size(570, 211);
			this.listViewInstances.TabIndex = 1;
			this.listViewInstances.UseCompatibleStateImageBehavior = false;
			this.listViewInstances.View = System.Windows.Forms.View.Tile;
			this.listViewInstances.ItemActivate += new System.EventHandler(this.listViewInstances_ItemActivate);
			// 
			// timerRender
			// 
			this.timerRender.Tick += new System.EventHandler(this.timerRender_Tick);
			// 
			// formMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(594, 257);
			this.Controls.Add(this.listViewInstances);
			this.Controls.Add(this.toolStripMain);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Name = "formMain";
			this.Text = "OperationalUI v0.2";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formMain_KeyDown);
			this.Load += new System.EventHandler(this.formMain_Load);
			this.toolStripMain.ResumeLayout(false);
			this.toolStripMain.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStripMain;
		private System.Windows.Forms.ToolStripButton buttonMap;
		private System.Windows.Forms.ToolStripButton buttonDataset;
		private UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView listViewInstances;
		private System.Windows.Forms.ColumnHeader columName;
		private System.Windows.Forms.ColumnHeader columnMode;
		private System.Windows.Forms.ColumnHeader columnGay;
		private System.Windows.Forms.ImageList imageListTile;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton buttonAllToFront;
		private System.Windows.Forms.ToolStripButton buttonConnectToSim;
		private System.Windows.Forms.Timer timerRender;
	}
}