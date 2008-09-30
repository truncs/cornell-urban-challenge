using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Shapes;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a lane of a way
	/// </summary>
	[Serializable]
	public class ArbiterLane : IDisplayObject, INetworkObject, ISupraLaneComponent, IFQMPlanable, INavigableTravelArea, IVehicleArea
	{
		#region Lane Members

		/// <summary>
		/// Line path of the lane
		/// </summary>
		private LinePath laneLinePath;

		/// <summary>
		/// list of waypoints of lane in order
		/// </summary>
		private List<ArbiterWaypoint> waypointList;

		/// <summary>
		/// Unique id of the lane
		/// </summary>
		public ArbiterLaneId LaneId;

		/// <summary>
		/// Lane to left of this lane
		/// </summary>
		public ArbiterLane LaneOnLeft;

		/// <summary>
		/// Lane to right of this lane
		/// </summary>
		public ArbiterLane LaneOnRight;

		/// <summary>
		/// Approximate width of lane
		/// </summary>
		public double Width;

		/// <summary>
		/// Way this lane is a part of
		/// </summary>
		public ArbiterWay Way;

		/// <summary>
		/// Partitions of the lane
		/// </summary>
		public List<ArbiterLanePartition> Partitions;		

		/// <summary>
		/// Waypoints within the lane
		/// </summary>
		public Dictionary<ArbiterWaypointId, ArbiterWaypoint> Waypoints;

		/// <summary>
		/// Lane boundary type to the left
		/// </summary>
		public ArbiterLaneBoundary BoundaryLeft;

		/// <summary>
		/// Lane boundary type to the right
		/// </summary>
		public ArbiterLaneBoundary BoundaryRight;

		/// <summary>
		/// Safety zones of hte lane
		/// </summary>
		public List<ArbiterSafetyZone> SafetyZones;

		/// <summary>
		/// Path of the partitions
		/// </summary>
		public Path PartitionPath;

		/// <summary>
		/// Polygon of the lane
		/// </summary>
		public Polygon LanePolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneId"></param>
		public ArbiterLane(ArbiterLaneId laneId)
		{
			this.LaneId = laneId;
			this.Waypoints = new Dictionary<ArbiterWaypointId, ArbiterWaypoint>();
			this.SafetyZones = new List<ArbiterSafetyZone>();
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two lanes are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterLane)
			{
				// check if the numbers are equal
				return ((ArbiterLane)obj).LaneId.Equals(this.LaneId);
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
			return this.LaneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the Lane number
			return this.LaneId.ToString();
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			if (filter(this))
			{
				Coordinates closest = this.GetClosest(loc);

				if ((float)loc.DistanceTo(closest) < 5 + tol)
				{
					return new HitTestResult(this, true, (float)loc.DistanceTo(closest));
				}
			}

			return new HitTestResult(this, false, float.MaxValue);
		}

		/// <summary>
		/// Renders the lane splint
		/// </summary>
		/// <param name="g"></param>
		/// <param name="t"></param>
		/// <remarks>TODO: set lane spline</remarks>
		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			Color c = DrawingUtility.ColorArbiterLaneSpline;

			if (DrawingUtility.DisplayArbiterLanes)
			{
				Coordinates cp = t.GetWorldPoint(new PointF(t.ScreenSize.Width/2, t.ScreenSize.Height/2));
				Coordinates lp = this.LanePath().GetClosestPoint(cp).Location;
				string s = this.LaneId.ToString();
				DrawingUtility.DrawControlLabel(lp, Color.DarkBlue, s, ContentAlignment.MiddleCenter, ControlPointStyle.None, g, t);
			}

			bool displayPolygon = false;
			switch (this.LaneId.Number)
			{
				case 1:
					displayPolygon = DrawingUtility.DisplayArbiterLanePolygon1;
					break;
				case 2:
					displayPolygon = DrawingUtility.DisplayArbiterLanePolygon2;
					break;
				case 3:
					displayPolygon = DrawingUtility.DisplayArbiterLanePolygon3;
					break;
				case 4:
					displayPolygon = DrawingUtility.DisplayArbiterLanePolygon4;
					break;
			}

			if (displayPolygon && this.LanePolygon != null)
			{
				// show intersection polygon
				HatchBrush hBrush1 = new HatchBrush(HatchStyle.ForwardDiagonal, DrawingUtility.ColorArbiterLanePolygon, Color.White);

				// populate polygon
				List<PointF> polyPoints = new List<PointF>();
				foreach (Coordinates lpp in this.LanePolygon.points)
				{
					polyPoints.Add(DrawingUtility.ToPointF(lpp));
				}

				// draw poly and fill
				g.FillPolygon(hBrush1, polyPoints.ToArray());

				DrawingUtility.DrawControlPolygon(this.LanePolygon, DrawingUtility.ColorArbiterLanePolygon, System.Drawing.Drawing2D.DashStyle.Solid, g, t);
			}

			if(DrawingUtility.DisplayArbiterLanePath)
			{
				DrawingUtility.DrawControlLine(this.laneLinePath, g, t, new Pen(Color.MediumVioletRed), Color.MediumVioletRed);
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
			return DrawingUtility.DisplayArbiterLanes || DrawingUtility.DisplayArbiterLanePolygon1 ||
				DrawingUtility.DisplayArbiterLanePolygon2 || DrawingUtility.DisplayArbiterLanePolygon3 ||
				DrawingUtility.DisplayArbiterLanePolygon4 || DrawingUtility.DisplayArbiterLanePath;
		}

		#endregion

		#region Lane Functions		

		/// <summary>
		/// gets closest waypoint within some max distance
		/// </summary>
		/// <param name="position"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public ArbiterWaypoint GetClosestWaypoint(Coordinates position, double maxDistance)
		{
			ArbiterWaypoint closest = null;
			double curMin = maxDistance;

			foreach (ArbiterWaypoint aw in this.Waypoints.Values)
			{
				if (aw.Position.DistanceTo(position) < curMin)
				{
					closest = aw;
					curMin = aw.Position.DistanceTo(position);
				}
			}

			return closest;
		}

		/// <summary>
		/// Gets the closest partition to the input position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public ArbiterLanePartition GetClosestPartition(Coordinates position)
		{
			// closest partition
			ArbiterLanePartition closest = null;
			double minDistance = Double.MaxValue;

			// loop partitions
			foreach (ArbiterLanePartition alp in this.Partitions)
			{
				// get distance of pt to partition
				double tmpDistance = alp.DistanceTo(position);

				// if less than cur min
				if (tmpDistance < minDistance)
				{
					// set as cur min
					closest = alp;
					minDistance = tmpDistance;
				}
			}

			// return
			return closest;
		}

		/// <summary>
		/// Check if the other line path intersects this line path
		/// </summary>
		/// <param name="other"></param>
		/// <param name="tol"></param>
		/// <returns></returns>
		public bool Intersects(Path other, double tol)
		{
			// start point want to be outside of this lane
			PointOnPath current = this.PartitionPath.StartPoint;

			double increment = tol / 2.0;
			double dist = 0;

			while (dist == 0)
			{
				Coordinates pt = this.PartitionPath.GetClosest(current.pt).pt;
				PointOnPath tmp = other.GetClosest(pt);
				if (tmp.pt.DistanceTo(pt) <= tol)
				{
					return true;
				}

				dist = increment;
				current = this.PartitionPath.AdvancePoint(current, ref dist);
			}

			return false;
		}

		/// <summary>
		/// Checks if the location is within the lane
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public bool IsInside(Coordinates loc)
		{
			LinePath.PointOnPath pop = this.GetClosestPoint(loc);

			if (pop.Equals(this.LanePath().StartPoint))
				return false;
			else if (pop.Equals(this.LanePath().EndPoint))
				return false;
			else
				return true;
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

		#region INavigableTravelArea Members

		/// <summary>
		/// Gets exits downstream, or the goal we are currently reaching
		/// </summary>
		/// <param name="currentPosition"></param>
		/// <param name="ignorable"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public List<DownstreamPointOfInterest> Downstream(Coordinates currentPosition, List<ArbiterWaypoint> ignorable, INavigableNode goal)
		{
			// downstream final
			List<DownstreamPointOfInterest> waypoints = new List<DownstreamPointOfInterest>();

			foreach (ArbiterLane al in Way.Lanes.Values)
			{
				if (al.Equals(this) || (al.GetClosestPartition(currentPosition).Type != PartitionType.Startup && 
					((this.LaneOnLeft != null && this.LaneOnLeft.Equals(al) && this.BoundaryLeft != ArbiterLaneBoundary.SolidWhite) ||
					(this.LaneOnRight != null && this.LaneOnRight.Equals(al) && this.BoundaryRight != ArbiterLaneBoundary.SolidWhite))))
				{
					// get starting waypoint
					ArbiterWaypoint waypoint = null;

					// get closest partition
					ArbiterLanePartition alp = al.GetClosestPartition(currentPosition);
					if (alp.Initial.Position.DistanceTo(currentPosition) < TahoeParams.VL - 2)
						waypoint = alp.Initial;
					else if (alp.IsInside(currentPosition) || alp.Final.Position.DistanceTo(currentPosition) < TahoeParams.VL)
						waypoint = alp.Final;
					else if (alp.Initial.Position.DistanceTo(currentPosition) < alp.Final.Position.DistanceTo(currentPosition))
						waypoint = alp.Initial;
					else if (alp.Initial.Position.DistanceTo(currentPosition) < alp.Final.Position.DistanceTo(currentPosition) && alp.Final.NextPartition == null)
						waypoint = null;
					else
						waypoint = null;

					// check waypoint exists
					if (waypoint != null)
					{
						// save start
						ArbiterWaypoint initial = waypoint;

						// initial cost
						double initialCost = 0.0;

						if (al.Equals(this))
							initialCost = currentPosition.DistanceTo(waypoint.Position) / waypoint.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
						else if (waypoint.WaypointId.Number != 1)
						{
							// get closest partition
							ArbiterWaypoint tmpI = this.GetClosestWaypoint(currentPosition, Double.MaxValue);
							initialCost = NavigationPenalties.ChangeLanes * Math.Abs(this.LaneId.Number - al.LaneId.Number);
							initialCost += currentPosition.DistanceTo(tmpI.Position) / tmpI.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
						}
						else
						{
							// get closest partition
							ArbiterWaypoint tmpI = this.GetClosestWaypoint(currentPosition, Double.MaxValue);
							ArbiterWaypoint tmpF = this.GetClosestWaypoint(waypoint.Position, Double.MaxValue);
							initialCost = NavigationPenalties.ChangeLanes * Math.Abs(this.LaneId.Number - al.LaneId.Number);
							initialCost += currentPosition.DistanceTo(tmpI.Position) / tmpI.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
							initialCost += this.TimeCostInLane(tmpI, tmpF, new List<ArbiterWaypoint>());
						}

						// loop while waypoint not null
						while (waypoint != null)
						{
							if (waypoint.IsCheckpoint && (goal is ArbiterWaypoint) && ((ArbiterWaypoint)goal).WaypointId.Equals(waypoint.WaypointId))
							{
								double timeCost = initialCost + this.TimeCostInLane(initial, waypoint, ignorable);
								DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
								dpoi.DistanceToPoint = al.DistanceBetween(currentPosition, waypoint.Position);
								dpoi.IsExit = false;
								dpoi.IsGoal = true;
								dpoi.PointOfInterest = waypoint;
								dpoi.TimeCostToPoint = timeCost;
								waypoints.Add(dpoi);
							}
							else if (waypoint.IsExit && !ignorable.Contains(waypoint))
							{
								double timeCost = initialCost + this.TimeCostInLane(initial, waypoint, ignorable);
								DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
								dpoi.DistanceToPoint = al.DistanceBetween(currentPosition, waypoint.Position);
								dpoi.IsExit = true;
								dpoi.IsGoal = false;
								dpoi.PointOfInterest = waypoint;
								dpoi.TimeCostToPoint = timeCost;
								waypoints.Add(dpoi);
							}
							else if (waypoint.NextPartition == null && !ignorable.Contains(waypoint))
							{
								DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
								dpoi.DistanceToPoint = al.DistanceBetween(currentPosition, waypoint.Position);
								dpoi.IsExit = false;
								dpoi.IsGoal = false;
								dpoi.PointOfInterest = waypoint;
								dpoi.TimeCostToPoint = Double.MaxValue;
								waypoints.Add(dpoi);
							}

							waypoint = waypoint.NextPartition != null ? waypoint.NextPartition.Final : null;
						}
					}
				}
			}

			return waypoints;
		}

		/// <summary>
		/// Gets time cost between two waypoints in the same lane
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		public double TimeCostInLane(ArbiterWaypoint initial, ArbiterWaypoint final, List<ArbiterWaypoint> ignorable)
		{
			ArbiterWaypoint current = initial;
			double cost = ignorable.Contains(initial) ? 0.0 : initial.ExtraTimeCost;

			while (current != null)
			{
				if (current.Equals(final))
				{
					return cost;
				}
				else
				{
					if(current.NextPartition != null)
						cost += current.NextPartition.TimeCost();

					if (current.NextPartition != null)
						current = current.NextPartition.Final;
					else
						current = null;
				}
			}

			return Double.MaxValue;
		}

		#endregion

		#region IVehicleArea Members

		public double DistanceTo(Coordinates loc)
		{
			return this.GetClosest(loc).DistanceTo(loc);
		}

		public bool ContainsVehicle(Coordinates center, double length, double width, Coordinates heading)
		{
			if (this.DistanceTo(center) - (width / 2.0) < this.Width / 2.0)
				return true;
			else
				return false;
		}

		public string DefaultAreaId()
		{
			return this.Partitions[0].ToString();
		}

		#endregion		

		#region IFQMPlanable Members

		/// <summary>
		/// Gets closest coordinate to  location
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public Coordinates GetClosest(Coordinates loc)
		{
			return this.laneLinePath.GetPoint(this.GetClosestPoint(loc));
		}

		/// <summary>
		/// Gets closest point on the lante to the location
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public LinePath.PointOnPath GetClosestPoint(Coordinates loc)
		{
			return this.LanePath().GetClosestPoint(loc);
		}

		/// <summary>
		/// Distance between two points along lane
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <returns></returns>
		public double DistanceBetween(Coordinates c1, Coordinates c2)
		{
			return this.LanePath().DistanceBetween(
				this.LanePath().GetClosestPoint(c1),
				this.LanePath().GetClosestPoint(c2));
		}

		/// <summary>
		/// Distance between two waypoints
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="w2"></param>
		/// <returns></returns>
		public double DistanceBetween(ArbiterWaypoint w1, ArbiterWaypoint w2)
		{
			return this.LanePath(w1, w2).PathLength;
		}

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="w1">Starting waypoint of search</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt)
		{
			ArbiterWaypoint tmp = w1;
			while (tmp != null)
			{
				if (tmp.WaypointTypeEquals(wt))
					return tmp;

				if (tmp.NextPartition != null)
					tmp = tmp.NextPartition.Final;
				else
					tmp = null;
			}

			return null;
		}

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="loc">Location to start looking from</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <param name="ignorable">ignorable waypoints</param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(Coordinates loc, WaypointType wt, List<ArbiterWaypoint> ignorable)
		{
			return this.GetNext(this.GetClosestPartition(loc).Final, wt, ignorable);
		}

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="loc">Location to start looking from</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <param name="ignorable">ignorable waypoints</param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(Coordinates loc, List<WaypointType> wts, List<ArbiterWaypoint> ignorable)
		{
			return this.GetNext(this.GetClosestPartition(loc).Final, wts, ignorable);
		}

		/// <summary>
		/// Path of lane from a waypoint for a certain distance
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="distance">Distance to get path</param>
		/// <returns></returns>
		public LinePath LanePath(ArbiterWaypoint w1, double distance)
		{
			return this.LanePath().SubPath(this.LanePath().GetClosestPoint(w1.Position), distance);
		}

		/// <summary>
		/// Path of lane from a waypoint for a certain distance
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="distance">Distance to get path</param>
		/// <returns></returns>
		public LinePath LanePath(Coordinates c1, Coordinates c2)
		{
			return this.LanePath().SubPath(this.LanePath().GetClosestPoint(c1), this.LanePath().GetClosestPoint(c2));
		}

		/// <summary>
		/// Path of lane between two waypoints
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="w2">Final waypoint</param>
		/// <returns></returns>
		public LinePath LanePath(ArbiterWaypoint w1, ArbiterWaypoint w2)
		{
			return this.LanePath().SubPath(
				this.LanePath().GetClosestPoint(w1.Position), 
				this.LanePath().GetClosestPoint(w2.Position));
		}

		/// <summary>
		/// Path of lane
		/// </summary>
		public LinePath LanePath()
		{
			return this.laneLinePath;
		}		
		
		/// <summary>
		/// Sets the lane path
		/// </summary>
		public void SetLanePath(LinePath path)
		{
			this.laneLinePath = path;
		}

		/// <summary>
		/// Gets next waypoint of a certain type ignoring certain waypoints
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="wt"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt, List<ArbiterWaypoint> ignorable)
		{
			ArbiterWaypoint tmp = w1;
			while (tmp != null)
			{
				if (tmp.WaypointTypeEquals(wt) && !ignorable.Contains(tmp))
					return tmp;
				if (tmp.NextPartition != null)
					tmp = tmp.NextPartition.Final;
				else
					tmp = null;
			}

			return null;
		}

		/// <summary>
		/// Gets next waypoint of a certain type ignoring certain waypoints
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="wt"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, List<WaypointType> wts, List<ArbiterWaypoint> ignorable)
		{
			ArbiterWaypoint tmp = w1;
			while (tmp != null)
			{
				bool back = false;
				foreach(WaypointType wt in wts)
				{
					if(tmp.WaypointTypeEquals(wt))
						back = true;
				}

				if (back && !ignorable.Contains(tmp))
					return tmp;
				if (tmp.NextPartition != null)
					tmp = tmp.NextPartition.Final;
				else
					tmp = null;
			}

			return null;
		}

		/// <summary>
		/// components of this area
		/// </summary>
		public List<IVehicleArea> AreaComponents
		{
			get { return new List<IVehicleArea>(new IVehicleArea[]{ this }); }
		}

		/// <summary>
		/// current max speed at position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public double CurrentMaximumSpeed(Coordinates position)
		{
			return this.Way.Segment.SpeedLimits.MaximumSpeed;
		}

		/// <summary>
		/// List of waypoints
		/// </summary>
		public List<ArbiterWaypoint> WaypointList
		{
			get
			{
				return this.waypointList;
			}
			set
			{
				this.waypointList = value;
			}
		}

		/// <summary>
		/// Get waypoints from initial to final
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		public List<ArbiterWaypoint> WaypointsInclusive(ArbiterWaypoint initial, ArbiterWaypoint final)
		{
			List<ArbiterWaypoint> middle = new List<ArbiterWaypoint>();
			int i = waypointList.IndexOf(initial);
			int j = waypointList.IndexOf(final);
			for (int k = i; k <= j; k++)
			{
				middle.Add(waypointList[k]);
			}
			return middle;
		}

		#endregion

		#region Get Next Reverse

		/// <summary>
		/// Gets distance to next illegal place to travel in reverse
		/// </summary>
		public double ReverseDistanceToNextIllegal(Coordinates pos, List<ArbiterWaypoint> ignorable)
		{
			double minDist = double.MaxValue;			
			LinePath.PointOnPath posPoint = this.LanePath().GetClosestPoint(pos);
			List<WaypointType> badTypes = new List<WaypointType>();
			badTypes.Add(WaypointType.Stop);
			badTypes.Add(WaypointType.End);

			// get distance to each safety zone
			foreach (ArbiterLane al in this.Way.Lanes.Values)
			{
				ArbiterWaypoint aw = al.GetNext(pos, badTypes, ignorable); 
				double d = this.LanePath().DistanceBetween(this.LanePath().GetClosestPoint(aw.Position), posPoint);
				if(d < minDist)
					minDist = d;
			}

			return minDist;
		}

		#endregion

		public int NumberOfLanesLeft(Coordinates position, bool forwards)
		{
			if(forwards)
			{
				if (this.LaneOnLeft != null)
				{
					// our lane #
					int n = this.LaneId.Number;

					// left lane #
					int l = this.LaneOnLeft.LaneId.Number;

					// count
					int count = 0;

					// if left lane numbers going down
					if (n > l)
					{
						// loop over left lanes going down
						for (int i = n - 1; i > 0; i--)
						{
							// determine who is close to this position
							ArbiterLaneId lane1 = new ArbiterLaneId(i, this.Way.Segment.Way1.WayId);
							ArbiterLaneId lane2 = new ArbiterLaneId(i, this.Way.Segment.Way2.WayId);

							// get lane
							ArbiterLane lane = null;
							if (this.Way.Segment.Lanes.ContainsKey(lane1))
								lane = this.Way.Segment.Lanes[lane1];
							else if (this.Way.Segment.Lanes.ContainsKey(lane2))
								lane = this.Way.Segment.Lanes[lane2];
							
							// check lane exists
							if (lane != null)
							{
								// determine if close and inside
								bool close = lane.LanePath().GetClosestPoint(position).Location.DistanceTo(position) < 15;
								bool inside = lane.IsInside(position);

								// add if both
								if (close && inside)
									count++;
							}
						}
					}
					else
					{
						// loop over left lanes
						for (int i = n + 1; i <= this.Way.Segment.Lanes.Count; i++)
						{
							// determine who is close to this position
							ArbiterLaneId lane1 = new ArbiterLaneId(i, this.Way.Segment.Way1.WayId);
							ArbiterLaneId lane2 = new ArbiterLaneId(i, this.Way.Segment.Way2.WayId);

							// get lane
							ArbiterLane lane = null;
							if (this.Way.Segment.Lanes.ContainsKey(lane1))
								lane = this.Way.Segment.Lanes[lane1];
							else if (this.Way.Segment.Lanes.ContainsKey(lane2))
								lane = this.Way.Segment.Lanes[lane2];

							// check lane exists
							if (lane != null)
							{
								// determine if close and inside
								bool close = lane.LanePath().GetClosestPoint(position).Location.DistanceTo(position) < 15;
								bool inside = lane.IsInside(position);

								// add if both
								if (close && inside)
									count++;
							}
						}
					}

					// return the count
					return count;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				return this.NumberOfLanesRight(position, true);
			}		
		}

		public int NumberOfLanesRight(Coordinates position, bool forwards)
		{
			if (forwards)
			{
				if (this.LaneOnRight != null)
				{
					// our lane #
					int n = this.LaneId.Number;

					// left lane #
					int l = this.LaneOnRight.LaneId.Number;

					// count
					int count = 0;

					// if left lane numbers going down
					if (n > l)
					{
						// loop over left lanes going down
						for (int i = n - 1; i >= 0; i--)
						{
							// determine who is close to this position
							ArbiterLaneId lane1 = new ArbiterLaneId(i, this.Way.Segment.Way1.WayId);
							ArbiterLaneId lane2 = new ArbiterLaneId(i, this.Way.Segment.Way2.WayId);

							// get lane
							ArbiterLane lane = null;
							if (this.Way.Segment.Lanes.ContainsKey(lane1))
								lane = this.Way.Segment.Lanes[lane1];
							else if (this.Way.Segment.Lanes.ContainsKey(lane2))
								lane = this.Way.Segment.Lanes[lane2];

							// check lane exists
							if (lane != null)
							{
								// determine if close and inside
								bool close = lane.LanePath().GetClosestPoint(position).Location.DistanceTo(position) < 15;
								bool inside = lane.IsInside(position);

								// add if both
								if (close && inside)
									count++;
							}
						}
					}
					else
					{
						// loop over left lanes
						for (int i = n + 1; i < this.Way.Segment.Lanes.Count; i++)
						{
							// determine who is close to this position
							ArbiterLaneId lane1 = new ArbiterLaneId(i, this.Way.Segment.Way1.WayId);
							ArbiterLaneId lane2 = new ArbiterLaneId(i, this.Way.Segment.Way2.WayId);

							// get lane
							ArbiterLane lane = null;
							if (this.Way.Segment.Lanes.ContainsKey(lane1))
								lane = this.Way.Segment.Lanes[lane1];
							else if (this.Way.Segment.Lanes.ContainsKey(lane2))
								lane = this.Way.Segment.Lanes[lane2];

							// check lane exists
							if (lane != null)
							{
								// determine if close and inside
								bool close = lane.LanePath().GetClosestPoint(position).Location.DistanceTo(position) < 15;
								bool inside = lane.IsInside(position);

								// add if both
								if (close && inside)
									count++;
							}
						}
					}

					// return the count
					return count;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				return this.NumberOfLanesLeft(position, true);
			}	
		}

		public LinePath ReversePath
		{
			get
			{
				LinePath lp = this.LanePath().Clone();
				lp.Reverse();
				return lp;
			}
		}

		public void ReformPath()
		{
			List<Coordinates> coords = new List<Coordinates>();
			coords.Add(this.Partitions[0].Initial.Position);
			foreach (ArbiterLanePartition alp in this.Partitions)
			{
				coords.AddRange(alp.NotInitialPathCoords());
			}
			this.laneLinePath = new LinePath(coords);
		}

		#region IFQMPlanable Members


		public bool RelativelyInside(Coordinates c)
		{
			LinePath.PointOnPath pop = this.LanePath().GetClosestPoint(c);

			// check distance 
			if (this.IsInside(c) && pop.Location.DistanceTo(c) < this.Width * 2.5)
				return true;
			else if (pop.Location.DistanceTo(c) < TahoeParams.VL * 1.5)
				return true;
			else
				return false;
		}

		#endregion

		#region IFQMPlanable Members


		public void SparseDetermination(Coordinates coordinates, out bool sparseDownstream, out bool sparseNow, out double sparseDistance)
		{
			ArbiterLanePartition alp = this.GetClosestPartition(coordinates);
			if(alp.Type == PartitionType.Sparse)
			{
				sparseDownstream = true;
				sparseDistance = 0.0;
				sparseNow = true;
				return;
			}

			while (alp != null)
			{
				if (alp.Type == PartitionType.Sparse)
				{
					sparseDownstream = true;
					sparseDistance = this.DistanceBetween(coordinates, alp.Initial.Position);
					sparseNow = false;
					return;
				}

				alp = alp.Final.NextPartition;
			}

			sparseDownstream = false;
			sparseDistance = double.MaxValue;
			sparseNow = false;
		}

		#endregion
	}
}
