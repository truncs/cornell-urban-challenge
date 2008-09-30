using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.MessagingService {

	/// <summary>
	/// Simple interface with a twoway Ping method for liveness test.
	/// </summary>
	public interface IPingable {

		/// <summary>
		/// Keepalive ping. Called by the IChannel every couple of seconds.
		/// </summary>
		void Ping();

	}

}
