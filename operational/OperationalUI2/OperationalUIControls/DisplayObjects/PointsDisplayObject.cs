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
	public class PointsDisplayObject : IHittable, IAttachable<Coordinates[]>, IClearable, ISimpleColored {
		private string name;
		private Color color;
		private Coordinates[] points;
		private ControlPointStyle style;

		public PointsDisplayObject(string name, Color color, ControlPointStyle style){
			this.name = name;
			this.color = color;
			this.style = style;
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (points == null) {
				return HitTestResult.NoHit;
			}

			double dist = tol;
			Coordinates pt = Coordinates.NaN;
			for (int i = 0; i < points.Length; i++) {
				if (points[i].DistanceTo(loc) < dist) {
					dist = points[i].DistanceTo(loc);
					pt = points[i];
				}
			}

			if (!pt.IsNaN) {
				return new HitTestResult(this, true, pt, null);
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			if (points == null)
				return;

			for (int i = 0; i < points.Length; i++) {
				DrawingUtility.DrawControlPoint(g, points[i], color, null, ContentAlignment.BottomCenter, style, wt);
			}
		}

		public string Name {
			get { return name; }
		}

		#endregion

		#region IAttachable<Coordinates[]> Members

		public void SetCurrentValue(Coordinates[] value, string label) {
			points = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			points = null;
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
