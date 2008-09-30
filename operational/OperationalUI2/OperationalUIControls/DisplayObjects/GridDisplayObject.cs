using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

using UrbanChallenge.Common;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class GridDisplayObject : IRenderable, ISimpleColored {
		private const float nominal_pixel_width = 1.0f;
		private const float nominal_label_spacing = 3.0f;
		private const float min_pixel_spacing = 3.0f;
		private const float max_lines = 400;

		private Color color = Color.LightGray;
		private float spacing = 5;
		private Font labelFont = new Font("Segoe UI", 8.0f);
		private bool showLabels = true;

		private string name = "Grid";

		public Font LabelFont {
			get { return labelFont; }
			set { labelFont = value; }
		}

		public bool ShowLabels {
			get { return showLabels; }
			set { showLabels = value; }
		}

		public float Spacing {
			get { return spacing; }
			set { spacing = value; }
		}

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			Coordinates wll = wt.WorldLowerLeft;
			Coordinates wur = wt.WorldUpperRight;

			PointF ll = new PointF((float)wll.X, (float)wll.Y);
			PointF ur = new PointF((float)wur.X, (float)wur.Y);

			float startX = (float)Math.Floor(wll.X / spacing) * spacing;
			float endX = (float)Math.Ceiling(wur.X / spacing) * spacing;
			float startY = (float)Math.Floor(wll.Y / spacing) * spacing;
			float endY = (float)Math.Ceiling(wur.Y / spacing) * spacing;

			IPen p = g.CreatePen();
			p.Color = color;
			p.Width = nominal_pixel_width/wt.Scale;

			string formatString;
			if (spacing >= 1) {
				formatString = "F0";
			}
			else if (spacing >= 0.1) {
				formatString = "F1";
			}
			else if (spacing >= 0.01) {
				formatString = "F2";
			}
			else {
				formatString = "F4";
			}

			// find the largest value (in magnitude) that we'll need to draw, assuming this will be the max length string
			float testVal = Math.Max(Math.Max(Math.Abs(startX), Math.Abs(endX)), Math.Max(Math.Abs(startY), Math.Abs(endY)));
			string testString = testVal.ToString(formatString);
			SizeF nomStringSize = g.MeasureString(testString, labelFont);
			SizeF unitStringSize = g.MeasureString(testString + " m", labelFont);

			float pixelSpacing = spacing * wt.Scale;
			bool drawLabels = showLabels && pixelSpacing >= (nomStringSize.Width + nominal_label_spacing*2);
			bool drawUnits = pixelSpacing >= (unitStringSize.Width + nominal_label_spacing*2);

			float labelSpacing = nominal_label_spacing/wt.Scale;
			
			// don't draw if there are too many lines
			if ((endX - startX) / spacing <= max_lines && (endY - startY) / spacing <= max_lines && pixelSpacing >= min_pixel_spacing) {
				for (float x = startX; x <= endX; x += spacing) {
					g.DrawLine(p, new PointF(x, ll.Y), new PointF(x, ur.Y));
				}

				for (float y = startY; y <= endY; y += spacing) {
					g.DrawLine(p, new PointF(ll.X, y), new PointF(ur.X, y));
				}

				if (drawLabels) {
					float minX = ll.X + unitStringSize.Width/wt.Scale + 2*labelSpacing;
					for (float x = startX; x <= endX; x += spacing) {
						if (x > minX) {
							g.DrawString(x.ToString(formatString) + (drawUnits ? " m" : ""), labelFont, Color.Black, new PointF(x + labelSpacing, ll.Y + labelSpacing + nomStringSize.Height/wt.Scale));
						}
					}

					for (float y = startY; y <= endY; y += spacing) {
						g.DrawString(y.ToString(formatString) + (drawUnits ? " m" : ""), labelFont, Color.Black, new PointF(ll.X + labelSpacing, y + labelSpacing));
					}
				}
			}
		}

		public string Name {
			get { return name; }
			set { name = value; }
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
