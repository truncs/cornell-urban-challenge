using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public enum ControlPointStyle {
		None,
		SmallBox,
		LargeBox,
		LargeX,
		SmallX,
		LargeCircle,
		SmallCircle
	}

	public static class DrawingUtility {
		private const float cp_large_size = 9;
		private const float cp_small_size = 5;
		private const float cp_label_space = 5;

		private static readonly Font label_font = new Font("Verdana", 8.25f, FontStyle.Regular);
		public static void DrawControlPoint(IGraphics g, Coordinates loc, Color color, string label, ContentAlignment align, ControlPointStyle style, WorldTransform wt) {
			DrawControlPoint(g, loc, color, label, align, style, true, wt);
		}

		public static void DrawControlPoint(IGraphics g, Coordinates loc, Color color, string label, ContentAlignment align, ControlPointStyle style, bool drawTextBox, WorldTransform wt) {
			// figure out the size the control box needs to be in world coordinates to make it 
			// show up as appropriate in view coordinates

			// invert the scale
			float scaled_size = 0;
			if (style == ControlPointStyle.LargeBox || style == ControlPointStyle.LargeCircle || style == ControlPointStyle.LargeX) {
				scaled_size = cp_large_size / wt.Scale;
			}
			else {
				scaled_size = cp_small_size / wt.Scale;
			}

			float scaled_offset = 1 / wt.Scale;

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF(-scaled_size / 2, -scaled_size / 2, scaled_size, scaled_size);
			rect.Offset(Utility.ToPointF(loc));
			if (style == ControlPointStyle.LargeBox) {
				g.FillRectangle(Color.White, rect);

				// shrink the rect down a little (nominally 1 pixel)
				rect.Inflate(-scaled_offset, -scaled_offset);
				g.FillRectangle(color, rect);
			}
			else if (style == ControlPointStyle.LargeCircle) {
				g.FillEllipse(Color.White, rect);

				// shrink the rect down a little (nominally 1 pixel)
				rect.Inflate(-scaled_offset, -scaled_offset);
				g.FillEllipse(color, rect);
			}
			else if (style == ControlPointStyle.LargeX) {
				using (IPen p = g.CreatePen()) {
					p.Width = 3/wt.Scale;
					p.Color = Color.White;
					g.DrawLine(p, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Bottom));
					g.DrawLine(p, new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top));

					p.Width = scaled_offset;
					p.Color = color;
					g.DrawLine(p, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Bottom));
					g.DrawLine(p, new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top));
				}
			}
			else if (style == ControlPointStyle.SmallBox) {
				g.FillRectangle(color, rect);
			}
			else if (style == ControlPointStyle.SmallCircle) {
				g.FillEllipse(color, rect);
			}
			else if (style == ControlPointStyle.SmallX) {
				using (IPen p = g.CreatePen()) {
					p.Width = 3/wt.Scale;
					p.Color = color;
					g.DrawLine(p, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Bottom));
					g.DrawLine(p, new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top));
				}
			}

			if (!string.IsNullOrEmpty(label)) {
				SizeF strSize = g.MeasureString(label, label_font);
				float x = 0, y = 0;

				if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight) {
					x = (float)loc.X + cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter) {
					x = (float)loc.X - strSize.Width / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.BottomLeft || align == ContentAlignment.MiddleLeft || align == ContentAlignment.TopLeft) {
					x = (float)loc.X - (strSize.Width + cp_label_space) / wt.Scale;
				}

				if (align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomRight) {
					y = (float)loc.Y - cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleRight) {
					y = (float)loc.Y + strSize.Height / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.TopCenter || align == ContentAlignment.TopLeft || align == ContentAlignment.TopRight) {
					y = (float)loc.Y + (strSize.Height + cp_label_space) / wt.Scale;
				}

				PointF text_loc = new PointF(x, y);

				if (drawTextBox) {
					RectangleF text_rect = new RectangleF(text_loc.X - 4/wt.Scale, text_loc.Y - 4/wt.Scale, strSize.Width/wt.Scale, strSize.Height/wt.Scale);
					g.FillRectangle(Color.FromArgb(127, Color.White), text_rect);
				}

				g.DrawString(label, label_font, color, text_loc);
			}
		}

		public static void DrawArrow(IGraphics g, Coordinates startingLoc, Coordinates direction, double len, double headSize, Color lineColor, WorldTransform wt) {
			Coordinates endingLoc = startingLoc + direction.Normalize(len);
			Coordinates headPt0 = endingLoc + direction.Rotate(135*Math.PI/180.0).Normalize(headSize);
			Coordinates headPt1 = endingLoc + direction.Rotate(-135*Math.PI/180.0).Normalize(headSize);

			IPen pen = g.CreatePen();
			pen.Width = 3/wt.Scale;
			pen.Color = Color.White;

			PointF ptfStart = Utility.ToPointF(startingLoc);
			PointF ptfEnd = Utility.ToPointF(endingLoc);
			PointF ptfHeadPt0 = Utility.ToPointF(headPt0);
			PointF ptfHeadPt1 = Utility.ToPointF(headPt1);

			PointF[] headPts = new PointF[] { ptfHeadPt0, ptfEnd, ptfHeadPt1 };
			g.DrawLine(pen, ptfStart, ptfEnd);
			g.DrawLines(pen, headPts);

			pen.Width = 1/wt.Scale;
			pen.Color = lineColor;
			g.DrawLine(pen, ptfStart, ptfEnd);
			g.DrawLines(pen, headPts);
		}

		public static void DrawControlLine(IGraphics g, Color c, DashStyle style, Coordinates loc1, Coordinates loc2, WorldTransform wt) {
			float pw = 1.25f / wt.Scale;

			using (IPen p = g.CreatePen()) {
				p.Color = c;
				p.Width = pw;
				p.DashStyle = style;

				g.DrawLine(p, Utility.ToPointF(loc1), Utility.ToPointF(loc2));
			}
		}
	}
}
