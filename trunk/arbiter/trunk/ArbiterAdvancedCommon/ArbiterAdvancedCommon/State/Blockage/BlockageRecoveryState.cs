using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Severity of recovery maneuver
	/// </summary>
	public enum BlockageRecoveryDEFCON
	{
		UTURN = 0,
		TURN = 1,
		CHANGELANES_ONCOMING = 2,
		CHANGELANES_FORWARD = 3,
		WIDENBOUNDS = 4,
		REVERSE = 5,
		INITIAL = 6
	}

	/// <summary>
	/// Status of the recovery maneuver
	/// </summary>
	public enum BlockageRecoverySTATUS
	{
		ENCOUNTERED = 0,
		EXECUTING = 1,
		COMPLETED = 2,
		BLOCKED = 3
	}

	/// <summary>
	/// Recovering from blockage
	/// </summary>
	public class BlockageRecoveryState : BlockageState, IState
	{
		/// <summary>
		/// Type of recovery behavior we are executing
		/// </summary>
		public Behavior RecoveryBehavior;

		/// <summary>
		/// State to go to if complete behavior successfully
		/// </summary>
		public IState CompletionState;

		/// <summary>
		/// State to go to if need to abort
		/// </summary>
		public IState AbortState;

		/// <summary>
		/// Severity of recovery behavior
		/// </summary>
		public BlockageRecoveryDEFCON Defcon;
		
		/// <summary>
		/// State of completion of the recovery behavior
		/// </summary>
		public BlockageRecoverySTATUS RecoveryStatus;

		/// <summary>
		/// Recovery blockage
		/// </summary>
		public ITacticalBlockage Blockage;

		/// <summary>
		/// State which we encountered the blockage
		/// </summary>
		public EncounteredBlockageState EncounteredState;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="recoveryBehaviorType"></param>
		/// <param name="completionState"></param>
		/// <param name="abortState"></param>
		/// <param name="defcon"></param>
		public BlockageRecoveryState(Behavior recoveryBehavior, IState completionState,
			IState abortState, BlockageRecoveryDEFCON defcon, EncounteredBlockageState ebs, BlockageRecoverySTATUS status)
		{
			this.RecoveryBehavior = recoveryBehavior;
			this.CompletionState = completionState;
			this.AbortState = abortState;
			this.Defcon = defcon;
			this.RecoveryStatus = status;
			this.EncounteredState = ebs;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "BlockageRecovery";
		}

		public string LongDescription()
		{
			return "BlockageRecoveryState: Defcon: " + this.Defcon.ToString() + " Status: " + this.RecoveryStatus.ToString();
		}

		public string StateInformation()
		{
			return "D: " + this.Defcon.ToString() + " S: " + this.RecoveryStatus.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			return this.RecoveryBehavior != null ? this.RecoveryBehavior : new NullBehavior();
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

		/// <summary>
		/// State string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.LongDescription();
		}
	}
}
