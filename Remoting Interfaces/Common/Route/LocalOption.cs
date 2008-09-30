using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Describes a specific IConnectWaypoints to take and its associated time to goal
	/// </summary>
	[Serializable]
	public class LocalOption : IComparable, IComparer<LocalOption>
	{
		private IConnectWaypoints specificPath;
		private double time;

		/// <summary>
		/// Specific path to take in a LocalRoute
		/// </summary>
		/// <param name="time"></param>
		/// <param name="specificPath"></param>
		public LocalOption(double time, IConnectWaypoints specificPath)
		{
			this.time = time;
			this.specificPath = specificPath;
		}

		/// <summary>
		/// Time it would take to reach the goal if planning from the current position
		/// </summary>
		public double Time
		{
			get { return time; }
			set { time = value; }
		}

		/// <summary>
		/// Denotes the specific path
		/// </summary>
		public IConnectWaypoints SpecificPath
		{
			get { return specificPath; }
			set { specificPath = value; }
		}

		#region IComparable Members

		/// <summary>
		/// Compares RouteSegments based upon cost
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is LocalOption)
			{
				LocalOption other = (LocalOption)obj;
				if (this.time < other.time)
				{
					return -1;
				}
				else if (this.time == other.time)
				{
					return 0;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				throw new ArgumentException("Invalid object type", "obj");
			}
		}

		#endregion

		#region IComparer<LocalOption> Members

		public int Compare(LocalOption x, LocalOption y)
		{
			if (x.Time < y.Time)
			{
				return -1;
			}
			else if (x.Time == y.Time)
			{
				return 0;
			}
			else
			{
				return 1;
			}
		}

		#endregion
	}
}
