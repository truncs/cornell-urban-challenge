using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Generates zones from xyRndf
	/// </summary>
	[Serializable]
	public class ZoneGeneration
	{
		private ICollection<SimpleZone> xyZones;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="xyZones"></param>
		public ZoneGeneration(ICollection<SimpleZone> xyZones)
		{
			this.xyZones = xyZones;
		}

		/// <summary>
		/// Generates the arbiter zones form the internal xy zones for the input road network
		/// </summary>
		/// <param name="arn"></param>
		/// <returns></returns>
		/// <remarks>TODO: add zone cost map, adjacency of parking spots, figure out width</remarks>
		public ArbiterRoadNetwork GenerateZones(ArbiterRoadNetwork arn)
		{
			Dictionary<ArbiterZoneId, ArbiterZone> zones = new Dictionary<ArbiterZoneId,ArbiterZone>();
			List<IArbiterWaypoint> waypoints = new List<IArbiterWaypoint>();

			foreach (SimpleZone sz in xyZones)
			{
				ArbiterZoneId azi = new ArbiterZoneId(int.Parse(sz.ZoneID));

				#region Generate Perimeter

				// old perim
				ZonePerimeter zp = sz.Perimeter;

				// perim id
				ArbiterPerimeterId api = new ArbiterPerimeterId(GenerationTools.GetId(zp.PerimeterID)[1], azi);

				#region Perimeter Waypoints

				List<ArbiterPerimeterWaypoint> perimeterWaypoints = new List<ArbiterPerimeterWaypoint>();

				foreach (PerimeterPoint pp in zp.PerimeterPoints)
				{
					// id
					ArbiterPerimeterWaypointId apwi = new ArbiterPerimeterWaypointId(
						GenerationTools.GetId(pp.ID)[2], api);

					// point
					ArbiterPerimeterWaypoint apw = new ArbiterPerimeterWaypoint(apwi, pp.position);

					// add
					perimeterWaypoints.Add(apw);
					waypoints.Add(apw);
					arn.DisplayObjects.Add(apw);
					arn.LegacyWaypointLookup.Add(pp.ID, apw);
				}

				#endregion

				// generate perimeter
				ArbiterPerimeter ap = new ArbiterPerimeter(api, perimeterWaypoints);
				arn.DisplayObjects.Add(ap);

				// set per in points
				foreach (ArbiterPerimeterWaypoint apw in perimeterWaypoints)
				{
					apw.Perimeter = ap;
				}

				#region Set Defined Links

				// set links among perimeter nodes
				for (int i = 1; i <= ap.PerimeterPoints.Count; i++)
				{
					ArbiterPerimeterWaypointId apwi = new ArbiterPerimeterWaypointId(i, ap.PerimeterId);
					ArbiterPerimeterWaypoint apw = ap.PerimeterPoints[apwi];

					if (i < ap.PerimeterPoints.Count)
					{
						ArbiterPerimeterWaypointId apwiNext = new ArbiterPerimeterWaypointId(i+1, ap.PerimeterId);
						apw.NextPerimeterPoint = ap.PerimeterPoints[apwiNext];
					}
					else
					{
						ArbiterPerimeterWaypointId apwiNext = new ArbiterPerimeterWaypointId(1, ap.PerimeterId);
						apw.NextPerimeterPoint = ap.PerimeterPoints[apwiNext];
					}
				}

				#endregion

				#endregion

				#region Generate Parking Spots

				List<ArbiterParkingSpot> parkingSpots = new List<ArbiterParkingSpot>();

				#region Parking Spots

				foreach (ParkingSpot ps in sz.ParkingSpots)
				{
					// spot id
					int apsiNum = GenerationTools.GetId(ps.SpotID)[1];
					ArbiterParkingSpotId apsi = new ArbiterParkingSpotId(apsiNum, azi);

					// spot width
					double spotWidth;

					// check if spot width not set
					if(ps.SpotWidth == null || ps.SpotWidth == "" || ps.SpotWidth == "0")
					{
						spotWidth = 3.0;
					}
					else
					{
						// convert feet to meters
						spotWidth = double.Parse(ps.SpotWidth) * 0.3048;
					}

					// spot
					ArbiterParkingSpot aps = new ArbiterParkingSpot(spotWidth, apsi);
					arn.DisplayObjects.Add(aps);

					// waypoints
					List<ArbiterParkingSpotWaypoint> parkingSpotWaypoints = new List<ArbiterParkingSpotWaypoint>();

					#region Parking Spot Waypoints

					#region Waypoint 1

					// id
					int apwi1Number = GenerationTools.GetId(ps.Waypoint1.ID)[2];
					ArbiterParkingSpotWaypointId apwi1 = new ArbiterParkingSpotWaypointId(apwi1Number, apsi);

					// generate waypoint 1
					ArbiterParkingSpotWaypoint apw1 = new ArbiterParkingSpotWaypoint(
						ps.Waypoint1.Position, apwi1, aps);

					// set
					parkingSpotWaypoints.Add(apw1);
					waypoints.Add(apw1);
					arn.DisplayObjects.Add(apw1);
					arn.LegacyWaypointLookup.Add(ps.Waypoint1.ID, apw1);
					apw1.ParkingSpot = aps;

					// checkpoint or not?
					if (ps.CheckpointWaypointID == ps.Waypoint1.ID)
					{
						apw1.IsCheckpoint = true;
						apw1.CheckpointId = int.Parse(ps.CheckpointID);
						aps.Checkpoint = apw1;
						arn.Checkpoints.Add(apw1.CheckpointId, apw1);
					}
					else
					{
						aps.NormalWaypoint = apw1;
					}

					#endregion

					#region Waypoint 2

					// id
					int apwi2Number = GenerationTools.GetId(ps.Waypoint2.ID)[2];
					ArbiterParkingSpotWaypointId apwi2 = new ArbiterParkingSpotWaypointId(apwi2Number, apsi);

					// generate waypoint 2
					ArbiterParkingSpotWaypoint apw2 = new ArbiterParkingSpotWaypoint(
						ps.Waypoint2.Position, apwi2, aps);

					// set
					parkingSpotWaypoints.Add(apw2);
					waypoints.Add(apw2);
					arn.DisplayObjects.Add(apw2);
					arn.LegacyWaypointLookup.Add(ps.Waypoint2.ID, apw2);
					apw2.ParkingSpot = aps;

					// checkpoint or not?
					if (ps.CheckpointWaypointID == ps.Waypoint2.ID)
					{
						apw2.IsCheckpoint = true;
						apw2.CheckpointId = int.Parse(ps.CheckpointID);
						aps.Checkpoint = apw2;
						arn.Checkpoints.Add(apw2.CheckpointId, apw2);
					}
					else
					{
						aps.NormalWaypoint = apw2;
					}

					#endregion

					#endregion

					// set waypoints
					aps.SetWaypoints(parkingSpotWaypoints);

					// add
					parkingSpots.Add(aps);
				}

				#endregion

				#endregion

				#region Create Zone

				// create zone
				ArbiterZone az = new ArbiterZone(azi, ap, parkingSpots, arn);

				// zone
				az.SpeedLimits = new ArbiterSpeedLimit();
				az.SpeedLimits.MaximumSpeed = 2.24;
				az.SpeedLimits.MinimumSpeed = 2.24;

				// add to final dictionary
				zones.Add(az.ZoneId, az);
				arn.DisplayObjects.Add(az);

				#endregion
			}

			// set zones
			arn.ArbiterZones = zones;

			// add waypoints
			foreach (IArbiterWaypoint iaw in waypoints)
			{
				// set waypoint
				arn.ArbiterWaypoints.Add(iaw.AreaSubtypeWaypointId, iaw);
			}

			// return 
			return arn;
		}

	} // end class


} // end namespace
