using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;

namespace CarBrowser.Micros {
	class MicroPowerStatusEventArgs : EventArgs {
		private bool[] enabled;
		private PowerState[] powerState;

		public MicroPowerStatusEventArgs(bool[] enabled, PowerState[] powerState) {
			this.enabled = enabled;
			this.powerState = powerState;
		}

		public bool GetEnabled(int port) {
			return enabled[port-1];
		}

		public PowerState GetPowerState(int port) {
			return powerState[port-1];
		}

		public int Count {
			get { return enabled.Length; }
		}
	}
	
	class MicroPowerInterface {
		public event EventHandler<MicroPowerStatusEventArgs> MicroPowerStatusReceived;

		private ISynchronizeInvoke syncInvoke;
		private UdpClient client;
		private IPEndPoint microPowerEndpoint;

		public MicroPowerInterface(IPEndPoint microPowerEndpoint, ISynchronizeInvoke syncInvoke) {
			try
			{
				this.syncInvoke = syncInvoke;
				this.client = new UdpClient(30010);
				this.client.JoinMulticastGroup(IPAddress.Parse("239.132.1.10"));
				this.client.BeginReceive(OnReceive, client);
				this.microPowerEndpoint = microPowerEndpoint;
			}
			catch (SocketException)
			{

			}
		}

		public void ResetPort(int port) {
			byte[] data = new byte[] { (byte)MicroMessageCodes.PowerReset, (byte)port };
			client.Send(data, 2, microPowerEndpoint);
		}

		public void SetPortEnabled(int port, bool enabled) {
			byte[] data = new byte[] { (byte)MicroMessageCodes.PowerEnable, (byte)port, (byte)(enabled ? 1 : 0) };
			client.Send(data, 3, microPowerEndpoint);
		}

		public void SetEnabledAll(bool enabled) {
			byte[] data = new byte[] { (byte)MicroMessageCodes.PowerEnableAll, (byte)(enabled ? 1 : 0) };
			client.Send(data, 2, microPowerEndpoint);
		}

		public void FlashConfig() {
			byte[] data = new byte[] { (byte)MicroMessageCodes.PowerSave };
			client.Send(data, 1, microPowerEndpoint);
		}

		private void OnReceive(IAsyncResult ar) {
			try {
				UdpClient udp = (UdpClient)ar.AsyncState;
				IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = udp.EndReceive(ar, ref endpoint);

				if (data[0] == 0) {
					int n_dev = 20;
					bool[] enabled = new bool[n_dev];
					PowerState[] powerState = new PowerState[n_dev];
					for (int i = 0; i < n_dev; i++) {
						enabled[i] = (data[i+1] & 0x80) != 0;
						powerState[i] = (PowerState)(data[i+1] & 0x7f);
					}

					OnStatusReceived(new MicroPowerStatusEventArgs(enabled, powerState));
				}

				udp.BeginReceive(OnReceive, udp);
			}
			catch (ObjectDisposedException) {
			}
		}

		private void OnStatusReceived(MicroPowerStatusEventArgs e) {
			try {
				if (MicroPowerStatusReceived != null) {
					if (syncInvoke != null) {
						syncInvoke.Invoke(MicroPowerStatusReceived, new object[] { this, e });
					}
					else {
						MicroPowerStatusReceived(this, e);
					}
				}
			}
			catch (Exception ex) {
				Debug.WriteLine("exception in power callback: \n" + ex.Message);
			}
		}
	}
}
