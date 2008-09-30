using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.State;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Type of travelling
	/// </summary>
	public enum TravellingType
	{
		Navigation,
		NavigationStopLine,
		StopLine,
		Vehicle,
		Blockage
	}

	/// <summary>
	/// Parameters for travelling along the lane
	/// </summary>
	public struct TravelingParameters : IComparable
	{
		/// <summary>
		/// Type of parameterization this represents
		/// </summary>
		public TravellingType Type;

		/// <summary>
		/// Speed
		/// </summary>
		public double RecommendedSpeed;

		/// <summary>
		/// Distance left
		/// </summary>
		public double DistanceToGo;

		/// <summary>
		/// Behavior to perform give choose this parameterization
		/// </summary>
		public Behavior Behavior;

		/// <summary>
		/// Decorators for the behavior
		/// </summary>
		public List<BehaviorDecorator> Decorators;

		/// <summary>
		/// Next state to take
		/// </summary>
		public IState NextState;

		/// <summary>
		/// Flag if using speed and not distance
		/// </summary>
		public bool UsingSpeed;

		/// <summary>
		/// Vehicles to ignore given this parameterization
		/// </summary>
		public List<int> VehiclesToIgnore;

		/// <summary>
		/// Speed command currently used
		/// </summary>
		public SpeedCommand SpeedCommand;

		#region IComparable Members

		/// <summary>
		/// COmpare this parameterization to another
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is TravelingParameters)
			{
				TravelingParameters other = (TravelingParameters)obj;

				if (!this.UsingSpeed || !other.UsingSpeed)
				{
					if (this.DistanceToGo < other.DistanceToGo)
						return -1;
					else if (this.DistanceToGo > other.DistanceToGo)
						return 1;
					else
						return 0;
				}
				else
				{
					if (this.RecommendedSpeed < other.RecommendedSpeed)
						return -1;
					else if (this.RecommendedSpeed > other.RecommendedSpeed)
						return 1;
					else
						return 0;
				}
			}
			else
			{
				throw new Exception("obj not Traveling Params: " + obj.GetType().ToString());
			}
		}

		#endregion
	}
}
