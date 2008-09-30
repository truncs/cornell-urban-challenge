using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using UrbanChallenge.Common;

namespace Remora.Display
{
	public class DisplayGrid : IDisplayObject
	{
		private float spacing = 5;
		private Color color = Color.LightGray;

		public float Spacing
		{
			get { return spacing; }
			set { spacing = value; }
		}

		#region IDisplayObject Members

		public RectangleF BoundingBox
		{
			get { return RectangleF.Empty; }
		}

		public HitTestResult HitTest(Coordinates loc, float tol)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(Graphics g, WorldTransform t)
		{
			Coordinates wll = t.WorldLowerLeft;
			Coordinates wur = t.WorldUpperRight;

			PointF ll = new PointF((float)wll.X, (float)wll.Y);
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
			}
		}

		public bool MoveAllowed
		{
			get { return false; }
		}

		public void BeingMove(Coordinates orig, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public SelectionMode Selected
		{
			get
			{
				return SelectionMode.NotSelected;
			}
			set
			{
				// do nothing
			}
		}


		public IDisplayObject Parent
		{
			get { return null; }
		}

		#endregion
	}
}
