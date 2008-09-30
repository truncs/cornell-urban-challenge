using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.ComponentModel;
using UrbanChallenge.Common.Utility;

namespace CarBrowser.Micros {
	class MicroEventArgs : EventArgs {
		private bool boolResult;
		private MicroTimestamp tsResult;
		private IPAddress address;
		private bool timedOut;

		public MicroEventArgs(bool boolResult, IPAddress address) {
			this.boolResult = boolResult;
			this.address = address;
			this.timedOut = false;
		}

		public MicroEventArgs(MicroTimestamp ts, IPAddress address) {
			this.tsResult = ts;
			this.address = address;
			this.timedOut = false;
			this.boolResult = false;
		}

		public MicroEventArgs(IPAddress address) {
			this.address = address;
			this.timedOut = true;
		}

		public bool BoolResult {
			get { return boolResult; }
		}

		public MicroTimestamp TimestampResult {
			get { return tsResult; }
		}

		public IPAddress Address {
			get { return address; }
		}

		public bool TimedOut {
			get { return timedOut; }
		}
	}
	
	class MicroTimingInterface {
		private class PendingOp {
			public DateTime startTime;
			public MicroMessageCodes messageCode;
			public bool cancelled;

			public PendingOp(MicroMessageCodes messageCode) {
				this.messageCode = messageCode;
				this.startTime = DateTime.Now;
				this.cancelled = false;
			}
		}

		private UdpClient client;
		private Timer timer;
		private ISynchronizeInvoke syncInvoke;

		private Dictionary<IPAddress, PendingOp> pendingOps = new Dictionary<IPAddress, PendingOp>();

		public event EventHandler<MicroEventArgs> TimestampReceived;
		public event EventHandler<MicroEventArgs> PulseReceived;
		public event EventHandler<MicroEventArgs> SyncReceived;
		public event EventHandler<MicroEventArgs> ModeReceived;
		public event EventHandler<MicroEventArgs> ResyncAcknowledged;

		public MicroTimingInterface(ISynchronizeInvoke syncInvoke) {
			this.syncInvoke = syncInvoke;
			try
			{
				client = new UdpClient(20);
				client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				client.BeginReceive(OnReceive, client);
			}
			catch { }
			timer = new Timer(TimerProc, null, 1000, 1000);
		}

		public bool BeginGetTimestamp(IPAddress address) {
			lock (pendingOps) {
				if (pendingOps.ContainsKey(address) && pendingOps[address] != null) {
					return false;
				}
				else {
					SendCommand(MicroMessageCodes.GetTimestamp, address);
					return true;
				}
			}
		}

		public bool BeginGetPulse(IPAddress address) {
			lock (pendingOps) {
				if (pendingOps.ContainsKey(address) && pendingOps[address] != null) {
					return false;
				}
				else {
					SendCommand(MicroMessageCodes.TimingPulse, address);
					return true;
				}
			}
		}

		public bool BeginGetSync(IPAddress address) {
			lock (pendingOps) {
				if (pendingOps.ContainsKey(address) && pendingOps[address] != null) {
					return false;
				}
				else {
					SendCommand(MicroMessageCodes.TimingSync, address);
					return true;
				}
			}
		}

		public bool BeginGetMode(IPAddress address) {
			lock (pendingOps) {
				if (pendingOps.ContainsKey(address) && pendingOps[address] != null) {
					return false;
				}
				else {
					SendCommand(MicroMessageCodes.TimingMode, address);
					return true;
				}
			}
		}

		public bool BeginCommandResync(IPAddress address) {
			lock (pendingOps) {
				if (pendingOps.ContainsKey(address) && pendingOps[address] != null) {
					return false;
				}
				else {
					SendCommand(MicroMessageCodes.TimingResync, address);
					return true;
				}
			}
		}

		private void SendCommand(MicroMessageCodes code, IPAddress address) {
			pendingOps[address] = new PendingOp(code);
			client.Send(new byte[] { (byte)code }, 1, new IPEndPoint(address, 20));
		}

		public bool AnyPendingOps(IPAddress address) {
			PendingOp pendingOp;
			lock (pendingOps) {
				if (pendingOps.TryGetValue(address, out pendingOp) && pendingOp != null && !pendingOp.cancelled) {
					return true;
				}
			}

			return false;
		}

		public bool AnyPendingOps() {
			lock (pendingOps) {
				foreach (KeyValuePair<IPAddress, PendingOp> op in pendingOps) {
					if (op.Value != null && !op.Value.cancelled) {
						return true;
					}
				}

				return false;
			}
		}

