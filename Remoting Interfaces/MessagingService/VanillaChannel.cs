using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UrbanChallenge.MessagingService {

	[Serializable]
	public sealed class VanillaChannel : IChannel {

		public VanillaChannel(IRemoteChannel remoteInstance) {
			this.remoteInstance = remoteInstance;
		}

		public string Name {
			get { return remoteInstance.Name; }
		}

		public uint Subscribe(IChannelListener listener) {
			Console.WriteLine("Got a subsribe VANILLA" + listener.ToString());
			return remoteInstance.SubscribeStub(new VanillaChannelListener(listener));
		}

		public void Unsubscribe(uint token) {
			remoteInstance.Unsubscribe(token);
		}

		public void PublishUnreliably(object message) {
			remoteInstance.PublishUnreliably(message);
		}

		public void PublishUnreliably(object message, ChannelSerializerInfo info)
		{
			remoteInstance.PublishUnreliably(message);
		}
		public void PublishUnreliably(object message, ChannelSerializerInfo serializerInfo, int seqNum)
		{
			throw new NotImplementedException();
		}


		public void PublishReliably(object message) {
			remoteInstance.PublishReliably(message);
		}

		public uint NoOfListeners {
			get { return remoteInstance.NoOfListeners; }
		}

		private IRemoteChannel remoteInstance;


		#region IDisposable Members

		public void Dispose()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}

}
