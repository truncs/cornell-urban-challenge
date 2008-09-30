using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class PolygonDisplayObject : IHittable, IAttachable<Polygon>, IClearable, ISimpleColored, IProvideContextMenu {
		private const float nom_pixel_width = 1.0f;

		private Color color;
		private Polygon poly;
		private bool hitBody;

		private bool drawCentroid;
		private bool drawFilled;

		private string name;

		private ToolStripMenuItem[] menuItems;

		public PolygonDisplayObject(string name, Color color, bool hitBody) {
			this.name = name;
			this.color = color;
			this.hitBody = hitBody;
			this.drawCentroid = true;

			InitMenuItems();
		}

		private void InitMenuItems() {
			ToolStripMenuItem menuDrawCentroid = new ToolStripMenuItem("Draw Centroid", null, menuDrawCentroid_Click);
			ToolStripMenuItem menuDrawFilled = new ToolStripMenuItem("Draw Filled", null, menuDrawFilled_Click);

			menuItems = new ToolStripMenuItem[] { menuDrawCentroid, menuDrawFilled };
		}

		private void menuDrawCentroid_Click(object sender, EventArgs e) {
			this.drawCentroid = !this.drawCentroid;
		}

		private void menuDrawFilled_Click(object sender, EventArgs e) {
			this.drawFilled = !this.drawFilled;
		}

		public bool HitBody {
			get { return hitBody; }
			set { hitBody = value; }
		}

		public bool DrawCentroid {
			get { return drawCentroid; }
			set { drawCentroid = value; }
		}

		public bool DrawFilled {
			get { return drawFilled; }
			set { drawFilled = value; }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			if (poly == null)
				return RectangleF.Empty;
			else 
				return Utility.ToRectangleF(poly.CalculateBoundingRectangle());
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (poly == null)
				return HitTestResult.NoHit;

			Polygon originalPoly = poly;

			if (hitBody) {
				Polygon inflatedPoly = poly.Inflate(tol);
				bool hit = inflatedPoly.IsInside(loc);
				if (hit) {
					// find the closest point, see if it's in the original poly
					if (!originalPoly.IsInside(loc)) {
						// it's not in the original polygon, so it must be outside by tol
						// find the closest point on the edge of the original polygon
						double minDist = double.MaxValue;
						Coordinates closestPoint = loc;

						foreach (LineSegment ls in originalPoly.GetSegmentEnumerator()) {
							Coordinates testPoint = ls.ClosestPoint(loc);
							double dist = testPoint.DistanceTo(loc);
							if (dist < minDist) {
								minDist = dist;
								closestPoint = testPoint;
							}
						}

						return new HitTestResult(this, true, closestPoint, null);
					}
					else {
						return new HitTestResult(this, true, loc, null);
					}
				}
			}
			else {
				// find the closest point on the edge within tol
				double minDist = tol;
				Coordinates closestPoint = Coordinates.NaN;

				foreach (LineSegment ls in originalPoly.GetSegmentEnumerator()) {
					Coordinates testPoint = ls.ClosestPoint(loc);
					double dist = testPoint.DistanceTo(loc);
					if (dist < minDist) {
						minDist = dist;
						closestPoint = testPoint;
					}
				}

				if (!closestPoint.IsNaN) {
					return new HitTestResult(this, true, closestPoint, null);
				}
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			Polygon curPoly = poly;
			if (curPoly == null) {
				return;
			}

			IPen pen = g.CreatePen();
			pen.Width = nom_pixel_width/wt.Scale;
			pen.Color = color;

			if (drawFilled) {
				g.FillPolygon(Color.FromArgb(100, color), Utility.ToPointF(curPoly));
			}

			g.DrawPolygon(pen, Utility.ToPointF(curPoly));

			if (drawCentroid) {
				DrawingUtility.DrawControlPoint(g, curPoly.GetCentroid(), color, null, ContentAlignment.BottomCenter, ControlPointStyle.LargeX, wt);
			}

			pen.Dispose();
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<Polygon> Members

		public void SetCurrentValue(Polygon value, string label) {
			poly = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			poly = null;
		}

		#endregion

		#region ISimpleColored Members

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		#endregion

		#region IProvideContextMenu Members

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			return menuItems;
		}

		public void OnMenuOpening() {
			menuItems[0].Checked = drawCentroid;
			menuItems[1].Checked = drawFilled;
		}

		#endregion
	}
}
