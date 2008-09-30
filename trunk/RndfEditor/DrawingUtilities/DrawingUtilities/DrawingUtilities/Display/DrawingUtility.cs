using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace RndfEditor.Display.Utilities
{
	/// <summary>
	/// Controls the type of point we are drawing
	/// </summary>
	public enum ControlPointStyle
	{
		None,
		SmallBox,
		LargeBox,
		LargeX,
		SmallX,
		LargeCircle,
		SmallCircle
	}

	/// <summary>
	/// Drawing Utility helps draw objects at the right size to the screen
	/// </summary>
	[Serializable]
	public static class DrawingUtility
	{
		#region Initialize brushes and pens

		public const float cp_large_size = 8;
		private const float cp_small_size = 5;
		private const float cp_label_space = 8;

		private static Font labelFont;

		#endregion

		#region Constructors

		/// <summary>
		/// Create the drawing utility
		/// </summary>
		static DrawingUtility()
		{
			labelFont = new Font("Verdana", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
		}

		#endregion

		#region Tool Draw

		public static Color ColorRndfEditorTemporaryPoint = Color.RoyalBlue;
		public static bool DrawRndfEditorTemporaryPoint = true;
		public static bool DrawRndfEditorTemporaryPointId = false;

		public static Color ColorArbiterIntersectionWrappingHelpers = Color.Purple;

		public static Color ColorToolAngle = Color.DeepSkyBlue;
		public static bool DrawToolAngle = true;

		public static Color ColorToolRuler = Color.DeepSkyBlue;
		public static bool DrawToolRuler = true;

		public static Color ColorToolPointAnalysis = Color.Purple;
		public static bool DrawToolPointAnalysis = true;

		#endregion

		#region Arbiter Draw

		public static bool DrawArbiterIntersections = false;
		public static Color ColorArbiterIntersectionBoundaryPolygon = Color.SkyBlue;
		public static Color ColorArbiterIntersectionStoppedExit = Color.RosyBrown;
		public static Color ColorArbiterIntersectionIncomingLanePoints = Color.SlateBlue;
		public static Color ColorArbiterIntersectionExits = Color.Red;
		public static Color ColorArbiterIntersection = Color.SkyBlue;
		public static Color ColorArbiterIntersectionEntries = Color.Blue;

		public static bool DrawArbiterSafetyZones = false;
		public static Color ColorArbiterSafetyZone = Color.Red;		

		public static bool DrawArbiterInterconnects = false;
		public static Color ColorArbiterInterconnect = Color.LightSeaGreen;

		public static bool DisplayArbiterWaypointCheckpointId = false;
		public static Color ColorDisplayArbiterCheckpoint = Color.Orange;

		public static Color ColorArbiterWaypoint = Color.MediumSeaGreen;
		public static Color ColorArbiterWaypointStop = Color.Red;
		public static Color ColorArbiterWaypointCheckpoint = Color.Orange;
		public static Color ColorArbiterWaypointStopCheckpoint = Color.OrangeRed;
		public static bool DrawArbiterWaypoint = true;
		public static bool DisplayArbiterWaypointId = false;

		public static Color ColorArbiterUserPartition = Color.PaleVioletRed;
		public static bool DrawArbiterUserPartition = false;

		public static Color ColorArbiterUserWaypoint = Color.RoyalBlue;
		public static Color ColorArbiterUserWaypointSelected = Color.Red;
		public static bool DrawArbiterUserWaypoint = true;
		public static bool DisplayArbiterUserWaypointId = false;

		public static Color ColorArbiterLaneSpline = Color.SeaGreen;
		public static bool DrawArbiterLaneSpline = false;

		public static Color ColorArbiterLanePartitionDefault = Color.BlueViolet;
		public static Color ColorArbiterLanePartitionWay1 = Color.DarkBlue;
		public static Color ColorArbiterLanePartitionWay2 = Color.DarkOrange;
		public static bool DrawArbiterLanePartition = true;
		public static bool DrawArbiterLanePartitionWays = false;

		public static Color ColorArbiterZoneColor = Color.Purple;
		public static bool DisplayArbiterZone = true;

		public static Color ColorArbiterParkingSpotWaypointSelected = Color.Red;
		public static Color ColorArbiterParkingSpotWaypointCheckpoint = Color.OrangeRed;
		public static Color ColorArbiterParkingSpotWaypoint = Color.SaddleBrown;
		public static bool DrawArbiterParkingSpotWaypoint = true;
		public static bool DisplayArbiterParkingSpotWaypointId = false;

		public static Color ColorArbiterPerimeterWaypointSelected = Color.Red;
		public static Color ColorArbiterPerimeterWaypoint = Color.Brown;
		public static bool DrawArbiterPerimeterWaypoint = true;
		public static bool DisplayArbiterPerimeterWaypointId = false;

		public static Color ColorArbiterParkingSpot = Color.Brown;
		public static bool DrawArbiterParkingSpot = true;

		public static Color ColorArbiterPerimiter = Color.SaddleBrown;
		public static bool DrawArbiterPerimeter = true;

		public static Color ColorArbiterZoneStayOutPolygon = Color.Chocolate;
		public static Color ColorArbiterZoneNavigationPoints = Color.LightSeaGreen;
		public static bool DrawArbiterZoneMap = false;

		public static bool DisplayArbiterLanes = false;
		public static bool DisplayArbiterLanePath = false;

		public static Color ColorArbiterLanePolygon = Color.DarkGreen;
		public static bool DisplayArbiterLanePolygon1 = false;
		public static bool DisplayArbiterLanePolygon2 = false;
		public static bool DisplayArbiterLanePolygon3 = false;
		public static bool DisplayArbiterLanePolygon4 = false;

		#endregion

		#region Car Draw

		public static bool DrawAiVehicle = true;
		public static bool DrawTrafficVehicles = true;
		public static bool DrawDeletedVehicles = false;

		public static bool DrawSimObstacles = true;
		public static bool DrawSimObstacleIds = false;
		public static Color ColorSimObstacles = Color.Red;
		public static Color ColorSimSelectedObstacle = Color.Orange;
		public static Color ColorSimObstacleBlockage = Color.Black;

		public static bool DrawSimCars = true;
		public static bool DrawSimCarId = false;
		public static bool DrawSimCarDeleted = false;
		public static Color ColorSimDeletedCar = Color.Black;
		public static Color ColorSimTrafficCar = Color.SeaGreen;
		public static Color ColorSimAiCar = Color.SkyBlue;
		public static Color ColorSimUnboundCar = Color.Navy;
		public static Color ColorSimSelectedVehicle = Color.DarkOrange;

		#endregion

		#region Information Draw

		public static Color ColorNavigationBest = Color.Green;
		public static Color ColorNavigation2ndBest = Color.DarkBlue;
		public static bool DrawNavigationRoutes = true;
		public static bool DrawNavigationBestRoute = true;
		public static bool DrawNavigation2ndBestRoute = false;

		#endregion

		#region Sim Draw

		#endregion

		/// <summary>
		/// Gets bounding box to make object show correctly in viewer
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="style"></param>
		/// <param name="wt"></param>
		/// <returns></returns>
		public static RectangleF GetBoundingBox(Coordinates loc, ControlPointStyle style, WorldTransform wt)
		{
			// figure out the size the control box needs to be in world coordinates to make it 
			// show up as appropriate in view coordinates

			// invert the scale
			float scaled_offset = 1 / wt.Scale;
			float scaled_size = 0;
			if (style == ControlPointStyle.LargeBox || style == ControlPointStyle.LargeCircle || style == ControlPointStyle.LargeX)
			{
				scaled_size = cp_large_size / wt.Scale;
			}
			else
			{
				scaled_size = cp_small_size / wt.Scale;
			}

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)loc.X - scaled_size / 2, (float)loc.Y - scaled_size / 2, scaled_size, scaled_size);
			return rect;
		}

		/// <summary>
		/// Draw a point in the world
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="color"></param>
		/// <param name="label"></param>
		/// <param name="align"></param>
		/// <param name="style"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		public static void DrawControlPoint(Coordinates loc, Color color, string label, ContentAlignment align, ControlPointStyle style, Graphics g, WorldTransform wt)
		{
			// Determine size of bounding box
			//RectangleF rect = GetBoundingBox(loc, style, wt);
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = 0;
			if (style == ControlPointStyle.LargeBox || style == ControlPointStyle.LargeCircle || style == ControlPointStyle.LargeX)
			{
				scaled_size = cp_large_size / wt.Scale;
			}
			else
			{
				scaled_size = cp_small_size / wt.Scale;
			}

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)loc.X - scaled_size / 2, (float)loc.Y - scaled_size / 2, scaled_size, scaled_size);

			if (style == ControlPointStyle.LargeBox)
			{
				g.FillRectangle(Brushes.White, rect);

				// shrink the rect down a little (nominally 1 pixel)
				rect.Inflate(-scaled_offset, -scaled_offset);
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillRectangle(b, rect);
				}
			}
			else if (style == ControlPointStyle.LargeCircle)
			{
				g.FillEllipse(Brushes.White, rect);

				// shrink the rect down a little (nominally 1 pixel)
				rect.Inflate(-scaled_offset, -scaled_offset);
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillEllipse(b, rect);
				}
			}
			else if (style == ControlPointStyle.LargeX)
			{
				using (Pen p = new Pen(Color.White, 3 / wt.Scale))
				{
					p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
					g.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Bottom);
					g.DrawLine(p, rect.Left, rect.Bottom, rect.Right, rect.Top);
				}

				using (Pen p = new Pen(color, scaled_offset))
				{
					p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
					g.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Bottom);
					g.DrawLine(p, rect.Left, rect.Bottom, rect.Right, rect.Top);
				}
			}
			else if (style == ControlPointStyle.SmallBox)
			{
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillRectangle(b, rect);
				}
			}
			else if (style == ControlPointStyle.SmallCircle)
			{
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillEllipse(b, rect);
				}
			}
			else if (style == ControlPointStyle.SmallX)
			{
				using (Pen p = new Pen(color, 3 / wt.Scale))
				{
					p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
					g.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Bottom);
					g.DrawLine(p, rect.Left, rect.Bottom, rect.Right, rect.Top);
				}
			}

			#region Draw the object

			// draw a small circle
			/*if (style == ControlPointStyle.SmallCircle)
			{
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillEllipse(b, rect);
				}
			}
			else if (style == ControlPointStyle.LargeX)
			{
				using (Pen p = new Pen(Color.White, 3 / wt.Scale))
				{
					p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
					g.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Bottom);
					g.DrawLine(p, rect.Left, rect.Bottom, rect.Right, rect.Top);
				}

				using (Pen p = new Pen(color, scaled_offset))
				{
					p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
					g.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Bottom);
					g.DrawLine(p, rect.Left, rect.Bottom, rect.Right, rect.Top);
				}
			}*/

			if (label != null)
			{
				SizeF strSize = g.MeasureString(label, labelFont);
				float x = 0, y = 0;

				if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight)
				{
					x = (float)loc.X + cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter)
				{
					x = (float)loc.X - strSize.Width / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.BottomLeft || align == ContentAlignment.MiddleLeft || align == ContentAlignment.TopLeft)
				{
					x = (float)loc.X - (strSize.Width + cp_label_space) / wt.Scale;
				}

				if (align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomRight)
				{
					y = (float)loc.Y - cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleRight)
				{
					y = (float)loc.Y + strSize.Height / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.TopCenter || align == ContentAlignment.TopLeft || align == ContentAlignment.TopRight)
				{
					y = (float)loc.Y + (strSize.Height + cp_label_space) / wt.Scale;
				}

				PointF[] pts = new PointF[] { new PointF(x, y) };
				g.Transform.TransformPoints(pts);
				PointF text_loc = pts[0];

				Matrix t = g.Transform.Clone();
				g.ResetTransform();
				using (SolidBrush b = new SolidBrush(color))
				{
					g.DrawString(label, labelFont, b, text_loc);
				}
				g.Transform = t;
			}

			#endregion
		}

		/// <summary>
		/// Draw a point in the world
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="color"></param>
		/// <param name="label"></param>
		/// <param name="align"></param>
		/// <param name="style"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		public static void DrawControlLabel(Coordinates loc, Color color, string label, ContentAlignment align, ControlPointStyle style, Graphics g, WorldTransform wt)
		{
			// Determine size of bounding box
			//RectangleF rect = GetBoundingBox(loc, style, wt);
			float scaled_offset = 1 / wt.Scale;

			if (label != null)
			{
				SizeF strSize = g.MeasureString(label, labelFont);
				float x = 0, y = 0;

				if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight)
				{
					x = (float)loc.X + cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter)
				{
					x = (float)loc.X - strSize.Width / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.BottomLeft || align == ContentAlignment.MiddleLeft || align == ContentAlignment.TopLeft)
				{
					x = (float)loc.X - (strSize.Width + cp_label_space) / wt.Scale;
				}

				if (align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomRight)
				{
					y = (float)loc.Y - cp_label_space / wt.Scale;
				}
				else if (align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleRight)
				{
					y = (float)loc.Y + strSize.Height / (2 * wt.Scale);
				}
				else if (align == ContentAlignment.TopCenter || align == ContentAlignment.TopLeft || align == ContentAlignment.TopRight)
				{
					y = (float)loc.Y + (strSize.Height + cp_label_space) / wt.Scale;
				}

				PointF[] pts = new PointF[] { new PointF(x, y) };
				g.Transform.TransformPoints(pts);
				PointF text_loc = pts[0];

				Matrix t = g.Transform.Clone();
				g.ResetTransform();
				using (SolidBrush b = new SolidBrush(color))
				{
					g.DrawString(label, labelFont, b, text_loc);
				}
				g.Transform = t;
			}
		}

		/// <summary>
		/// Draw a line
		/// </summary>
		/// <param name="loc1"></param>
		/// <param name="loc2"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		/// <param name="p"></param>
		/// <param name="c"></param>
		public static void DrawControlLine(Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt, Pen p, Color c)
		{
			float pw = 1.25f / wt.Scale;

			using (p = new Pen(c, pw))
			{
				p.EndCap = LineCap.ArrowAnchor;
				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		/// <summary>
		/// Draw a line
		/// </summary>
		/// <param name="loc1"></param>
		/// <param name="loc2"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		/// <param name="p"></param>
		/// <param name="c"></param>
		public static void DrawControlLine(LinePath lp, Graphics g, WorldTransform wt, Pen p, Color c)
		{
			float pw = 1.25f / wt.Scale;

			using (p = new Pen(c, pw))
			{
				for (int i = 0; i < lp.Count - 1; i++)
				{
					Coordinates loc1 = lp[i];
					Coordinates loc2 = lp[i + 1];
					p.EndCap = LineCap.ArrowAnchor;
					g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
				}
			}
		}

		/// <summary>
		/// Draw a polygon
		/// </summary>
		/// <param name="p"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		public static void DrawControlPolygon(Polygon p, Color c, DashStyle d, Graphics g, WorldTransform wt)
		{
			// points of poly
			List<Coordinates> points = p.points;

			// loop and draw
			for (int i = 0; i < points.Count; i++)
			{
				if (i == points.Count - 1)
				{
					DrawColoredControlLine(c, d, points[i], points[0], g, wt);
				}
				else
				{
					DrawColoredControlLine(c, d, points[i], points[i + 1], g, wt);
				}
			}
		}

		/// <summary>
		/// Draw a thick line
		/// </summary>
		/// <param name="loc1"></param>
		/// <param name="loc2"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		/// <param name="p"></param>
		/// <param name="c"></param>
		public static void DrawThickControlLine(Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt, Pen p, Color c)
		{
			float pw = 1.25f / (wt.Scale / 2);

			using (p = new Pen(c, pw))
			{
				p.EndCap = LineCap.ArrowAnchor;
				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		/// <summary>
		/// Draw a line
		/// </summary>
		/// <param name="loc1"></param>
		/// <param name="loc2"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		/// <param name="p"></param>
		/// <param name="c"></param>
		public static void DrawBlockedLine(Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt)
		{
			float pw = 2;

			using (Pen p = new Pen(Color.Black, pw))
			{
				p.DashStyle = DashStyle.Dash;
				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		/// <summary>
		/// Draw a line
		/// </summary>
		/// <param name="loc1"></param>
		/// <param name="loc2"></param>
		/// <param name="g"></param>
		/// <param name="wt"></param>
		/// <param name="p"></param>
		/// <param name="c"></param>
		public static void DrawControlLine(Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt, Pen p, Color i, Color f)
		{
			float pw = 1.25f / wt.Scale;

			using (p = new Pen(new LinearGradientBrush(ToPointF(loc1), ToPointF(loc2), i, f), pw))
			{
				p.EndCap = LineCap.ArrowAnchor;
				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}
		
		public static PointF ToPointF(Coordinates c)
		{
			return new PointF((float)c.X, (float)c.Y);
		}

		public static Coordinates ToCoord(PointF pt)
		{
			return new Coordinates(pt.X, pt.Y);
		}

		public static readonly DisplayObjectFilter DefaultFilter = new DisplayObjectFilter(delegate(IDisplayObject obj) { return true; });

		public static RectangleF CalcBoundingBox(params PointF[] pts)
		{
			float minx = float.MaxValue, miny = float.MaxValue;
			float maxx = float.MinValue, maxy = float.MinValue;

			for (int i = 0; i < pts.Length; i++)
			{
				if (pts[i].X < minx)
					minx = pts[i].X;

				if (pts[i].X > maxx)
					maxx = pts[i].X;

				if (pts[i].Y < miny)
					miny = pts[i].Y;

				if (pts[i].Y > maxy)
					maxy = pts[i].Y;
			}

			return new RectangleF(minx, miny, maxx - minx, maxy - miny);
		}

		public static RectangleF CalcBoundingBox(params Coordinates[] coords)
		{
			PointF[] pts = new PointF[coords.Length];
			for (int i = 0; i < pts.Length; i++)
				pts[i] = new PointF((float)coords[i].X, (float)coords[i].Y);

			return CalcBoundingBox(pts);
		}

		public static void DrawControlLine(Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt)
		{
			float pw = 1.25f / wt.Scale;

			using (Pen p = new Pen(Color.White, pw))
			{
				p.DashStyle = DashStyle.Dash;

				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		public static void DrawColoredControlLine(Color c, DashStyle d, Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt)
		{
			float pw = 1.25f / wt.Scale;

			using (Pen p = new Pen(c, pw))
			{
				p.DashStyle = d;

				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		public static void DrawColoredArrowControlLine(Color c, DashStyle d, Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt)
		{
			float pw = 1.25f / wt.Scale;
			
			using (Pen p = new Pen(c, pw))
			{
				p.EndCap = LineCap.ArrowAnchor;
				p.DashStyle = d;				

				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		public static IDisplayObject GetRoot(IDisplayObject obj)
		{
			if (obj == null)
				return null;

			while (obj.Parent != null)
			{
				obj = obj.Parent;
			}

			return obj;
		}
	}
}
