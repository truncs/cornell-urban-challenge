using System;
using System.Collections.Generic;

namespace UrbanChallenge.MessagingService {

	/// <summary>
	/// Possible channel modes.
	/// </summary>
	public enum ChannelMode {

		/// <summary>
		/// A Vanilla channel uses implicit serialization of arguments to
		/// remote calls. This is fine for built-in data types (e.g. int),
		/// but fails for application-specific types, because the
		/// MessagingService can not deserialize them in want of the
		/// necessary assembly file.
		/// </summary>
		Vanilla,

		/// <summary>
		/// Bytestream channels serialize objects at the client side and
		/// pass them through the MessagingService in the form of byte
		/// arrays. The subscriber deserializes the original object from
		/// the byte array (happens transparently when the subscriber uses
		/// a Bytestream channel as well). This scheme removes the need
		/// for having the passed object type defined at the MessagingService
		/// since it only sees a byte array, but is incompatible to CORBA
		/// clients, because these clients lack the required formatter
		/// classes from .net. If you need to pass complex data between .net
		/// and CORBA, use a Vanilla channel and send byte arrays using your
		/// own, proprietory marshalling routines.
		/// </summary>
		Bytestream,


		/// <summary>
		/// This channel uses UDP multicast. It uses the binary serializer
		/// in .net and should only be used for .net to .net communication.
        /// As for the Bytestream, the message service needs no knowledge
        /// of the type; in fact, the server never gets to see the data.
		/// </summary>
		UdpMulticast

	}

	/// <summary>
	/// The IChannelFactory manages IChannels for messaging.
	/// </summary>
	public interface IChannelFactory {

		/// <summary>All active IChannels.</summary>
		ICollection<string> Channels { get; }

		/// <summary>
		/// Returns the ICHannel of the given name. Creates a new IChannel if no
		/// IChannel of given name exists yet.
		/// </summary>
		/// <param name="channelName">The name of the IChannel.</param>
		/// <param name="byteStream">The channel mode.</param>
		/// <returns>The IChannel.</returns>
		IChannel GetChannel(string channelName, ChannelMode mode);

	}

}
