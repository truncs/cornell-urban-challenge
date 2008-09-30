using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Starting up outside of a chute
	/// </summary>
	public class StartupOffChuteState : TravelState, IState
	{
		/// <summary>
		/// Lane Partition to go to
		/// </summary>
		public ArbiterLanePartition Final;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public StartupOffChuteState(ArbiterLanePartition final)
		{
			this.Final = final;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "StartupOffChuteState";
		}

		public string StateInformation()
		{
			return this.Final.ToString();
		}

		public string LongDescription()
		{
			return "Starting up off chute: " + this.Final.ToString();
		}

		/// <summary>
		/// Resume from pause
		/// </summary>
		/// <returns></returns>
		public Behavior Resume(VehicleState currentState, double speed)
		{
			// turn behavior into chute
			TurnBehavior b = new TurnBehavior(this.Final.Lane.LaneId, this.Final.PartitionPath,
				this.Final.PartitionPath.ShiftLateral(this.Final.Lane.Width / 2.0),
				this.Final.PartitionPath.ShiftLateral(-this.Final.Lane.Width / 2.0),
				new ScalarSpeedCommand(1.4), null);

			// return behavior
			return b;
		}

		public bool CanResume()
		{
			return true;
		}

		public List<BehaviorDecorator> DefaultStateDecorators
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
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}

