using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Collections;

namespace CompressionChannel {
	public class CompressedServerChannelSinkProvider : IServerChannelSinkProvider {
		private IServerChannelSinkProvider _next;

		public CompressedServerChannelSinkProvider() {
		}

		public CompressedServerChannelSinkProvider(IDictionary properties, ICollection providerData) {
			// don't care
		}

		#region IServerChannelSinkProvider Members

		public IServerChannelSink CreateSink(IChannelReceiver channel) {
			IServerChannelSink nextSink = null;
			if (_next != null) {
				// Call CreateSink on the next sink provider in the chain.  This will return
				// to us the actual next sink object.  If the next sink is null, uh oh!
				if ((nextSink = _next.CreateSink(channel)) == null) return null;
			}

			// Create this sink, passing to it the previous sink in the chain so that it knows
			// to whom messages should be passed.
			return new CompressedServerChannelSink(nextSink);
		}

		public void GetChannelData(IChannelDataStore channelData) {
			// don't care
		}

		public IServerChannelSinkProvider Next {
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
