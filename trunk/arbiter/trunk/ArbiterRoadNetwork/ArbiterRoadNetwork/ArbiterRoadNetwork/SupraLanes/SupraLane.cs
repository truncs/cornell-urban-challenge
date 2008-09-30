using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	public enum SLComponentType
	{
		Initial,
		Interconnect,
		Final
	}

	public class SupraLane : IFQMPlanable, INavigableTravelArea
	{
		#region Supra Lane

		/// <summary>
		/// Components of the lane
		/// </summary>
		public SupraLaneComponentList Components;

		/// <summary>
		/// Path of the lane
		/// </summary>
		private LinePath supraPath;

		/// <summary>
		/// Waypoints in the supra lane
		/// </summary>
		private List<ArbiterWaypoint> supraWaypoints;

		/// <summary>
		/// initial lane
		/// </summary>
		public ArbiterLane Initial;

		/// <summary>
		/// final lane
		/// </summary>
		public ArbiterLane Final;

		/// <summary>
		/// interconnect
		/// </summary>
		public ArbiterInterconnect Interconnect;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="next"></param>
		/// <param name="final"></param>
		public SupraLane(ArbiterLane initial, ArbiterInterconnect next, ArbiterLane final)
		{
			Components = new SupraLaneComponentList();
			Components.Add(initial);
			Components.Add(next);
			Components.Add(final);
			this.Initial = initial;
			this.Final = final;
			this.Interconnect = next;

			if (!this.Initial.Equals(this.Final))
			{
				this.supraWaypoints = new List<ArbiterWaypoint>();
				this.supraWaypoints.AddRange(this.Initial.WaypointsInclusive(this.Initial.WaypointList[0], (ArbiterWaypoint)this.Interconnect.InitialGeneric));
				this.supraWaypoints.AddRange(this.Final.WaypointsInclusive((ArbiterWaypoint)this.Interconnect.FinalGeneric, this.Final.WaypointList[this.Final.WaypointList.Count - 1]));
				this.SetDefaultLanePath();
			}
			else
			{
				this.supraWaypoints = this.Initial.WaypointList;
				this.supraPath = this.Initial.LanePath();
				this.supraPath.Add(this.Interconnect.FinalGeneric.Position);
			}
		}

		/// <summary>
		/// Sets the path of the supra lane
		/// </summary>
		public void SetDefaultLanePath()
		{
			// new path
			this.supraPath = new LinePath();

			// get initial waypoints
			this.supraPath = this.Initial.LanePath(this.Initial.WaypointList[0], (ArbiterWaypoint)this.Interconnect.InitialGeneric);
			this.supraPath.AddRange(this.Final.LanePath((ArbiterWaypoint)this.Interconnect.FinalGeneric, this.Final.WaypointList[this.Final.WaypointList.Count-1]));
		}

		/// <summary>
		/// Determines proper speed commands given we want to stop at a certain waypoint
		/// </summary>
		/// <param name="waypoint"></param>
		/// <param name="lane"></param>
		/// <param name="position"></param>
		/// <param name="enCovariance"></param>
		/// <param name="stopSpeed"></param>
		/// <param name="stopDistance"></param>
		public double RequiredSpeed(ArbiterWaypoint waypoint, double speedAtWaypoint, double currentMax, IFQMPlanable lane, Coordinates position)
		{
			// get dist to waypoint
			double stopSpeedDistance = lane.DistanceBetween(position, waypoint.Position);

			// segment max speed
			double segmentMaxSpeed = currentMax;

			// distance to stop from max v given desired acceleration
			double stopEnvelopeLength = (Math.Pow(speedAtWaypoint, 2) - Math.Pow(currentMax, 2)) / (2.0 * -0.5);

			// check if we are within profile
			if (stopSpeedDistance > 0 && stopSpeedDistance < stopEnvelopeLength)
			{
				// get speed along profile
				double requiredSpeed = speedAtWaypoint + ((stopSpeedDistance / stopEnvelopeLength) * (segmentMaxSpeed - speedAtWaypoint));
				return requiredSpeed;
			}
			else
			{
				return segmentMaxSpeed;
			}
		}

		/// <summary>
		/// Gets the closest component to a location
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public SLComponentType ClosestComponent(Coordinates loc)
		{
			LinePath iLp = this.InitialPath(loc);
			double init = iLp.GetPoint(iLp.GetClosestPoint(loc)).DistanceTo(loc);

			LinePath fLp = this.FinalPath(loc);
			double fin = fLp.GetPoint(fLp.GetClosestPoint(loc)).DistanceTo(loc);

			double inter = Interconnect.DistanceTo(loc);

			if (init < inter && init < fin)
				return SLComponentType.Initial;
			else if (fin < inter && fin < init)
				return SLComponentType.Final;
			else
				return SLComponentType.Interconnect;
		}

		#endregion

		#region INavigableTravelArea Members

		/// <summary>
		/// Gets exit or goal downstream
		/// </summary>
		/// <returns></returns>
		public List<DownstreamPointOfInterest> Downstream(Coordinates currentPosition, List<ArbiterWaypoint> ignorable, INavigableNode goal)
		{
			List<DownstreamPointOfInterest> downstream = Initial.Downstream(currentPosition, ignorable, goal);
			double addedDist = this.DistanceBetween(currentPosition, Interconnect.FinalGeneric.Position);
			//double addedTime = this.Inside(currentPosition).Equals(Interconnect) ? 0.0 : Initial.TimeCostInLane(Initial.GetClosestPartition(currentPosition).Final, (ArbiterWaypoint)Interconnect.InitialGeneric) + Interconnect.ExtraCost;
			List<DownstreamPointOfInterest> secondary = Final.Downstream(Interconnect.FinalGeneric.Position, ignorable, goal);
			foreach (DownstreamPointOfInterest dpoi in secondary)
			{
				//dpoi.TimeCostToPoint += addedTime;
				dpoi.DistanceToPoint += addedDist;
			}
			downstream.AddRange(secondary);
			return downstream;
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
			return this.supraPath.GetPoint(this.GetClosestPoint(loc));
		}

		/// <summary>
		/// Closest point to the supra lane
		/// </summary>
		/// <param name="loc"></param>
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
		/// Path of the supra lane
		/// </summary>
		/// <returns></returns>
		public LinePath LanePath()
		{
			return this.supraPath;
		}

		/// <summary>
		/// Sets the path of the supra lane
		/// </summary>
		/// <param name="path"></param>
		public void SetLanePath(LinePath path)
		{
			this.supraPath = path;
		}

		/// <summary>
		/// Get components of teh supra lane
		/// </summary>
		public List<IVehicleArea> AreaComponents
		{
			get 
			{
				List<IVehicleArea> areas = new List<IVehicleArea>();
				areas.Add(Initial);
				areas.Add(Interconnect);
				areas.Add(Final);
				return areas;
			}
		}

		/// <summary>
		/// Gets the maximum speed depending upon where we are
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public double CurrentMaximumSpeed(Coordinates position)
		{			
			SLComponentType slct = this.ClosestComponent(position);

			if (slct == SLComponentType.Initial)
			{
				double maxInit = this.Initial.Way.Segment.SpeedLimits.MaximumSpeed;
				double maxInter = this.RequiredSpeed((ArbiterWaypoint)this.Interconnect.InitialGeneric, this.Interconnect.MaximumDefaultSpeed, maxInit, this, position);
				//double maxFinal = this.RequiredSpeed((ArbiterWaypoint)this.Interconnect.FinalGeneric, this.Final.Way.Segment.SpeedLimits.MaximumSpeed, this.Final.Way.Segment.SpeedLimits.MaximumSpeed, this, position);
				double max = Math.Min(maxInter, maxInit);//Math.Min(maxInit, Math.Min(maxInter, maxFinal));
				return max;
			}
			else if (slct == SLComponentType.Interconnect)
			{
				double maxInter = this.Interconnect.MaximumDefaultSpeed;
				double maxFinal = this.RequiredSpeed((ArbiterWaypoint)this.Interconnect.FinalGeneric, this.Final.Way.Segment.SpeedLimits.MaximumSpeed, this.Final.Way.Segment.SpeedLimits.MaximumSpeed, this, position);
				double max = Math.Min(maxInter, maxFinal);
				return max;
			}
			else
			{
				return this.Final.Way.Segment.SpeedLimits.MaximumSpeed;
			}
		}

		/// <summary>
		/// Waypoints in order from the supra lane
		/// </summary>
		public List<ArbiterWaypoint> WaypointList
		{
			get
			{
				return this.supraWaypoints;
			}
			set
			{
				this.supraWaypoints = value;
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
			int i = this.supraWaypoints.IndexOf(initial);
			int j = this.supraWaypoints.IndexOf(final);
			for (int k = i; k <= j; k++)
			{
				middle.Add(this.WaypointList[k]);
			}
			return middle;
		}

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="w1">Starting waypoint of search</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt)
		{
			return this.GetNext(w1, wt, new List<ArbiterWaypoint>());
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
			List<WaypointType> wts = new List<WaypointType>();
			wts.Add(wt);

			SLComponentType closest = this.ClosestComponent(loc);
			if (closest == SLComponentType.Initial)
				return this.GetNext(this.Initial.GetClosestPartition(loc).Final, wts, ignorable);
			else if (closest == SLComponentType.Final)
				return this.GetNext(this.Final.GetClosestPartition(loc).Final, wts, ignorable);
			else
				return this.GetNext(this.Final.GetClosestPartition(loc).Final, wts, ignorable);
		}

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="wt"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt, List<ArbiterWaypoint> ignorable)
		{
			List<WaypointType> wts = new List<WaypointType>();
			wts.Add(wt);
			return this.GetNext(w1, wts, ignorable);
		}

		public ArbiterWaypoint GetNext(Coordinates loc, List<WaypointType> wts, List<ArbiterWaypoint> ignorable)
		{
			SLComponentType closest = this.ClosestComponent(loc);
			if (closest == SLComponentType.Initial)
				return this.GetNext(this.Initial.GetClosestPartition(loc).Final, wts, ignorable);
			else if(closest == SLComponentType.Final)
				return this.GetNext(this.Final.GetClosestPartition(loc).Final, wts, ignorable);
			else
				return this.GetNext(this.Final.GetClosestPartition(this.Interconnect.FinalGeneric.Position).Final, wts, ignorable);
		}

		public ArbiterWaypoint GetNext(ArbiterWaypoint w1, List<WaypointType> wts, List<ArbiterWaypoint> ignorable)
		{
			if (!this.Initial.Equals(this.Final))
			{
				for (int i = this.WaypointList.IndexOf(w1); i < this.WaypointList.Count; i++)
				{
					foreach (WaypointType wt in wts)
					{
						if (!(wt == WaypointType.End && this.WaypointList[i].Lane.Equals(this.Initial)) &&
							this.WaypointList[i].WaypointTypeEquals(wt) && !ignorable.Contains(this.WaypointList[i]))
							return this.WaypointList[i];
					}
				}
			}
			else
			{
				for (int i = this.WaypointList.IndexOf(w1); i < this.WaypointList.Count; i++)
				{
					foreach (WaypointType wt in wts)
					{
						if (!(wt == WaypointType.End && this.WaypointList[i].Lane.Equals(this.Initial)) && 
							this.WaypointList[i].WaypointTypeEquals(wt) && !ignorable.Contains(this.WaypointList[i]))
							return this.WaypointList[i];
					}
				}

				for (int i = 0; i < this.WaypointList.IndexOf(w1); i++)
				{
					foreach (WaypointType wt in wts)
					{
						if (!(wt == WaypointType.End && this.WaypointList[i].Lane.Equals(this.Initial)) && 
							this.WaypointList[i].WaypointTypeEquals(wt) && !ignorable.Contains(this.WaypointList[i]))
							return this.WaypointList[i];
					}
				}
			}

			return null;
		}

		#endregion

		/// <summary>
		/// Min distance the initial path starts from
		/// </summary>
		public double MinDistFromStart = 50.0;

		/// <summary>
		/// Min distance the initial path starts from
		/// </summary>
		public double FinalSubtractedDist = -20.0;

		/// <summary>
		/// Gets the final path to go into
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public LinePath FinalPath(Coordinates loc)
		{
			if (this.Initial.Equals(this.Final))
			{
				return this.Final.LanePath((ArbiterWaypoint)this.Interconnect.FinalGeneric, MinDistFromStart + FinalSubtractedDist);
			}
			else
				return this.Final.LanePath((ArbiterWaypoint)this.Interconnect.FinalGeneric, this.Final.WaypointList[this.Final.WaypointList.Count-1]);
		}

		/// <summary>
		/// Initial path we are coming from
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public LinePath InitialPath(Coordinates loc)
		{
			if (this.Initial.Equals(this.Final))
			{
				// get our current position minus the subtracted dist
				LinePath.PointOnPath current = this.Final.GetClosestPoint(this.Interconnect.FinalGeneric.Position);
				current = this.Final.LanePath().AdvancePoint(current, MinDistFromStart + FinalSubtractedDist);
				
				// get the default end path from this
				LinePath lp = this.Final.LanePath(this.Final.LanePath().GetPoint(current), this.Interconnect.InitialGeneric.Position);
				return lp;
			}
			else
			{
				return this.Initial.LanePath(this.Initial.WaypointList[0], (ArbiterWaypoint)this.Interconnect.InitialGeneric);
			}
		}

		public Polygon IntersectionPolygon
		{
			get
			{
				if (this.Interconnect.InitialGeneric is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)this.Interconnect.InitialGeneric;

					if (this.Final.Way.Segment.RoadNetwork.IntersectionLookup.ContainsKey(aw.AreaSubtypeWaypointId))
						return this.Final.Way.Segment.RoadNetwork.IntersectionLookup[aw.AreaSubtypeWaypointId].IntersectionPolygon;
				}

				return null;
			}
		}

		#region IFQMPlanable Members


		public bool RelativelyInside(Coordinates c)
		{
			LinePath.PointOnPath pop = this.LanePath().GetClosestPoint(c);

			// check distance 
			if (!pop.Equals(this.LanePath().EndPoint) && !pop.Equals(this.LanePath().StartPoint) &&
				pop.Location.DistanceTo(c) < TahoeParams.VL * 2.0)
				return true;
			else if (pop.Location.DistanceTo(c) < TahoeParams.VL * 1.5)
				return true;
			else
				return false;
		}

		#endregion

		public void SparseDetermination(Coordinates coordinates, out bool sparseDownstream, out bool sparseNow, out double sparseDistance)
		{
			if (this.ClosestComponent(coordinates) == SLComponentType.Initial)
			{
				// check for sparse waypoints downstream in initial lane before inteconnect
				this.Initial.SparseDetermination(coordinates, out sparseDownstream, out sparseNow, out sparseDistance);
				if (sparseDownstream && sparseDistance < this.DistanceBetween(coordinates, this.Interconnect.InitialGeneric.Position))
				{
					return;
				}
				else
				{
					// check for sparse waypoints downstream after interconnect
					bool sparseDownstreamFinal;
					bool sparseNowFinal;
					double sparseDistanceFinal;
					this.Final.SparseDetermination(this.Interconnect.FinalGeneric.Position, out sparseDownstreamFinal, out sparseNowFinal, out sparseDistanceFinal);
					if (sparseDownstreamFinal)
					{
						sparseDistanceFinal += this.DistanceBetween(coordinates, this.Interconnect.FinalGeneric.Position);
						sparseNowFinal = false;
						sparseDownstream = true;
						sparseNow = false;
						sparseDistance = sparseDistanceFinal;
					}
				}
			}
			else if (this.ClosestComponent(coordinates) == SLComponentType.Interconnect)
			{
				// check for sparse waypoints downstream after interconnect
				bool sparseDownstreamFinal;
				bool sparseNowFinal;
				double sparseDistanceFinal;
				this.Final.SparseDetermination(this.Interconnect.FinalGeneric.Position, out sparseDownstreamFinal, out sparseNowFinal, out sparseDistanceFinal);
				if (sparseDownstreamFinal)
				{
					sparseDistanceFinal += this.DistanceBetween(coordinates, this.Interconnect.FinalGeneric.Position);
					sparseNowFinal = false;
					sparseDownstream = true;
					sparseNow = false;
					sparseDistance = sparseDistanceFinal;
				}
				else
				{
					sparseDownstream = false;
					sparseNow = false;
					sparseDistance = Double.MaxValue;
				}
			}
			// check for if in interconnect or final lane
			else
			{
				// check for sparse waypoints downstream in initial lane before inteconnect
				this.Final.SparseDetermination(coordinates, out sparseDownstream, out sparseNow, out sparseDistance);
			}
		}
	}
}
