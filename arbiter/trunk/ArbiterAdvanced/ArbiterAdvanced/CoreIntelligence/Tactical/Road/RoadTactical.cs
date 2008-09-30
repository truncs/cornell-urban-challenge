using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Plans in a road situation
	/// </summary>
	public class RoadTactical
	{
		private TaskReasoning taskReasoning;
		public ForwardReasoning forwardReasoning;
		public ILateralReasoning leftLateralReasoning;
		public ILateralReasoning rightLateralReasoning;
		public OpposingReasoning opposingReasoning;
		public LaneChangeReasoning laneChangeReasoning;
		private RoadMonitor roadMonitor;

		/// <summary>
		/// Constructor
		/// </summary>
		public RoadTactical()
		{
			taskReasoning = new TaskReasoning();						
			this.roadMonitor = new RoadMonitor(this);
			this.laneChangeReasoning = new LaneChangeReasoning();
		}

		/// <summary>
		/// Reset values held over time in all planners
		/// </summary>
		public void ResetAll()
		{
			taskReasoning.Reset();

			if (this.opposingReasoning != null)
				opposingReasoning.Reset();

			if(forwardReasoning != null)
				forwardReasoning.Reset();

			if (leftLateralReasoning != null)
				leftLateralReasoning.Reset();

			if (rightLateralReasoning != null)
				rightLateralReasoning.Reset();

			if(laneChangeReasoning != null)
				laneChangeReasoning.Reset();

			if(roadMonitor != null)
				roadMonitor.Reset();
		}

		/// <summary>
		/// Plans what maneuer we should take next
		/// </summary>
		/// <param name="planningState"></param>
		/// <param name="navigationalPlan"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		/// <param name="blockage"></param>
		/// <returns></returns>
		public Maneuver Plan(IState planningState, RoadPlan navigationalPlan, VehicleState vehicleState,
			SceneEstimatorTrackedClusterCollection vehicles, SceneEstimatorUntrackedClusterCollection obstacles, 
			List<ITacticalBlockage> blockages, double vehicleSpeed)
		{
			// assign vehicles to their lanes
			this.roadMonitor.Assign(vehicles);

			// navigation tasks
			this.taskReasoning.navigationPlan = navigationalPlan;

			#region Stay in lane

			// maneuver given we are in a lane
			if (planningState is StayInLaneState)
			{
				// get state
				StayInLaneState sils = (StayInLaneState)planningState;

				// check reasoning if needs to be different
				if (this.forwardReasoning == null || !this.forwardReasoning.Lane.Equals(sils.Lane))
				{
					if (sils.Lane.LaneOnLeft == null)
						this.leftLateralReasoning = new LateralReasoning(null, SideObstacleSide.Driver);
					else if (sils.Lane.LaneOnLeft.Way.Equals(sils.Lane.Way))
						this.leftLateralReasoning = new LateralReasoning(sils.Lane.LaneOnLeft, SideObstacleSide.Driver);
					else
						this.leftLateralReasoning = new OpposingLateralReasoning(sils.Lane.LaneOnLeft, SideObstacleSide.Driver);

					if (sils.Lane.LaneOnRight == null)
						this.rightLateralReasoning = new LateralReasoning(null, SideObstacleSide.Passenger);
					else if (sils.Lane.LaneOnRight.Way.Equals(sils.Lane.Way))
						this.rightLateralReasoning = new LateralReasoning(sils.Lane.LaneOnRight, SideObstacleSide.Passenger);
					else
						this.rightLateralReasoning = new OpposingLateralReasoning(sils.Lane.LaneOnRight, SideObstacleSide.Passenger);
					
					this.forwardReasoning = new ForwardReasoning(this.leftLateralReasoning, this.rightLateralReasoning, sils.Lane);
				}

				// populate navigation with road plan
				taskReasoning.SetRoadPlan(navigationalPlan, sils.Lane);

				// as penalties for lane changes already taken into account, can just look at
				// best lane plan to figure out what to do
				TypeOfTasks bestTask = taskReasoning.Best;

				// get the forward lane plan
				Maneuver forwardManeuver = forwardReasoning.ForwardManeuver(sils.Lane, vehicleState, navigationalPlan, blockages, sils.WaypointsToIgnore);

				// get the secondary
				Maneuver? secondaryManeuver = forwardReasoning.AdvancedSecondary(sils.Lane, vehicleState, navigationalPlan, blockages, sils.WaypointsToIgnore, bestTask);  //forwardReasoning.SecondaryManeuver(sils.Lane, vehicleState, navigationalPlan, blockages, sils.WaypointsToIgnore, bestTask);

				// check behavior type for uturn
				if (secondaryManeuver.HasValue && secondaryManeuver.Value.PrimaryBehavior is UTurnBehavior)
					return secondaryManeuver.Value;

				// check if we wish to change lanes here
				if (bestTask != TypeOfTasks.Straight)
				{
					// parameters
					LaneChangeParameters parameters;
					secondaryManeuver = this.forwardReasoning.AdvancedDesiredLaneChangeManeuver(sils.Lane, bestTask == TypeOfTasks.Left ? true : false, navigationalPlan.BestPlan.laneWaypointOfInterest.PointOfInterest,
						navigationalPlan, vehicleState, blockages, sils.WaypointsToIgnore, new LaneChangeInformation(LaneChangeReason.Navigation, this.forwardReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle), secondaryManeuver, out parameters);
				}

				// final maneuver
				Maneuver finalManeuver = secondaryManeuver.HasValue ? secondaryManeuver.Value : forwardManeuver;

				// set opposing vehicle flag
				if (false && this.leftLateralReasoning != null && this.leftLateralReasoning is OpposingLateralReasoning && finalManeuver.PrimaryBehavior is StayInLaneBehavior)
				{
					StayInLaneBehavior silb = (StayInLaneBehavior)finalManeuver.PrimaryBehavior;
					OpposingLateralReasoning olr = (OpposingLateralReasoning)this.leftLateralReasoning;
					olr.ForwardMonitor.ForwardVehicle.Update(olr.lane, vehicleState);
					if (olr.ForwardMonitor.ForwardVehicle.CurrentVehicle != null)
					{
						ForwardVehicleTrackingControl fvtc = olr.ForwardMonitor.ForwardVehicle.GetControl(olr.lane, vehicleState);
						BehaviorDecorator[] bds = new BehaviorDecorator[finalManeuver.PrimaryBehavior.Decorators.Count];
						finalManeuver.PrimaryBehavior.Decorators.CopyTo(bds);
						finalManeuver.PrimaryBehavior.Decorators = new List<BehaviorDecorator>(bds);
						silb.Decorators.Add(new OpposingLaneDecorator(fvtc.xSeparation, olr.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed));
						ArbiterOutput.Output("Added Opposing Lane Decorator: " + fvtc.xSeparation.ToString("F2") + "m, " + olr.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed.ToString("f2") + "m/s");
					}
					finalManeuver.PrimaryBehavior = silb;					
				}

				// return the final
				return finalManeuver;
			}

			#endregion

			#region Stay in supra lane

			else if (CoreCommon.CorePlanningState is StayInSupraLaneState)
			{
				// get state
				StayInSupraLaneState sisls = (StayInSupraLaneState)planningState;

				// check reasoning
				if (this.forwardReasoning == null || !this.forwardReasoning.Lane.Equals(sisls.Lane))
				{
					if (sisls.Lane.Initial.LaneOnLeft == null)
						this.leftLateralReasoning = new LateralReasoning(null, SideObstacleSide.Driver);
					else if (sisls.Lane.Initial.LaneOnLeft.Way.Equals(sisls.Lane.Initial.Way))
						this.leftLateralReasoning = new LateralReasoning(sisls.Lane.Initial.LaneOnLeft, SideObstacleSide.Driver);
					else
						this.leftLateralReasoning = new OpposingLateralReasoning(sisls.Lane.Initial.LaneOnLeft, SideObstacleSide.Driver);

					if (sisls.Lane.Initial.LaneOnRight == null)
						this.rightLateralReasoning = new LateralReasoning(null, SideObstacleSide.Passenger);
					else if (sisls.Lane.Initial.LaneOnRight.Way.Equals(sisls.Lane.Initial.Way))
						this.rightLateralReasoning = new LateralReasoning(sisls.Lane.Initial.LaneOnRight, SideObstacleSide.Passenger);
					else
						this.rightLateralReasoning = new OpposingLateralReasoning(sisls.Lane.Initial.LaneOnRight, SideObstacleSide.Passenger);

					this.forwardReasoning = new ForwardReasoning(this.leftLateralReasoning, this.rightLateralReasoning, sisls.Lane);
				}

				// populate navigation with road plan
				taskReasoning.SetSupraRoadPlan(navigationalPlan, sisls.Lane);

				// as penalties for lane changes already taken into account, can just look at
				// best lane plan to figure out what to do
				// TODO: NOTE THAT THIS BEST TASK SHOULD BE IN THE SUPRA LANE!! (DO WE NEED THIS)
				TypeOfTasks bestTask = taskReasoning.Best;

				// get the forward lane plan
				Maneuver forwardManeuver = forwardReasoning.ForwardManeuver(sisls.Lane, vehicleState, navigationalPlan, blockages, sisls.WaypointsToIgnore);

				// get hte secondary
				Maneuver? secondaryManeuver = forwardReasoning.AdvancedSecondary(sisls.Lane, vehicleState, navigationalPlan, blockages, new List<ArbiterWaypoint>(), bestTask);//forwardReasoning.SecondaryManeuver(sisls.Lane, vehicleState, navigationalPlan, blockages, sisls.WaypointsToIgnore, bestTask);

				// final maneuver
				Maneuver finalManeuver = secondaryManeuver.HasValue ? secondaryManeuver.Value : forwardManeuver;

				// check if stay in lane
				if (false && this.leftLateralReasoning != null && this.leftLateralReasoning is OpposingLateralReasoning && finalManeuver.PrimaryBehavior is SupraLaneBehavior)
				{					
					SupraLaneBehavior silb = (SupraLaneBehavior)finalManeuver.PrimaryBehavior;
					OpposingLateralReasoning olr = (OpposingLateralReasoning)this.leftLateralReasoning;
					olr.ForwardMonitor.ForwardVehicle.Update(olr.lane, vehicleState);
					if (olr.ForwardMonitor.ForwardVehicle.CurrentVehicle != null)
					{
						ForwardVehicleTrackingControl fvtc = olr.ForwardMonitor.ForwardVehicle.GetControl(olr.lane, vehicleState);
						BehaviorDecorator[] bds = new BehaviorDecorator[finalManeuver.PrimaryBehavior.Decorators.Count];
						finalManeuver.PrimaryBehavior.Decorators.CopyTo(bds);
						finalManeuver.PrimaryBehavior.Decorators = new List<BehaviorDecorator>(bds);
						silb.Decorators.Add(new OpposingLaneDecorator(fvtc.xSeparation, olr.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed));
						ArbiterOutput.Output("Added Opposing Lane Decorator: " + fvtc.xSeparation.ToString("F2") + "m, " + olr.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed.ToString("f2") + "m/s");
					}
					finalManeuver.PrimaryBehavior = silb;
				}

				// return the final
				return finalManeuver;

				// notify
				/*if (secondaryManeuver.HasValue)
					ArbiterOutput.Output("Secondary Maneuver");

				// check for forward's secondary maneuver for desired behavior other than going straight
				if (secondaryManeuver.HasValue)
				{
					// return the secondary maneuver
					return secondaryManeuver.Value;
				}
				// otherwise our default behavior and posibly desired is going straight
				else
				{
					// return default forward maneuver
					return forwardManeuver;
				}*/
			}

			#endregion

			#region Change Lanes State

			// maneuver given we are changing lanes
			else if (planningState is ChangeLanesState)
			{
				// get state
				ChangeLanesState cls = (ChangeLanesState)planningState;
				LaneChangeReasoning lcr = new LaneChangeReasoning();
				Maneuver final = lcr.PlanLaneChange(cls, vehicleState, navigationalPlan, blockages, new List<ArbiterWaypoint>());
				
				#warning need to filter through waypoints to ignore so don't get stuck by a stop line
				//Maneuver final = new Maneuver(cls.Resume(vehicleState, vehicleSpeed), cls, cls.DefaultStateDecorators, vehicleState.Timestamp);

				// return the final planned maneuver
				return final;

				/*if (!cls.parameters..TargetIsOnComing)
				{
					// check reasoning
					if (this.forwardReasoning == null || !this.forwardReasoning.Lane.Equals(cls.TargetLane))
					{
						if (cls.TargetLane.LaneOnLeft.Way.Equals(cls.TargetLane.Way))
							this.leftLateralReasoning = new LateralReasoning(cls.TargetLane.LaneOnLeft);
						else
							this.leftLateralReasoning = new OpposingLateralReasoning(cls.TargetLane.LaneOnLeft);

						if (cls.TargetLane.LaneOnRight.Way.Equals(cls.TargetLane.Way))
							this.rightLateralReasoning = new LateralReasoning(cls.TargetLane.LaneOnRight);
						else
							this.rightLateralReasoning = new OpposingLateralReasoning(cls.TargetLane.LaneOnRight);

						this.forwardReasoning = new ForwardReasoning(this.leftLateralReasoning, this.rightLateralReasoning, cls.TargetLane);
					}

					
					// get speed command
					double speed;
					double distance;
					this.forwardReasoning.ForwardMonitor.StoppingParams(new ArbiterWaypoint(cls.TargetUpperBound.pt, null), cls.TargetLane, vehicleState.Front, vehicleState.ENCovariance, out speed, out distance);
					SpeedCommand sc = new ScalarSpeedCommand(Math.Max(speed, 0.0));
					cls.distanceLeft = distance;

					// get behavior
					ChangeLaneBehavior clb = new ChangeLaneBehavior(cls.InitialLane.LaneId, cls.TargetLane.LaneId, cls.InitialLane.LaneOnLeft != null && cls.InitialLane.LaneOnLeft.Equals(cls.TargetLane),
						distance, sc, new List<int>(), cls.InitialLane.PartitionPath, cls.TargetLane.PartitionPath, cls.InitialLane.Width, cls.TargetLane.Width);
					
					// plan over the target lane
					//Maneuver targetManeuver = forwardReasoning.ForwardManeuver(cls.TargetLane, vehicleState, !cls.TargetIsOnComing, blockage, cls.InitialLaneState.IgnorableWaypoints);

					// plan over the initial lane
					//Maneuver initialManeuver = forwardReasoning.ForwardManeuver(cls.InitialLane, vehicleState, !cls.InitialIsOncoming, blockage, cls.InitialLaneState.IgnorableWaypoints);

					// generate the change lanes command
					//Maneuver final = laneChangeReasoning.PlanLaneChange(cls, initialManeuver, targetManeuver);
					
				}
				else
				{
					throw new Exception("Change lanes into oncoming not supported yet by road tactical");
				}*/
			}

			#endregion

			#region Opposing Lanes State

			// maneuver given we are in an opposing lane
			else if (planningState is OpposingLanesState)
			{
				// get state
				OpposingLanesState ols = (OpposingLanesState)planningState;
				ArbiterWayId opposingWay = ols.OpposingWay;

				ols.SetClosestGood(vehicleState);
				ols.ResetLaneAgent = false;

				// check reasoning
				if (this.opposingReasoning == null || !this.opposingReasoning.Lane.Equals(ols.OpposingLane))
				{
					if (ols.OpposingLane.LaneOnRight == null)
						this.leftLateralReasoning = new LateralReasoning(null, SideObstacleSide.Driver);
					else if (!ols.OpposingLane.LaneOnRight.Way.Equals(ols.OpposingLane.Way))
						this.leftLateralReasoning = new LateralReasoning(ols.OpposingLane.LaneOnRight, SideObstacleSide.Driver);
					else
						this.leftLateralReasoning = new OpposingLateralReasoning(ols.OpposingLane.LaneOnRight, SideObstacleSide.Driver);

					if (ols.OpposingLane.LaneOnLeft == null)
						this.rightLateralReasoning = new LateralReasoning(null, SideObstacleSide.Passenger);
					else if (!ols.OpposingLane.LaneOnLeft.Way.Equals(ols.OpposingLane.Way))
						this.rightLateralReasoning = new LateralReasoning(ols.OpposingLane.LaneOnLeft, SideObstacleSide.Passenger);
					else
						this.rightLateralReasoning = new OpposingLateralReasoning(ols.OpposingLane.LaneOnLeft, SideObstacleSide.Passenger);

					this.opposingReasoning = new OpposingReasoning(this.leftLateralReasoning, this.rightLateralReasoning, ols.OpposingLane);
				}

				// get the forward lane plan
				Maneuver forwardManeuver = this.opposingReasoning.ForwardManeuver(ols.OpposingLane, ols.ClosestGoodLane, vehicleState, navigationalPlan, blockages);

				// get the secondary maneuver
				Maneuver? secondaryManeuver = null;
				if(ols.ClosestGoodLane != null)
					secondaryManeuver = this.opposingReasoning.SecondaryManeuver(ols.OpposingLane, ols.ClosestGoodLane, vehicleState, blockages, ols.EntryParameters);

				// check for reasonings secondary maneuver for desired behavior other than going straight
				if (secondaryManeuver != null)
				{
					// return the secondary maneuver
					return secondaryManeuver.Value;
				}
				// otherwise our default behavior and posibly desired is going straight
				else
				{
					// return default forward maneuver
					return forwardManeuver;
				}
			}

			#endregion

			#region not imp
			/*
			#region Uturn

			// we are making a uturn
			else if (planningState is uTurnState)
			{
				// get the uturn state
				uTurnState uts = (uTurnState)planningState;

				// get the final lane we wish to be in
				ArbiterLane targetLane = uts.TargetLane;

				// get operational state
				Type operationalBehaviorType = CoreCommon.Communications.GetCurrentOperationalBehavior();

				// check if we have completed the uturn
				bool complete = operationalBehaviorType == typeof(StayInLaneBehavior);

				// default next behavior
				Behavior nextBehavior = new StayInLaneBehavior(targetLane.LaneId, new ScalarSpeedCommand(CoreCommon.OperationalStopSpeed), new List<int>());
				nextBehavior.Decorators = TurnDecorators.NoDecorators;

				// check if complete
				if (complete)
				{
					// stay in lane
					List<ArbiterLaneId> aprioriLanes = new List<ArbiterLaneId>();
					aprioriLanes.Add(targetLane.LaneId);
					return new Maneuver(nextBehavior, new StayInLaneState(targetLane), null, null, aprioriLanes, true);
				}
				// otherwise keep same
				else
				{
					// set abort behavior
					((StayInLaneBehavior)nextBehavior).SpeedCommand = new ScalarSpeedCommand(0.0);

					// maneuver
					return new Maneuver(uts.DefaultBehavior, uts, nextBehavior, new StayInLaneState(targetLane));
				}
			}

			#endregion*/

			#endregion

			#region Unknown

			// unknown state
			else
			{
				throw new Exception("Unknown Travel State type: planningState: " + planningState.ToString() + "\n with type: " + planningState.GetType().ToString());
			}

			#endregion
		}
	}
}
