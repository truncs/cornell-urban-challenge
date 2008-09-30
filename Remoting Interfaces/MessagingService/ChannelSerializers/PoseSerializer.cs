using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common;

namespace UrbanChallenge.MessagingService.ChannelSerializers {
	static class PoseSerializer {
		public static object Deserialize(Stream s, ChannelSerializerInfo serializerInfo, string channel_name) {
			BinaryReader reader = new BinaryReader(s);

			if (serializerInfo == ChannelSerializerInfo.PoseRelativeSerializer) {
				return ParseV2RelPacket(reader);
			}
			else if (serializerInfo == ChannelSerializerInfo.PoseAbsoluteSerializer) {
				return ParseV2AbsPacket(reader);
			}

			// unknown packet type
			return null;
		}

		private static object ParseV2RelPacket(BinaryReader br) {
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

			return d;
		}

		private static object ParseV2AbsPacket(BinaryReader br) {
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

			return d;
		}
	}
}
