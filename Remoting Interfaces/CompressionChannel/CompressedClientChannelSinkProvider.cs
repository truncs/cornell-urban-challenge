using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Collections;

namespace CompressionChannel {
	public class CompressedClientChannelSinkProvider : IClientChannelSinkProvider {

		private IClientChannelSinkProvider _next;

		public CompressedClientChannelSinkProvider() {
		}

		public CompressedClientChannelSinkProvider(IDictionary properties, ICollection providerData) {
			// don't care
		}

		#region IClientChannelSinkProvider Members

		public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData) {
			IClientChannelSink nextSink = null;

			if (_next != null) {
				// Call CreateSink on the next sink provier in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _next.CreateSink(channel, url, remoteChannelData)) == null) return null;
			}

			// Create this sink, passing to it the previous sink in the chain so that it knows
			// to whom messages should be passed.
			return new CompressedClientChannelSink(nextSink);
		}

		public IClientChannelSinkProvider Next {
			get {
				return _next;
			}
			set {
				_next = value;
			}
		}

		#endregion
	}
}
