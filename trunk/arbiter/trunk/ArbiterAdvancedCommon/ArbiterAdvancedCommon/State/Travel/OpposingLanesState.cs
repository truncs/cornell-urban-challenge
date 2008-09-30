using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class OpposingLanesState : TravelState, IState
	{
		#region Opposing Lanes State Members

		/// <summary>
		/// Way of the opposing lanes state
		/// </summary>
		public ArbiterWayId OpposingWay;

		/// <summary>
		/// Lane we are in
		/// </summary>
		public ArbiterLane OpposingLane;

		/// <summary>
		/// Closest lane to the current one
		/// </summary>
		public ArbiterLane ClosestGoodLane;

		/// <summary>
		/// Whether or not to reset the lane agent
		/// </summary>
		private bool resetLaneAgent = false;

		/// <summary>
		/// Parameters we entered the opposing lane with
		/// </summary>
		public LaneChangeParameters? EntryParameters;

		#endregion

		public OpposingLanesState(ArbiterLane opposingLane, bool resetLaneAgent, IState previous, VehicleState vs)
		{
			this.resetLaneAgent = resetLaneAgent;
			this.OpposingLane = opposingLane;
			this.OpposingWay = opposingLane.Way.WayId;
			this.ClosestGoodLane = null;

			this.SetClosestGood(vs);

			if (previous is ChangeLanesState)
			{
				EntryParameters = ((ChangeLanesState)previous).Parameters;
			}
			else if (previous is OpposingLanesState)
			{
				EntryParameters = ((OpposingLanesState)previous).EntryParameters;
			}
			else
			{
				EntryParameters = null;
			}
		}

		public void SetClosestGood(VehicleState vs)
		{
			ArbiterLane current = this.OpposingLane.LaneOnLeft;
			while (current != null)
			{
				if (!current.Way.WayId.Equals(OpposingWay) && current.RelativelyInside(vs.Front))
				{
					this.ClosestGoodLane = current;
					break;
				}

				if (!current.Way.WayId.Equals(this.OpposingLane.Way.WayId))
					current = current.LaneOnRight;
				else
					current = current.LaneOnLeft;
			}
		}

		public bool HitGoal(VehicleState state, Coordinates goal, IAreaSubtypeWaypointId id)
		{
			// check if forced
			if (this.EntryParameters.HasValue && this.EntryParameters.Value.ForcedOpposing)
			{
				// get other way
				ArbiterWay other = this.OpposingLane.Way.WayId.Number == 1 ? this.OpposingLane.Way.Segment.Way2 : this.OpposingLane.Way.Segment.Way1;

				// check goal in other way
				if(id is ArbiterWaypointId && ((ArbiterWaypointId)id).LaneId.WayId.Equals(other.WayId))
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
			return "OpposingLaneState: " + OpposingLane.LaneId.ToString();
		}

		public string LongDescription()
		{
			return "OpposingLaneState: " + OpposingLane.LaneId.ToString();
		}

		public string StateInformation()
		{
			return "OpposingLaneState: " + OpposingLane.LaneId.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			return new StayInLaneBehavior(OpposingLane.LaneId, new ScalarSpeedCommand(0.0), new List<int>());
		}

		public bool CanResume()
		{
			return true;
		}

		public List<UrbanChallenge.Behaviors.BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.NoDecorators; }
		}

		public bool UseLaneAgent
		{
			get { return true; }
		}

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
		{
			get
			{
				return new InternalState(OpposingLane.LaneId, OpposingLane.LaneId);
			}
			set
			{
				
			}
		}

		public bool ResetLaneAgent
		{
			get
			{
				return this.resetLaneAgent;
			}
			set
			{
				this.resetLaneAgent = value;
			}
		}

		#endregion
	}
}
