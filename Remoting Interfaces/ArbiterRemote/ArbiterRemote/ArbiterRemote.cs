using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Arbiter.ArbiterCommon;
using System.Runtime.Remoting.Messaging;

namespace UrbanChallenge.Arbiter.Communication
{
	/// <summary>
	/// Remoting Interface to the Arbiter
	/// </summary>
	[Serializable]
	public abstract class ArbiterRemote : MarshalByRefObject
	{
		/// <summary>
		/// Stops the Arbiter Remotely if able
		/// </summary>
		/// <returns></returns>
		public abstract void Stop();

		/// <summary>
		/// Retrieves the Rndf the Arbiter is using
		/// </summary>
		/// <returns></returns>
		public abstract RndfNetwork Rndf();

		/// <summary>
		/// Retrieves the Mdf the Arbiter is using
		/// </summary>
		/// <returns></returns>
		public abstract Mdf Mdf();

		/// <summary>
		/// Attempts to set an mdf
		/// </summary>
		/// <param name="mdf"></param>
		/// <returns>true if mdf set successfully</returns>
		/// <remarks>needs to be in pause</remarks>
		public abstract bool SetMdf(Mdf mdf);

		/// <summary>
		/// Restarts the Rndf and Mdf using the input
		/// </summary>
		/// <param name="rndf"></param>
		/// <param name="mdf"></param>
		[OneWay]
		public abstract void Restart(RndfNetwork rndf, Mdf mdf, ArbiterMode mode);

		/// <summary>
		/// Pings the Arbiter to make sure we're still connected
		/// </summary>
		/// <returns></returns>
		public abstract bool Ping();
	}
}
