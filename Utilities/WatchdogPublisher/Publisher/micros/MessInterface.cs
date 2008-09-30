using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Net.Sockets;
using System.Net;
using UrbanChallenge.Common.Utility;

namespace CarBrowser.Micros {
	enum MessClass {
		Startup = 0,
		FatalError = 1,
		Warning = 2,
		Info = 3,
		NameDecl = 4
	}

	enum MessSourceType {
		PC = 0,
		NetMCU = 1,
		OtherMCU = 2
	}

	class MessPacket {
		private DateTime localTime;
		private CarTimestamp ts;
		private MessClass messClass;
		private int packetType;
		private MessSourceType sourceType;

		private bool messageIsHex;
		private string stringPayload;
		private byte[] rawPayload;

		public MessPacket(byte[] packet, int length) {
			localTime = HighResDateTime.Now;

			bool hasTimestamp = (packet[0] & 0x40) != 0;
			messageIsHex = (packet[0] & 0x20) != 0;
			messClass = (MessClass)((packet[0] >> 3) & 0x3);
			packetType = packet[0] & 0x7;

			if (messClass == MessClass.Info && packetType == 0x7) {
				messClass = MessClass.NameDecl;
			}

			if ((packet[1] & 0x80) == 0) {
				sourceType = MessSourceType.PC;
			}
			else {
				if ((packet[1] & 0x40) == 0) {
					sourceType = MessSourceType.NetMCU;
				}
				else {
					sourceType = MessSourceType.OtherMCU;
				}
			}

			int payloadOffset = 2;
			if (hasTimestamp) {
				payloadOffset = 8;

				int secs = ((int)packet[2] << 8) + packet[3];
				int ticks = ((int)packet[4] << 24) + ((int)packet[5] << 16) + ((int)packet[6] << 8) + ((int)packet[7]);

				ts = new CarTimestamp(secs, ticks);
			}
			else {
				ts = CarTimestamp.Invalid;
			}

			rawPayload = new byte[length-payloadOffset];
			Buffer.BlockCopy(packet, payloadOffset, rawPayload, 0, rawPayload.Length);

			if (messageIsHex) {
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < rawPayload.Length; i++) {
					sb.Append(rawPayload[i].ToString("x2"));

					if (i < rawPayload.Length-1) {
						sb.Append(' ');
					}
				}

				stringPayload = sb.ToString();
			}
			else {
				stringPayload = Encoding.ASCII.GetString(rawPayload);
			}
		}

		public CarTimestamp Timestamp {
			get { return ts; }
		}

		public MessClass MessClass {
			get { return messClass; }
		}

		public int PacketType {
			get { return packetType; }
		}

		public MessSourceType SourceType {
			get { return sourceType; }
		}

		public bool MessageIsHex {
			get { return messageIsHex; }
		}

		public string PayloadString {
			get { return stringPayload; }
		}

		public byte[] RawPayload {
			get { return rawPayload; }
		}
	}

	class MessPacketReceivedEventArgs : EventArgs {
		private MessPacket packet;

		public MessPacketReceivedEventArgs(MessPacket packet) {
			this.packet = packet;
		}

		public MessPacket Packet {
			get { return packet; }
		}
	}

	class MessInterface {
		public event EventHandler<MessPacketReceivedEventArgs> MessPacketReceived;

		private Socket socket;
		private byte[] buf;
		private EndPoint receiveEndpoint;

		public MessInterface() {
			this.receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

			this.buf = new byte[65536];
			
			receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.Bind(new IPEndPoint(IPAddress.Any, 30040));
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.132.1.40")));
			socket.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref receiveEndpoint, ReceiveCallback, socket);
		
		}

		private void ReceiveCallback(IAsyncResult ar) {
			try {
				Socket socket = (Socket)ar.AsyncState;
				SocketFlags sf = SocketFlags.None;
				IPPacketInformation packetInfo;
				int bytesRecevied = socket.EndReceiveMessageFrom(ar, ref sf, ref receiveEndpoint, out packetInfo);
				if (bytesRecevied >= 2) {
					OnMessPacketReceived(buf, bytesRecevied);
				}

				receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
				socket.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref receiveEndpoint, ReceiveCallback, socket);
			}
			catch (Exception) {

			}
		}

		private void OnMessPacketReceived(byte[] data, int length) {
			try {
				if (MessPacketReceived != null) {
					MessPacketReceived(this, new MessPacketReceivedEventArgs(new MessPacket(data, length)));
				}
			}
			catch (Exception) {
			}
		}

	}
}
