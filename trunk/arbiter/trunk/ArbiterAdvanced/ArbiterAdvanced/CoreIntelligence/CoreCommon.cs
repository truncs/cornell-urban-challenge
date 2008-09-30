using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.State;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Blockage;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence
{
	public static class CoreCommon
	{
		/// <summary>
		/// Current road network
		/// </summary>
		public static ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Current mission
		/// </summary>
		public static ArbiterMissionDescription Mission;

		/// <summary>
		/// Communications to the outside world
		/// </summary>
		public static Communicator Communications;

		/// <summary>
		/// Current core planning state
		/// </summary>
		public static IState CorePlanningState;

		/// <summary>
		/// Lane agent reasoning about lanes
		/// </summary>
		public static LaneAgent CoreLaneAgent;

		/// <summary>
		/// Operational dist at which it should be asked to stop
		/// </summary>
		public static double OperationalStopDistance = TahoeParams.VL;

		/// <summary>
		/// Stop line search dist
		/// </summary>
		public static double OperationslStopLineSearchDistance = TahoeParams.VL;

		/// <summary>
		/// Speed to ask the operational to stop
		/// </summary>
		public static double OperationalStopSpeed = 1.7;

		/// <summary>
		/// Desried accel
		/// </summary>
		public static double DesiredAcceleration = 0.5;

		/// <summary>
		/// Maximum negative acceleration
		/// </summary>
		public static double MaximumNegativeAcceleration = -1.0;

		/// <summary>
		/// Desired negative acceleration
		/// </summary>
		public static double DesiredNegativeAcceleration = -0.5;

		/// <summary>
		/// Distance at which accel breaks from max to negative
		/// </summary>
		public static double AccelerationBreakDistance = 20.0;

		/// <summary>
		/// Minimum speed of the operational
		/// </summary>
		public static double OperationalMinSpeed = 1.0;

		/// <summary>
		/// Current arebiter information
		/// </summary>
		public static ArbiterInformation CurrentInformation;

		/// <summary>
		/// Default navigation time cost of blockages
		/// </summary>
		public static double DefaultNavigationTimeCost = 2000;

		/// <summary>
		/// 4 second blockage recovery cooldown
		/// </summary>
		public static double BlockageCooldownMilliseconds = 4000;

		public static Navigator Navigation;

		public static BlockageTactical BlockageDirector;
	}
}
