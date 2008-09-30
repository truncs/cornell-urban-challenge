using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace CarBrowser.Micros {
	class SocketDataReceivedEventArgs {
		private IPEndPoint source;
		private IPEndPoint dest;
		private byte[] data;
		private int bytesRead;

		public SocketDataReceivedEventArgs(IPEndPoint source, IPEndPoint dest, byte[] data, int bytesRead) {
			this.source = source;
			this.dest = dest;
			this.data = data;
			this.bytesRead = bytesRead;
		}

		public IPEndPoint Source {
			get { return source; }
		}

		public IPEndPoint Destination {
			get { return dest; }
		}

		public byte[] Data {
			get { return data; }
		}

		public int BytesRead {
			get { return bytesRead; }
		}
	}

	delegate void SocketDataReceivedEventHandler(SocketDataReceivedEventArgs e);

	class SharedSocket {
		private class AddressEntry {
			// set to any to receive from all source address
			public IPAddress sourceAddress;
			// set to any to receive for all destination address
			// set to loopback to receive for the local address only
			public IPAddress destAddress;
			public SocketDataReceivedEventHandler callback;

			public AddressEntry(IPAddress sourceAddress, IPAddress destAddress, SocketDataReceivedEventHandler callback) {
				this.sourceAddress = sourceAddress;
				this.destAddress = destAddress;
				this.callback = callback;
			}
		}

		private Socket socket;
		private int port;
		private List<AddressEntry> addressEntries;
		private EndPoint receiveEndpoint;
		private byte[] buffer;

		public SharedSocket(int port) {
			addressEntries = new List<AddressEntry>();

			this.port = port;

			buffer = new byte[65535];
			receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.Bind(new IPEndPoint(IPAddress.Any, port));
			socket.BeginReceiveMessageFrom(buffer, 0, buffer.Length, SocketFlags.None, ref receiveEndpoint, OnDataReceived, null);
		}

		// set source to any to receive from any source address, otherwise will only report those source addresses that match
		// set dest to any to receive data for any destination address, localhost to get only packets destined to the local
		//   unicast address, set to any other address (including multicast, broadcast) to get packets for only that destination
		public void Subscribe(IPAddress sourceAddress, IPAddress destAddress, SocketDataReceivedEventHandler callback) {
			AddressEntry entry = new AddressEntry(sourceAddress, destAddress, callback);

			lock (addressEntries) {
				// determine if this is a multicast destination
				if (IsMulticast(destAddress)) {
					// it is, so join the group if we aren't already in it
					bool inGroup = false;
					foreach (AddressEntry ent in addressEntries) {
						if (ent.destAddress.Equals(destAddress)) {
							inGroup = true;
						}
					}

					if (!inGroup) {
						socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(destAddress));
					}
				}

				if (IsBroadcast(destAddress)) {
					if (!socket.EnableBroadcast) {
						socket.EnableBroadcast = true;
					}
				}

				// add to the list of address entries
				addressEntries.Add(entry);
			}
		}

		// returns true if this was the last entry
		public void Unsubscribe(IPAddress sourceAddress, IPAddress destAddress, SocketDataReceivedEventHandler callback) {
			lock (addressEntries) {
				int addressCount = 0;
				bool isMulticast = IsMulticast(destAddress);
				bool isBroadcast = IsBroadcast(destAddress);
				bool isSpecialAddress = isMulticast || isBroadcast;

				int removeIndex = -1;

				for (int i = 0; i < addressEntries.Count; i++) {
					AddressEntry entry = addressEntries[i];
					if (entry.callback.Equals(callback) && entry.sourceAddress.Equals(sourceAddress) && entry.destAddress.Equals(destAddress)) {
						removeIndex = i;
					}
					else {
						if (isSpecialAddress && entry.destAddress.Equals(destAddress)) {
							addressCount++;
						}
					}
				}

				if (removeIndex != -1) {
					addressEntries.RemoveAt(removeIndex);
					if (isMulticast && addressCount == 0) {
						// remove the multicast address
						socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(destAddress));
					}
					else if (isBroadcast && addressCount == 0) {
						socket.EnableBroadcast = false;
					}
				}
			}
		}

		private void OnDataReceived(IAsyncResult ar) {
			bool doBeginReceive = true;
			try {
				SocketFlags flags = SocketFlags.None;
				IPPacketInformation packetInfo;
				int bytesRead = socket.EndReceiveMessageFrom(ar, ref flags, ref receiveEndpoint, out packetInfo);

				IPEndPoint fromEndpoint = receiveEndpoint as IPEndPoint;
				IPAddress destAddress = packetInfo.Address;

				SocketDataReceivedEventArgs e = new SocketDataReceivedEventArgs(fromEndpoint, new IPEndPoint(destAddress, port), buffer, bytesRead);

				lock (addressEntries) {
					foreach (AddressEntry entry in addressEntries) {
						if (CompareSourceIP(entry.sourceAddress, fromEndpoint.Address) && CompareDestIP(entry.destAddress, destAddress)) {
							entry.callback(e);
						}
					}
				}
			}
			catch (ObjectDisposedException) {
				doBeginReceive = false;
			}

			if (doBeginReceive) {
				int tryCount = 0;
				while (tryCount < 3) {
					try {
						receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
						socket.BeginReceiveMessageFrom(buffer, 0, buffer.Length, SocketFlags.None, ref receiveEndpoint, OnDataReceived, null);
						break;
					}
					catch (Exception) {
					}
					tryCount++;
				}
			}
		}

		private bool CompareSourceIP(IPAddress source, IPAddress test) {
			if (source.Equals(IPAddress.Any)) {
				return true;
			}
			else {
				return source.Equals(test);
			}
		}

		private bool CompareDestIP(IPAddress dest, IPAddress test) {
			if (dest.Equals(IPAddress.Any)) {
				return true;
			}
			else if (dest.Equals(IPAddress.Loopback)) {
				return IsUnicast(test);
			}
			else {
				return dest.Equals(test);
			}
		}

		private bool IsUnicast(IPAddress address) {
			byte[] addrBytes = address.GetAddressBytes();
			return ((addrBytes[0] & 0xf0) != 0xe0) && (addrBytes[3] != 0xff);
		}

		private bool IsMulticast(IPAddress address) {
			byte[] addrBytes = address.GetAddressBytes();
			return (addrBytes[0] & 0xf0) == 0xe0;
		}

		private bool IsBroadcast(IPAddress address) {
			byte[] addrBytes = address.GetAddressBytes();
			return addrBytes[3] == 0xff;
		}
	}
}
