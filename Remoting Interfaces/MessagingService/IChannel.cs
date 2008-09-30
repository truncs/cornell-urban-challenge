using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace UrbanChallenge.MessagingService {

	/// <summary>
	/// A single channel for messaging. Essentially a named level of
	/// indirection that forwards published messages to all
	/// IChannelListeners who subscribed.
	/// </summary>
	public interface IChannel : IDisposable {

		/// <summary>
		/// Unique name of the IChannel.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Subscribe to receive messages from the IChannel.
		/// </summary>
		/// <param name="listener">The receiver object.</param>
		/// <returns>The subscription token.</returns>
		uint Subscribe(IChannelListener listener);

		/// <summary>
		/// Unsubscribes from receiving messages from the IChannel.
		/// </summary>
		/// <param name="token">The subscription token.</param>
		void Unsubscribe(uint token);

		/// <summary>
		/// Forwards the message unreliably (and asynchronously) to all subscribers.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		[OneWay]
		void PublishUnreliably(object message);

		/// <summary>
		/// Forwards the message unreliably (and asynchronously) to all subscribers.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		[OneWay]
		void PublishUnreliably(object message, ChannelSerializerInfo info);

		[OneWay]
		void PublishUnreliably(object message, ChannelSerializerInfo serializerInfo, int seqNum);

		/// <summary>
		/// Forwards the message reliably to all subscribers.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		void PublishReliably(object message);

		/// <summary>
		/// Number of subscribers.
		/// </summary>
		uint NoOfListeners { get; }

	}

}