		private void TimerProc(object obj) {
			List<KeyValuePair<IPAddress, PendingOp>> timedOut = new List<KeyValuePair<IPAddress, PendingOp>>();
			lock (pendingOps) {
				DateTime now = DateTime.Now;
				foreach (KeyValuePair<IPAddress, PendingOp> op in pendingOps) {
					if (op.Value != null) {
						if (now - op.Value.startTime > TimeSpan.FromSeconds(1)) {
							timedOut.Add(op);
						}
					}
				}

				foreach (KeyValuePair<IPAddress, PendingOp> op in timedOut) {
					pendingOps.Remove(op.Key);
				}
			}

			foreach (KeyValuePair<IPAddress, PendingOp> op in timedOut) {
				switch (op.Value.messageCode) {
					case MicroMessageCodes.GetTimestamp:
						RaiseTimestampEvent(op.Key, new MicroTimestamp(), false);
						break;

					case MicroMessageCodes.TimingMode:
						RaiseBoolEvent(op.Key, false, false, ModeReceived);
						break;

					case MicroMessageCodes.TimingPulse:
						RaiseBoolEvent(op.Key, false, false, PulseReceived);
						break;

					case MicroMessageCodes.TimingResync:
						RaiseBoolEvent(op.Key, false, false, ResyncAcknowledged);
						break;

					case MicroMessageCodes.TimingSync:
						RaiseBoolEvent(op.Key, false, false, SyncReceived);
						break;
				}
			}
		}

		private void OnReceive(IAsyncResult ar) {
			UdpClient udp = (UdpClient)ar.AsyncState;
			try {
				IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = udp.EndReceive(ar, ref endpoint);

				// check if we have any pending operations
				PendingOp op = null;
				lock (pendingOps) {
					pendingOps.TryGetValue(endpoint.Address, out op);
					pendingOps[endpoint.Address] = null;
				}

				if (op != null && !op.cancelled) {
					// figure out what this message is
					if (op.messageCode == MicroMessageCodes.GetTimestamp && data.Length == 8) {
						// we have a value message, parse 
						BigEndianBinaryReader reader = new BigEndianBinaryReader(data);
						reader.ReadUInt16();
						ushort secs = reader.ReadUInt16();
						int ticks = reader.ReadInt32();
						RaiseTimestampEvent(endpoint.Address, new MicroTimestamp(secs, ticks), true);
					}
					else if (op.messageCode == MicroMessageCodes.TimingMode && data.Length == 3) {
						bool server = data[2] == 1;
						RaiseBoolEvent(endpoint.Address, server, true, ModeReceived);
					}
					else if (op.messageCode == MicroMessageCodes.TimingPulse && data.Length == 3) {
						bool result = data[2] == 1;
						RaiseBoolEvent(endpoint.Address, result, true, PulseReceived);
					}
					else if (op.messageCode == MicroMessageCodes.TimingResync && data.Length == 3) {
						bool result = data[2] == 0;
						RaiseBoolEvent(endpoint.Address, result, true, ResyncAcknowledged);
					}
					else if (op.messageCode == MicroMessageCodes.TimingSync && data.Length == 3) {
						bool result = data[2] == 1;
						RaiseBoolEvent(endpoint.Address, result, true, SyncReceived);
					}
				}

				udp.BeginReceive(OnReceive, udp);
			}
			catch (ObjectDisposedException) {
			}
		}

		private void RaiseTimestampEvent(IPAddress address, MicroTimestamp ts, bool success) {
			if (TimestampReceived != null) {
				MicroEventArgs e;
				if (success){
					e = new MicroEventArgs(ts, address);
				}
				else {
					e = new MicroEventArgs(address);
				}
				if (syncInvoke != null) {
					syncInvoke.Invoke(TimestampReceived, new object[] { this, e });
				}
				else {
					TimestampReceived(this, e);
				}
			}
		}

		private void RaiseBoolEvent(IPAddress address, bool result, bool success, EventHandler<MicroEventArgs> ev) {
			if (ev != null) {
				MicroEventArgs e;
				if (success) {
					e = new MicroEventArgs(result, address);
				}
				else {
					e = new MicroEventArgs(address);
				}

				if (syncInvoke != null) {
					syncInvoke.Invoke(ev, new object[] { this, e });
				}
				else {
					ev(this, e);
				}
			}
		}
	}
}
