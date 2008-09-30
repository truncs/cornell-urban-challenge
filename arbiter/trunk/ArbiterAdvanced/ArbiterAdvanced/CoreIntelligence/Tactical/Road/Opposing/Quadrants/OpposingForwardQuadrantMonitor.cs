using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Tracking;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Quadrants
{
	/// <summary>
	/// Monitors the road ahead of us for an opposing lane
	/// </summary>
	public class OpposingForwardQuadrantMonitor
	{
		public double NextNavigationStopDistance;

		/// <summary>
		/// Forward vehicle we are tracking
		/// </summary>
		public OpposingForwardVehicleTracker ForwardVehicle;

		public TravelingParameters NaviationParameters;

		private TravelingParameters? currentParamters;

		public TravelingParameters? CurrentParamters
		{
			get { return currentParamters; }
			set { currentParamters = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public OpposingForwardQuadrantMonitor()
		{
			this.ForwardVehicle = new OpposingForwardVehicleTracker();
		}

		/// <summary>
		/// Reset the forward monitor
		/// </summary>
		public void Reset()
		{	
			this.ForwardVehicle.Reset();
		}

		/// <summary>
		/// Behavior given we stay in the current lane
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <param name="downstreamPoint"></param>
		/// <returns></returns>
		public Maneuver PrimaryManeuver(ArbiterLane lane, ArbiterLane closestGood, VehicleState state, List<ITacticalBlockage> blockages)
		{
			// possible parameterizations
			List<TravelingParameters> tps = new List<TravelingParameters>();

			#region Nav

			// get the next thing we need to stop at no matter what and parameters for stopping at it
			ArbiterWaypoint navStop;
			double navStopSpeed;
			double navStopDistance;
			StopType navStopType;

			// make sure we stop an extra 3VL from any navigational stop
			double extraDistance = 0.0;

			// get next stop in this opposing lane
			this.NextOpposingNavigationalStop(lane, closestGood, state.Front, extraDistance, 
				 out navStopSpeed, out navStopDistance, out navStopType, out navStop);

			// set global stop distance
			this.NextNavigationStopDistance = navStopDistance;

			// create parameterization of the stop
			TravelingParameters navParams = this.NavStopParameterization(lane, navStopSpeed, navStopDistance, navStop, navStopType, state);
			this.NaviationParameters = navParams;

			// add to nav parames to arbiter information
			CoreCommon.CurrentInformation.FQMBehavior = navParams.Behavior.ToShortString();
			CoreCommon.CurrentInformation.FQMBehaviorInfo = navParams.Behavior.ShortBehaviorInformation();
			CoreCommon.CurrentInformation.FQMSpeedCommand = navParams.Behavior.SpeedCommandString();
			CoreCommon.CurrentInformation.FQMDistance = navParams.DistanceToGo.ToString("F6");
			CoreCommon.CurrentInformation.FQMSpeed = navParams.RecommendedSpeed.ToString("F6");
			CoreCommon.CurrentInformation.FQMState = navParams.NextState.ShortDescription();
			CoreCommon.CurrentInformation.FQMStateInfo = navParams.NextState.StateInformation();
			CoreCommon.CurrentInformation.FQMStopType = navStopType.ToString();
			CoreCommon.CurrentInformation.FQMWaypoint = navStop.ToString();
			CoreCommon.CurrentInformation.FQMSegmentSpeedLimit = lane.CurrentMaximumSpeed(state.Position).ToString("F1");

			// add nav parameter
			tps.Add(navParams);

			#endregion

			#region Vehicle

			// forward vehicle update
			this.ForwardVehicle.Update(lane, state);			

			if (this.ForwardVehicle.ShouldUseForwardTracker)
			{
				// get forward vehicle params
				TravelingParameters vehicleParams = this.ForwardVehicle.Follow(lane, state);

				// add vehicle param
				tps.Add(vehicleParams);
			}

			#endregion

			#region Blockages

			/*
			// get the blockage stop parameters
			bool stopAtBlockage;
			double blockageSpeed;
			double blockageDistance;
			this.StopForBlockages(lane, state, List<ITacticalBlockage> blockages, out stopAtBlockage, out blockageSpeed, out blockageDistance);
			*/

			#endregion

			// sort params by most urgent
			tps.Sort();

			// set current
			this.currentParamters = tps[0];

			// out of navigation, blockages, and vehicle following determine the actual primary behavior parameters
			return new Maneuver(tps[0].Behavior, tps[0].NextState, tps[0].Decorators, state.Timestamp);
		}

		/// <summary>
		/// Generate the traveling parameterization for the desired behaivor
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="navStopSpeed"></param>
		/// <param name="navStopDistance"></param>
		/// <param name="navStop"></param>
		/// <param name="navStopType"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		private TravelingParameters NavStopParameterization(ArbiterLane lane, double navStopSpeed, double navStopDistance, ArbiterWaypoint navStop, StopType navStopType, VehicleState state)
		{
			// get min dist
			double distanceCutOff = CoreCommon.OperationalStopDistance;

			// turn direction default
			List<BehaviorDecorator> decorators = TurnDecorators.NoDecorators;

			// create new params
			TravelingParameters tp = new TravelingParameters();

			#region Get Maneuver

			Maneuver m = new Maneuver();
			bool usingSpeed = true;

			// get lane path
			LinePath lp = lane.LanePath().Clone();
			lp.Reverse();

			#region Distance Cutoff

			// check if distance is less than cutoff
			if (navStopDistance < distanceCutOff)
			{
				// default behavior
				tp.SpeedCommand = new StopAtDistSpeedCommand(navStopDistance);
				Behavior b = new StayInLaneBehavior(lane.LaneId, new StopAtDistSpeedCommand(navStopDistance), new List<int>(), lp, lane.Width, lane.NumberOfLanesLeft(state.Front, false), lane.NumberOfLanesRight(state.Front, false));

				// stopping so not using speed param
				usingSpeed = false;

				IState nextState = CoreCommon.CorePlanningState;
				m = new Maneuver(b, nextState, decorators, state.Timestamp);
			}

			#endregion

			#region Outisde Distance Envelope

			// not inside distance envalope
			else
			{
				// get lane
				ArbiterLane al = lane;

				// default behavior
				tp.SpeedCommand = new ScalarSpeedCommand(Math.Min(navStopSpeed, 2.24));
				Behavior b = new StayInLaneBehavior(al.LaneId, new ScalarSpeedCommand(Math.Min(navStopSpeed, 2.24)), new List<int>(), lp, al.Width, al.NumberOfLanesRight(state.Front, false), al.NumberOfLanesLeft(state.Front, false));

				// standard behavior is fine for maneuver
				m = new Maneuver(b, CoreCommon.CorePlanningState, decorators, state.Timestamp);
			}

			#endregion

			#endregion

			#region Parameterize
			
			tp.Behavior = m.PrimaryBehavior;
			tp.Decorators = m.PrimaryBehavior.Decorators;
			tp.DistanceToGo = navStopDistance;
			tp.NextState = m.PrimaryState;
			tp.RecommendedSpeed = navStopSpeed;
			tp.Type = TravellingType.Navigation;
			tp.UsingSpeed = usingSpeed;
			tp.VehiclesToIgnore = new List<int>();			

			// return navigation params
			return tp;

			#endregion
		}

		/// <summary>
		/// Gets the next navigational stop relavant to us (stop or end) in the closest good lane or our current opposing lane
		/// </summary>
		/// <param name="closestGood"></param>
		/// <param name="coordinates"></param>
		/// <param name="ignorable"></param>
		/// <param name="navStopSpeed"></param>
		/// <param name="navStopDistance"></param>
		/// <param name="navStopType"></param>
		/// <param name="navStop"></param>
		private void NextOpposingNavigationalStop(ArbiterLane opposing, ArbiterLane closestGood, Coordinates coordinates, double extraDistance, 
			out double navStopSpeed, out double navStopDistance, out StopType navStopType, out ArbiterWaypoint navStop)
		{
			ArbiterWaypoint current = null;
			double minDist = Double.MaxValue;
			StopType st = StopType.EndOfLane;

			#region Closest Good Parameterization

			foreach (ArbiterWaypoint aw in closestGood.WaypointList)
			{
				if (aw.IsStop || aw.NextPartition == null)
				{
					double dist = closestGood.DistanceBetween(coordinates, aw.Position);

					if (dist < minDist && dist >= 0)
					{
						current = aw;
						minDist = dist;
						st = aw.IsStop ? StopType.StopLine : StopType.EndOfLane;
					}
				}
			}

			#endregion

			#region Opposing Parameterization

			ArbiterWaypoint opStart = opposing.GetClosestPartition(coordinates).Initial;
			int startIndex = opposing.WaypointList.IndexOf(opStart);

			for (int i = startIndex; i >= 0; i--)
			{
				ArbiterWaypoint aw = opposing.WaypointList[i];
				if (aw.IsStop || aw.PreviousPartition == null)
				{
					double dist = opposing.DistanceBetween(aw.Position, coordinates);

					if (dist < minDist && dist >= 0)
					{
						current = aw;
						minDist = dist;
						st = aw.IsStop ? StopType.StopLine : StopType.EndOfLane;
					}
				}
			}

			#endregion

			double tmpDistanceIgnore;
			this.StoppingParams(current, closestGood, coordinates, extraDistance, out navStopSpeed, out tmpDistanceIgnore);
			navStop = current;
			navStopDistance = minDist;
			navStopType = st;
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
		public void StoppingParams(ArbiterWaypoint waypoint, ArbiterLane lane, Coordinates position, double extraDistance,
			out double stopSpeed, out double stopDistance)
		{
			// get dist to waypoint
			stopDistance = lane.DistanceBetween(position, waypoint.Position) - extraDistance;

			// speed tools
			stopSpeed = SpeedTools.GenerateSpeed(stopDistance, CoreCommon.OperationalStopSpeed, 2.24);
		}

		/// <summary>
		/// Checks if hte opposing lane is clear to pass an opposing vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public bool ClearForDisabledVehiclePass(ArbiterLane lane, VehicleState state, double vUs, Coordinates minReturn)
		{
			// update the forward vehicle
			this.ForwardVehicle.Update(lane, state);

			// check if the rear vehicle exists and is moving along with us
			if (this.ForwardVehicle.ShouldUseForwardTracker && this.ForwardVehicle.CurrentVehicle != null)
			{
				// distance from other to us
				double currentDistance = lane.DistanceBetween(this.ForwardVehicle.CurrentVehicle.ClosestPosition, state.Front) - (2 * TahoeParams.VL);
				double minChangeDist = lane.DistanceBetween(minReturn, state.Front);

				// check if he's within min return dist
				if(currentDistance > minChangeDist)
				{
					// params
					double vOther = this.ForwardVehicle.CurrentVehicle.StateMonitor.Observed.speedValid ? this.ForwardVehicle.CurrentVehicle.Speed : lane.Way.Segment.SpeedLimits.MaximumSpeed;

					// get distance of envelope for him to slow to our speed
					double xEnvelope = (Math.Pow(vUs, 2.0) - Math.Pow(vOther, 2.0)) / (2.0 * -0.5);

					// check to see if vehicle is outside of the envelope to slow down for us after 3 seconds
					double xSafe = currentDistance - minChangeDist - (xEnvelope + (vOther * 15.0));
					return xSafe > 0 ? true : false;					
				}
				else
					return false;
			}
			else
				return true;
		}
	}
}
