using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// The driver is actively controlling the vehicle
	/// </summary>
	public class UserControlledState : IState
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public UserControlledState()
		{
		}

		/// <summary>
		/// Initial Lane the vehicle is in
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID InitialLane
		{
			get { return null; }
		}

		/// <summary>
		/// Final Lane the vehicle is going to
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID FinalLane
		{
			get { return null; }
		}

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public string ShortDescription()
		{
			return ("UserControlled State");
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public string LongDescription()
		{
			return ("State: UserControlled");
		}

		/// <summary>
		/// String representation of the state
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.ShortDescription();
		}
	}
}
