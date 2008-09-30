using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;


namespace WatchdogCommunication
{
	public class WatchdogComm : IDisposable 
	{
		public event EventHandler<WatchdogMessageEventArgs <StartPublishMessage>> GotStartPublishMessage;
		public event EventHandler<WatchdogMessageEventArgs<StopPublishMessage>> GotStopPublishMessage;
		public event EventHandler<WatchdogMessageEventArgs<StartStopPublishMessageReply>> GotStartPublishMessageReply;
		public event EventHandler<WatchdogMessageEventArgs <WatchdogStatusMessage>> GotWatchdogStatusMessage;
		public event EventHandler<WatchdogMessageEventArgs<WatchdogTerminateMessage>> GotWatchdogTerminateMessage;
		public event EventHandler<WatchdogMessageEventArgs<CommandMessage>> GotCommandMessage;
		private byte[] Buf;
		private Socket Sock;

		private IPAddress MultIP = IPAddress.Parse("239.132.1.254");
		private int MultPort = 30254;
		private static BinaryFormatter serializer = new BinaryFormatter();

		public WatchdogComm()
		{
						
		}
		public void Init()
		{
			BuildSocket();
		}
		public void SendMessage<MessageType>(MessageType msg) where MessageType : WatchdogCommMessage 
		{
			if (this.Sock == null) return;
			try
			{
				MemoryStream ms = new MemoryStream();
				msg.senderName = Dns.GetHostName();
				serializer.Serialize(ms, msg);
				if (ms.ToArray().Length > 65000) Trace.WriteLine("WARNING: OVERSIZE MESSAGE!!!!");
				Sock.SendTo(ms.ToArray(), new IPEndPoint(MultIP, MultPort));
			}
			catch (SocketException)
			{
			}
		}
		

		private void BuildSocket()
		{
			
			lock (this)
			{
				if (this.Sock == null)
				{
					Trace.WriteLine("Rebuilding socket...");
					if (this.Buf == null)
						this.Buf = new byte[65536];
					this.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					this.Sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.Sock.Bind(new IPEndPoint(IPAddress.Any, this.MultPort));
					this.Sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.MultIP));
					this.Sock.BeginReceive(this.Buf, 0, this.Buf.Length, SocketFlags.None, ReceiveCallback, null);
				}
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			if (this.Sock == null) return;
			try
			{
				int bytesReceived = Sock.EndReceive(ar);
				if (bytesReceived > 0)
				{
					MemoryStream stream = new MemoryStream(this.Buf, 0, bytesReceived, false, true);
					ProcessStuff(stream.ToArray());
				}
			}
			catch (SocketException ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			Sock.BeginReceive(this.Buf, 0, this.Buf.Length, SocketFlags.None, ReceiveCallback, null);
		}

		void ProcessStuff(byte[] message)
		{
			MemoryStream ms = new MemoryStream(message);
			WatchdogCommMessage msg = (WatchdogCommMessage)serializer.Deserialize(ms);

			if (msg.GetName() == StartPublishMessage.GetClassName ())
			{
        if (GotStartPublishMessage!=null)
				GotStartPublishMessage(this, new WatchdogMessageEventArgs<StartPublishMessage> ((StartPublishMessage)msg));
			}
			if (msg.GetName() == StopPublishMessage.GetClassName())
			{
				if (GotStopPublishMessage != null)
					GotStopPublishMessage(this, new WatchdogMessageEventArgs<StopPublishMessage>((StopPublishMessage)msg));
			}
			else if (msg.GetName() == WatchdogStatusMessage.GetClassName())
			{
                if (GotWatchdogStatusMessage != null)
				GotWatchdogStatusMessage(this, new WatchdogMessageEventArgs<WatchdogStatusMessage>((WatchdogStatusMessage)msg));
			}
			else if (msg.GetName() == StartStopPublishMessageReply.GetClassName())
			{
				if (GotStartPublishMessageReply != null)
					GotStartPublishMessageReply(this, new WatchdogMessageEventArgs<StartStopPublishMessageReply>((StartStopPublishMessageReply)msg));
			}
			else if (msg.GetName() == WatchdogTerminateMessage.GetClassName())
			{
				if (GotWatchdogTerminateMessage != null)
					GotWatchdogTerminateMessage(this, new WatchdogMessageEventArgs<WatchdogTerminateMessage>((WatchdogTerminateMessage)msg));
			}
			else if (msg.GetName() == CommandMessage.GetClassName())
			{
				if (GotCommandMessage != null)
					GotCommandMessage(this, new WatchdogMessageEventArgs<CommandMessage>((CommandMessage)msg));
			}
		}
		public static string GetMachineName()
		{
			return Dns.GetHostName();
		}

		#region IDisposable Members

		public void Dispose()
		{
			try
			{
				KillSocket();
			}
			catch (ObjectDisposedException)
			{
				//silent
			}
		}

		#endregion

		public void KillSocket()
		{
			try
			{
				Sock.Close();
				Sock = null;
			}
			catch (ObjectDisposedException)
			{
				//silent
			}
		}
	}
}
