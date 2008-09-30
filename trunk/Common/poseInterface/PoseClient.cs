using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common.Pose;

namespace UrbanChallenge.Pose {
	public class PoseAbsReceivedEventArgs : EventArgs {
		private PoseAbsData d;

		public PoseAbsReceivedEventArgs(PoseAbsData d) {
			this.d = d;
		}

		public PoseAbsData PoseAbsData {
			get { return d; }
		}
	}

	public class PoseRelReceivedEventArgs : EventArgs {
		private PoseRelData d;

		public PoseRelReceivedEventArgs(PoseRelData d) {
			this.d = d;
		}

		public PoseRelData PoseRelData {
			get { return d; }
		}
	}

	public class PoseClient {
		private IPAddress groupAddr;
		private ushort groupPort;

		private Socket s;
		private byte[] buf = new byte[4096];

		public event EventHandler<PoseAbsReceivedEventArgs> PoseAbsReceived;
		public event EventHandler<PoseRelReceivedEventArgs> PoseRelReceived;

		public PoseClient(IPAddress groupAddr, ushort groupPort) {
			this.groupAddr = groupAddr;
			this.groupPort = groupPort;
		}

		public void Start() {
			s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			IPAddress localAddr = IPAddress.Any;
			/*foreach (NetworkInterface nif in NetworkInterface.GetAllNetworkInterfaces()) {
				if (nif.SupportsMulticast && nif.OperationalStatus == OperationalStatus.Up) {
					IPInterfaceProperties props = nif.GetIPProperties();
					UnicastIPAddressInformationCollection addrs = props.UnicastAddresses;
					foreach (UnicastIPAddressInformation ipaddr in addrs) {
						byte[] bytes = ipaddr.Address.GetAddressBytes();
						if (bytes[0] == 192 && bytes[1] == 168 && bytes[2] == 1)
							localAddr = ipaddr.Address;
					}
				}
			}*/

			s.Bind(new IPEndPoint(localAddr, groupPort));
			s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(groupAddr));

			Console.WriteLine("starting pose listener");
			s.BeginReceive(buf, 0, buf.Length, SocketFlags.None, OnReadComplete, null);
		}

		private void OnReadComplete(IAsyncResult ar) {
			int bytesRead = 0;
			try {
				bytesRead = s.EndReceive(ar);
			}
			catch (Exception ex) {
				Console.WriteLine("error reading from pose port: " + ex.Message);
			}

			if (bytesRead > 0) {
				MemoryStream ms = new MemoryStream(buf, 0, bytesRead, false);
				BinaryReader br = new BinaryReader(ms);

				if (buf[0] == 1) {
					// v1 rel pose message
					// read the packet type
					br.ReadInt32();
					ParseV1RelPacket(br);
				}
				else if (buf[0] == 2) {
					// could be either a v1 abs message, or a v2 relative or absolute message
					// check to see if the second byte matches the v1 signature
					if (buf[1] == 0) {
						br.ReadInt32();
						// read the packet type from the stream so the position advances
						ParseV1AbsPacket(br);
					}
					else {
						// read the version from the stream
						byte version = br.ReadByte();
						// read the message type
						byte packet_type = br.ReadByte();
						// read the sequence number
						uint seq_num = br.ReadUInt32();

						// now we can do stuffs
						if (packet_type == 4) {
							// relative packet
							ParseV2RelPacket(br);
						}
						else if (packet_type == 5) {
							ParseV2AbsPacket(br);
						}
					}
				}
			}
			else {
				Console.WriteLine("no bytes read");
			}

			s.BeginReceive(buf, 0, buf.Length, SocketFlags.None, OnReadComplete, null);
		}

