using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Lifetime;

namespace UrbanChallenge.MessagingService {

	internal sealed class VanillaChannelListener : MarshalByRefObject, IChannelListenerStub {

		public VanillaChannelListener(IChannelListener sink) {
			this.sink = sink;
		}

		public void UnreliableMessageArrived(string channelName, object message) {
			sink.MessageArrived(channelName, message);
		}

		public void ReliableMessageArrived(string channelName, object message) {
			sink.MessageArrived(channelName, message);
		}

		public void Ping() {
			// Nothing to do.
		}

		public override object InitializeLifetimeService() {
            return null;
		}

        private IChannelListener sink;

	}

}
