using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common;

namespace Simulator.Display.DisplayObjects
{
	/// <summary>
	/// Displays a grid
	/// </summary>
	[Serializable]
	public class GridDisplay : IDisplayObject
	{
		/// <summary>
		/// size of the grid, default is 5m
		/// </summary>
		private float spacing = 5.0F;

		/// <summary>
		/// Color of the grid
		/// </summary>
		private Color color = Color.LightGray;

		/// <summary>
		/// Whether or not to show the grid
		/// </summary>
		private bool showGrid = true;

		/// <summary>
		/// Constructor
		/// </summary>
		public GridDisplay()
		{
		}

		/// <summary>
		/// Color of the grid
		/// </summary>
		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		/// <summary>
		/// The size of the grid
		/// </summary>
		public float Spacing
		{
			get { return spacing; }
			set { spacing = value; }
		}

		/// <summary>
		/// Whether or not to show the grid
		/// </summary>
		public bool ShowGrid
		{
			get { return showGrid; }
			set { showGrid = value; }
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Perform a test to see if this is selected
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="tol"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			// return a no-hit
			return new HitTestResult(this, false, float.MaxValue);
		}

		/// <summary>
		/// Renders the grid
		/// </summary>
		/// <param name="g"></param>
		/// <param name="t"></param>
		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			// check if we are showinghte grid
			Coordinates wll = t.WorldLowerLeft;
			Coordinates wur = t.WorldUpperRight;

			// sapcing
			double tmpSpacing = (double)spacing;

			// num x lines
			double xNum = Math.Ceiling(Math.Abs(wll.X - wur.X) / tmpSpacing);

			// num y lines
			double yNum = Math.Ceiling(Math.Abs(wll.Y - wur.Y) / tmpSpacing);

			// check length
			if (xNum < 400 && yNum < 400)
			{
				// pen width
				float pw = 1.25f / t.Scale;

				// pen
				using (Pen p = new Pen(color, pw))
				{
					// draw x lines
					for (double i = Math.Floor(wll.X / tmpSpacing) * tmpSpacing; i <= Math.Ceiling(wur.X / tmpSpacing) * tmpSpacing; i += tmpSpacing)
					{
						g.DrawLine(p, DrawingUtility.ToPointF(new Coordinates(i, wur.Y)), DrawingUtility.ToPointF(new Coordinates(i, wll.Y)));
					}

					// draw y lines
					for (double j = Math.Floor(wll.Y / tmpSpacing) * tmpSpacing; j <= Math.Ceiling(wur.Y / tmpSpacing) * tmpSpacing; j += tmpSpacing)
					{
						g.DrawLine(p, DrawingUtility.ToPointF(new Coordinates(wll.X, j)), DrawingUtility.ToPointF(new Coordinates(wur.X, j)));
					}
				}
			}

			/*PointF ll = new PointF((float)wll.X, (float)wll.Y);
			PointF ur = new PointF((float)wur.X, (float)wur.Y);

			float startX = (float)Math.Floor(wll.X / spacing) * spacing;
			float endX = (float)Math.Ceiling(wur.X / spacing) * spacing;
			float startY = (float)Math.Floor(wll.Y / spacing) * spacing;
			float endY = (float)Math.Ceiling(wur.Y / spacing) * spacing;

			using (Pen p = new Pen(color, 1 / t.Scale))
			{
				if (endX - startX / spacing < 400 && endY - startY / spacing < 400)
				{
					for (float x = startX; x <= endX; x += spacing)
					{
						g.DrawLine(p, x, ll.Y, x, ur.Y);
					}

					for (float y = startY; y <= endY; y += spacing)
					{
						g.DrawLine(p, ll.X, y, ur.X, y);
					}
				}
			}*/
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public SelectionType Selected
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

		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool CanDelete
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public List<IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// If we should draw the object
		/// </summary>
		/// <returns></returns>
		public bool ShouldDraw()
		{
			// return if we are shupposed to show the grid
			return showGrid;
		}

		#endregion
	}
}
