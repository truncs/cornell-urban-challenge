using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// The ai has just started up
	/// </summary>
	public class StartUpState : IState
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public StartUpState()
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
			return ("StartUp State");
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public string LongDescription()
		{
			return ("State: StartUp");
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