		private void ParseV1RelPacket(BinaryReader br) {
			PoseRelData d = new PoseRelData();
			int ts_secs = br.ReadInt32();
			int ts_ticks = br.ReadInt32();
			d.timestamp = new CarTimestamp(ts_secs, ts_ticks);
			d.dt = br.ReadDouble();

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 4; j++) {
					d.transform[i, j] = br.ReadDouble();
				}
			}

			if (PoseRelReceived != null) {
				PoseRelReceived(this, new PoseRelReceivedEventArgs(d));
			}
		}

		private void ParseV2RelPacket(BinaryReader br) {
			PoseRelData d = new PoseRelData();

			int ts_secs = br.ReadInt32();
			int ts_ticks = br.ReadInt32();
			d.timestamp = new CarTimestamp(ts_secs, ts_ticks);

			int flag = br.ReadInt32();
			// ignore flags for now

			d.dt = br.ReadDouble();

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 4; j++) {
					d.transform[i, j] = br.ReadDouble();
				}
			}

			if (PoseRelReceived != null) {
				PoseRelReceived(this, new PoseRelReceivedEventArgs(d));
			}
		}

		private void ParseV1AbsPacket(BinaryReader br) {
			PoseAbsData d = new PoseAbsData();
			int ts_secs = br.ReadInt32();
			int ts_ticks = br.ReadInt32();
			d.timestamp = new CarTimestamp(ts_secs, ts_ticks);
			
			d.yaw = br.ReadDouble();
			d.pitch = br.ReadDouble();
			d.roll = br.ReadDouble();

			d.ecef_px = br.ReadDouble();
			d.ecef_py = br.ReadDouble();
			d.ecef_pz = br.ReadDouble();

			d.veh_vx = br.ReadDouble();
			d.veh_vy = br.ReadDouble();
			d.veh_vz = br.ReadDouble();

			d.ecef_vx = br.ReadDouble();
			d.ecef_vy = br.ReadDouble();
			d.ecef_vz = br.ReadDouble();

			d.bax = br.ReadDouble();
			d.bay = br.ReadDouble();
			d.baz = br.ReadDouble();

			d.bwx = br.ReadDouble();
			d.bwy = br.ReadDouble();
			d.bwz = br.ReadDouble();

			d.gps_px = br.ReadDouble();
			d.gps_py = br.ReadDouble();
			d.gps_pz = br.ReadDouble();

			// skip the next 27 doubles, pose covariance
			for (int i = 0; i < 27; i++) {
				br.ReadDouble();
			}

			// check if there is still stuff at the end
			if (br.BaseStream.Length != br.BaseStream.Position) {
				d.hp_px = br.ReadDouble();
				d.hp_py = br.ReadDouble();
				d.hp_pz = br.ReadDouble();
			}

			if (br.BaseStream.Length != br.BaseStream.Position) {
				d.sep_heading = br.ReadDouble();
				d.sep_pitch = br.ReadDouble();
				d.sep_roll = br.ReadDouble();
			}

			// assume these flags are OK
			d.gps_pos_valid = true;
			d.hp_pos_valid = true;
			d.sep_att_valid = true;
			// assume HP correction mode
			d.correction_mode = PoseCorrectionMode.HP;

			if (PoseAbsReceived != null) {
				PoseAbsReceived(this, new PoseAbsReceivedEventArgs(d));
			}
		}

		private void ParseV2AbsPacket(BinaryReader br) {
			PoseAbsData d = new PoseAbsData();
			int ts_secs = br.ReadInt32();
			int ts_ticks = br.ReadInt32();

			d.timestamp = new CarTimestamp(ts_secs, ts_ticks);

			int flags = br.ReadInt32();
			d.correction_mode = (PoseCorrectionMode)(flags & 0x00000F00);
			d.gps_pos_valid = (flags & 0x00000010) != 0;
			d.sep_att_valid = (flags & 0x00000020) != 0;
			d.hp_pos_valid = (flags & 0x00000040) != 0;

			d.yaw = br.ReadDouble();
			d.pitch = br.ReadDouble();
			d.roll = br.ReadDouble();

			d.ecef_px = br.ReadDouble();
			d.ecef_py = br.ReadDouble();
			d.ecef_pz = br.ReadDouble();

			d.veh_vx = br.ReadDouble();
			d.veh_vy = br.ReadDouble();
			d.veh_vz = br.ReadDouble();

			d.ecef_vx = br.ReadDouble();
			d.ecef_vy = br.ReadDouble();
			d.ecef_vz = br.ReadDouble();

			d.bax = br.ReadDouble();
			d.bay = br.ReadDouble();
			d.baz = br.ReadDouble();

			d.bwx = br.ReadDouble();
			d.bwy = br.ReadDouble();
			d.bwz = br.ReadDouble();

			// skip the next 27 doubles, pose covariance
			for (int i = 0; i < 27; i++) {
				br.ReadDouble();
			}

			d.gps_px = br.ReadDouble();
			d.gps_py = br.ReadDouble();
			d.gps_pz = br.ReadDouble();

			d.hp_px = br.ReadDouble();
			d.hp_py = br.ReadDouble();
			d.hp_pz = br.ReadDouble();

			d.sep_heading = br.ReadDouble();
			d.sep_pitch = br.ReadDouble();
			d.sep_roll = br.ReadDouble();

			if (PoseAbsReceived != null) {
				PoseAbsReceived(this, new PoseAbsReceivedEventArgs(d));
			}
		}
	}
}
