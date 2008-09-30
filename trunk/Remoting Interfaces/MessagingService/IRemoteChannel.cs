using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.MessagingService {

    /// <summary>
    /// Used to subscribe stubs to the remote channel object.
    /// </summary>
	public interface IRemoteChannel : IChannel {

		/// <summary>
		/// Used internally to subscribe stubs to the remote channel object.
		/// </summary>
		/// <param name="stub">The stub object.</param>
		/// <returns>The subscription token.</returns>
		uint SubscribeStub(IChannelListenerStub stub);

	}

}
