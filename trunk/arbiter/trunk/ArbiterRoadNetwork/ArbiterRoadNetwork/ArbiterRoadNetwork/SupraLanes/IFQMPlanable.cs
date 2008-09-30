using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Type of exit
	/// </summary>
	public enum FQMStopType
	{
		EndOfLane,
		FinalExit,
		Stop
	}

	/// <summary>
	/// Type of exit
	/// </summary>
	public enum ExitType
	{
		Exit,
		Stop
	}

	/// <summary>
	/// items that can be planned over by the forward quadrant monitor
	/// </summary>
	public interface IFQMPlanable
	{
		/// <summary>
		/// Gets closest coordinate to  location
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		Coordinates GetClosest(Coordinates loc);

		/// <summary>
		/// Gets closest point on the lante to the location
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		LinePath.PointOnPath GetClosestPoint(Coordinates loc);

		/// <summary>
		/// Distance between two points along lane
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <returns></returns>
		double DistanceBetween(Coordinates c1, Coordinates c2);

		/// <summary>
		/// Distance between two waypoints
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="w2"></param>
		/// <returns></returns>
		double DistanceBetween(ArbiterWaypoint w1, ArbiterWaypoint w2);

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="w1">Starting waypoint of search</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <returns></returns>
		ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt);

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="w1">Starting waypoint of search</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <param name="ignorable">ignorable waypoints</param>
		/// <returns></returns>
		ArbiterWaypoint GetNext(ArbiterWaypoint w1, WaypointType wt, List<ArbiterWaypoint> ignorable);

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="loc">Location to start looking from</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <param name="ignorable">ignorable waypoints</param>
		/// <returns></returns>
		ArbiterWaypoint GetNext(Coordinates loc, WaypointType wt, List<ArbiterWaypoint> ignorable);

		/// <summary>
		/// Get the next waypoint of a certain type
		/// </summary>
		/// <param name="loc">Location to start looking from</param>
		/// <param name="wt">Waypoint type to look for</param>
		/// <param name="ignorable">ignorable waypoints</param>
		/// <returns></returns>
		ArbiterWaypoint GetNext(Coordinates loc, List<WaypointType> wts, List<ArbiterWaypoint> ignorable);

		/// <summary>
		/// Gets next waypoint of a certain type ignoring certain waypoints
		/// </summary>
		/// <param name="w1"></param>
		/// <param name="wt"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		ArbiterWaypoint GetNext(ArbiterWaypoint w1, List<WaypointType> wts, List<ArbiterWaypoint> ignorable);

		/// <summary>
		/// Path of lane from a waypoint for a certain distance
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="distance">Distance to get path</param>
		/// <returns></returns>
		LinePath LanePath(ArbiterWaypoint w1, double distance);

		/// <summary>
		/// Path of lane between two waypoints
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="w2">Final waypoint</param>
		/// <returns></returns>
		LinePath LanePath(ArbiterWaypoint w1, ArbiterWaypoint w2);

		/// <summary>
		/// Path of lane from a waypoint for a certain distance
		/// </summary>
		/// <param name="w1">Initial waypoint</param>
		/// <param name="distance">Distance to get path</param>
		/// <returns></returns>
		LinePath LanePath(Coordinates c1, Coordinates c2);

		/// <summary>
		/// Path of lane
		/// </summary>
		LinePath LanePath();

		/// <summary>
		/// Sets the lane path
		/// </summary>
		void SetLanePath(LinePath path);

		/// <summary>
		/// Vehicle areas to worry about
		/// </summary>
		List<IVehicleArea> AreaComponents
		{
			get;
		}

		/// <summary>
		/// Get the maximum speed at a certain position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		double CurrentMaximumSpeed(Coordinates position);

		/// <summary>
		/// List of waypoints in order for the FQM lane
		/// </summary>
		List<ArbiterWaypoint> WaypointList
		{
			get;
			set;
		}

		/// <summary>
		/// Get waypoints from initial to final
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		List<ArbiterWaypoint> WaypointsInclusive(ArbiterWaypoint initial, ArbiterWaypoint final);

		/// <summary>
		/// Checks if the coordinate is close to being inside or is inside the planable lane
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		bool RelativelyInside(Coordinates c);

		void SparseDetermination(Coordinates coordinates, out bool sparseDownstream, out bool sparseNow, out double sparseDistance);
	}
}
