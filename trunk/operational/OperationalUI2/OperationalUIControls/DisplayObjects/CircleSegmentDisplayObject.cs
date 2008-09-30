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
	public class CircleSegmentDisplayObject : IRenderable, IAttachable<CircleSegment>, IClearable {
		private const float nomPixelWidth = 1;

		bool valid;
		CircleSegment segment;
		Color color;
		string name;

		public CircleSegmentDisplayObject(string name, Color color) {
			this.name = name;
			this.color = color;
		}

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			IPen p = g.CreatePen();
			p.Color = color;
			p.Width = nomPixelWidth / wt.Scale;

			PointF[] points = Utility.ToPointF(segment.ToPoints(30));
			g.DrawLines(p, points);

			p.Dispose();
		}

		public string Name {
			get { return name; }
		}

		#endregion

		#region IAttachable<CircleSegment> Members

		public void SetCurrentValue(CircleSegment value, string label) {
			valid = true;
			segment = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			valid = false;
		}

		#endregion
	}
}
