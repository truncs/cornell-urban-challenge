using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UrbanChallenge.MessagingService {

	[Serializable]
	public sealed class BytestreamChannel : IChannel {

		public BytestreamChannel(IRemoteChannel remoteInstance) {
			this.remoteInstance = remoteInstance;
		}

		public string Name {
			get { return remoteInstance.Name; }
		}

		public uint Subscribe(IChannelListener listener) {
			Console.WriteLine("Got a subsribe BYTESTREAM" + Environment.NewLine + listener.ToString());
			Console.WriteLine(this.NoOfListeners);
			return remoteInstance.SubscribeStub(new BytestreamChannelListener(listener));
		}

		public void Unsubscribe(uint token) {
			remoteInstance.Unsubscribe(token);
		}

		public void PublishUnreliably(object message) {
			MemoryStream stream = new MemoryStream();
			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, message);
			byte[] serializedMessage = stream.GetBuffer();
			remoteInstance.PublishUnreliably(serializedMessage);
		}

		public void PublishUnreliably(object message, ChannelSerializerInfo info)
		{
			MemoryStream stream = new MemoryStream();
			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, message);
			byte[] serializedMessage = stream.GetBuffer();
			remoteInstance.PublishUnreliably(serializedMessage);
		}

		public void PublishUnreliably(object message, ChannelSerializerInfo serializerInfo, int seqNum)
		{
			throw new NotImplementedException();
		}

		public void PublishReliably(object message) {
			MemoryStream stream = new MemoryStream();
			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, message);
			byte[] serializedMessage = stream.GetBuffer();
			remoteInstance.PublishReliably(serializedMessage);
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
