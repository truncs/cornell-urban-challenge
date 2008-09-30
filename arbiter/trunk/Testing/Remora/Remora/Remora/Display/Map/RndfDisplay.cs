using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common;
using GpsToolkit;
using UrbanChallenge.Common.Splines;

namespace Remora.Display
{
	public class RndfDisplay : IDisplayObject
	{
		private RndfNetwork rndf;

		public RndfDisplay(RndfNetwork rndf)
		{
			this.rndf = rndf;
		}

		public RndfNetwork Rndf
		{
			get { return rndf; }
			set { rndf = value; }
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if(rndf != null && DrawingUtility.DrawRndf)
			{
				foreach (Segment segment in rndf.Segments.Values)
				{
					foreach (Way way in segment.Ways.Values)
					{
						foreach (Lane lane in way.Lanes.Values)
						{
							foreach (LanePartition lanePartition in lane.LanePartitions)
							{
								// Draw Line representing the Lane Partition w/ added WayColor if it is wanted
								if (DrawingUtility.DrawLanePartitions)
								{
									if (way.WayID.WayNumber == 1)
										DrawingUtility.DrawControlLine(lanePartition.InitialWaypoint.Position, lanePartition.FinalWaypoint.Position, DrawingUtility.LanePartitionWay1Color, g, t);
									else
										DrawingUtility.DrawControlLine(lanePartition.InitialWaypoint.Position, lanePartition.FinalWaypoint.Position, DrawingUtility.LanePartitionWay2Color, g, t);
								}

								foreach (UserPartition userPartition in lanePartition.UserPartitions)
								{
									// Draw Line representing the User Partition w/ added WayColor if it is wanted
									if (DrawingUtility.DrawUserPartitions)
									{
										if (way.WayID.WayNumber == 1)
											DrawingUtility.DrawControlLine(userPartition.InitialWaypoint.Position, userPartition.FinalWaypoint.Position, DrawingUtility.UserPartitionWay1Color, g, t);
										else
											DrawingUtility.DrawControlLine(userPartition.InitialWaypoint.Position, userPartition.FinalWaypoint.Position, DrawingUtility.UserPartitionWay2Color, g, t);
									}
									
									// Draw Final User Waypoints if User Waypoints and if wanted
									if (DrawingUtility.DrawUserWaypoints)
									{
										if (userPartition.FinalWaypoint is UserWaypoint)
										{
											Color c = DrawingUtility.GetWaypointColor(userPartition.FinalWaypoint);
											
											if(DrawingUtility.DrawUserWaypointText)
											{
												DrawingUtility.DrawControlPoint(userPartition.FinalWaypoint.Position, c, userPartition.FinalWaypoint.ToString(), ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
											}
											else
											{
												DrawingUtility.DrawControlPoint(userPartition.FinalWaypoint.Position, c, null, ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
											}
										}
									}
								}
							}

							// draw splines
							if (DrawingUtility.DisplayLaneSplines)
							{
								List<Coordinates> waypointLocations = new List<Coordinates>();

								foreach (RndfWayPoint waypoint in lane.Waypoints.Values)
								{
									waypointLocations.Add(waypoint.Position);
								}

								// get spline
								List<CubicBezier> c2Spline = RndfTools.SplineC2FromPoints(waypointLocations);
								float nomPixelWidth = 2.0f;
								float penWidth = nomPixelWidth / t.Scale;
								Pen pen = new Pen(Color.FromArgb(100, Color.DarkSeaGreen), penWidth);
								foreach (CubicBezier cb in c2Spline)
								{
									g.DrawBezier(pen, DrawingUtility.ToPointF(cb.P0), DrawingUtility.ToPointF(cb.P1), DrawingUtility.ToPointF(cb.P2), DrawingUtility.ToPointF(cb.P3));
								}

							}

							if (DrawingUtility.DrawRndfWaypoints)
							{
								foreach (RndfWayPoint rndfWayPoint in lane.Waypoints.Values)
								{
									if (DrawingUtility.DrawRndfWaypointText)
									{
										DrawingUtility.DrawControlPoint(rndfWayPoint.Position, DrawingUtility.GetWaypointColor(rndfWayPoint), rndfWayPoint.WaypointID.ToString(), ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
									}
									else
									{
										DrawingUtility.DrawControlPoint(rndfWayPoint.Position, DrawingUtility.GetWaypointColor(rndfWayPoint), null, ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
									}

									if (DrawingUtility.DisplayRndfGoals && rndfWayPoint.IsCheckpoint)
									{
										DrawingUtility.DrawControlPoint(rndfWayPoint.Position, DrawingUtility.GetWaypointColor(rndfWayPoint), rndfWayPoint.CheckpointNumber.ToString(), ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
									}
								}
							}
						}
					}
				}

				if (DrawingUtility.DrawInterconnects)
				{
					foreach (Interconnect interconnect in rndf.Interconnects.Values)
					{
						DrawingUtility.DrawControlLine(interconnect.InitialWaypoint.Position, interconnect.FinalWaypoint.Position, DrawingUtility.InterconnectColor, g, t);
					}
				}

				if (DrawingUtility.DisplayIntersectionSplines)
				{
					foreach (Interconnect interconnect in rndf.Interconnects.Values)
					{
						try
						{
							Coordinates d0 = interconnect.InitialWaypoint.Position - interconnect.InitialWaypoint.PreviousLanePartition.InitialWaypoint.Position;
							Coordinates p0 = interconnect.InitialWaypoint.Position;
							Coordinates dn = interconnect.FinalWaypoint.NextLanePartition.FinalWaypoint.Position - interconnect.FinalWaypoint.Position;
							Coordinates pn = interconnect.FinalWaypoint.Position;
							List<Coordinates> coords = new List<Coordinates>();
							coords.Add(p0);
							if (interconnect.UserPartitions != null)
							{
								for (int i = 1; i < interconnect.UserPartitions.Count; i++)
								{
									coords.Add(interconnect.UserPartitions[i].InitialWaypoint.Position);
								}
							}
							coords.Add(pn);

							List<CubicBezier> c2Spline = RndfTools.SplineC2FromSegmentAndDerivatives(coords, d0, dn);
							float nomPixelWidth = 2.0f;
							float penWidth = nomPixelWidth / t.Scale;
							Pen pen = new Pen(Color.FromArgb(100, Color.DarkSeaGreen), penWidth);
							foreach (CubicBezier cb in c2Spline)
							{
								g.DrawBezier(pen, DrawingUtility.ToPointF(cb.P0), DrawingUtility.ToPointF(cb.P1), DrawingUtility.ToPointF(cb.P2), DrawingUtility.ToPointF(cb.P3));
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.ToString());
						}
					}
				}

				if (DrawingUtility.DisplayIntersectionBounds && rndf.intersections != null)
				{
					foreach (Intersection intersection in rndf.intersections.Values)
					{
						if (intersection.Perimeter != null)
						{
							foreach (BoundaryLine boundary in intersection.Perimeter)
							{
								DrawingUtility.DrawColoredControlLine(DrawingUtility.IntersectionAreaColor, boundary.p1, boundary.p2, g, t);
							}
						}
					}
				}
			}
		}

		#endregion


	}
}
