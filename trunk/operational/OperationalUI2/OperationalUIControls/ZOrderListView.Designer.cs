namespace UrbanChallenge.OperationalUI.Controls {
	partial class ZOrderListView {
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ZOrderListView));
			this.toolStripZOrder = new System.Windows.Forms.ToolStrip();
			this.buttonMoveUp = new System.Windows.Forms.ToolStripButton();
			this.buttonMoveDown = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.buttonMoveToTop = new System.Windows.Forms.ToolStripButton();
			this.buttonMoveToBottom = new System.Windows.Forms.ToolStripButton();
			this.imageListDuder = new System.Windows.Forms.ImageList(this.components);
			this.listViewItems = new UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView();
			this.columnName = new System.Windows.Forms.ColumnHeader();
			this.toolStripZOrder.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripZOrder
			// 
			this.toolStripZOrder.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buttonMoveUp,
            this.buttonMoveDown,
            this.toolStripSeparator1,
            this.buttonMoveToTop,
            this.buttonMoveToBottom});
			this.toolStripZOrder.Location = new System.Drawing.Point(0, 0);
			this.toolStripZOrder.Name = "toolStripZOrder";
			this.toolStripZOrder.Size = new System.Drawing.Size(297, 25);
			this.toolStripZOrder.TabIndex = 0;
			this.toolStripZOrder.Text = "toolStrip1";
			// 
			// buttonMoveUp
			// 
			this.buttonMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.buttonMoveUp.Image = ((System.Drawing.Image)(resources.GetObject("buttonMoveUp.Image")));
			this.buttonMoveUp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonMoveUp.Name = "buttonMoveUp";
			this.buttonMoveUp.Size = new System.Drawing.Size(23, 22);
			this.buttonMoveUp.Text = "Move Up";
			this.buttonMoveUp.Click += new System.EventHandler(this.buttonMoveUp_Click);
			// 
			// buttonMoveDown
			// 
			this.buttonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.buttonMoveDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonMoveDown.Image")));
			this.buttonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonMoveDown.Name = "buttonMoveDown";
			this.buttonMoveDown.Size = new System.Drawing.Size(23, 22);
			this.buttonMoveDown.Text = "Move Down";
			this.buttonMoveDown.Click += new System.EventHandler(this.buttonMoveDown_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// buttonMoveToTop
			// 
			this.buttonMoveToTop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.buttonMoveToTop.Image = ((System.Drawing.Image)(resources.GetObject("buttonMoveToTop.Image")));
			this.buttonMoveToTop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonMoveToTop.Name = "buttonMoveToTop";
			this.buttonMoveToTop.Size = new System.Drawing.Size(23, 22);
			this.buttonMoveToTop.Text = "Move To Top";
			this.buttonMoveToTop.Click += new System.EventHandler(this.buttonMoveToTop_Click);
			// 
			// buttonMoveToBottom
			// 
			this.buttonMoveToBottom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.buttonMoveToBottom.Image = ((System.Drawing.Image)(resources.GetObject("buttonMoveToBottom.Image")));
			this.buttonMoveToBottom.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.buttonMoveToBottom.Name = "buttonMoveToBottom";
			this.buttonMoveToBottom.Size = new System.Drawing.Size(23, 22);
			this.buttonMoveToBottom.Text = "Move To Bottom";
			this.buttonMoveToBottom.Click += new System.EventHandler(this.buttonMoveToBottom_Click);
			// 
			// imageListDuder
			// 
			this.imageListDuder.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListDuder.ImageStream")));
			this.imageListDuder.TransparentColor = System.Drawing.Color.Transparent;
			this.imageListDuder.Images.SetKeyName(0, "obj");
			// 
			// listViewItems
			// 
			this.listViewItems.AllowDrop = true;
			this.listViewItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName});
			this.listViewItems.FullRowSelect = true;
			this.listViewItems.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listViewItems.HideSelection = false;
			this.listViewItems.Location = new System.Drawing.Point(3, 28);
			this.listViewItems.MultiSelect = false;
			this.listViewItems.Name = "listViewItems";
			this.listViewItems.Size = new System.Drawing.Size(291, 464);
			this.listViewItems.SmallImageList = this.imageListDuder;
			this.listViewItems.TabIndex = 1;
			this.listViewItems.UseCompatibleStateImageBehavior = false;
			this.listViewItems.View = System.Windows.Forms.View.Details;
			this.listViewItems.DragEnter += new System.Windows.Forms.DragEventHandler(this.listViewItems_DragEnter);
			this.listViewItems.ItemActivate += new System.EventHandler(this.listViewItems_ItemActivate);
			this.listViewItems.DragDrop += new System.Windows.Forms.DragEventHandler(this.listViewItems_DragDrop);
			this.listViewItems.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listViewItems_ItemChecked);
			this.listViewItems.Resize += new System.EventHandler(this.listViewItems_Resize);
			this.listViewItems.DragOver += new System.Windows.Forms.DragEventHandler(this.listViewItems_DragOver);
			this.listViewItems.SelectedIndexChanged += new System.EventHandler(this.listViewItems_SelectedIndexChanged);
			this.listViewItems.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.listViewItems_ItemDrag);
			this.listViewItems.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.listViewItems_ColumnWidthChanging);
			// 
			// columnName
			// 
			this.columnName.Text = "Name";
			this.columnName.Width = 287;
			// 
			// ZOrderListView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.listViewItems);
			this.Controls.Add(this.toolStripZOrder);
			this.Name = "ZOrderListView";
			this.Size = new System.Drawing.Size(297, 495);
			this.toolStripZOrder.ResumeLayout(false);
			this.toolStripZOrder.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStripZOrder;
		private System.Windows.Forms.ToolStripButton buttonMoveUp;
		private System.Windows.Forms.ToolStripButton buttonMoveDown;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton buttonMoveToTop;
		private System.Windows.Forms.ToolStripButton buttonMoveToBottom;
		private UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView listViewItems;
		private System.Windows.Forms.ImageList imageListDuder;
		private System.Windows.Forms.ColumnHeader columnName;
	}
}
