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
	public class CircleDisplayObject2 : IHittable, IAttachable<double>, IAttachable<Coordinates>, IClearable, ISimpleColored {
		// point at which we treat this as a line
		private const double curvature_tol = 1e-3;
		private const float nominal_pixel_width = 2.0f;

		private Coordinates edgePoint;
		private Coordinates tangent;
		private double curvature;

		private string edgePointLabel;
		private string tangentLabel;
		private string curvatureLabel;

		private Color color;

		private string name;

		public CircleDisplayObject2(string name, Color color) {
			this.name = name;
			this.color = color;
		}

		public CircleDisplayObject2() {
			edgePoint = Coordinates.NaN;
			tangent = Coordinates.NaN;
			curvature = double.NaN;
		}

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		public Coordinates EdgePoint {
			get { return edgePoint; }
			set { edgePoint = value; }
		}

		public Coordinates Tangent {
			get { return tangent; }
			set { tangent = value; }
		}

		public double Curvature {
			get { return curvature; }
			set { curvature = value; }
		}

		public string EdgePointLabel {
			get { return edgePointLabel; }
			set { edgePointLabel = value; }
		}

		public string TangentLabel {
			get { return tangentLabel; }
			set { tangentLabel = value; }
		}

		public string CurvatureLabel {
			get { return curvatureLabel; }
			set { curvatureLabel = value; }
		}

		public bool IsValid {
			get { return !(edgePoint.IsNaN || tangent.IsNaN || double.IsNaN(curvature)); }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (!IsValid) {
				return HitTestResult.NoHit;
			}

			// check if we're treating this as a line or as a circle
			if (Math.Abs(curvature) < 1e-3) {
				Line l = new Line(edgePoint, edgePoint + tangent);
				Coordinates closestPoint = l.ClosestPoint(loc);
				double dist = closestPoint.DistanceTo(loc);
				if (dist < tol) {
					return new HitTestResult(this, true, closestPoint, null);
				}
			}
			else {
				// treat as a circle
				Circle c = Circle.FromPointSlopeRadius(edgePoint, tangent, 1/curvature);
				Coordinates closestPoint = c.ClosestPoint(loc);
				double dist = closestPoint.DistanceTo(loc);
				if (dist < tol) {
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
			float penWidth = nominal_pixel_width / wt.Scale;
			IPen p = g.CreatePen();
			p.Width = penWidth;
			p.Color = color;

			// check if the radius is too big
			if (Math.Abs(curvature) < curvature_tol) {
				// draw a straight line
				p.DashStyle = DashStyle.Dash;
				Coordinates t = tangent.Normalize()*100;
				PointF start = Utility.ToPointF(edgePoint - t);
				PointF end = Utility.ToPointF(edgePoint + t);
				g.DrawLine(p, start, end);
			}
			else {
				// calculate center point
				Circle c = Circle.FromPointSlopeRadius(edgePoint, tangent, 1/curvature);
				Rect rect = c.GetBoundingRectangle();
				g.DrawEllipse(p, Utility.ToRectangleF(rect));
			}
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<double> Members

		public void SetCurrentValue(double value, string label) {
			if (label == curvatureLabel) {
				curvature = value;
			}
		}

		#endregion

		#region IAttachable<Coordinates> Members

		public void SetCurrentValue(Coordinates value, string label) {
			if (label == edgePointLabel) {
				edgePoint = value;
			}
			else if (label == tangentLabel) {
				tangent = value;
			}
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			edgePoint = Coordinates.NaN;
			tangent = Coordinates.NaN;
			curvature = double.NaN;
		}

		#endregion
	}
}
