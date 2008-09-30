using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Splines;
using System.Drawing;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Direction of turn
	/// </summary>
	public enum ArbiterTurnDirection
	{
		Straight = 0,

		Right = 1,

		Left = 2,

		UTurn = 3,

		Unknown = 4,
	}

	[Serializable]
	public class ArbiterInterconnect : NavigableEdge, IConnectAreaWaypoints, IDisplayObject, INetworkObject, ISupraLaneComponent, IVehicleArea
	{
		#region Interconnect Members

		private IArbiterWaypoint initialWaypoint;
		private IArbiterWaypoint finalWaypoint;
		private List<ArbiterUserPartition> userPartitions;
		private ArbiterTurnDirection turnDirection;

		public Polygon TurnPolygon;
		public List<Coordinates> InnerCoordinates;

		/// <summary>
		/// the blockage contained in this partition
		/// </summary>
		private NavigationBlockage blockage;

		/// <summary>
		/// Id of the interconnect
		/// </summary>
		public ArbiterInterconnectId InterconnectId;

		/// <summary>
		/// Path of the interconnect
		/// </summary>
		public LinePath InterconnectPath;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		public ArbiterInterconnect(IArbiterWaypoint initial, IArbiterWaypoint final) : 
			base(false, null, false, null, new List<IConnectAreaWaypoints>(), initial, final)
		{
			this.initialWaypoint = initial;
			this.finalWaypoint = final;

			this.InterconnectId = new ArbiterInterconnectId(initialWaypoint.AreaSubtypeWaypointId, finalWaypoint.AreaSubtypeWaypointId);

			// create a path of the partition and get the closest
			List<Coordinates> ips = new List<Coordinates>();
			ips.Add(initial.Position);
			ips.Add(final.Position);
			this.InterconnectPath = new LinePath(ips);

			// nav edge stuff
			this.Contained.Add(this);
			this.blockage = new NavigationBlockage(0.0);

			this.TurnPolygon = this.DefaultPoly();
			this.InnerCoordinates = new List<Coordinates>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		public ArbiterInterconnect(IArbiterWaypoint initial, IArbiterWaypoint final, ArbiterTurnDirection turnDirection)
			: base(false, null, false, null, new List<IConnectAreaWaypoints>(), initial, final)
		{
			this.initialWaypoint = initial;
			this.finalWaypoint = final;

			this.InterconnectId = new ArbiterInterconnectId(initialWaypoint.AreaSubtypeWaypointId, finalWaypoint.AreaSubtypeWaypointId);

			// create a path of the partition and get the closest
			List<Coordinates> ips = new List<Coordinates>();
			ips.Add(initial.Position);
			ips.Add(final.Position);
			this.InterconnectPath = new LinePath(ips);

			// nav edge stuff
			this.Contained.Add(this);
			this.blockage = new NavigationBlockage(0.0);

			this.TurnDirection = turnDirection;

			this.TurnPolygon = this.DefaultPoly();
			this.InnerCoordinates = new List<Coordinates>();
		}

		public Polygon DefaultPoly()
		{
			ArbiterInterconnect ai = this;

			// width
			double width = 3.0;
			if (ai.InitialGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint aw = (ArbiterWaypoint)ai.InitialGeneric;
				width = width < aw.Lane.Width ? aw.Lane.Width : width;
			}
			if (ai.FinalGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint aw = (ArbiterWaypoint)ai.FinalGeneric;
				width = width < aw.Lane.Width ? aw.Lane.Width : width;
			}

			List<Coordinates> polyPoints = new List<Coordinates>();
			LinePath lp = ai.InterconnectPath.ShiftLateral(width / 2.0);
			LinePath rp = ai.InterconnectPath.ShiftLateral(-width / 2.0);
			polyPoints.AddRange(lp);
			polyPoints.AddRange(rp);
			return Polygon.GrahamScan(polyPoints);
		}

		public double MaximumDefaultSpeed
		{
			get
			{
				ArbiterInterconnect ai = this;

				// set the minimum maximum speed = 4mph
				double minSpeed = 1.78816;

				try
				{
					if (ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint)
					{
						// waypoint
						ArbiterWaypoint awI = (ArbiterWaypoint)ai.InitialGeneric;
						ArbiterWaypoint awF = (ArbiterWaypoint)ai.FinalGeneric;

						List<Coordinates> interCoords = new List<Coordinates>();
						Coordinates init = awI.Position - awI.PreviousPartition.Vector().Normalize(ai.InterconnectPath.PathLength);
						Coordinates fin = awF.Position + awF.NextPartition.Vector().Normalize(ai.InterconnectPath.PathLength);
						interCoords.Add(init);
						interCoords.Add(awI.Position);
						interCoords.Add(awF.Position);
						interCoords.Add(fin);

						double initMax = awI.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
						double finalMax = awF.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
						double curvatureMax = MaximumSpeed(interCoords, minSpeed);
						return Math.Min(Math.Min(initMax, finalMax), curvatureMax);
					}
					else
					{
						return minSpeed;
					}
				}
				catch(Exception e)
				{
					Console.WriteLine(e.ToString());
					return minSpeed;
				}
			}
			set
			{
			}
		}

		/// <summary>
		/// Extra cost associated with this interconnect
		/// </summary>
		public double ExtraCost
		{
			get
			{
				// set initial cost of the edge to 0
				double cost = NavigationPenalties.Interconnect;

				// get the road network
				ArbiterRoadNetwork arn = null;
				if (this.initialWaypoint is ArbiterWaypoint || this.finalWaypoint is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = this.finalWaypoint is ArbiterWaypoint ? (ArbiterWaypoint)this.finalWaypoint : (ArbiterWaypoint)this.initialWaypoint;
					arn = aw.Lane.Way.Segment.RoadNetwork;
				}
				else if (this.initialWaypoint is ArbiterPerimeterWaypoint || this.finalWaypoint is ArbiterPerimeterWaypoint)
				{					
					ArbiterPerimeterWaypoint apw = this.finalWaypoint is ArbiterPerimeterWaypoint ? (ArbiterPerimeterWaypoint)this.finalWaypoint : (ArbiterPerimeterWaypoint)this.initialWaypoint;
					arn = apw.Perimeter.Zone.RoadNetwork;
				}

				// check u-turn
				if (this.finalWaypoint is ArbiterWaypoint && this.initialWaypoint is ArbiterWaypoint)
				{
					ArbiterWaypoint f = (ArbiterWaypoint)this.finalWaypoint;
					ArbiterWaypoint i = (ArbiterWaypoint)this.initialWaypoint;
					if (f.Lane.Way.Segment.Equals(i.Lane.Way.Segment) && !f.Lane.Way.Equals(i.Lane.Way))
						cost += NavigationPenalties.UTurnPenalty;
				}

				// check if intersection exists and check for priority overlap
				if (arn.IntersectionLookup.ContainsKey(this.initialWaypoint.AreaSubtypeWaypointId))
				{
					ArbiterIntersection ai = arn.IntersectionLookup[this.initialWaypoint.AreaSubtypeWaypointId];
					if (ai.PriorityLanes.ContainsKey(this))
					{
						List<IntersectionInvolved> iis = ai.PriorityLanes[this];

						if (iis.Count > 0)
							cost += NavigationPenalties.TurnOverPriorityDefault + (iis.Count * NavigationPenalties.TurnOverPriorityExtra);
					}
				}

				// check for multiple stops if we are a stop
				if (this.initialWaypoint is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)this.initialWaypoint;
					if (aw.IsStop && aw.Lane.Way.Segment.RoadNetwork.IntersectionLookup.ContainsKey(aw.AreaSubtypeWaypointId))
					{
						ArbiterIntersection aint = aw.Lane.Way.Segment.RoadNetwork.IntersectionLookup[aw.AreaSubtypeWaypointId];
						cost += Math.Max(((aint.StoppedExits.Count - 1) * NavigationPenalties.TurnOverPriorityExtra), 0.0);
					}
				}

				// return final cost
				return cost;
			}
		}

		#endregion

		#region IConnectAreaWaypoints Members

		public IConnectAreaWaypointsId ConnectionId
		{
			get { return this.InterconnectId; }
		}

		public IArbiterWaypoint InitialGeneric
		{
			get { return this.initialWaypoint; }
		}

		public IArbiterWaypoint FinalGeneric
		{
			get { return this.finalWaypoint; }
		}

		public List<ArbiterUserPartition> UserPartitions
		{
			get
			{
				return this.userPartitions;
			}
			set
			{
				this.userPartitions = value;
			}
		}

		public double DistanceTo(Coordinates loc)
		{
			return (loc.DistanceTo(this.InterconnectPath.GetPoint(this.InterconnectPath.GetClosestPoint(loc))));
		}

		public double DistanceTo(IConnectAreaWaypoints icaw)
		{
			LinePath.PointOnPath current = this.InterconnectPath.StartPoint;
			double inc = 1.0;
			double dist = 0;
			double minDist = double.MaxValue;

			while (dist == 0)
			{
				double tmpDist = icaw.DistanceTo(this.InterconnectPath.GetPoint(current));

				if (tmpDist < minDist)
					minDist = tmpDist;

				dist = inc;
				current = this.InterconnectPath.AdvancePoint(current, ref dist);
			}

			return minDist;
		}

		public NavigationBlockage Blockage
		{
			get
			{
				return this.blockage;
			}
			set
			{
				this.blockage = value;
			}
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two zones are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterInterconnect)
			{
				// check if the numbers are equal
				return ((ArbiterInterconnect)obj).InterconnectId.Equals(this.InterconnectId);
			}

			// otherwise not equal
			return false;
		}

		/// <summary>
		/// Hash code for id
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			// for top levels is just the number
			return this.InterconnectId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.InterconnectId.ToString();
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			PointF[] pts = new PointF[4];
			pts[0] = new PointF((float)this.InitialGeneric.Position.X, (float)this.InitialGeneric.Position.Y);
			pts[1] = new PointF((float)this.InitialGeneric.Position.X, (float)this.FinalGeneric.Position.Y);
			pts[2] = new PointF((float)this.FinalGeneric.Position.X, (float)this.FinalGeneric.Position.Y);
			pts[3] = new PointF((float)this.FinalGeneric.Position.X, (float)this.InitialGeneric.Position.Y);
			return DrawingUtility.CalcBoundingBox(pts);
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if (t.ShouldDraw(this.GetBoundingBox(t)))
			{
				DrawingUtility.DrawColoredControlLine(
					DrawingUtility.ColorArbiterInterconnect,
					System.Drawing.Drawing2D.DashStyle.DashDot,
					this.initialWaypoint.Position,
					this.finalWaypoint.Position,
					g, t);

				if (this.InitialGeneric is ArbiterWaypoint && ((ArbiterWaypoint)this.InitialGeneric).WaypointId.LaneId.WayId.Number == 1)
				{
					DrawingUtility.DrawControlPolygon(this.TurnPolygon, Color.DarkBlue, System.Drawing.Drawing2D.DashStyle.DashDot, g, t);

					if (this.InnerCoordinates.Count > 1)
						DrawingUtility.DrawControlPoint(this.InnerCoordinates[1], Color.DarkBlue, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}
				else
				{
					DrawingUtility.DrawControlPolygon(this.TurnPolygon, Color.DarkGreen, System.Drawing.Drawing2D.DashStyle.DashDot, g, t);

					if (this.InnerCoordinates.Count > 1)
						DrawingUtility.DrawControlPoint(this.InnerCoordinates[1], Color.DarkGreen, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}
			}
		}

		public bool MoveAllowed
		{
			get { return false; }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public SelectionType Selected
		{
			get
			{
				return SelectionType.NotSelected;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { return null; }
		}

		public bool CanDelete
		{
			get { return false; }
		}

		public List<IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawArbiterInterconnects;
		}

		#endregion		
	
		#region ISupraLaneComponent Members

		private ISupraLaneComponent nextComponent;
		private ISupraLaneComponent previousComponent;

		public ISupraLaneComponent NextComponent
		{
			get
			{
				return this.nextComponent;
			}
			set
			{
				this.nextComponent = value;
			}
		}

		public ISupraLaneComponent PreviousComponent
		{
			get
			{
				return this.previousComponent;
			}
			set
			{
				this.previousComponent = value;
			}
		}

		#endregion

		/// <summary>
		/// Gets maximum speed over a path
		/// </summary>
		/// <param name="coordinatePath"></param>
		/// <returns></returns>
		public static double MaximumSpeed(List<Coordinates> coordinatePath, double minSpeed)
		{
			// generate path
			List<CubicBezier> cb = SplineC2FromPoints(coordinatePath);

			// get max curvature
			double? maxCurvature = Curvature(cb);

			// if curvature exists
			if (maxCurvature.HasValue)
			{
				// get minimum radius or curvature
				double r = Math.Abs(1.0 / maxCurvature.Value);

				// set the static friction
				double us = 0.2;

				// gravity value
				double g = 9.8;

				// get the maximum velocity based upon curvature or max v
				double maxSpeed = Math.Max(Math.Sqrt(us * r * g), minSpeed);

				// return the speed
				return maxSpeed;
			}
			else
			{
				return minSpeed;
			}
		}

		/// <summary>
		/// Gets the maximum curvature along the spline
		/// </summary>
		/// <param name="bezierSpline"></param>
		/// <param name="distanceAlong"></param>
		/// <returns></returns>
		private static double? Curvature(List<CubicBezier> bezierSpline)
		{
			double maxCurvature = Double.MinValue;

			for (int i = 0; i < bezierSpline.Count; i++)
			{
				double arcLength = bezierSpline[i].ArcLength;
				double increment = 0.5;
				double arcAlong = 0.0;

				while (arcAlong <= arcLength)
				{
					double curvature = bezierSpline[i].Curvature(arcAlong / arcLength);

					if (Math.Abs(curvature) > maxCurvature)
						maxCurvature = Math.Abs(curvature);

					arcAlong += increment;
				}
			}

			if(maxCurvature == Double.MinValue)
				return null;
			else
				return maxCurvature;
		}

		/// <summary>
		/// Generates a C2 spline from a list of input points
		/// </summary>
		/// <param name="coordinates"></param>
		/// <returns></returns>
		public static List<CubicBezier> SplineC2FromPoints(List<Coordinates> coordinates)
		{
			// final list of beziers
			List<CubicBezier> spline = new List<CubicBezier>();

			// generate spline
			CubicBezier[] bez = SmoothingSpline.BuildC2Spline(coordinates.ToArray(), null, null, 0.5);

			// loop through individual beziers
			foreach (CubicBezier cb in bez)
			{
				// add to final spline
				spline.Add(cb);
			}

			// return final list of beziers
			return spline;
		}

		#region IVehicleArea Members

		public bool ContainsVehicle(Coordinates center, double length, double width, Coordinates heading)
		{
			if (this.DistanceTo(center) < width * 2.0)
				return true;
			else
				return false;
		}

		public string DefaultAreaId()
		{
			return this.ToString();
		}

		#endregion

		/// <summary>
		/// Checks if the location is within the lane
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public bool IsInside(Coordinates loc)
		{
			LinePath.PointOnPath pop = this.InterconnectPath.GetClosestPoint(loc);

			if (pop.Equals(this.InterconnectPath.StartPoint))
				return false;
			else if (pop.Equals(this.InterconnectPath.EndPoint))
				return false;
			else
				return true;
		}

		public Path OldPath
		{
			get
			{
				List<IPathSegment> ips = new List<IPathSegment>();
				ips.Add(new LinePathSegment(initialWaypoint.Position, finalWaypoint.Position));
				return new Path(ips, CoordinateMode.AbsoluteProjected);
			}
		}

		public int ComparePriority(ArbiterInterconnect other)
		{
			if(this.initialWaypoint is ITraversableWaypoint && ((ITraversableWaypoint)initialWaypoint).IsStop)
			{
				if (other.InitialGeneric is ITraversableWaypoint && !((ITraversableWaypoint)other.InitialGeneric).IsStop)
					return 1;
			}
			else
			{
				if (other.InitialGeneric is ITraversableWaypoint && ((ITraversableWaypoint)other.InitialGeneric).IsStop)
					return -1;
			}

			if (this.TurnDirection == ArbiterTurnDirection.Unknown || other.TurnDirection == ArbiterTurnDirection.Unknown)
				return 0;

			if (this.TurnDirection == ArbiterTurnDirection.Left)
			{
				if (other.TurnDirection == ArbiterTurnDirection.UTurn)
					return -1;
				if (other.TurnDirection == ArbiterTurnDirection.Left)
					return 0;
				else
					return 1;
			}
			else if (this.TurnDirection == ArbiterTurnDirection.Right)
			{
				if (other.TurnDirection == ArbiterTurnDirection.Right)
					return 0;
				else if (other.TurnDirection == ArbiterTurnDirection.Straight)
					return 1;
				else
					return -1;
			}
			else if (this.TurnDirection == ArbiterTurnDirection.Straight)
			{
				if (other.TurnDirection == ArbiterTurnDirection.Straight)
					return 0;
				else
					return -1;
			}
			else if (this.TurnDirection == ArbiterTurnDirection.UTurn)
			{
				if (other.TurnDirection == ArbiterTurnDirection.UTurn)
					return 0;
				else
					return 1;
			}

			else
				return 1;
		}

		#region IConnectAreaWaypoints Members


		public ArbiterInterconnect ToInterconnect
		{
			get { return this; }
		}

		#endregion

		public ArbiterTurnDirection TurnDirection
		{
			get { return turnDirection; }
			set { turnDirection = value; }
		}
	}
}
