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
	public class CircleDisplayObject : IHittable, IAttachable<Circle>, IClearable, ISimpleColored {
		private const float nominal_pixel_width = 2.0f;

		private Circle circle = Circle.Infinite;

		private Color color;

		private string name;

		public CircleDisplayObject(string name, Color color) {
			this.name = name;
			this.color = color;
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			if (circle.Equals(Circle.Infinite))
				return RectangleF.Empty;
			else
				return Utility.ToRectangleF(circle.GetBoundingRectangle());
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (circle.Equals(Circle.Infinite))
				return HitTestResult.NoHit;

			Coordinates closestPoint = circle.ClosestPoint(loc);
			double dist = closestPoint.DistanceTo(loc);
			if (dist < tol) {
				return new HitTestResult(this, true, closestPoint, null);
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			if (circle.Equals(Circle.Infinite))
				return;

			float penWidth = nominal_pixel_width / wt.Scale;
			IPen p = g.CreatePen();
			p.Width = penWidth;
			p.Color = color;
			g.DrawEllipse(p, Utility.ToRectangleF(circle.GetBoundingRectangle()));
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<Circle> Members

		public void SetCurrentValue(Circle value, string label) {
			circle = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			circle = Circle.Infinite;
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
