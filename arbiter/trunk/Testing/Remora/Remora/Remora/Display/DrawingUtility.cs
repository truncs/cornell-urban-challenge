using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;
using System.Drawing.Drawing2D;

namespace Remora.Display
{
	/// <summary>
	/// Controls the type of point we are drawing
	/// </summary>
	public enum ControlPointStyle
	{
		SmallBox,
		LargeBox,
		LargeX,
		SmallX,
		LargeCircle,
		SmallCircle
	}

	/// <summary>
	/// Used to draw the objects at the right size to the screen
	/// </summary>
	public class DrawingUtility
	{
		// Obstacle
		public static Color pointObstacleColor = Color.Maroon;

		// General
		public static bool DrawRndf = true;
		public static bool DrawCurrentLocation = true;

		// Vehicle
		public static readonly Color NormalVehicleColor = Color.Green;
		public static readonly Color OurVehicleColor = Color.SkyBlue;

		// User Partitions //
		public static bool DrawUserPartitions = true;
		public static readonly Color UserPartitionWay1Color = Color.SlateBlue;
		public static readonly Color UserPartitionWay2Color = Color.SeaGreen;

		// User Waypoints //
		public static bool DrawUserWaypoints = false;
		public static bool DrawUserWaypointText = false;
		public static readonly Color UserWaypointColor = Color.MediumTurquoise;		

		// Lane Partitions //
		public static bool DrawLanePartitions = false;
		public static readonly Color LanePartitionWay1Color = Color.DarkSlateBlue;
		public static readonly Color LanePartitionWay2Color = Color.DarkSeaGreen;

		// Rndf Waypoints //
		public static bool DrawRndfWaypoints = true;
		public static bool DrawRndfWaypointText = false;
		public static readonly Color WaypointColor = Color.LimeGreen;
		public static readonly Color CheckpointColor = Color.Orange;
		public static readonly Color StoplineColor = Color.Red;
		public static readonly Color CheckStopColor = Color.Pink;
		public static bool DisplayRndfGoals = false;

		// Interconnects //
		public static bool DrawInterconnects = false;
		public static readonly Color InterconnectColor = Color.LimeGreen;
		public static bool DisplayIntersectionBounds = true;
		public static readonly Color IntersectionAreaColor = Color.Lavender;

		// Other //
		private const float cp_large_size = 8;
		private const float cp_small_size = 5;
		private const float cp_label_space = 8;
        private static Font labelFont;

		// Route //
		public static Color RouteColor = Color.Black;
		public static Color CurrentGoalColor = Color.Purple;
		public static bool DisplayCurrentGoal = true;
		public static bool DisplayFullRoute = true;

		// Splines //
		public static bool DisplayLaneSplines = true;
		public static bool DisplayIntersectionSplines = false;

		// Lane Path //
		public static Color ArbiterLanePath = Color.PaleVioletRed;
		public static bool DrawArbiterLanePath = false;
		public static Color OperationalLanePath = Color.Orange;
		public static bool DrawOperationalLanePath = false;

		// Pose Log //
		public static bool LogPose = true;
		public static bool RestartPoseLog = false;
		public static bool DisplayPoseLog = true;

		// vehicles
		public static bool DisplayDeletedVehicles = false;
		public static Color OccludedVehicleColor = Color.Red;


        #region Constructors

        /// <summary>
        /// Create teh drawing utility
        /// </summary>
        static DrawingUtility()
        {
            labelFont = new Font("Verdana", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
        }

        #endregion


		#region Drawing Object

		public static void DrawVehicle(Coordinates loc, double angle, Graphics g, WorldTransform t)
		{
		}

		public static void DrawControlLine(Coordinates loc1, Coordinates loc2, Color c, Graphics g, WorldTransform wt)
		{
			float pw = 1.25f / wt.Scale;

			using (Pen p = new Pen(c, pw))
			{
				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}

		public static void DrawColoredControlLine(Color c, Coordinates loc1, Coordinates loc2, Graphics g, WorldTransform wt)
		{
			float pw = 1.25f / wt.Scale;

			using (Pen p = new Pen(c, pw))
			{
				p.DashStyle = DashStyle.Dash;

				g.DrawLine(p, ToPointF(loc1), ToPointF(loc2));
			}
		}


		/// <summary>
		/// Checks what type parameters the waypoint holds and sets color accordingly
		/// </summary>
		/// <param name="wp"></param>
		/// <returns></returns>
		public static Color GetWaypointColor(IWaypoint wp)
		{
			// check if its correct type of waypoint
			if (wp is RndfWayPoint)
			{
				// cast
				RndfWayPoint waypointData = (RndfWayPoint)wp;

				// assign color based upon parameters
				if (waypointData.IsStop && waypointData.IsCheckpoint)
				{
					return CheckStopColor;
				}
				else if (waypointData.IsCheckpoint)
				{
					return CheckpointColor;
				}
				else if (waypointData.IsStop)
				{
					return StoplineColor;
				}
				else
				{
					return WaypointColor;
				}
			}
			else if(wp is UserWaypoint)
			{
				return UserWaypointColor;
			}
			else
			{
				throw new ArgumentException("argument not WaypointData type", "wp");
			}
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
			RectangleF rect = GetBoundingBox(loc, style, wt);

			#region Draw the object

			float scaled_offset = 1 / wt.Scale;

			// draw a small circle
			if (style == ControlPointStyle.SmallCircle)
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

		#endregion

		#region Utility

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

		public static PointF ToPointF(Coordinates c)
		{
			return new PointF((float)c.X, (float)c.Y);
		}

		public static Coordinates ToCoord(PointF pt)
		{
			return new Coordinates(pt.X, pt.Y);
		}

		#endregion
	}
}
