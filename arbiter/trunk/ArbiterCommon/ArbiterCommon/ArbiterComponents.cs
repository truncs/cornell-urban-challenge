using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Splines;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Type of environment we are running in
	/// </summary>
	/// <remarks>Used only for facade reference</remarks>
	public enum OperationalMode
	{
		Normal,
		Simulation,
		Test
	}

	/// <summary>
	/// Describes the Current Execution Mode of the Arbiter
	/// </summary>
	public enum ArbiterMode
	{
		/// <summary>
		/// Debug has various Log and Messaging Output to Help Analyze Execution
		/// </summary>
		Debug,

		/// <summary>
		/// Release has minimal Log and Messaging Output to Streamline Execution
		/// </summary>
		Release
	}

	/// <summary>
	/// State the Arbiter is in
	/// </summary>
	public enum ArbiterDirective
	{
		/// <summary>
		/// Come to stop in the current behavior
		/// </summary>
		Pause,

		/// <summary>
		/// Behave normally
		/// </summary>
		Run
	}

	/// <summary>
	/// Accessible Class that Contains Global Fields Than Need to be Known Around the Arbiter
	/// </summary>
	public static class ArbiterComponents
	{
		/// <summary>
		/// Operational mode of the arbiter
		/// </summary>
		/// <remarks>defaults to normal</remarks>
		public static OperationalMode OperationalDirective = OperationalMode.Normal;

		/// <summary>
		/// Do we want to run the Arbiter with full debugging output or streamlined in a release mode
		/// </summary>
		public static ArbiterMode ArbiterMode;

		// Things to Output in Debug Mode
		public static bool DebugRoute = true;			
		public static bool DebugBehavior = true;
		public static bool DebugGoal = true;
		public static bool DebugPosition;	
		public static bool DebugVehicleState;

		// Thigns to Output in Release Mode
		public static bool ReleaseRoute;
		public static bool ReleasePosition;
		public static bool ReleaseBehavior;
		public static bool ReleaseGoal;
		public static bool ReleaseVehicleState;

		/// <summary>
		/// Static Component of the Least Distance from a StopLine that the Arbiter
		/// will push a stopAtLine behavior (in meters). Can be more based upon speed.
		/// </summary>
		public static double StopAtLineDistance = 200;

		/// <summary>
		/// Sets if the vehicle is to wait indefinitely for an intersection to clear
		/// </summary>
		public static bool IntersectionWaitIndefinitely = true;

		/// <summary>
		/// Sets the max speed in an intersection. If merging, speed is set to max of that in the following segment
		/// </summary>
		public static double IntersectionMaxSpeed = 1.7;

		public static double SafetyZoneMaxSpeed = 5.0;

		public static double StopLineSearchSpeed = 2.0;

		/// <summary>
		/// Change this manually to set if we're using the TestDataServer or the real Operational Controller
		/// </summary>
		public static bool UsingTestDataServer = false;

		/// <summary>
		/// The mandate of the Arbiter
		/// </summary>
		public static ArbiterDirective ArbiterDirective = ArbiterDirective.Pause;

		/// <summary>
		/// Plan at 10Hz
		/// </summary>
		public static int ArbiterCycleTime = 100;

		/// <summary>
		/// Planning frequency of the arbiter in seconds
		/// </summary>
		public static double PlanningFrequency = 0.1;

		/// <summary>
		/// The currently used rndf network
		/// </summary>
		public static RndfNetwork RndfNetwork;

		/// <summary>
		/// Current State of the Arbiter
		/// </summary>
		public static IState ArbiterState;

		/// <summary>
		/// Speed to be at for the operational layer to stop
		/// </summary>
		public static double OperationalStopSpeed = 1.7;

		/// <summary>
		/// Maximum acceleration
		/// </summary>
		public static double MaximumAcceleration = 1;

		/// <summary>
		/// Distance the operational needs to stop
		/// </summary>
		public static double OperationalStopDistance = 7;

		/// <summary>
		/// Defines the stopping acceleration to make more reasonable stops
		/// </summary>
		public static double StoppingAcceleration = 0.5;

		/// <summary>
		/// Defines the paths of each lane (preprocessed)
		/// </summary>
		public static Dictionary<LaneID, Path> ForwardsLanePaths;

		/// <summary>
		/// Defines the reversed paths of each lane (preprocessed)
		/// </summary>
		public static Dictionary<LaneID, Path> ReverseLanePaths;

		/// <summary>
		/// Lane Splines of the forward lanes
		/// </summary>
		public static Dictionary<LaneID, List<CubicBezier>> ForwardLaneSplines;
		
		/// <summary>
		/// Turn splines of every turn
		/// </summary>
		public static Dictionary<InterconnectID, Path> TurnPaths;
	}
}
