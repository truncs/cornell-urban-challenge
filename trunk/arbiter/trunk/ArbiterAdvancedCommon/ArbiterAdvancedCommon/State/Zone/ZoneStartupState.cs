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
	public class ZoneStartupState : ZoneState, IState
	{
		#region Zone Startup State Members

		public bool unreachablePathExists;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		public ZoneStartupState(ArbiterZone zone)
			: base(zone)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		public ZoneStartupState(ArbiterZone zone, bool unreachable)
			: base(zone)
		{
			this.unreachablePathExists = unreachable;
		}

		#endregion

		#region IState Members

		public string ShortDescription()
		{
			return "ZoneStartupState";
		}

		public string LongDescription()
		{
			return "ZoneStartup State:" + this.Zone.ToString();
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