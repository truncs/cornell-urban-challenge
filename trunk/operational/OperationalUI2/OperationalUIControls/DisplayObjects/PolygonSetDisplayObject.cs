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

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class PolygonSetDisplayObject : IHittable, IAttachable<Polygon[]>, IClearable, ISimpleColored {
		private const float nom_pixel_width = 1.0f;

		private Polygon[] polys;
		private Color color;
		private bool hitBody;

		private string name;

		public PolygonSetDisplayObject(string name, Color color, bool hitBody) {
			this.name = name;
			this.color = color;
			this.hitBody = hitBody;
		}

		public bool HitBody {
			get { return hitBody; }
			set { hitBody = value; }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			Polygon[] polys = this.polys;
			if (polys == null) {
				return HitTestResult.NoHit;
			}

			// return the hit for the first polygon that is a hit
			foreach (Polygon poly in polys) {
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
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			Polygon[] polys = this.polys;
			if (polys == null)
				return;

			IPen p = g.CreatePen();
			p.Width = nom_pixel_width/wt.Scale;
			p.Color = color;

			foreach (Polygon poly in polys) {
				g.DrawPolygon(p, Utility.ToPointF(poly));
			}

			p.Dispose();
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<Polygon[]> Members

		public void SetCurrentValue(Polygon[] value, string label) {
			polys = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			polys = null;
		}

		#endregion

		#region ISimpleColored Members

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		#endregion
	}
}
