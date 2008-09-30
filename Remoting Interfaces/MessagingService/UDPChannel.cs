using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using UrbanChallenge.MessagingService.ChannelSerializers;

namespace UrbanChallenge.MessagingService 
{

	[Serializable]
	public sealed class UDPChannel : IChannel , IDisposable 
	{
		private static int deathCount = 0;
		private readonly Dictionary<uint, IChannelListener> listeners = new Dictionary<uint, IChannelListener>();
		private readonly string name;
		private readonly IPAddress ip;
		private readonly Int32 port;

		[NonSerialized]
		public const byte versionNumber = 2;

		[NonSerialized]
		private IFormatter formatter;

		[NonSerialized]
		private byte[] buf;

		[NonSerialized]
		private Socket sock;

		[NonSerialized]
		private uint tokenCount;

		[NonSerialized]
		private int seqNumberRX;

		[NonSerialized]
		private int seqNumberTX;

		[NonSerialized]
		private int packetRXCount;

		[NonSerialized]
		private int initRXSeqNumber;

		public IPAddress IP
		{
			get { return ip; }
		}

		public int Port
		{
			get { return port; }
		}

		public UDPChannel(string name, IPAddress ip, int port) {
			this.name = name;
			this.ip = ip;
			this.port = port;
			seqNumberTX = 0;
			seqNumberRX = 0;
			initRXSeqNumber = int.MinValue ;
			packetRXCount = 0;
		}

		public void UnsubscribeAll() {
			Console.WriteLine("This is not implemented...");
			throw new NotImplementedException("This feature is not implemented in UDP Channel.");
		}

		public string Name {
			get { return this.name; }
		}

		private void ReceiveCallback(IAsyncResult ar) {
			Socket s = (Socket)ar.AsyncState;
			bool disposed = false;
			try
			{
				int bytesReceived = s.EndReceive(ar);
				if (bytesReceived > 0)
				{
					HandleMessage(bytesReceived);
				}				
			}
			catch (ObjectDisposedException)
			{
				//...die
				disposed = true;
				deathCount--;
				Console.WriteLine("Lost Channel " + this.name);
			}
			catch (SocketException ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			lock (listeners)
			{
				if ((!disposed) && (this.sock != null))
					s.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, s);
			}
		}

		private void WriteHeader(Stream stream, int seqNumber)
		{
			BinaryWriter bw = new BinaryWriter(stream);
			bw.Write((byte)versionNumber);
			bw.Write((byte)ChannelSerializerInfo.BinarySerializer);			
			bw.Write((int)seqNumber);
		
		}

		public int GetTotalPacketsRecieved()
		{
			return this.packetRXCount;
		}

		/// <summary>
		/// Returns the drop rate on this channel as a percentage (0 - 100)
		/// </summary>
		/// <returns></returns>
		public double GetChannelDropRate()
		{			
			double theoreticalTotal = this.seqNumberRX - this.initRXSeqNumber;
			if (theoreticalTotal == 0) return 0;
			double goodPercentage = packetRXCount / theoreticalTotal * 100.0;
			return 100.0 - goodPercentage;
		}

		private ChannelSerializerInfo ReadHeader(Stream stream)
		{
			BinaryReader br = new BinaryReader(stream);
			byte version = br.ReadByte();
			if (version != UDPChannel.versionNumber)
			{
				//Console.WriteLine ("UDPChannel Version Mismatch. Local version is " + UDPChannel.versionNumber + ". Remote version is: " + version + " Dropping message.");
				//return ChannelSerializerInfo.ExplicitlyUnsupported;
				throw new InvalidOperationException("UDPChannel Version Mismatch. Local version is " + UDPChannel.versionNumber + ". Remote version is: " + version);
			}			
			ChannelSerializerInfo serializerType = (ChannelSerializerInfo)br.ReadByte();
			//jump ship now if this is an unsupported message
			if (serializerType == ChannelSerializerInfo.ExplicitlyUnsupported) return ChannelSerializerInfo.ExplicitlyUnsupported; 

			seqNumberRX = br.ReadInt32();
			//check if we need to init the first sequence number
			if (initRXSeqNumber == int.MinValue) initRXSeqNumber = seqNumberRX;
			return serializerType;
		}

