using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Speed limit for an area
	/// </summary>
	[Serializable]
	public class ArbiterSpeedLimit
	{
		private double averageSpeed;

		/// <summary>
		/// Area the speed limit applies to
		/// </summary>
		public IAreaId Area;

		/// <summary>
		/// Maximum Speed of the Area
		/// </summary>
		public double MaximumSpeed;

		/// <summary>
		/// Minimum Speed of the Area
		/// </summary>
		public double MinimumSpeed;

		/// <summary>
		/// Whether we have traveled the area or not
		/// </summary>
		public bool Traveled;

		/// <summary>
		/// Cosntructor
		/// </summary>
		public ArbiterSpeedLimit()
		{
		}

		/// <summary>
		/// Average Speed of the Area
		/// </summary>
		public double AverageSpeed
		{
			get
			{
				if (this.averageSpeed == 0)
					return (this.MaximumSpeed + this.MinimumSpeed) / 2.0;
				else
					return this.averageSpeed;
			}
			set
			{
				this.averageSpeed = value;
			}
		}

		/// <summary>
		/// String representing the speed
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "Max: " + this.MaximumSpeed.ToString("F1") + ", Min: " + this.MinimumSpeed.ToString("F1");
		}
	}
}
