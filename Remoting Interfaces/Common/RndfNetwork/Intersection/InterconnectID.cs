using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	[Serializable]
	public class InterconnectLanes
	{
		private LaneID initial;
		private LaneID final;

		public InterconnectLanes(LaneID initial, LaneID final)
		{
			this.initial = initial;
			this.final = final;
		}

		public LaneID Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		public LaneID Final
		{
			get { return final; }
			set { final = value; }
		}

		public override bool Equals(object obj)
		{
			if (obj is InterconnectLanes)
			{
				InterconnectLanes other = (InterconnectLanes)obj;
				return (this.initial.Equals(other.Initial) && this.final.Equals(other.Final));
			}
			else
			{
				throw new ArgumentException("Argument not InterconnectLanes", "obj");
			}
		}

		public override int GetHashCode()
		{
			return initial.GetHashCode() << 16 + final.GetHashCode();
		}

		public override string ToString()
		{
			return initial.ToString() + " - " + final.ToString();
		}
	}

	/// <summary>
	/// ID number of an exit entry pair
	/// </summary>
	[Serializable]
	public class InterconnectID
	{
		private RndfWaypointID initialID;
		private RndfWaypointID finalID;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		public InterconnectID(RndfWaypointID initial, RndfWaypointID final)
		{
			this.initialID = initial;
			this.finalID = final;
		}

		/// <summary>
		/// ID of final waypoint
		/// </summary>
		public RndfWaypointID FinalID
		{
			get { return finalID; }
			set { finalID = value; }
		}

		/// <summary>
		/// ID of initial waypoint
		/// </summary>
		public RndfWaypointID InitialID
		{
			get { return initialID; }
			set { initialID = value; }
		}

		/// <summary>
		/// String representation of the ID
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.initialID.ToString() + " - " + this.finalID.ToString();
		}

		public override int GetHashCode()
		{
			return (this.initialID.GetHashCode() << 24) + this.finalID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is InterconnectID)
			{
				InterconnectID test = (InterconnectID)obj;
				return test.FinalID.Equals(this.FinalID) && test.InitialID.Equals(this.InitialID);
			}
			else
			{
				throw new ArgumentException("Wrong Type", "obj");
			}
		}
	}
}
