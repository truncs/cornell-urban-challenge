using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Defines when the vehicle has been paused and is fully out of ai control
	/// </summary>
	/// <remarks>When coming out of paused go to start up state</remarks>
	public class PausedState : IState
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public PausedState()
		{
		}

		#region IState Members

		public LaneID InitialLane
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public LaneID FinalLane
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public string ShortDescription()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public string LongDescription()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}
}
