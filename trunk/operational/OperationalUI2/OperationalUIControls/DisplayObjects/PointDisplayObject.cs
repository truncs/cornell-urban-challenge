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
	public class PointDisplayObject : IHittable, IAttachable<Coordinates>, IClearable, ISimpleColored {
		private Coordinates location = Coordinates.NaN;
		private ControlPointStyle style = ControlPointStyle.LargeX;
		private Color color;
		private string label;
		private ContentAlignment labelAlignment;

		private string name;

		public PointDisplayObject(string name, string label, Color color, ControlPointStyle style, ContentAlignment labelAlignment) {
			this.name = name;
			this.label = label;
			this.color = color;
			this.style = style;
			this.labelAlignment = labelAlignment;
		}

		public string Label {
			get { return label; }
			set { label = value; }
		}

		public ContentAlignment LabelAlignment {
			get { return labelAlignment; }
			set { labelAlignment = value; }
		}

		public ControlPointStyle Style {
			get { return style; }
			set { style = value; }
		}

		public Coordinates Location {
			get { return location; }
			set { location = value; }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (location.IsNaN) {
				return HitTestResult.NoHit;
			}

			if (location.DistanceTo(loc) < tol) {
				return new HitTestResult(this, true, location, null);
			}
			else {
				return HitTestResult.NoHit;
			}
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			if (location.IsNaN)
				return;

			DrawingUtility.DrawControlPoint(g, location, color, label, labelAlignment, style, wt);
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<Coordinates> Members

		public void SetCurrentValue(Coordinates value, string label) {
			location = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			location = Coordinates.NaN;
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
