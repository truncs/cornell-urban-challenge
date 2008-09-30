using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a road graph based upon an rndf navigable by the ai
	/// </summary>
	[Serializable]
	public class ArbiterRoadNetwork : INetworkObject
	{
		/// <summary>
		/// Name of the network
		/// </summary>
		public string Name;

		/// <summary>
		/// Creation date of the underlying rndf
		/// </summary>
		public string CreationDate;

		/// <summary>
		/// Waypoints in teh network
		/// </summary>
		public Dictionary<IAreaSubtypeWaypointId, IArbiterWaypoint> ArbiterWaypoints;

		/// <summary>
		/// Easy way to look up legacy waypoints
		/// </summary>
		public Dictionary<string, IArbiterWaypoint> LegacyWaypointLookup;

		/// <summary>
		/// Zones of the network
		/// </summary>
		public Dictionary<ArbiterZoneId, ArbiterZone> ArbiterZones;

		/// <summary>
		/// Segments of the network
		/// </summary>
		public Dictionary<ArbiterSegmentId, ArbiterSegment> ArbiterSegments;

		/// <summary>
		/// Interconnects of the network
		/// </summary>
		public Dictionary<ArbiterInterconnectId, ArbiterInterconnect> ArbiterInterconnects;

		/// <summary>
		/// Intersections of the network
		/// </summary>
		public Dictionary<ArbiterIntersectionId, ArbiterIntersection> ArbiterIntersections;

		/// <summary>
		/// Looks up an intersection from an exits waypoint id
		/// </summary>
		public Dictionary<IAreaSubtypeWaypointId, ArbiterIntersection> IntersectionLookup;

		/// <summary>
		/// Safety zones
		/// </summary>
		public List<ArbiterSafetyZone> ArbiterSafetyZones;

		/// <summary>
		/// All the display objects that are a part of this network
		/// </summary>
		public List<IDisplayObject> DisplayObjects;

		/// <summary>
		/// The projection this rndf was made a part of
		/// </summary>
		public PlanarProjection PlanarProjection;

		/// <summary>
		/// Checkpoint lookups
		/// </summary>
		public Dictionary<int, IArbiterWaypoint> Checkpoints;

		/// <summary>
		/// Possible vehicle areas
		/// </summary>
		public List<IVehicleArea> VehicleAreas;

		/// <summary>
		/// Maps scene road graph id's to vehicle area maps
		/// </summary>
		public Dictionary<string, IVehicleArea> VehicleAreaMap;

		/// <summary>
		/// Constructor
		/// </summary>
		public ArbiterRoadNetwork()
		{
			this.Name = "";
			this.CreationDate = "";
			this.DisplayObjects = new List<IDisplayObject>();
			this.ArbiterWaypoints = new Dictionary<IAreaSubtypeWaypointId, IArbiterWaypoint>();
			this.ArbiterZones = new Dictionary<ArbiterZoneId, ArbiterZone>();
			this.ArbiterInterconnects = new Dictionary<ArbiterInterconnectId, ArbiterInterconnect>();
			this.LegacyWaypointLookup = new Dictionary<string, IArbiterWaypoint>();
			this.ArbiterSegments = new Dictionary<ArbiterSegmentId, ArbiterSegment>();
			this.ArbiterSafetyZones = new List<ArbiterSafetyZone>();
			this.ArbiterIntersections = new Dictionary<ArbiterIntersectionId, ArbiterIntersection>();
			this.IntersectionLookup = new Dictionary<IAreaSubtypeWaypointId, ArbiterIntersection>();
			this.Checkpoints = new Dictionary<int, IArbiterWaypoint>();
		}

		/// <summary>
		/// Sets the speed limits of the areas of the network
		/// </summary>
		/// <param name="speedLimits"></param>
		public void SetSpeedLimits(List<ArbiterSpeedLimit> speedLimits)
		{
			// loop over speed
			foreach (ArbiterSpeedLimit asl in speedLimits)
			{
				// check if segment
				if (asl.Area is ArbiterSegmentId)
				{
					// get seg
					ArbiterSegment asg = this.ArbiterSegments[(ArbiterSegmentId)asl.Area];

					// set speed
					asg.SpeedLimits = asl;
					asg.SpeedLimits.MaximumSpeed = Math.Min(asg.SpeedLimits.MaximumSpeed, 13.4112);
				}
				// check if zone
				else if (asl.Area is ArbiterZoneId)
				{
					// get zone
					ArbiterZone az = this.ArbiterZones[(ArbiterZoneId)asl.Area];

					// set speed
					az.SpeedLimits = asl;
					az.SpeedLimits.MaximumSpeed = Math.Min(az.SpeedLimits.MaximumSpeed, 2.24);
					az.SpeedLimits.MinimumSpeed = Math.Min(az.SpeedLimits.MinimumSpeed, 2.24);
				}
				// unknown
				else
				{
					// notify
					Console.WriteLine("Unknown speed limit area type: " + asl.Area.ToString());
				}
			}
		}

		/// <summary>
		/// Gets closest lane to a point
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ArbiterLane ClosestLane(Coordinates point)
		{
			ArbiterLane closest = null;
			double best = Double.MaxValue;

			foreach (ArbiterSegment asg in ArbiterSegments.Values)
			{
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					PointOnPath closestPoint = al.PartitionPath.GetClosest(point);
					double tmp = closestPoint.pt.DistanceTo(point);
					if (tmp < best)
					{
						closest = al;
						best = tmp;
					}
				}
			}

			return closest;
		}

		/// <summary>
		/// Returns if point is in zone
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ArbiterZone InZone(Coordinates point)
		{
			foreach (ArbiterZone az in this.ArbiterZones.Values)
			{
				if (az.Perimeter.PerimeterPolygon.IsInside(point))
					return az;
			}

			return null;
		}

		/// <summary>
		/// Checks if the point is in an intersection
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ArbiterIntersection InIntersection(Coordinates point)
		{
			foreach(ArbiterIntersection ai in this.ArbiterIntersections.Values)
			{
				if(ai.IntersectionPolygon.IsInside(point))
					return ai;
			}

			return null;
		}

		/// <summary>
		/// Get interconnect closest to our position and orientation
		/// </summary>
		/// <param name="point"></param>
		/// <param name="heading"></param>
		/// <returns></returns>
		public ArbiterInterconnect ClosestInterconnect(Coordinates point, Coordinates heading)
		{
			ArbiterInterconnect closest = null;
			double best = Double.MaxValue;

			foreach (ArbiterInterconnect ai in this.ArbiterInterconnects.Values)
			{
				// get closest
				double distance = ai.InterconnectPath.GetPoint(ai.InterconnectPath.GetClosestPoint(point)).DistanceTo(point);

				// get heading of interconnect approximately
				Coordinates interHeading = ai.FinalGeneric.Position - ai.InitialGeneric.Position;

				if (distance < best && Math.Abs(interHeading.ToDegrees() - heading.ToDegrees()) < 45.0)
				{
					best = distance;
					closest = ai;
				}
			}

			return closest;
		}

		public void GenerateVehicleAreas()
		{
			this.VehicleAreas = new List<IVehicleArea>();
			this.VehicleAreaMap = new Dictionary<string, IVehicleArea>();

			foreach (ArbiterSegment asg in this.ArbiterSegments.Values)
			{
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					this.VehicleAreas.Add(al);

					foreach (ArbiterLanePartition alp in al.Partitions)
					{
						this.VehicleAreaMap.Add(alp.ToString(), al);
					}
				}
			}

			foreach (ArbiterZone az in this.ArbiterZones.Values)
			{
				this.VehicleAreas.Add(az);
				this.VehicleAreaMap.Add(az.ToString(), az);
			}

			foreach (ArbiterInterconnect ai in this.ArbiterInterconnects.Values)
			{
				this.VehicleAreas.Add(ai);
				this.VehicleAreaMap.Add(ai.ToString(), ai);
			}

			//foreach (ArbiterIntersection aint in this.ArbiterIntersections.Values)
			//{
			//	this.VehicleAreas.Add(aint);
			//}
		}

		public double MissionAverageMaxSpeed
		{
			get
			{
				if (this.ArbiterSegments.Count == 0)
					return 4.48;
				else
				{
					double total = 0.0;
					foreach (ArbiterSegment asg in this.ArbiterSegments.Values)
					{
						total += asg.SpeedLimits.MaximumSpeed;
					}
					return total / this.ArbiterSegments.Count;
				}
			}
		}
	}
}
