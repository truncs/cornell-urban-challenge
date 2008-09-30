namespace UrbanChallenge.OperationalUI.Controls {
	partial class DisplayObjectTreeView {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisplayObjectTreeView));
			this.imageListMain = new System.Windows.Forms.ImageList(this.components);
			this.contextMenuDisplayObject = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuRenderDisplayObject = new System.Windows.Forms.ToolStripMenuItem();
			this.menuSelectColor = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.colorDialog1 = new System.Windows.Forms.ColorDialog();
			this.contextMenuDisplayObject.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageListMain
			// 
			this.imageListMain.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListMain.ImageStream")));
			this.imageListMain.TransparentColor = System.Drawing.Color.Transparent;
			this.imageListMain.Images.SetKeyName(0, "folder open");
			this.imageListMain.Images.SetKeyName(1, "folder closed");
			this.imageListMain.Images.SetKeyName(2, "display object");
			// 
			// contextMenuDisplayObject
			// 
			this.contextMenuDisplayObject.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRenderDisplayObject,
            this.menuSelectColor,
            this.toolStripSeparator});
			this.contextMenuDisplayObject.Name = "contextMenuDisplayObject";
			this.contextMenuDisplayObject.Size = new System.Drawing.Size(138, 54);
			// 
			// menuRenderDisplayObject
			// 
			this.menuRenderDisplayObject.Name = "menuRenderDisplayObject";
			this.menuRenderDisplayObject.Size = new System.Drawing.Size(137, 22);
			this.menuRenderDisplayObject.Text = "Render";
			this.menuRenderDisplayObject.Click += new System.EventHandler(this.menuRenderDisplayObject_Click);
			// 
			// menuSelectColor
			// 
			this.menuSelectColor.Image = ((System.Drawing.Image)(resources.GetObject("menuSelectColor.Image")));
			this.menuSelectColor.Name = "menuSelectColor";
			this.menuSelectColor.Size = new System.Drawing.Size(137, 22);
			this.menuSelectColor.Text = "Select Color";
			this.menuSelectColor.Click += new System.EventHandler(this.menuSelectColor_Click);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(134, 6);
			// 
			// colorDialog1
			// 
			this.colorDialog1.AnyColor = true;
			this.colorDialog1.FullOpen = true;
			// 
			// DisplayObjectTreeView
			// 
			this.FullRowSelect = true;
			this.ImageIndex = 0;
			this.ImageList = this.imageListMain;
			this.Indent = 16;
			this.PathSeparator = "/";
			this.SelectedImageIndex = 0;
			this.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.DisplayObjectTreeView_NodeMouseDoubleClick);
			this.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.DisplayObjectTreeView_AfterCollapse);
			this.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.DisplayObjectTreeView_NodeMouseClick);
			this.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.DisplayObjectTreeView_AfterExpand);
			this.contextMenuDisplayObject.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ImageList imageListMain;
		private System.Windows.Forms.ContextMenuStrip contextMenuDisplayObject;
		private System.Windows.Forms.ToolStripMenuItem menuRenderDisplayObject;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.ToolStripMenuItem menuSelectColor;
	}
}
