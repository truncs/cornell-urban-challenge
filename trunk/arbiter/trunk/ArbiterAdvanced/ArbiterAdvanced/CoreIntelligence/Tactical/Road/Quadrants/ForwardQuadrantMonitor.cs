using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Reasons about the lane ahead
	/// </summary>
	public class ForwardQuadrantMonitor
	{
		/// <summary>
		/// Forward vehicle we are tracking
		/// </summary>
		public ForwardVehicleTracker ForwardVehicle;

		/// <summary>
		/// Current travelling parameters chosen by fqm
		/// </summary>
		private TravelingParameters currentParameters;

		/// <summary>
		/// Current following params
		/// </summary>
		private TravelingParameters? followingParameters;

		/// <summary>
		/// Current navigation params
		/// </summary>
		private TravelingParameters navigationParameters;

		/// <summary>
		/// Lane parameters
		/// </summary>
		private TravelingParameters laneParameters;

		/// <summary>
		/// Constructor
		/// </summary>
		public ForwardQuadrantMonitor()
		{
			this.ForwardVehicle = new ForwardVehicleTracker();
		}

		#region Maneuvers

		/// <summary>
		/// Behavior given we stay in the current lane
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <param name="downstreamPoint"></param>
		/// <returns></returns>
		public TravelingParameters Primary(IFQMPlanable lane, VehicleState state, RoadPlan roadPlan, 
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, bool log)
		{
			// possible parameterizations
			List<TravelingParameters> tps = new List<TravelingParameters>();

			#region Lane Major Parameterizations with Current Lane Goal Params, If Best Goal Exists in Current Lane

			// check if the best goal is in the current lane
			ArbiterWaypoint lanePoint = null;
			if (lane.AreaComponents.Contains(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Lane))
				lanePoint = roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest;

			// get the next thing we need to stop at no matter what and parameters for stopping at it
			ArbiterWaypoint laneNavStop;
			double laneNavStopSpeed;
			double laneNavStopDistance;
			StopType laneNavStopType;
			this.NextNavigationalStop(lane, lanePoint, state.Front, state.ENCovariance, ignorable,
				out laneNavStopSpeed, out laneNavStopDistance, out laneNavStopType, out laneNavStop);

			// create parameterization of the stop
			TravelingParameters laneNavParams = this.NavStopParameterization(lane, roadPlan, laneNavStopSpeed, laneNavStopDistance, laneNavStop, laneNavStopType, state);
			this.navigationParameters = laneNavParams;
			this.laneParameters = laneNavParams;
			tps.Add(laneNavParams);

			#region Log
			if (log)
			{
				// add to current parames to arbiter information
				CoreCommon.CurrentInformation.FQMBehavior = laneNavParams.Behavior.ToShortString();
				CoreCommon.CurrentInformation.FQMBehaviorInfo = laneNavParams.Behavior.ShortBehaviorInformation();
				CoreCommon.CurrentInformation.FQMSpeedCommand = laneNavParams.Behavior.SpeedCommandString();
				CoreCommon.CurrentInformation.FQMDistance = laneNavParams.DistanceToGo.ToString("F6");
				CoreCommon.CurrentInformation.FQMSpeed = laneNavParams.RecommendedSpeed.ToString("F6");
				CoreCommon.CurrentInformation.FQMState = laneNavParams.NextState.ShortDescription();
				CoreCommon.CurrentInformation.FQMStateInfo = laneNavParams.NextState.StateInformation();
				CoreCommon.CurrentInformation.FQMStopType = laneNavStopType.ToString();
				CoreCommon.CurrentInformation.FQMWaypoint = laneNavStop.ToString();
				CoreCommon.CurrentInformation.FQMSegmentSpeedLimit = lane.CurrentMaximumSpeed(state.Position).ToString("F1");
			}
			#endregion

			#endregion

			#region Forward Vehicle Parameterization

			// forward vehicle update
			this.ForwardVehicle.Update(lane, state);

			// clear current params
			this.followingParameters = null;

			// check not in a sparse area
			bool sparseArea = lane is ArbiterLane ?
				((ArbiterLane)lane).GetClosestPartition(state.Front).Type == PartitionType.Sparse :
				((SupraLane)lane).ClosestComponent(state.Front) == SLComponentType.Initial && ((SupraLane)lane).Initial.GetClosestPartition(state.Front).Type == PartitionType.Sparse;
			
			// exists forward vehicle
			if (!sparseArea && this.ForwardVehicle.ShouldUseForwardTracker)
			{
				// get forward vehicle params, set lane decorators
				TravelingParameters vehicleParams = this.ForwardVehicle.Follow(lane, state, ignorable);
				vehicleParams.Behavior.Decorators.AddRange(this.laneParameters.Decorators);
				this.FollowingParameters = vehicleParams;

				#region Log
				if (log)
				{
					// add to current parames to arbiter information
					CoreCommon.CurrentInformation.FVTBehavior = vehicleParams.Behavior.ToShortString();
					CoreCommon.CurrentInformation.FVTSpeed = this.ForwardVehicle.FollowingParameters.RecommendedSpeed.ToString("F3");
					CoreCommon.CurrentInformation.FVTSpeedCommand = vehicleParams.Behavior.SpeedCommandString();
					CoreCommon.CurrentInformation.FVTDistance = vehicleParams.DistanceToGo.ToString("F2");
					CoreCommon.CurrentInformation.FVTState = vehicleParams.NextState.ShortDescription();
					CoreCommon.CurrentInformation.FVTStateInfo = vehicleParams.NextState.StateInformation();

					// set xSeparation
					CoreCommon.CurrentInformation.FVTXSeparation = this.ForwardVehicle.ForwardControl.xSeparation.ToString("F0");
				}
				#endregion

				// check if we're stopped behind fv, allow wait timer if true, stop wait timer if not behind fv
				bool forwardVehicleStopped = this.ForwardVehicle.CurrentVehicle.IsStopped;
				bool forwardSeparationGood = this.ForwardVehicle.ForwardControl.xSeparation < TahoeParams.VL * 2.5;
				bool wereStopped = CoreCommon.Communications.GetVehicleSpeed().Value < 0.1;
				bool forwardDistanceToGo = vehicleParams.DistanceToGo < 3.5;
				if (forwardVehicleStopped && forwardSeparationGood && wereStopped && forwardDistanceToGo)
					this.ForwardVehicle.StoppedBehindForwardVehicle = true;
				else
				{
					this.ForwardVehicle.StoppedBehindForwardVehicle = false;
					this.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Stop();
					this.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Reset();
				}

				// add vehicle param
				tps.Add(vehicleParams);
			}
			else
			{
				// no forward vehicle
				this.followingParameters = null;
				this.ForwardVehicle.StoppedBehindForwardVehicle = false;
			}

			#endregion

			#region Sparse Waypoint Parameterization

			// check for sparse waypoints downstream
			bool sparseDownstream;
			bool sparseNow;
			double sparseDistance;
			lane.SparseDetermination(state.Front, out sparseDownstream, out sparseNow, out sparseDistance);

			// check if sparse areas downstream
			if (sparseDownstream)
			{
				// set the distance to the sparse area
				if (sparseNow)
					sparseDistance = 0.0;

				// get speed
				double speed = SpeedTools.GenerateSpeed(sparseDistance, 2.24, lane.CurrentMaximumSpeed(state.Front));

				// maneuver
				Maneuver m = new Maneuver();
				bool usingSpeed = true;
				SpeedCommand sc = new ScalarSpeedCommand(speed);

				#region Parameterize Given Speed Command

				// check if lane
				if (lane is ArbiterLane)
				{
					// get lane
					ArbiterLane al = (ArbiterLane)lane;

					// default behavior
					Behavior b = new StayInLaneBehavior(al.LaneId, new ScalarSpeedCommand(speed), new List<int>(), al.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));
					b.Decorators = this.laneParameters.Decorators;

					// standard behavior is fine for maneuver
					m = new Maneuver(b, new StayInLaneState(al, CoreCommon.CorePlanningState), this.laneParameters.Decorators, state.Timestamp);
				}
				// check if supra lane
				else if (lane is SupraLane)
				{
					// get lane
					SupraLane sl = (SupraLane)lane;

					// get sl state
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;

					// get default beheavior
					Behavior b = sisls.GetBehavior(new ScalarSpeedCommand(speed), state.Front, new List<int>());
					b.Decorators = this.laneParameters.Decorators;

					// standard behavior is fine for maneuver
					m = new Maneuver(b, sisls, this.laneParameters.Decorators, state.Timestamp);
				}

				#endregion

				#region Parameterize

				// create new params
				TravelingParameters tp = new TravelingParameters();
				tp.Behavior = m.PrimaryBehavior;
				tp.Decorators = this.laneParameters.Decorators;
				tp.DistanceToGo = Double.MaxValue;
				tp.NextState = m.PrimaryState;
				tp.RecommendedSpeed = speed;
				tp.Type = TravellingType.Navigation;
				tp.UsingSpeed = usingSpeed;
				tp.SpeedCommand = sc;
				tp.VehiclesToIgnore = new List<int>();

				// return navigation params
				tps.Add(tp);

				#endregion
			}

			#endregion

			// sort params by most urgent
			tps.Sort();

			// set current params
			this.currentParameters = tps[0];

			// get behavior to check add vehicles to ignore
			if (this.currentParameters.Behavior is StayInLaneBehavior)
				((StayInLaneBehavior)this.currentParameters.Behavior).IgnorableObstacles = this.ForwardVehicle.VehiclesToIgnore;

			// out of navigation, blockages, and vehicle following determine the actual primary parameters for this lane
			return tps[0];
		}

		#endregion

		#region Functions

		/// <summary>
		/// Makes new parameterization for nav
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="lanePlan"></param>
		/// <param name="speed"></param>
		/// <param name="distance"></param>
		/// <param name="stopType"></param>
		/// <returns></returns>
		public TravelingParameters NavStopParameterization(IFQMPlanable lane, RoadPlan roadPlan, double speed, double distance, 
			ArbiterWaypoint stopWaypoint, StopType stopType, VehicleState state)
		{
			// get min dist
			double distanceCutOff = stopType == StopType.StopLine ? CoreCommon.OperationslStopLineSearchDistance : CoreCommon.OperationalStopDistance;

			#region Get Decorators

			// turn direction default
			ArbiterTurnDirection atd = ArbiterTurnDirection.Straight;
			List<BehaviorDecorator> decorators = TurnDecorators.NoDecorators;

			// check if need decorators
			if (lane is ArbiterLane &&
				stopWaypoint.Equals(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest) && 
				roadPlan.BestPlan.laneWaypointOfInterest.IsExit && 
				distance < 40.0)
			{
				if (roadPlan.BestPlan.laneWaypointOfInterest.BestExit == null)
					ArbiterOutput.Output("NAV BUG: lanePlan.laneWaypointOfInterest.BestExit: FQM NavStopParameterization");
				else
				{
					switch (roadPlan.BestPlan.laneWaypointOfInterest.BestExit.TurnDirection)
					{
						case ArbiterTurnDirection.Left:
							decorators = TurnDecorators.LeftTurnDecorator;
							atd = ArbiterTurnDirection.Left;
							break;
						case ArbiterTurnDirection.Right:
							atd = ArbiterTurnDirection.Right;
							decorators = TurnDecorators.RightTurnDecorator;
							break;
						case ArbiterTurnDirection.Straight:
							atd = ArbiterTurnDirection.Straight;
							decorators = TurnDecorators.NoDecorators;
							break;
						case ArbiterTurnDirection.UTurn:
							atd = ArbiterTurnDirection.UTurn;
							decorators = TurnDecorators.LeftTurnDecorator;
							break;
					}
				}
			}
			else if (lane is SupraLane)
			{
				SupraLane sl = (SupraLane)lane;
				double distToInterconnect = sl.DistanceBetween(state.Front, sl.Interconnect.InitialGeneric.Position);

				if ((distToInterconnect > 0 && distToInterconnect < 40.0) || sl.ClosestComponent(state.Front) == SLComponentType.Interconnect)
				{
					switch (sl.Interconnect.TurnDirection)
					{
						case ArbiterTurnDirection.Left:
							decorators = TurnDecorators.LeftTurnDecorator;
							atd = ArbiterTurnDirection.Left;
							break;
						case ArbiterTurnDirection.Right:
							atd = ArbiterTurnDirection.Right;
							decorators = TurnDecorators.RightTurnDecorator;
							break;
						case ArbiterTurnDirection.Straight:
							atd = ArbiterTurnDirection.Straight;
							decorators = TurnDecorators.NoDecorators;
							break;
						case ArbiterTurnDirection.UTurn:
							atd = ArbiterTurnDirection.UTurn;
							decorators = TurnDecorators.LeftTurnDecorator;
							break;
					}
				}
			}

			#endregion

			#region Get Maneuver

			Maneuver m = new Maneuver();
			bool usingSpeed = true;
			SpeedCommand sc = new StopAtDistSpeedCommand(distance);

			#region Distance Cutoff

			// check if distance is less than cutoff
			if (distance < distanceCutOff && stopType != StopType.EndOfLane)
			{
				// default behavior
				Behavior b = new StayInLaneBehavior(stopWaypoint.Lane.LaneId, new StopAtDistSpeedCommand(distance), new List<int>(), lane.LanePath(), stopWaypoint.Lane.Width, stopWaypoint.Lane.NumberOfLanesLeft(state.Front, true), stopWaypoint.Lane.NumberOfLanesRight(state.Front, true));

				// stopping so not using speed param
				usingSpeed = false;				

				// exit is next
				if (stopType == StopType.Exit)
				{
					// exit means stopping at a good exit in our current lane
					IState nextState = new StoppingAtExitState(stopWaypoint.Lane, stopWaypoint, atd, true, roadPlan.BestPlan.laneWaypointOfInterest.BestExit, state.Timestamp, state.Front);
					m = new Maneuver(b, nextState, decorators, state.Timestamp);
				}

				// stop line is left
				else if (stopType == StopType.StopLine)
				{
					// determine if hte stop line is the best exit
					bool isNavExit = roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Equals(stopWaypoint);

					// get turn direction
					atd = isNavExit ? atd : ArbiterTurnDirection.Straight;

					// predetermine interconnect if best exit
					ArbiterInterconnect desired = null;
					if (isNavExit)
						desired = roadPlan.BestPlan.laneWaypointOfInterest.BestExit;
					else if (stopWaypoint.NextPartition != null && state.Front.DistanceTo(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position) > 25)
						desired = stopWaypoint.NextPartition.ToInterconnect;

					// set decorators
					decorators = isNavExit ? decorators : TurnDecorators.NoDecorators;

					// stop at the stop
					IState nextState = new StoppingAtStopState(stopWaypoint.Lane, stopWaypoint, atd, isNavExit, desired);
					b = new StayInLaneBehavior(stopWaypoint.Lane.LaneId, new StopAtLineSpeedCommand(), new List<int>(), lane.LanePath(), stopWaypoint.Lane.Width, stopWaypoint.Lane.NumberOfLanesLeft(state.Front, true), stopWaypoint.Lane.NumberOfLanesRight(state.Front, true));
					m = new Maneuver(b, nextState, decorators, state.Timestamp);
					sc = new StopAtLineSpeedCommand();
				}
				else if(stopType == StopType.LastGoal)
				{
					// stop at the last goal
					IState nextState = new StayInLaneState(stopWaypoint.Lane, CoreCommon.CorePlanningState);
					m = new Maneuver(b, nextState, decorators, state.Timestamp);
				}
			}

			#endregion

			#region Outisde Distance Envelope

			// not inside distance envalope
			else
			{
				// set speed
				sc = new ScalarSpeedCommand(speed);

				// check if lane
				if (lane is ArbiterLane)
				{
					// get lane
					ArbiterLane al = (ArbiterLane)lane;

					// default behavior
					Behavior b = new StayInLaneBehavior(al.LaneId, new ScalarSpeedCommand(speed), new List<int>(), al.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));

					// standard behavior is fine for maneuver
					m = new Maneuver(b, new StayInLaneState(al, CoreCommon.CorePlanningState), decorators, state.Timestamp);
				}
				// check if supra lane
				else if (lane is SupraLane)
				{
					// get lane
					SupraLane sl = (SupraLane)lane;

					// get sl state
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;

					// get default beheavior
					Behavior b = sisls.GetBehavior(new ScalarSpeedCommand(speed), state.Front, new List<int>());

					// standard behavior is fine for maneuver
					m = new Maneuver(b, sisls, decorators, state.Timestamp);
				}
			}

			#endregion

			#endregion

			#region Parameterize

			// create new params
			TravelingParameters tp = new TravelingParameters();
			tp.Behavior = m.PrimaryBehavior;
			tp.Decorators = m.PrimaryBehavior.Decorators;
			tp.DistanceToGo = distance;
			tp.NextState = m.PrimaryState;
			tp.RecommendedSpeed = speed;
			tp.Type = TravellingType.Navigation;
			tp.UsingSpeed = usingSpeed;
			tp.SpeedCommand = sc;
			tp.VehiclesToIgnore = new List<int>();

			// return navigation params
			return tp;

			#endregion
		}

		/// <summary>
		/// Determines the point at which we need to stop in the current lane
		/// </summary>
		/// <param name="lanePoint"></param>
		/// <param name="position"></param>
		/// <param name="stopRequired"></param>
		/// <param name="stopSpeed"></param>
		/// <param name="stopDistance"></param>
		public void NextNavigationalStop(IFQMPlanable lane, ArbiterWaypoint lanePoint, Coordinates position, double[] enCovariance, List<ArbiterWaypoint> ignorable,
			out double stopSpeed, out double stopDistance, out StopType stopType, out ArbiterWaypoint stopWaypoint)
		{
			// variables for default next stop line or end of lane
			this.NextLaneStop(lane, position, enCovariance, ignorable, out stopWaypoint, out stopSpeed, out stopDistance, out stopType);

			if (lanePoint != null)
			{
				// check if the point downstream is the last checkpoint
				if (CoreCommon.Mission.MissionCheckpoints.Count == 1 && lanePoint.WaypointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId))
				{
					ArbiterWaypoint cpStop = lanePoint;
					double cpStopSpeed;
					double cpStopDistance;
					StopType cpStopType = StopType.LastGoal;
					this.StoppingParams(cpStop, lane, position, enCovariance, out cpStopSpeed, out cpStopDistance);

					if (cpStopDistance <= stopDistance)
					{
						stopSpeed = cpStopSpeed;
						stopDistance = cpStopDistance;
						stopType = cpStopType;
						stopWaypoint = cpStop;
					}
				}
				// check if point is not the checkpoint and is an exit
				else if (lanePoint.IsExit && !lanePoint.WaypointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId))
				{
					ArbiterWaypoint exitStop = lanePoint;
					double exitStopSpeed;
					double exitStopDistance;
					StopType exitStopType = lanePoint.IsStop ? StopType.StopLine : StopType.Exit;
					this.StoppingParams(exitStop, lane, position, enCovariance, out exitStopSpeed, out exitStopDistance);

					if (exitStopDistance <= stopDistance)
					{
						stopSpeed = exitStopSpeed;
						stopDistance = exitStopDistance;
						stopType = exitStopType;
						stopWaypoint = exitStop;
					}
				}
			}
		}

		/// <summary>
		/// Next point at which to stop
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="position"></param>
		/// <param name="stopPoint"></param>
		/// <param name="stopSpeed"></param>
		/// <param name="stopDistance"></param>
		public void NextLaneStop(IFQMPlanable lane, Coordinates position, double[] enCovariance, List<ArbiterWaypoint> ignorable,
			out ArbiterWaypoint stopPoint, out double stopSpeed, out double stopDistance, out StopType stopType)
		{
			// get the stop
			List<WaypointType> wts = new List<WaypointType>();
			wts.Add(WaypointType.Stop);
			wts.Add(WaypointType.End);
			stopPoint = lane.GetNext(position, wts, ignorable);

			// set stop type
			stopType = stopPoint.IsStop ? StopType.StopLine : StopType.EndOfLane;

			// parameterize
			this.StoppingParams(stopPoint, lane, position, enCovariance, out stopSpeed, out stopDistance);
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
		public void StoppingParams(ArbiterWaypoint waypoint, IFQMPlanable lane, Coordinates position, double[] enCovariance,
			out double stopSpeed, out double stopDistance)
		{
			// get dist to waypoint
			stopDistance = lane.DistanceBetween(position, waypoint.Position);			

			// subtract distance based upon type to help calculate speed
			double stopTypeDistance = waypoint.IsStop ? CoreCommon.OperationslStopLineSearchDistance : CoreCommon.OperationalStopDistance;
			double stopSpeedDistance = stopDistance - stopTypeDistance;

			// check if we are positive distance away
			if (stopSpeedDistance >= 0)
			{
				// segment max speed
				double segmentMaxSpeed = lane.CurrentMaximumSpeed(position);
				
				// get speed using constant acceleration
				stopSpeed = SpeedTools.GenerateSpeed(stopSpeedDistance, CoreCommon.OperationalStopSpeed, segmentMaxSpeed);
			}
			else
			{
				// inside stop dist
				stopSpeed = (stopDistance / stopTypeDistance) * CoreCommon.OperationalStopSpeed;
				stopSpeed = stopSpeed < 0 ? 0.0 : stopSpeed;
			}
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
		public void StoppingParams(Coordinates coordinate, IFQMPlanable lane, Coordinates position, double[] enCovariance,
			out double stopSpeed, out double stopDistance)
		{
			// get dist to waypoint
			stopDistance = lane.DistanceBetween(position, coordinate);

			// subtract distance based upon type to help calculate speed
			double stopSpeedDistance = stopDistance - CoreCommon.OperationalStopDistance;

			// check if we are positive distance away
			if (stopSpeedDistance >= 0)
			{
				// segment max speed
				double segmentMaxSpeed = lane.CurrentMaximumSpeed(position);

				// get speed using constant acceleration
				stopSpeed = SpeedTools.GenerateSpeed(stopSpeedDistance, CoreCommon.OperationalStopSpeed, segmentMaxSpeed);
			}
			else
			{
				// inside stop dist
				stopSpeed = (stopDistance / CoreCommon.OperationalStopDistance) * CoreCommon.OperationalStopSpeed;
				stopSpeed = stopSpeed < 0 ? 0.0 : stopSpeed;
			}
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Helps with parameterizations for lateral reasoning
		/// </summary>
		/// <param name="referenceLane"></param>
		/// <param name="fqmLane"></param>
		/// <param name="goal"></param>
		/// <param name="vehicleFront"></param>
		/// <returns></returns>
		public TravelingParameters ParameterizationHelper(ArbiterLane referenceLane, ArbiterLane fqmLane,
			Coordinates goal, Coordinates vehicleFront, IState nextState, VehicleState state, VehicleAgent va)
		{
			// get traveling parameterized list
			List<TravelingParameters> tps = new List<TravelingParameters>();

			// get distance to the goal
			double goalDistance;
			double goalSpeed;
			this.StoppingParams(goal, referenceLane, vehicleFront, new double[] { }, out goalSpeed, out goalDistance);
			tps.Add(this.GetParameters(referenceLane, goalSpeed, goalDistance, state));

			// get next stop
			ArbiterWaypoint stopPoint;
			double stopSpeed;
			double stopDistance;
			StopType stopType;
			this.NextLaneStop(fqmLane, vehicleFront, new double[] { }, new List<ArbiterWaypoint>(),
				out stopPoint, out stopSpeed, out stopDistance, out stopType);
			this.StoppingParams(stopPoint.Position, referenceLane, vehicleFront, new double[] { },
				out stopSpeed, out stopDistance);
			tps.Add(this.GetParameters(referenceLane, stopSpeed, stopDistance, state));

			// get vehicle
			if (va != null)
			{
				VehicleAgent tmp = this.ForwardVehicle.CurrentVehicle;
				double tmpDist = this.ForwardVehicle.currentDistance;
				this.ForwardVehicle.CurrentVehicle = va;
				this.ForwardVehicle.currentDistance = referenceLane.DistanceBetween(state.Front, va.ClosestPosition);
				TravelingParameters tp = this.ForwardVehicle.Follow(referenceLane, state, new List<ArbiterWaypoint>());
				tps.Add(tp);
				this.ForwardVehicle.CurrentVehicle = tmp;
				this.ForwardVehicle.currentDistance = tmpDist;
			}

			// parameterize
			tps.Sort();
			return tps[0];
		}

		/// <summary>
		/// Gets parameterization
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="speed"></param>
		/// <param name="distance"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public TravelingParameters GetParameters(ArbiterLane lane, double speed, double distance, VehicleState state)
		{
			double distanceCutOff = CoreCommon.OperationslStopLineSearchDistance;
			Maneuver m = new Maneuver();
			SpeedCommand sc;
			bool usingSpeed = true;

			#region Distance Cutoff

			// check if distance is less than cutoff
			if (distance < distanceCutOff)
			{
				// default behavior
				sc = new StopAtDistSpeedCommand(distance);
				Behavior b = new StayInLaneBehavior(lane.LaneId, sc, new List<int>(), lane.LanePath(), lane.Width, lane.NumberOfLanesLeft(state.Front, true), lane.NumberOfLanesRight(state.Front, true));

				// stopping so not using speed param
				usingSpeed = false;

				// standard behavior is fine for maneuver
				m = new Maneuver(b, new StayInLaneState(lane, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, state.Timestamp);
			}

			#endregion

			#region Outisde Distance Envelope

			// not inside distance envalope
			else
			{
				// get lane
				ArbiterLane al = lane;

				// default behavior
				sc = new ScalarSpeedCommand(speed);
				Behavior b = new StayInLaneBehavior(al.LaneId, sc, new List<int>(), al.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));

				// standard behavior is fine for maneuver
				m = new Maneuver(b, new StayInLaneState(al, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, state.Timestamp);
			}

			#endregion

			#region Parameterize

			// create new params
			TravelingParameters tp = new TravelingParameters();
			tp.Behavior = m.PrimaryBehavior;
			tp.Decorators = m.PrimaryBehavior.Decorators;
			tp.DistanceToGo = distance;
			tp.NextState = m.PrimaryState;
			tp.RecommendedSpeed = speed;
			tp.Type = TravellingType.Navigation;
			tp.UsingSpeed = usingSpeed;
			tp.SpeedCommand = sc;
			tp.VehiclesToIgnore = new List<int>();

			// return navigation params
			return tp;

			#endregion
		}

		#endregion

		#region Accessors

		/// <summary>
		/// Current primary plan parameterization
		/// </summary>
		public TravelingParameters CurrentParameters
		{
			get { return currentParameters; }
		}
		
		/// <summary>
		/// Current navigation parameters
		/// </summary>
		public TravelingParameters NavigationParameters
		{
			get { return navigationParameters; }
			set { navigationParameters = value; }
		}

		/// <summary>
		/// Current vehicle following parameters
		/// </summary>
		public TravelingParameters? FollowingParameters
		{
			get { return followingParameters; }
			set { followingParameters = value; }
		}

		/// <summary>
		/// Parameters for following lane, vehicle without goal
		/// </summary>
		public TravelingParameters LaneParameters
		{
			get { return laneParameters; }
			set { laneParameters = value; }
		}

		#endregion
	}
}
