using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Penalties for nav in seconds
	/// </summary>
	[Serializable]
	public static class NavigationPenalties
	{
		public static double StopWaypoint = 15.0;
		public static double Interconnect = 5.0;
		public static double LeftTurn = 10.0;
		public static double ChangeLanes = 10.0;
		public static double ZoneMinSpeedDefault = 2.24;
		public static double ParkingSpotWaypoint = 30.0;
		public static double TurnOverPriorityDefault = 5.0;
		public static double TurnOverPriorityExtra = 10.0;
		public static double ZoneWaypoint = 120.0;
		public static double UTurnPenalty = 30.0;
	}
}
