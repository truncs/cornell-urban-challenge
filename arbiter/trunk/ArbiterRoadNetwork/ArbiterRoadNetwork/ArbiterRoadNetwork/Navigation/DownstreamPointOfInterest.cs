using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Point downstram of where we currently are that is of interest
	/// </summary>
	public class DownstreamPointOfInterest : IComparable
	{
		/// <summary>
		/// Point of interest
		/// </summary>
		public ArbiterWaypoint PointOfInterest;

		/// <summary>
		/// Distance along segment to point
		/// </summary>
		public double DistanceToPoint;

		/// <summary>
		/// Distance along segment to point in time
		/// </summary>
		public double TimeCostToPoint;

		/// <summary>
		/// ROute time
		/// </summary>
		public double RouteTime;

		/// <summary>
		/// Route itself
		/// </summary>
		public List<INavigableNode> BestRoute;

		/// <summary>
		/// is goal or not
		/// </summary>
		public bool IsGoal;

		/// <summary>
		/// Is exit or not
		/// </summary>
		public bool IsExit;

		/// <summary>
		/// Best exit of the dpoi if exit
		/// </summary>
		public ArbiterInterconnect BestExit;

		/// <summary>
		/// total time going through this poi to reach goal
		/// </summary>
		public double TotalTime
		{
			get
			{
				return this.RouteTime + this.TimeCostToPoint;
			}
		}

		#region IComparable Members

		/// <summary>
		/// Comparer
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is DownstreamPointOfInterest)
			{
				DownstreamPointOfInterest other = (DownstreamPointOfInterest)obj;

				if (this.TotalTime < other.TotalTime)
					return -1;
				else if (this.TotalTime > other.TotalTime)
					return 1;
				else
					return 0;
			}

			return -1;
		}

		#endregion

		public override bool Equals(object obj)
		{
			return (obj is DownstreamPointOfInterest) ? this.PointOfInterest.Equals(((DownstreamPointOfInterest)obj).PointOfInterest) : false;
		}

		public override int GetHashCode()
		{
			return PointOfInterest.GetHashCode();
		}

		public override string ToString()
		{
			return this.PointOfInterest.ToString();
		}

		public DownstreamPointOfInterest Clone()
		{
			DownstreamPointOfInterest tmp = new DownstreamPointOfInterest();
			tmp.BestExit = this.BestExit;
			tmp.BestRoute = this.BestRoute;
			tmp.DistanceToPoint = this.DistanceToPoint;
			tmp.IsExit = this.IsExit;
			tmp.IsGoal = this.IsGoal;
			tmp.PointOfInterest = this.PointOfInterest;
			tmp.RouteTime = this.RouteTime;
			tmp.TimeCostToPoint = this.TimeCostToPoint;
			return tmp;
		}
	}
}
