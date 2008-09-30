using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.Tools
{
	/// <summary>
	/// Tools for roads
	/// </summary>
	public static class RoadToolkit
	{
		/// <summary>
		/// Waypoints to ignore
		/// </summary>
		/// <param name="way"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static List<ArbiterWaypoint> WaypointsClose(ArbiterWay way, Coordinates position, ArbiterWaypoint ignoreSpecifically)
		{
			List<ArbiterWaypoint> waypoints = new List<ArbiterWaypoint>();

			foreach (ArbiterLane al in way.Lanes.Values)
			{
				ArbiterWaypoint aw = al.GetClosestWaypoint(position, TahoeParams.VL * 2.0);
				if(aw != null)
					waypoints.Add(aw);
			}

			if (ignoreSpecifically != null && !waypoints.Contains(ignoreSpecifically))
				waypoints.Add(ignoreSpecifically);

			return waypoints;			
		}

		/// <summary>
		/// Get turn signals for turn
		/// </summary>
		/// <param name="sl"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static List<BehaviorDecorator> GetSupraTurnDecorators(SupraLane sl, Coordinates position)
		{
			// dist to lookahead
			double dist = 30.0 + sl.Interconnect.InterconnectPath.PathLength;
			double distBetween = sl.DistanceBetween(position, sl.Interconnect.FinalGeneric.Position);

			if(distBetween >= 0 && distBetween <= dist)
			{
				if (sl.Interconnect.TurnDirection == ArbiterTurnDirection.Left)
					return TurnDecorators.LeftTurnDecorator;
				else if (sl.Interconnect.TurnDirection == ArbiterTurnDirection.Right)
					return TurnDecorators.RightTurnDecorator;								
			}

			return TurnDecorators.NoDecorators;
		}

		/// <summary>
		/// gets the distance for the vehicle to stop given it is traveling a certain speed
		/// </summary>
		/// <param name="speed"></param>
		/// <returns></returns>
		public static double DistanceToStop(double speed)
		{
			double scale = TahoeParams.VL / 4.48;
			return speed * scale;
		}
	}
}
