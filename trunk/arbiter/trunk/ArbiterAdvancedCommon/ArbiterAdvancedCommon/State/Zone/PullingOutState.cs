using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Describes pulling out of a parkign spot
	/// </summary>
	public class PullingOutState : ZoneState, IState
	{
		#region Parking Spot Members

		/// <summary>
		/// Parking spot we are pulling out of
		/// </summary>
		public ArbiterParkingSpot ParkingSpot;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		/// <param name="parkingSpot"></param>
		public PullingOutState(ArbiterZone zone, ArbiterParkingSpot parkingSpot)
			: base(zone)
		{
			this.ParkingSpot = parkingSpot;
		}

		#endregion

		#region IState Members

		public string ShortDescription()
		{
			return "PullingOutState";
		}

		public string LongDescription()
		{
			return "PullingOut: " + this.ParkingSpot.SpotId.ToString();
		}

		public string StateInformation()
		{
			return this.ParkingSpot.SpotId.ToString();
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
				throw new Exception("Internal state of zone not implemented");
			}
			set
			{
				
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

			}
		}

		#endregion
	}
}
