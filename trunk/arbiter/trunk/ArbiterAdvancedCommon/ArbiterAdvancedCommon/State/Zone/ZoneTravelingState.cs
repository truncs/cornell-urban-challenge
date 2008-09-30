using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Traveling in a zone from a specific start point
	/// </summary>
	public class ZoneTravelingState : ZoneState, IState
	{
		#region Zone Traveling State Members

		/// <summary>
		/// Starting waypoint of state
		/// </summary>
		public INavigableNode Start;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		public ZoneTravelingState(ArbiterZone zone, INavigableNode start) : base(zone)
		{
			this.Start = start;
		}

		#endregion

		#region IState Members

		public string ShortDescription()
		{
			return "ZoneTravelingState";
		}

		public string LongDescription()
		{
			return "ZoneTraveling State: " + this.Zone.ToString();
		}

		public string StateInformation()
		{
			return "Zone: " + this.Zone.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			return new HoldBrakeBehavior();
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
			get { return false; }
		}

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public bool ResetLaneAgent
		{
			get
			{
				return true;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}
