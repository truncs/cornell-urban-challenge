using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace LN200
{
	
	public delegate void GotLN200PacketDel(object sender, LN200PacketEventArgs e);

	public class LN200PacketEventArgs : EventArgs
	{
		public double time;		
		public LN200PacketEventArgs(double time)
		{
			this.time = time;			
		}
	}	

	public class LN200InterfaceRX
	{
		public event GotLN200PacketDel GotLN200Packet;					
		
		private IPAddress multIP = IPAddress.Parse("239.132.1.4");	
		private int multPort = 92;

			
		private byte[] buf;
		private Socket sock;

		private void BuildSocket()
		{
			lock (this)
			{				
				if (this.sock == null)
				{
					if (this.buf == null)
						this.buf = new byte[65536];
					this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					this.sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.sock.Bind(new IPEndPoint(IPAddress.Any, this.multPort));
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.multIP));
					this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
				}
			}
		}

		public LN200InterfaceRX()
		{			
			BuildSocket();											
		}		

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int bytesReceived = this.sock.EndReceive(ar);
				if (bytesReceived > 0)
				{
					MemoryStream stream = new MemoryStream(this.buf, 0, bytesReceived, false, true);
					Processstuff(stream.ToArray ());					
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
		}
	
		void Processstuff(byte[] message)
		{
			double time = 0;
			
				//format is SEQ(2) TIMESTAMP(6) , FLAG (1), DATA (28)
				if (message.Length != 37)
				{
					Console.WriteLine("stuff! LN200 Wrong Length!");
					return;
				}

				BinaryReader br = new BinaryReader(new MemoryStream(message));
				UInt16 seq = BigToLittle(br.ReadUInt16());	
				UInt16 secs = BigToLittle(br.ReadUInt16());
				UInt32 ticks = BigToLittle(br.ReadUInt32());


				time = (double)secs + ((double)ticks / 10000.0);

				if (GotLN200Packet != null)
					GotLN200Packet(this, new LN200PacketEventArgs(time));
		}


		#region bigtolittle
		public static UInt32 BigToLittle(UInt32 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToUInt32(stuffness, 0);
		}
		public static UInt16 BigToLittle(UInt16 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToUInt16(stuffness, 0);
		}
		public static Int16 BigToLittle(Int16 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToInt16(stuffness, 0);
		}
		#endregion
	}
	

}
