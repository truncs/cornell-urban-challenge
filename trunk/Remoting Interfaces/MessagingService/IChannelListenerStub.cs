using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Messaging;

namespace UrbanChallenge.MessagingService {

	public interface IChannelListenerStub : IPingable {

		/// <summary>
		/// The method that is invoked when a message is published at an IChannel.
		/// </summary>
		/// <param name="channelName">Name of the calling IChannel.</param>
		/// <param name="message">The message that was published.</param>
		[OneWay]
		void UnreliableMessageArrived(string channelName, object message);

		/// <summary>
		/// Equivalent to UnreliableMessageArrived, but not [oneway].
		/// </summary>
		/// <param name="channelName">Name of the calling IChannel.</param>
		/// <param name="message">The message that was published.</param>
		void ReliableMessageArrived(string channelName, object message);

	}

}
