using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.Common.Common;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common
{
	/// <summary>
	/// Simple cost of lane change types
	/// </summary>
	public enum LaneChangeCost
	{
		LeftToOpposing = 0,
		RightToOpposing = 1,
		ToForwardLane = 2
	}

	[Serializable]
	public struct LaneChangeParameters : IComparable
	{
		/// <summary>
		/// If this lane change is available currently
		/// </summary>
		public bool Available;

		/// <summary>
		/// Recommended means if good idea or not
		/// </summary>
		public bool Feasible;

		/// <summary>
		/// Initial lane
		/// </summary>
		public ArbiterLane Initial;

		/// <summary>
		/// Flag if the initial lane is oncoming
		/// </summary>
		public bool InitialOncoming;

		/// <summary>
		/// Target lane
		/// </summary>
		public ArbiterLane Target;

		/// <summary>
		/// Flag if the target is oncoming
		/// </summary>
		public bool TargetOncoming;

		/// <summary>
		/// Flag if lane change is to left
		/// </summary>
		public bool ToLeft;

		/// <summary>
		/// Behavior to perform
		/// </summary>
		public Behavior Behavior;

		/// <summary>
		/// Next state of the core
		/// </summary>
		public IState NextState;

		/// <summary>
		/// Turn signals to use
		/// </summary>
		public List<BehaviorDecorator> Decorators;

		/// <summary>
		/// Parameters for what to do when changing
		/// </summary>
		public TravelingParameters Parameters;

		/// <summary>
		/// Distance to the departure upper bound
		/// </summary>
		public double DistanceToDepartUpperBound;

		/// <summary>
		/// Upper bound for departure
		/// </summary>
		public Coordinates DepartUpperBound;

		/// <summary>
		/// Minimum place to return
		/// </summary>	
		public Coordinates DefaultReturnLowerBound;
		
		/// <summary>
		/// Minimum place we can return
		/// </summary>
		public Coordinates MinimumReturnComplete;

		/// <summary>
		/// Upper bound for our return
		/// </summary>
		public Coordinates DefaultReturnUpperBound;

		/// <summary>
		/// Reason for a lane change
		/// </summary>
		public LaneChangeReason Reason;

		/// <summary>
		/// If the vehicle was forced into an opposing state
		/// </summary>
		public bool ForcedOpposing;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="available"></param>
		/// <param name="feasible"></param>
		/// <param name="initial"></param>
		/// <param name="initialOncoming"></param>
		/// <param name="target"></param>
		/// <param name="targetOncoming"></param>
		/// <param name="toLeft"></param>
		/// <param name="behavior"></param>
		/// <param name="nextState"></param>
		/// <param name="decorators"></param>
		/// <param name="parameters"></param>
		/// <param name="departUppedBound"></param>
		/// <param name="defaultReturnLowerBound"></param>
		/// <param name="minimumReturnComplete"></param>
		/// <param name="defaultReturnUpperBound"></param>
		/// <param name="reason"></param>
		public LaneChangeParameters(bool available, bool feasible, ArbiterLane initial, bool initialOncoming,
			ArbiterLane target, bool targetOncoming, bool toLeft, Behavior behavior, double distanceToDepartUpperBound,
			IState nextState, List<BehaviorDecorator> decorators, TravelingParameters parameters,
			Coordinates departUppedBound, Coordinates defaultReturnLowerBound, Coordinates minimumReturnComplete,
			Coordinates defaultReturnUpperBound, LaneChangeReason reason)
		{
			this.Available = available;
			this.Feasible = feasible;
			this.Initial = initial;
			this.InitialOncoming = initialOncoming;
			this.Target = target;
			this.TargetOncoming = targetOncoming;
			this.ToLeft = toLeft;
			this.Behavior = behavior;
			this.NextState = nextState;
			this.Decorators = decorators;
			this.Parameters = parameters;
			this.DistanceToDepartUpperBound = distanceToDepartUpperBound;
			this.DepartUpperBound = departUppedBound;
			this.DefaultReturnLowerBound = defaultReturnLowerBound;
			this.MinimumReturnComplete = minimumReturnComplete;
			this.DefaultReturnUpperBound = defaultReturnUpperBound;
			this.Reason = reason;
			this.ForcedOpposing = false;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is LaneChangeParameters)
			{
				LaneChangeParameters other = (LaneChangeParameters)obj;

				if (this.Available && other.Available)
				{
					if (this.Feasible && other.Feasible)
					{
						if (this.TargetOncoming && other.TargetOncoming)
						{
							return this.Parameters.RecommendedSpeed.CompareTo(other.Parameters.RecommendedSpeed);
						}
						else if (!this.TargetOncoming && other.TargetOncoming)
						{
							return -1;
						}
						else if (this.TargetOncoming && !other.TargetOncoming)
						{
							return 1;
						}
						else
						{
							return this.Parameters.RecommendedSpeed.CompareTo(other.Parameters.RecommendedSpeed);
						}
					}
					else if (this.Feasible && !other.Feasible)
					{
						return -1;
					}
					else if (!this.Feasible && other.Feasible)
					{
						return 1;
					}
					else
					{
						return 0;
					}
				}
				else if (this.Available && !other.Available)
				{
					return -1;
				}
				else if (!this.Available && other.Available)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
			else
				return -1;
		}

		#endregion
	}
}
