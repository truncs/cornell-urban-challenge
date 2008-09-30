using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
	[Serializable]
	public class FullRoute : IComparable, IComparer<FullRoute>
	{
		private List<RndfWaypointID> routeNodes;
		private double timeCost;

		public FullRoute()
		{
		}

		public FullRoute(List<RndfWaypointID> routeNodes, double timeCost)
		{
			this.routeNodes = routeNodes;
			this.timeCost = timeCost;
		}

		public FullRoute(List<RndfWayPoint> waypointNodes, double timeCost)
		{
			routeNodes = new List<RndfWaypointID>();
			foreach (RndfWayPoint wp in waypointNodes)
			{
				routeNodes.Add(wp.WaypointID);
			}
			this.timeCost = timeCost;
		}

		public double TimeCost
		{
			get { return timeCost; }
			set { timeCost = value; }
		}

		public List<RndfWaypointID> RouteNodes
		{
			get { return routeNodes; }
			set { routeNodes = value; }
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is FullRoute)
			{
				FullRoute other = (FullRoute)obj;
				if (this.timeCost < other.TimeCost)
				{
					return -1;
				}
				else if (this.timeCost == other.TimeCost)
				{
					return 0;
				}
				else
					return 1;
			}
			else
			{
				throw new ArgumentException("Wrong ttype", "obj");
			}
		}

		#endregion

		#region IComparer<FullRoute> Members

		/// <summary>
		/// Compare two full routes by the time it takes
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int Compare(FullRoute x, FullRoute y)
		{
			if (x.TimeCost < y.TimeCost)
			{
				return -1;
			}
			else if (x.TimeCost == y.TimeCost)
			{
				return 0;
			}
			else
				return 1;
		}

		#endregion

		/// <summary>
		/// String representation of the Route
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if(this.routeNodes.Count < 2)
				return this.timeCost.ToString();
			else
				return ("Route: " + this.routeNodes[0].ToString() + " -> " + this.routeNodes[this.routeNodes.Count-1].ToString() + ", Time: " + this.timeCost.ToString());
		}

		public override bool Equals(object obj)
		{
			if (obj is FullRoute)
			{
				FullRoute other = (FullRoute)obj;
				if(this.timeCost == other.TimeCost && this.routeNodes.Count == other.routeNodes.Count)
				{
					for(int i = 0; i < this.routeNodes.Count; i++)
					{
						if (this.routeNodes[i] != other.RouteNodes[i])
						{
							return false;
						}
					}
					return true;
				}
				else
				{
					return false;
				}

			}
			else
			{
				throw new ArgumentException("Wrong Type", "obj");
			}
		}

		public override int GetHashCode() {
			int hashCode = timeCost.GetHashCode();

			foreach (RndfWaypointID id in routeNodes) {
				hashCode += id.GetHashCode();
			}

			return hashCode;
		}
	}
}
