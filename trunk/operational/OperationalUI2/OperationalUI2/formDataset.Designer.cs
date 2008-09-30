namespace UrbanChallenge.OperationalUI {
	partial class formDataset {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formDataset));
			this.listViewDataset = new UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView();
			this.columnName = new System.Windows.Forms.ColumnHeader();
			this.columnLastValue = new System.Windows.Forms.ColumnHeader();
			this.imageListSmall = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			// 
			// listViewDataset
			// 
			this.listViewDataset.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewDataset.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnLastValue});
			this.listViewDataset.FullRowSelect = true;
			this.listViewDataset.Location = new System.Drawing.Point(12, 12);
			this.listViewDataset.Name = "listViewDataset";
			this.listViewDataset.Size = new System.Drawing.Size(324, 464);
			this.listViewDataset.SmallImageList = this.imageListSmall;
			this.listViewDataset.TabIndex = 0;
			this.listViewDataset.UseCompatibleStateImageBehavior = false;
			this.listViewDataset.View = System.Windows.Forms.View.Details;
			this.listViewDataset.ItemActivate += new System.EventHandler(this.listViewDataset_ItemActivate);
			this.listViewDataset.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.listViewDataset_ItemDrag);
			// 
			// columnName
			// 
			this.columnName.Text = "Name";
			this.columnName.Width = 200;
			// 
			// columnLastValue
			// 
			this.columnLastValue.Text = "Last Value";
			this.columnLastValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.columnLastValue.Width = 120;
			// 
			// imageListSmall
			// 
			this.imageListSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListSmall.ImageStream")));
			this.imageListSmall.TransparentColor = System.Drawing.Color.Transparent;
			this.imageListSmall.Images.SetKeyName(0, "item");
			// 
			// formDataset
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(348, 488);
			this.Controls.Add(this.listViewDataset);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "formDataset";
			this.Text = "Dataset";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formDataset_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		private UrbanChallenge.OperationalUI.Controls.DoubleBufferedListView listViewDataset;
		private System.Windows.Forms.ColumnHeader columnName;
		private System.Windows.Forms.ColumnHeader columnLastValue;
		private System.Windows.Forms.ImageList imageListSmall;
	}
}