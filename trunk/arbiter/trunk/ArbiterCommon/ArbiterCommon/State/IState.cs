using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Inteface for keeping hte state of the vehicle
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// Initial Lane the vehicle is in
		/// </summary>
		LaneID InitialLane
		{
			get;
		}

		/// <summary>
		/// Final Lane the vehicle is going to
		/// </summary>
		LaneID FinalLane
		{
			get;
		}

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		string ShortDescription();

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		string LongDescription();
	}
}
