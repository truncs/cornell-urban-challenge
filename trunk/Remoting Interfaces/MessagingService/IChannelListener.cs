using System;

namespace UrbanChallenge.MessagingService {

	/// <summary>
	/// Listener interface to be implemented by objects which subscribe to
	/// an IChannel.
	/// </summary>
	public interface IChannelListener {

		/// <summary>
		/// The method that is invoked when a message is published at an IChannel.
		/// </summary>
		/// <param name="channelName">Name of the calling IChannel.</param>
		/// <param name="message">The message that was published.</param>
		void MessageArrived(string channelName, object message);

	}

	public interface IAdvancedChannelListener : IChannelListener
	{
		/// <summary>
		/// The method that is invoked when a message is published at an IChannel. This supports a few extended fields.
		/// </summary>
		/// <param name="channelName">Name of the calling IChannel.</param>
		/// <param name="message">The message that was published.</param>
		void MessageArrived(string channelName, object message, int seqNumber);
	}
}
