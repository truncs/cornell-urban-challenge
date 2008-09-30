using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// State of blockage
	/// </summary>
	public class EncounteredBlockageState : BlockageState, IState
	{
		/// <summary>
		/// Blockage
		/// </summary>
		public ITacticalBlockage TacticalBlockage;

		/// <summary>
		/// Saudi level of the blockage
		/// </summary>
		public SAUDILevel Saudi;

		/// <summary>
		/// Defcon of the blockage
		/// </summary>
		public BlockageRecoveryDEFCON Defcon;

		/// <summary>
		/// Current state of execution while encountering blockage
		/// </summary>
		public IState PlanningState;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tacticalBlockage"></param>
		/// <param name="currentState"></param>
		public EncounteredBlockageState(ITacticalBlockage tacticalBlockage, IState planningState)
		{
			this.TacticalBlockage = tacticalBlockage;
			this.PlanningState = planningState;
			this.Saudi = SAUDILevel.None;
			this.Defcon = BlockageRecoveryDEFCON.INITIAL;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tacticalBlockage"></param>
		/// <param name="currentState"></param>
		public EncounteredBlockageState(ITacticalBlockage tacticalBlockage, IState planningState, BlockageRecoveryDEFCON defcon, SAUDILevel saudi)
		{
			this.TacticalBlockage = tacticalBlockage;
			this.PlanningState = planningState;
			this.Saudi = saudi;
			this.Defcon = defcon;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "EncounteredBlockage";
		}

		public string LongDescription()
		{
			return "EncounteredBlockage: " + PlanningState.ToString() + "; " + TacticalBlockage.ToString();
		}

		public string StateInformation()
		{
			return PlanningState.ToString();
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
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion

		public override string ToString()
		{
			return this.LongDescription();
		}
	}
}