		private void HandleMessage(int bytesReceived)
		{			
			MemoryStream stream = new MemoryStream(this.buf, 0, bytesReceived, false, true);
			ChannelSerializerInfo serializerType = ReadHeader(stream);
			object o=null;
			switch (serializerType)
			{
				case ChannelSerializerInfo.BinarySerializer:
					if (this.formatter == null) this.formatter = new BinaryFormatter(); //lazy cum load.
					o = this.formatter.Deserialize(stream);
				break;
				case ChannelSerializerInfo.TestSerializer :
					o = TestSerializer.Deserialize(stream);
					break;
				case ChannelSerializerInfo.SceneEstimatorSerializer:
					o = SceneEstimatorSerializer.Deserialize(stream, name);
					if (o == null) return;
					break;

				case ChannelSerializerInfo.PoseAbsoluteSerializer:
				case ChannelSerializerInfo.PoseRelativeSerializer:
					o = PoseSerializer.Deserialize(stream, serializerType, name);
					if (o == null) return;
					break;

				case ChannelSerializerInfo.SideObstacleSerializer:
					o = SideObstaclesSerializer.Deserialize(stream, name);
					if (o == null) return;
					break;
				case ChannelSerializerInfo.SideRoadEdgeSerializer:
					o = SideRoadEdgeSerializer.Deserialize(stream, name);
					if (o == null) return;
					break;

				case ChannelSerializerInfo.RoadBearing:
					o = RoadBearingSerializer.Deserialize(stream);
					if (o == null) return;
					break;

				case ChannelSerializerInfo.ExplicitlyUnsupported:
					//drop this message
					return;
				default:
					throw new NotImplementedException("Attempt to use unsupported deserializer for Receive: " + serializerType.ToString ());
			}

			lock (this.listeners)
			{
				foreach (KeyValuePair<uint, IChannelListener> kvp in this.listeners)
				{
					IChannelListener listener = kvp.Value;
					try
					{
						if (listener is IAdvancedChannelListener)
						{
                            if (listener != null)
                            {
							(listener as IAdvancedChannelListener).MessageArrived(this.name, o, this.seqNumberRX);
                            }
						}
						if (listener != null)
						{
							listener.MessageArrived(this.name, o);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Listener Exception: " + ex.Message);
						Console.WriteLine("Stack Trace:");
						Console.WriteLine(ex.StackTrace);
					}
				}
			}
			packetRXCount++;
		}

		public uint Subscribe(IChannelListener listener) {
			//when a message comes in over udp, we want to fire the listener.MessageArrived function
			uint token = this.tokenCount++;
			Console.WriteLine("Subscribe req for udp channel. Token: " + token.ToString());
			if (this.sock == null)
				BuildSocket();
			lock (this.listeners)
			{
				this.listeners.Add(token, listener);
			}
			return token;
		}

		public void Unsubscribe(uint token) {
			// this is easy, just get rid of the callback
			// (wrong! added reference counting using NoOfListeners --Philipp)
			Console.WriteLine("Unsubscribe req for udp channel. Token: " + token.ToString());
			lock (this.listeners)
			{
				if (this.listeners.ContainsKey(token))
				{
					this.listeners.Remove(token);
					if (this.NoOfListeners == 0)
					{
						this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(this.ip));
						this.sock.Close();
						this.sock = null;
					}
				}
				else
					Console.WriteLine("Token " + token.ToString() + " does not exist.");
			}
		}

		public void PublishUnreliably(object message)
		{
			PublishUnreliably(message, ChannelSerializerInfo.BinarySerializer);
		}

		public void PublishUnreliably(object message, ChannelSerializerInfo serializerInfo)
		{
			PublishUnreliably(message, serializerInfo, seqNumberTX);
		}

		public void PublishUnreliably(object message, ChannelSerializerInfo serializerInfo, int seqNum) {
				//send the stuff over udp
			MemoryStream stream = new MemoryStream();
			WriteHeader(stream, seqNumberTX);
			switch (serializerInfo)
			{
				case ChannelSerializerInfo.BinarySerializer :					
					if (this.formatter == null)	this.formatter = new BinaryFormatter(); //lazy init
					formatter.Serialize(stream, message);					
				break;
				case ChannelSerializerInfo.TestSerializer:
					TestSerializer.Serialize(stream, message);
					break;
				default:
					throw new NotImplementedException("Unsupported serializer for Publish: " + serializerInfo.ToString ());					
			}

			if (this.sock == null) BuildSocket();

			byte[] binary = stream.ToArray();
			if (binary.Length > 65530) throw new InvalidOperationException("The message attempted to be sent is too large. It is : " + binary.Length + " bytes.");
			this.sock.SendTo(binary, new IPEndPoint(this.ip, this.port));

			seqNumberTX++;
		}

		public void PublishReliably(object message) {
			//they better not call this with udp
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		public uint NoOfListeners {
			get {
				return (uint) this.listeners.Count;
			}
		}

		private void BuildSocket() {
			lock (this) {
				if (this.sock == null) {
					if (this.buf == null)
						this.buf = new byte[65536];
					this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					this.sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);
					this.sock.Bind(new IPEndPoint(IPAddress.Any, this.port));
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.ip));
					this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, this.sock);
					deathCount++;
					Console.WriteLine("New Channel " + this.name);
					Console.WriteLine("Death Count is " + deathCount);
					
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			lock (listeners)
			{
				try
				{
					if (sock != null)
					{
						sock.Close();
						sock = null;
					}
				}
				catch (ObjectDisposedException)
				{
					//silent
				}
			}
		}

		#endregion
	}
}
