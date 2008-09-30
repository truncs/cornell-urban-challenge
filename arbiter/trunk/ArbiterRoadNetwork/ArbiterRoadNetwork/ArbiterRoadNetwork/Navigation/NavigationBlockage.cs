using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Blockage of a navigation edge
	/// </summary>
	[Serializable]
	public class NavigationBlockage
	{
		/// <summary>
		/// Lifetime of the blockage in seconds
		/// </summary>
		public double BlockageLifetime = 1800;

		/// <summary>
		/// Time since blockage occurred
		/// </summary>
		public double SecondsSinceObserved;

		/// <summary>
		/// if blockage exists
		/// </summary>
		public bool BlockageExists;

		/// <summary>
		/// Time cost of this blockage
		/// </summary>
		public double BlockageTimeCost;

		/// <summary>
		/// Coordinates of blockage
		/// </summary>
		public Coordinates BlockageCoordinates;

		public bool BlockageHasExisted = false;

		/// <summary>
		/// Constructor
		/// </summary>
		public NavigationBlockage(double timeCost)
		{
			this.BlockageExists = false;
			this.BlockageTimeCost = timeCost;
		}

		/// <summary>
		/// Cost of the current blockage
		/// </summary>
		public double BlockageCost
		{
			get
			{
				if (!BlockageExists)
				{
					return 0.0;
				}
				else
				{
					return this.ProbabilityExists * this.BlockageTimeCost;
				}
			}
		}

		/// <summary>
		/// Set that we have seen thsi blockage
		/// </summary>
		/// <returns></returns>
		public void Observered()
		{
			this.BlockageExists = true;
			this.SecondsSinceObserved = 0;
		}

		/// <summary>
		/// Probability blockage exists
		/// </summary>
		public double ProbabilityExists
		{
			get
			{
				if (!this.BlockageExists)
					return 0.0;
				else
				{
					if (SecondsSinceObserved > BlockageLifetime)
					{
						this.BlockageExists = false;
						return 0;
					}
					else
					{
						return 1 - (SecondsSinceObserved / BlockageLifetime);
					}
				}
			}
		}

		public bool ComputeBlockageExists()
		{
			if (!this.BlockageExists)
				return false;
			else if (SecondsSinceObserved > BlockageLifetime)
			{
				this.BlockageExists = false;
				return false;
			}
			else
			{
				this.BlockageHasExisted = true;
				return true;				
			}
		}
	}
}
