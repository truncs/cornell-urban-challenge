using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// State of parking in a spot
	/// </summary>
	public class ParkingState : ZoneState, IState
	{
		#region Parking State Members

		/// <summary>
		/// Spot we are pulling into
		/// </summary>
		public ArbiterParkingSpot ParkingSpot;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		/// <param name="parkingSpot"></param>
		public ParkingState(ArbiterZone zone, ArbiterParkingSpot parkingSpot)
			: base(zone)
		{
			this.ParkingSpot = parkingSpot;
		}

		#endregion

		#region IState Members

		public string ShortDescription()
		{
			return "ParkingState";
		}

		public string LongDescription()
		{
			return "Parking: " + ParkingSpot.ToString();
		}

		public string StateInformation()
		{
			return ParkingSpot.ToString();
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
