using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Forms;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace RndfEditor.Tools
{
	public class SparseTool : IEditorTool
	{
		[NonSerialized]
		public SparsePartitionToolbox Toolbox;

		public SparseTool(SparsePartitionToolbox toolbox)
		{
			this.Toolbox = toolbox;
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(RndfEditor.Display.Utilities.WorldTransform wt)
		{
			return new RectangleF();
		}

		public RndfEditor.Display.Utilities.HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, RndfEditor.Display.Utilities.WorldTransform wt, RndfEditor.Display.Utilities.DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, RndfEditor.Display.Utilities.WorldTransform t)
		{
			// draw the sparse polygon of the selected
			if (this.Toolbox.Partition != null)
			{
				DrawingUtility.DrawControlPolygon(
					this.Toolbox.Partition.SparsePolygon,
					Color.DarkSeaGreen, System.Drawing.Drawing2D.DashStyle.Solid, g, t);

				foreach (Coordinates c in this.Toolbox.Partition.SparsePolygon)
				{
					DrawingUtility.DrawControlPoint(c, Color.DarkSeaGreen, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallX, g, t);
				}

				foreach (Coordinates c in this.Toolbox.tmpPolyCoords)
				{
					DrawingUtility.DrawControlPoint(c, Color.DarkViolet, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}

				if (this.Toolbox.tmpPolyCoords.Count > 1)
				{
					for (int i = 0; i < this.Toolbox.tmpPolyCoords.Count - 1; i++)
					{
						DrawingUtility.DrawControlLine(this.Toolbox.tmpPolyCoords[i], this.Toolbox.tmpPolyCoords[i + 1], g, t, null, Color.DarkViolet);
					}
				}
			}
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public RndfEditor.Display.Utilities.SelectionType Selected
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public RndfEditor.Display.Utilities.IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool CanDelete
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public List<RndfEditor.Display.Utilities.IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(RndfEditor.Display.Utilities.IDisplayObject newSelection)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return true;
		}

		#endregion

		public void Click(Coordinates c)
		{
			if (Toolbox.Mode == SparseToolboxMode.Selection)
			{
				this.Toolbox.SelectPartition(c);
			}
			else if (Toolbox.Mode == SparseToolboxMode.Polygon)
			{
				if (this.Toolbox.Partition != null && this.Toolbox.tmpPolyCoords != null)
				{
					if (this.Toolbox.tmpPolyCoords.Count > 0 &&
						this.Toolbox.tmpPolyCoords[0].DistanceTo(c) < 2.5)
					{
						this.Toolbox.Partition.SparsePolygon = new Polygon(this.Toolbox.tmpPolyCoords);
						this.Toolbox.tmpPolyCoords = new List<Coordinates>();
						this.Toolbox.ResetButtons();
						this.Toolbox.Mode = SparseToolboxMode.None;
						this.Toolbox.Display.Invalidate();
					}
					else
						this.Toolbox.tmpPolyCoords.Add(c);
				}
			}
		}

		public void ShutDown()
		{
			this.Toolbox.ShutDown();
		}
	}
}
