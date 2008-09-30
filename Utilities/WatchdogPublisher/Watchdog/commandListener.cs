using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Watchdog
{
	public class CommandListener
	{
		private byte[] Buf;
		private Socket Sock;

		private IPAddress MultIP = IPAddress.Parse("239.132.1.254");
		private int RXPort = 30254;

		private void BuildSocket()
		{
			lock (this)
			{
				if (this.Sock == null)
				{
					if (this.Buf == null)
						this.Buf = new byte[65536];
					this.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					this.Sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.Sock.Bind(new IPEndPoint(IPAddress.Any, this.RXPort));
					this.Sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.MultIP));
					this.Sock.BeginReceive(this.Buf, 0, this.Buf.Length, SocketFlags.None, ReceiveCallback, null);
				}
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int bytesReceived = Sock.EndReceive(ar);
				if (bytesReceived > 0)
				{
					MemoryStream stream = new MemoryStream(this.Buf, 0, bytesReceived, false, true);
					ProcessShit(stream.ToArray());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			Sock.BeginReceive(this.Buf, 0, this.Buf.Length, SocketFlags.None, ReceiveCallback, null);
		}

		void ProcessShit(byte[] message)
		{

		}
	}
}
