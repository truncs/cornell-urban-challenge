using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// State of changing from one lane to another
	/// </summary>
	public class ChangeLanesState : TravelState, IState
	{	
		/// <summary>
		/// Parameters of the lane change
		/// </summary>
		public LaneChangeParameters Parameters;
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parameters"></param>
		public ChangeLanesState(LaneChangeParameters parameters)
		{
			this.Parameters = parameters;
		}

		public bool HitGoal(VehicleState state, Coordinates goal, IAreaSubtypeWaypointId id)
		{
			// check if forced
			if (this.Parameters.ForcedOpposing)
			{
				// get other way
				ArbiterWay other = !this.Parameters.TargetOncoming ? this.Parameters.Target.Way : this.Parameters.Initial.Way;

				// check goal in other way
				if (id is ArbiterWaypointId && ((ArbiterWaypointId)id).LaneId.WayId.Equals(other.WayId))
				{
					// center
					Coordinates vehicleCenter = state.Front - state.Heading.Normalize(TahoeParams.VL / 2.0);

					// check all lanes
					foreach (ArbiterLane al in other.Lanes.Values)
					{
						// get closest point to the center of this vehicle
						bool b = al.LanePath().GetClosestPoint(vehicleCenter).Location.DistanceTo(
							al.LanePath().GetClosestPoint(goal).Location) < TahoeParams.VL / 2.0;

						if (b)
							return true;
					}
				}
			}

			return false;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "ChangeLanesState";
		}

		public string StateInformation()
		{
			return Parameters.Initial.ToString() + " -> " + Parameters.Target.ToString() + ", dist: " + Parameters.DistanceToDepartUpperBound.ToString("F1");
		}

		public string LongDescription()
		{
			return "Changing Lanes: " + Parameters.Initial.ToString() + " -> " + Parameters.Target.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			double distLeft = Parameters.TargetOncoming ?
				Parameters.Target.DistanceBetween(Parameters.DepartUpperBound, currentState.Front) :
				Parameters.Target.DistanceBetween(currentState.Front, Parameters.DepartUpperBound);

			LaneChangeParameters chosen = this.Parameters;
			this.Parameters.DistanceToDepartUpperBound = distLeft;

			// distance to stop from max v given desired acceleration
			//double stopEnvelopeLength = -Math.Pow(this.Parameters.Target.Way.Segment.SpeedLimits.MaximumSpeed, 2) /
			//		(2.0 * -0.5);
			//double tmpSpeed = distLeft / this.Parameters.Parameters.DistanceToGo * this.Parameters.Parameters.RecommendedSpeed; 

			// create behavior
			ChangeLaneBehavior clb = new ChangeLaneBehavior(
				chosen.Initial.LaneId,
				chosen.Target.LaneId,
				chosen.ToLeft,
				distLeft,
				new ScalarSpeedCommand(chosen.Parameters.RecommendedSpeed),
				chosen.Parameters.VehiclesToIgnore,
				chosen.InitialOncoming ? chosen.Initial.ReversePath : chosen.Initial.LanePath(),
				chosen.TargetOncoming ? chosen.Target.ReversePath : chosen.Target.LanePath(),
				chosen.Initial.Width,
				chosen.Target.Width,
				chosen.InitialOncoming ? chosen.Initial.NumberOfLanesRight(currentState.Position, true) : chosen.Initial.NumberOfLanesLeft(currentState.Position, true),
				chosen.InitialOncoming ? chosen.Initial.NumberOfLanesLeft(currentState.Position, true) : chosen.Initial.NumberOfLanesRight(currentState.Position, true));

			// return behavior
			return clb;
		}

		public bool CanResume()
		{
			return true;
		}

		public List<UrbanChallenge.Behaviors.BehaviorDecorator> DefaultStateDecorators
		{
			get 
			{				
				return Parameters.ToLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator;
			}
		}

		public bool UseLaneAgent
		{
			get { return true; }
		}

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
		{
			get
			{
				return new InternalState(Parameters.Initial.LaneId, Parameters.Target.LaneId);
			}
			set
			{				
			}
		}

		public bool ResetLaneAgent
		{
			get
			{
				return false;
			}
			set
			{				
			}
		}

		#endregion
	}
}
