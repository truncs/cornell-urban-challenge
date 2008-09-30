using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Pose {
	public enum PoseCorrectionMode {
		None=0x00000100,
		WAAS=0x00000200,
		VBS=0x00000400,
		HP=0x00000800
	}

	[Serializable]
	public class PoseAbsData {
		public CarTimestamp timestamp;

		public double yaw, pitch, roll;
		public double ecef_px, ecef_py, ecef_pz;
		public double veh_vx, veh_vy, veh_vz;
		public double ecef_vx, ecef_vy, ecef_vz;

		public double bax, bay, baz;
		public double bwx, bwy, bwz;

		public Matrix3 ypr_cov;
		public Matrix3 ecef_pos_cov;
		public Matrix3 ecef_vel_cov;

		public bool gps_pos_valid;
		public double gps_px, gps_py, gps_pz;

		public bool hp_pos_valid;
		public double hp_px, hp_py, hp_pz;

		public bool sep_att_valid;
		public double sep_heading, sep_pitch, sep_roll;

		public PoseCorrectionMode correction_mode;
	}
}
