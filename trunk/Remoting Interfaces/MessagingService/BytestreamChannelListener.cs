using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Lifetime;

namespace UrbanChallenge.MessagingService {

	internal sealed class BytestreamChannelListener : MarshalByRefObject, IChannelListenerStub {

		public BytestreamChannelListener(IChannelListener sink) {
			this.sink = sink;
		}

		public void UnreliableMessageArrived(string channelName, object message) {
			byte[] serializedMessage = message as byte[];
			if (serializedMessage != null) {
				// Message has been serialized. Deserialize it.
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new MemoryStream(serializedMessage);
				object originalMessage = formatter.Deserialize(stream);
				sink.MessageArrived(channelName, originalMessage);
			} else {
				// Message has not been serialized, just pass it on.
				sink.MessageArrived(channelName, message);
			}
		}

		public void ReliableMessageArrived(string channelName, object message) {
			this.UnreliableMessageArrived(channelName, message);
		}

		public void Ping() {
			// Nothing to do.
		}

		private IChannelListener sink;

		public override object InitializeLifetimeService()
		{
			ILease lease = (ILease)base.InitializeLifetimeService();
			if (lease.CurrentState == LeaseState.Initial)
			{
				lease.InitialLeaseTime = TimeSpan.FromDays(2);
				lease.RenewOnCallTime = TimeSpan.FromMinutes(1);
			}

			return lease;
		}

	}

}
