using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using UrbanChallenge.MessagingService;

namespace CarBrowser.Channels {
	public enum ChannelType {
		UDPChannel,
		Unknown
	}

	class ChannelListener : IDisposable {
		public class ChannelSender {
			private IPEndPoint endpoint;
			private int receivedPacketCount;
			private int missedPacketCount;
			private int lastSeqNumber;

			private string hostname;

			public ChannelSender(IPEndPoint endpoint) {
				this.endpoint = endpoint;
				this.receivedPacketCount = 0;
				this.missedPacketCount = 0;
				this.lastSeqNumber = int.MinValue;

				Dns.BeginGetHostEntry(endpoint.Address, OnDnsResolved, null);
			}

			private void OnDnsResolved(IAsyncResult ar) {
				try {
					IPHostEntry hostEntry = Dns.EndGetHostEntry(ar);
					hostname = hostEntry.HostName;
				}
				catch (Exception) {
				}
			}

			public IPEndPoint Endpoint {
				get { return endpoint; }
			}

			public string Hostname {
				get { return hostname; }
			}

			public void OnPacketReceived(int seqNumber) {
				receivedPacketCount++;
				if (seqNumber == int.MinValue)
					return;

				if (lastSeqNumber != int.MinValue && seqNumber > lastSeqNumber) {
					if (lastSeqNumber + 1 != seqNumber) {
						missedPacketCount += seqNumber - lastSeqNumber - 1;
					}
				}

				lastSeqNumber = seqNumber;
			}

			public double DropRate {
				get {
					if (receivedPacketCount > 0 || missedPacketCount > 0) {
						return missedPacketCount / (double)(receivedPacketCount + missedPacketCount);
					}
					else {
						return 0;
					}
				}
			}

			public bool HasSeqNumber {
				get { return lastSeqNumber != int.MinValue; }
			}

			public int TotalPacketCount {
				get { return receivedPacketCount + missedPacketCount; }
			}

			public int ReceivedPacketCount {
				get { return receivedPacketCount; }
			}

			public int MissedPacketCount {
				get { return missedPacketCount; }
			}
		}

		private IPEndPoint endpoint;
		private ChannelType channelType;
		private Socket socket;
		private byte[] buf;
		private EndPoint receiveEndpoint;

		private bool hasData;
		private int packetCount;
		private int byteCount;
		private Stopwatch startTime;

		private List<ChannelSender> senders;

		private object lockobj = new object();

		public ChannelListener(IPEndPoint endpoint, ChannelType channelType) {
			this.endpoint = endpoint;
			this.channelType = channelType;
			this.senders = new List<ChannelSender>();

			this.receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

			if (this.buf == null)
				this.buf = new byte[65536];
		}

		public bool Listening {
			get { return socket != null; }
		}

		public bool HasData {
			get { return hasData; }
		}

		public void ResetStats() {
			lock (lockobj) {
				this.packetCount = 0;
				this.byteCount = 0;
				this.startTime = Stopwatch.StartNew();
				this.senders.Clear();

				if (socket == null) {
					hasData = false;
				}
			}
		}

		public void StartListening() {
			lock (lockobj) {
				ResetStats();

				hasData = true;

				if (socket == null) {
					receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

					socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					socket.Bind(new IPEndPoint(IPAddress.Any, this.endpoint.Port));
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.endpoint.Address));
					socket.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref receiveEndpoint, ReceiveCallback, socket);
				}
			}
		}

		public void StopListening() {
			lock (lockobj) {
				if (socket != null) {
					socket.Close();
					socket = null;
				}

				if (startTime != null) {
					startTime.Stop();
				}
			}
		}

		public double PacketRate {
			get {
				if (startTime == null) {
					return 0;
				}
				return packetCount / startTime.Elapsed.TotalSeconds; 
			}
		}

		public double ByteRate {
			get {
				if (startTime == null) {
					return 0;
				}
				return byteCount / startTime.Elapsed.TotalSeconds; 
			}
		}

		public string GetSendersString() {
			StringBuilder sb = new StringBuilder(senders.Count * 25);
			lock (lockobj) {
				for (int i = 0; i < senders.Count; i++) {
					double receiveRate = 1-senders[i].DropRate;
					string senderAddr = senders[i].Hostname;
					if (string.IsNullOrEmpty(senderAddr) || string.IsNullOrEmpty(senderAddr.Trim())) {
						senderAddr = senders[i].Endpoint.Address.ToString();
					}

					if (senders[i].HasSeqNumber) {
						sb.AppendFormat("{0} ({1:P0})", senderAddr, receiveRate);
					}
					else {
						sb.AppendFormat("{0}", senderAddr);
					}
					if (i != senders.Count-1) {
						sb.Append(", ");
					}
				}
			}

			return sb.ToString();
		}

		private void ReceiveCallback(IAsyncResult ar) {
			// end the receive
			Socket sock = (Socket)ar.AsyncState;
			try {
				SocketFlags sf = SocketFlags.None;
				IPPacketInformation packetInfo;
				int bytes = sock.EndReceiveMessageFrom(ar, ref sf, ref receiveEndpoint, out packetInfo);

				lock (lockobj) {
					IPEndPoint ep = receiveEndpoint as IPEndPoint;
					if (ep != null) {
						ChannelSender sender = null;
						for (int i = 0; i < senders.Count; i++) {
							if (senders[i].Endpoint.Equals(ep)) {
								sender = senders[i];
								break;
							}
						}

						if (sender == null) {
							sender = new ChannelSender(ep);
							senders.Add(sender);
						}

						// parse out the version, serialization type, sequence number
						int seqNumber = int.MinValue;
						switch (channelType) {
							case ChannelType.UDPChannel:
								ParseUDPChannelPacket(buf, bytes, ref seqNumber);
								break;
						}
						sender.OnPacketReceived(seqNumber);

						packetCount++;
						byteCount += bytes;
					}
				}

				receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
				sock.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref receiveEndpoint, ReceiveCallback, sock);
			}
			catch (Exception) {
			}
		}

		private void ParseUDPChannelPacket(byte[] buffer, int length, ref int seqNumber) {
			if (length < 6) {
				return;
			}

			byte ver = buffer[0];
			if (ver != UDPChannel.versionNumber) {
				return;
			}

			byte serType = buffer[1];
			seqNumber = BitConverter.ToInt32(buffer, 2);
		}

		#region IDisposable Members

		public void Dispose() {
			StopListening();
		}

		#endregion
	}
}
