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
	public class TrackedPathDisplayObject : IRenderable, IAttachable<Coordinates>, IClearable, ISimpleColored {
		private const float nom_pixel_width = 1.0f;
		private const double min_add_dist = 0.2;

		private List<PointF> points = new List<PointF>();
		private Coordinates lastPoint = Coordinates.NaN;
		private Color color;

		private string name;

		public TrackedPathDisplayObject(string name, Color color) {
			this.name = name;
			this.color = color;
		}

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			if (points == null || points.Count < 2) {
				return;
			}

			IPen pen = g.CreatePen();
			pen.Width = nom_pixel_width/wt.Scale;
			pen.Color = color;

			g.DrawLines(pen, points.ToArray());
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<Coordinates> Members

		public void SetCurrentValue(Coordinates value, string label) {
			if (lastPoint.IsNaN || lastPoint.DistanceTo(value) >= min_add_dist) {
				points.Add(Utility.ToPointF(value));
				lastPoint = value;
			}
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			points.Clear();
			lastPoint = Coordinates.NaN;
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
