using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	public abstract class TestServerFacade : MarshalByRefObject
	{
		/// <summary>
		/// Retreive the Rndf network
		/// </summary>
		public abstract RndfNetwork RndfNetwork
		{
			get;
		}

		/// <summary>
		/// Receive the goals
		/// </summary>
		public abstract Mdf Mdf
		{
			get;
		}

		/// <summary>
		/// Receive the vehicle state
		/// </summary>
		public abstract VehicleState VehicleState
		{
			get;
		}
	}

}
