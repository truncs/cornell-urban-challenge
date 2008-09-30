using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.State
{
	public static class StateReasoning
	{
		public static IState FilterStates(CarMode carMode, BehavioralDirector behavioral)
		{
			// check run mode
			if(carMode == CarMode.Run)
			{
				// check if goals left
				if (CoreCommon.Mission.MissionCheckpoints.Count > 0)
				{
					// good to go
					return CoreCommon.CorePlanningState;
				}
				// otherwise need to stop
				else
				{
					// no goals left
					return new NoGoalsLeftState();
				}
			}
			else if(carMode == CarMode.EStop)
			{
				// move to the estopped state
				return new eStoppedState();
			}
			else if(carMode == CarMode.Human)
			{
				// clear out standard (non-intersection) queuing values
				TacticalDirector.RefreshQueuingMonitors();

				// clear other timings
				behavioral.ResetTiming();

				// clear intersection timings
				if (IntersectionTactical.IntersectionMonitor != null)
					IntersectionTactical.IntersectionMonitor.ResetTimers();

				// human mode
				return new HumanState();
			}
			else if(carMode == CarMode.Pause || carMode == CarMode.TransitioningFromPause || carMode == CarMode.TransitioningToPause)
			{
				// check if the state is a paused state
				if (CoreCommon.CorePlanningState is PausedState)
				{
					// stay paused
					return CoreCommon.CorePlanningState;
				}
				// otherwise we need to go into pause
				else
				{
					// clear other timings
					behavioral.ResetTiming();

					// clear out standard (non-intersection) queuing values
					TacticalDirector.RefreshQueuingMonitors();
					behavioral.tactical.ResetTimers();

					if (IntersectionTactical.IntersectionMonitor != null)
						IntersectionTactical.IntersectionMonitor.ResetTimers();

					// make a new paused state with the previous state intact
					return new PausedState(CoreCommon.CorePlanningState);
				}
			}
			else
			{
				// notify of unknown state
				throw new Exception("Unknown car mode type");
			}			
		}

		private static bool waitRemoveLastGoal = false;

		public static INavigableNode FilterGoal(VehicleState state)
		{
			// get goal
			INavigableNode goal = CoreCommon.Mission.MissionCheckpoints.Count > 0 ?
				CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId] : null;

			if (waitRemoveLastGoal && CoreCommon.Mission.MissionCheckpoints.Count != 1)
				waitRemoveLastGoal = false;

			// id
			IArbiterWaypoint goalWp = null;
			if(goal != null)
				goalWp = CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId];

			// check lane change or opposing
			if (goal != null && 
				(CoreCommon.CorePlanningState is OpposingLanesState && ((OpposingLanesState)CoreCommon.CorePlanningState).HitGoal(state, goal.Position, goalWp.AreaSubtypeWaypointId)) ||
				(CoreCommon.CorePlanningState is ChangeLanesState && ((ChangeLanesState)CoreCommon.CorePlanningState).HitGoal(state, goal.Position, goalWp.AreaSubtypeWaypointId)))
			{
				if (CoreCommon.Mission.MissionCheckpoints.Count == 1)
				{
					waitRemoveLastGoal = true;
					ArbiterOutput.Output("Waiting to remove last Checkpoint: " + goal.ToString());
				}
				else
				{
					// set hit
					ArbiterOutput.Output("Reached Checkpoint: " + goal.ToString());
					CoreCommon.Mission.MissionCheckpoints.Dequeue();

					// update goal
					goal = CoreCommon.Mission.MissionCheckpoints.Count > 0 ?
						CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId] : null;
				}
			}
			else if (goal != null && CoreCommon.Mission.MissionCheckpoints.Count == 1 && waitRemoveLastGoal && 
				(CoreCommon.CorePlanningState is StayInLaneState || CoreCommon.CorePlanningState is StayInSupraLaneState))
			{
				// set hit
				ArbiterOutput.Output("Wait over, Reached Checkpoint: " + goal.ToString());
				CoreCommon.Mission.MissionCheckpoints.Dequeue();

				// update goal
				goal = CoreCommon.Mission.MissionCheckpoints.Count > 0 ?
					CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId] : null;	
			}
			// TODO implement full version of hit test
			// check if we have hit the goal (either by being in opposing lane or going to opposing and next to it or in lane and pass over it
			else if (goal != null)
			{
				bool reachedCp = false;

				if (CoreCommon.CorePlanningState is StayInLaneState)
				{
					StayInLaneState sils = (StayInLaneState)CoreCommon.CorePlanningState;
					if (goal is ArbiterWaypoint && ((ArbiterWaypoint)goal).Lane.Equals(sils.Lane))
					{
						if (CoreCommon.Mission.MissionCheckpoints.Count != 1)
						{
							double distanceAlong = sils.Lane.DistanceBetween(state.Front, goal.Position);
							if (Math.Abs(distanceAlong) < 1.5 + (1.5 * CoreCommon.Communications.GetVehicleSpeed().Value) / 5.0)
							{
								reachedCp = true;
							}
						}
						else
						{
							double distanceAlong = sils.Lane.DistanceBetween(state.Front, goal.Position);
							double distanceAlong2 = sils.Lane.DistanceBetween(state.Position, goal.Position);
							if (CoreCommon.Communications.GetVehicleSpeed().Value < 0.005 && Math.Abs(distanceAlong) < 0.3 ||
								CoreCommon.Communications.GetVehicleState().VehiclePolygon.IsInside(goal.Position) ||
								(distanceAlong <= 0.0 && distanceAlong2 >= 0))
							{
								reachedCp = true;
							}
						}
					}
				}
				else if (CoreCommon.CorePlanningState is ChangeLanesState)
				{
					ChangeLanesState cls = (ChangeLanesState)CoreCommon.CorePlanningState;
					if (cls.Parameters.Initial.Way.Equals(cls.Parameters.Target.Way) &&
						goal is ArbiterWaypoint && ((ArbiterWaypoint)goal).Lane.Equals(cls.Parameters.Target))
					{
						double distanceAlong = cls.Parameters.Target.DistanceBetween(state.Front, goal.Position);
						if (Math.Abs(distanceAlong) < 1.5 + (1.5 * CoreCommon.Communications.GetVehicleSpeed().Value) / 5.0)
						{
							reachedCp = true;
							ArbiterOutput.Output("Removed goal changing lanes");
						}
					}
				}

				if (reachedCp)
				{
					// set hit
					ArbiterOutput.Output("Reached Checkpoint: " + goal.ToString());
					CoreCommon.Mission.MissionCheckpoints.Dequeue();

					// update goal
					goal = CoreCommon.Mission.MissionCheckpoints.Count > 0 ?
						CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId] : null;
				}
			}

			// set goal info
			CoreCommon.CurrentInformation.RouteCheckpoint = CoreCommon.Mission.MissionCheckpoints.Count > 0 ? goal.ToString() : "NONE";
			CoreCommon.CurrentInformation.GoalsRemaining = CoreCommon.Mission.MissionCheckpoints.Count.ToString();
			CoreCommon.CurrentInformation.RouteCheckpointId = CoreCommon.Mission.MissionCheckpoints.Count > 0 ? CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString() : "NONE";

			// return current
			return goal;
		}
	}
}
