using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Defines a connection from one Lane to another over different Segments in the Rnd
	/// </summary>
	[Serializable]
	public class Interconnect : IConnectWaypoints
	{
		private RndfWayPoint finalWaypoint;
		private RndfWayPoint initialWaypoint;
		private List<UserPartition> userPartitions;
		private InterconnectID interconnectID;
		private List<Blockage> blockages;		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialWaypoint">beginning waypoint of interconnect</param>
		/// <param name="finalWaypoint">ending waypoint of interconnect</param>
		public Interconnect(RndfWayPoint initialWaypoint, RndfWayPoint finalWaypoint)
		{
			this.initialWaypoint = initialWaypoint;
			this.finalWaypoint = finalWaypoint;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialWaypoint">beginning waypoint of interconnect</param>
		/// <param name="finalWaypoint">ending waypoint of interconnect</param>
		public Interconnect(InterconnectID interconnectID, RndfWayPoint initialWaypoint, RndfWayPoint finalWaypoint)
		{
			this.initialWaypoint = initialWaypoint;
			this.finalWaypoint = finalWaypoint;
			this.interconnectID = interconnectID;
		}

		/// <summary>
		/// identification information for the interconnect
		/// </summary>
		public InterconnectID InterconnectID
		{
			get { return interconnectID; }
			set { interconnectID = value; }
		}

		/// <summary>
		/// More precise definition of turn
		/// </summary>
		public List<UserPartition> UserPartitions
		{
			get { return userPartitions; }
			set { userPartitions = value; }
		}

		/// <summary>
		/// End bound of the UserPrtition
		/// </summary>
		public RndfWayPoint FinalWaypoint
		{
			get { return finalWaypoint; }
			set { finalWaypoint = value; }
		}

		/// <summary>
		/// Beginning bound of the UserParition
		/// </summary>
		public RndfWayPoint InitialWaypoint
		{
			get { return initialWaypoint; }
			set { initialWaypoint = value; }
		}

		/// <summary>
		/// Blockages this connection hodls
		/// </summary>
		public List<Blockage> Blockages
		{
			get { return blockages; }
			set { blockages = value; }
		}

		/// <summary>
		/// String representation of the object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.InterconnectID.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is Interconnect)
			{
				return this.interconnectID.Equals(((Interconnect)obj).InterconnectID);
			}
			else
			{
				throw new ArgumentException("Wrong type", "obj");
			}
		}

		public override int GetHashCode()
		{
			return this.interconnectID.GetHashCode();
		}
	}
}
