using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Segemtn of Boundary of the intersection 
	/// </summary>
	[Serializable]	
	public struct BoundaryLine
	{
		public Coordinates p1;
		public Coordinates p2;

		public BoundaryLine(Coordinates p1, Coordinates p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}
	}

	/// <summary>
	/// Holds interconnections among segments in a specific intersection
	/// </summary>
	[Serializable]
	public class Intersection
	{
		private List<IConnectWaypoints> connections;
		private List<BoundaryLine> perimeter;

		/// <summary>
		/// Perimeter of the intersection
		/// </summary>
		public List<BoundaryLine> Perimeter
		{
			get { return perimeter; }
			set { perimeter = value; }
		}

		/// <summary>
		/// Connections among Waypoints in an intersection
		/// </summary>
		public List<IConnectWaypoints> Connections
		{
			get { return connections; }
			set { connections = value; }
		}

		public override bool Equals(object obj)
		{
			if (obj is Intersection)
			{
				Intersection other = (Intersection)obj;
				IConnectWaypoints firstOther = other.Connections[0];
				if (this.Connections[0].Equals(firstOther))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return this.Connections[0].GetHashCode();
		}
	}
}
