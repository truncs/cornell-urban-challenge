using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common.Arbiter
{
	/// <summary>
	/// Arbiter Information
	/// </summary>
	[Serializable]
	public class ArbiterInformation
	{
		// state
		public string CurrentState = "";
		public string CurrentStateInfo = "";

		// forward quadrant monitor
		public string FQMWaypoint = "";
		public string FQMSpeed = "";
		public string FQMDistance = "";
		public string FQMStopType = "";
		public string FQMState = "";
		public string FQMStateInfo = "";
		public string FQMBehavior = "";
		public string FQMBehaviorInfo = "";
		public string FQMSpeedCommand = "";
		public string FQMSegmentSpeedLimit = "";

		// next
		public string NextState = "";
		public string NextStateInfo = "";
		public string NextBehavior = "";
		public string NextBehaviorInfo = "";
		public string NextSpeedCommand = "";
		public string NextBehaviorTimestamp = "";
		public string NextBehaviorTurnSignals = "";

		// navigation
		public string Route1Wp = "";
		public string Route1Time = "";
		public RouteInformation Route1 = null;
		public string Route2Wp = "";
		public string Route2Time = "";
		public RouteInformation Route2 = null;
		public string RouteCheckpoint = "";
		public string RouteCheckpointId = "";
		public string GoalsRemaining = "";
	
		// forward vehicle tracker
		public string FVTSpeed = "";
		public string FVTDistance = "";
		public string FVTState = "";
		public string FVTStateInfo = "";
		public string FVTBehavior = "";
		public string FVTSpeedCommand = "";
		public string FVTXSeparation = "NaN";
		public int[] FVTIgnorable;

		// lane agent
		public string LAInitial = "";
		public string LATarget = "";
		public string LAProbabilityCorrect = "";
		public string LAPosteriorProbInitial = "";
		public string LAPosteriorProbTarget = "";
		public string LAConsistent = "";
		public string LASceneLikelyLane = "";

		// other
		public string Blockage = "NONE";

		// log the information
		public string LogString()
		{
			string spacer = "\n\n";
			string header = DateTime.Now.ToString() + " -----------------------------------------------------\n";
			string footer = " End -----------------------------------------------------";
			
			string stateString = "Current:\n State: " + this.CurrentState + ",  Current State Info: " + this.CurrentStateInfo + "\n\n";
			
			string forwardQuadrantString = "Forward Quadrant Monitor:\nWaypoint: " + this.FQMWaypoint + ",  Speed: " + FQMSpeed + ",   Distance: " + FQMDistance +
				",\n  Stop Type: " + FQMStopType + ",  State: " + FQMState + ",  State Info: " + FQMStateInfo +
				",\n  Behavior: " + FQMBehavior + ",  Behavior Info: " + FQMBehaviorInfo + ",  Speed Command: " + FQMSpeedCommand + ", Speed Limits: " + FQMSegmentSpeedLimit + "\n\n";
			
			string forwardVehicleString = "Forward Vehicle Tracker:\nSpeed: " + FVTSpeed + ",   Distance: " + FVTDistance +
				",\n  State: " + FVTState + ",  State Info: " + FVTStateInfo +
				",\n  Behavior: " + FVTBehavior + ",  Speed Command: " + FVTSpeedCommand + "\n\n";
			
			string nextString = "Next:\nNext State: " + this.NextState + ",  Next State Info: " + NextStateInfo + ",   Next Behavior: " + NextBehavior +
				",\n  Next Behavior Info: " + NextBehaviorInfo + ",  Next Speed Command: " + NextSpeedCommand + ", Turn Signals: " + NextBehaviorTurnSignals + ", Timestamp: " + NextBehaviorTimestamp + "\n\n";
			
			string navigationString = "Navigation:\nRoute1 Waypoint: " + Route1Wp + ",  Route1 Time: " + Route1Time + ",   Route2 Waypoint: " + Route2Wp +
				",\n  Route2 Time: " + Route2Time + ",  Checkpoint: " + RouteCheckpoint + ", Checkpoint Id: " + RouteCheckpointId + ", Goals Remaining: " + GoalsRemaining + "\n\n";
			
			string laneAgentString = "Lane Agent:\nInitial: " + LAInitial + ",  Target: " + LATarget + ",   Correct: " + LAProbabilityCorrect +
				",\n  Posterior Initial: " + LAPosteriorProbInitial + ",  Posterior Target: " + LAPosteriorProbTarget + ", Consistency: " + LAConsistent + ", Likely: " + LASceneLikelyLane + "\n\n";

			// return combined
			string logString = spacer + header + stateString + forwardQuadrantString + forwardVehicleString + nextString + navigationString + laneAgentString + footer + spacer;
			return logString;
		}

		// objects to display
		public List<ArbiterInformationDisplayObject> DisplayObjects;

		/// <summary>
		/// Constructor
		/// </summary>
		public ArbiterInformation()
		{
			this.DisplayObjects = new List<ArbiterInformationDisplayObject>();
		}
	}

	/// <summary>
	/// Route plans
	/// </summary>
	[Serializable]
	public class RouteInformation : IComparable
	{
		/// <summary>
		/// Route
		/// </summary>
		public List<Coordinates> RoutePlan;

		/// <summary>
		/// Time cost
		/// </summary>
		public double RouteTimeCost;

		/// <summary>
		/// Waypoint route is from
		/// </summary>
		public string Waypoint;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="plan"></param>
		/// <param name="time"></param>
		public RouteInformation(List<Coordinates> plan, double time, string waypoint)
		{
			this.RoutePlan = plan;
			this.RouteTimeCost = time;
			this.Waypoint = waypoint;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is RouteInformation)
			{
				RouteInformation other = (RouteInformation)obj;
				if (this.RouteTimeCost < other.RouteTimeCost)
					return -1;
				else if (this.RouteTimeCost == other.RouteTimeCost)
					return 0;
				else
					return 1;
			}
			else
			{
				throw new Exception("obj is not route information");
			}
		}

		#endregion
	}

	/// <summary>
	/// Type of object to display
	/// </summary>
	public enum ArbiterInformationDisplayObjectType
	{
		uTurnPolygon,
		leftBound,
		rightBound
	}

	/// <summary>
	/// Display object
	/// </summary>
	[Serializable]
	public struct ArbiterInformationDisplayObject
	{
		public Object DisplayObject;
		public ArbiterInformationDisplayObjectType Type;

		public ArbiterInformationDisplayObject(Object obj, ArbiterInformationDisplayObjectType type)
		{
			this.DisplayObject = obj;
			this.Type = type;
		}
	}
}
