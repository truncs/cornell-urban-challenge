using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;

namespace RndfEditor.Forms
{
	public enum SparseToolboxMode
	{
		None,
		Selection,
		Polygon
	}

	public partial class SparsePartitionToolbox : Form
	{
		public SparseToolboxMode Mode;
		public ArbiterLanePartition Partition;
		public RoadDisplay Display;
		public Editor Editor;
		public List<Coordinates> tmpPolyCoords;


		public DisplayObjectFilter PartitionFilter = new DisplayObjectFilter(
			delegate(IDisplayObject obj) { 
				return obj is ArbiterLanePartition ? true : false; });

		public SparsePartitionToolbox(Editor editor, RoadDisplay display)
		{
			InitializeComponent();
			this.Mode = SparseToolboxMode.None;
			this.Editor = editor;
			this.Display = display;
		}

		private void sparseToolboxSelectPartition_Click(object sender, EventArgs e)
		{
			if (this.sparseToolboxSelectPartition.CheckState == CheckState.Checked)
			{
				this.sparsetoolboxWrapPolygonButton.CheckState = CheckState.Unchecked;
				this.Mode = SparseToolboxMode.Selection;
			}
			else
			{
				this.Mode = SparseToolboxMode.None;
			}
		}

		private void sparsetoolboxWrapPolygonButton_Click(object sender, EventArgs e)
		{
			if (this.sparsetoolboxWrapPolygonButton.CheckState == CheckState.Checked)
			{
				this.sparseToolboxSelectPartition.CheckState = CheckState.Unchecked;
				this.Mode = SparseToolboxMode.Polygon;
			}
			else
			{
				this.Mode = SparseToolboxMode.None;
			}
		}

		private void SparseToolboxResetSparsePolygon_Click(object sender, EventArgs e)
		{
			this.ResetButtons();
			if (this.Partition != null)
				this.Partition.SetDefaultSparsePolygon();

			if (this.Partition != null)
			{
				this.Partition.selected = SelectionType.NotSelected;
				this.Partition = null;
			}

			this.Display.Invalidate();
		}

		public void SelectPartition(Coordinates c)
		{
			if (this.Partition != null)
			{
				this.Partition.selected = SelectionType.NotSelected;
				this.Partition = null;
			}

			this.tmpPolyCoords = new List<Coordinates>();

			HitTestResult htr = this.Display.HitTest(c, this.PartitionFilter);

			if (htr.Hit)
			{
				ArbiterLanePartition tmpPartition = (ArbiterLanePartition)htr.DisplayObject;

				if (tmpPartition.Lane.RelativelyInside(c))
				{
					this.Partition = (ArbiterLanePartition)htr.DisplayObject;
					this.Partition.selected = SelectionType.SingleSelected;
					this.Mode = SparseToolboxMode.None;
					this.ResetButtons();

					if (this.Partition.SparsePolygon == null)
						this.Partition.SetDefaultSparsePolygon();
				}
			}

			this.Display.Invalidate();
		}

		public void ShutDown()
		{
			if (this.Partition != null)
				this.Partition.selected = SelectionType.NotSelected;

			if (!this.IsDisposed)
			{
				this.Close();
			}

			this.Display.CurrentEditorTool = null;
			this.Display.Invalidate();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Editor.SparseToolboxButton.CheckState = CheckState.Unchecked;
			this.Display.CurrentEditorTool = null;
		}

		public void ResetButtons()
		{
			this.sparseToolboxSelectPartition.CheckState = CheckState.Unchecked;
			this.sparsetoolboxWrapPolygonButton.CheckState = CheckState.Unchecked;
		}
	}
}