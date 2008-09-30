#ifndef _POSE_MESSAGE_H
#define _POSE_MESSAGE_H

#define POSE_REL_PACKET_V1 1
#define POSE_ABS_PACKET_V1 2

#define POSE_REL_PACKET_V2 4
#define POSE_ABS_PACKET_V2 5

#define POSE_MSG_VERSION								2

#define POSE_MSG_VALID_FLAG							0x00000001

#define POSE_REL_RESET_FLAG							0x00000002

#define POSE_ABS_SEP_POS_VALID_FLAG			0x00000010
#define POSE_ABS_SEP_ATT_VALID_FLAG			0x00000020
#define POSE_ABS_TRIM_POS_VALID_FLAG		0x00000040

#define POSE_ABS_CORR_MASK              0x00000F00
#define POSE_ABS_CORR_NONE     					0x00000100
#define POSE_ABS_CORR_WAAS     					0x00000200
#define POSE_ABS_CORR_VBS     					0x00000400
#define POSE_ABS_CORR_HP     						0x00000800

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#pragma pack(push,1)

/****************************************************************
 * VERSION 1 MESSAGES                                           *
 ****************************************************************/

struct pose_rel_msg_v1 {
	// type of packet
	int packet_type;

	// ending timestamp (i.e. what time the transformation takes you to)
	int car_ts_secs;
	// in 10000ths of second
	int car_ts_ticks;

	// integration time
	double dt;

	// accumulated transformation matrix
	// stored in row major order
	double Rinit2veh[4][4];
};

struct pose_abs_msg_v1 {
	// type of packet
	int packet_type;

	// timestamp of pose data
	int car_ts_secs;
	int car_ts_ticks;

	// yaw, pitch, roll (in vehicle frame, rad)
	double yaw, pitch, roll;

	// position (ecef, m)
	double px, py, pz;

	// velocity (in vehicle frame, m/s)
	double veh_vx, veh_vy, veh_vz;

	// velocity (in ecef frame, m/s)
	double ecef_vx, ecef_vy, ecef_vz;

	// accelerometer biases
	double bax, bay, baz;
	// gyro biases
	double bwx, bwy, bwz;

	// last reported gps position
	double gps_px, gps_py, gps_pz;

	// ypr covariance matrix
	double cov_ypr[3][3];

	// position covariance matrix
	double cov_pos[3][3];

	// velocity covariance matrix
	double cov_vel[3][3];

	double hp_px, hp_py, hp_pz;
	double sep_heading, sep_pitch, sep_roll;
};

/****************************************************************
 * VERSION 2 MESSAGES                                           *
 ****************************************************************/

struct pose_rel_msg {
	// version of message
	unsigned char version;

	// type of message
	unsigned char packet_type;

	// sequence number of message
	unsigned int seq_num;

	// ending timestamp (i.e. what time the transformation takes you to)
	int car_ts_secs;
	// in 10000ths of second
	int car_ts_ticks;

	// validity flags
	unsigned int flags;

	// integration time
	double dt;

	// accumulated transformation matrix
	// stored in row major order
	double Rinit2veh[4][4];
};

struct pose_abs_msg {
	// version of message
	unsigned char version;

	// type of message
	unsigned char packet_type;

	// sequence number of message
	unsigned int seq_num;

	// timestamp of pose data
	int car_ts_secs;
	int car_ts_ticks;

	// validity flags
	unsigned int flags;

	// yaw, pitch, roll (in vehicle frame, rad)
	double yaw, pitch, roll;

	// position (ecef, m)
	double px, py, pz;

	// velocity (in vehicle frame, m/s)
	double veh_vx, veh_vy, veh_vz;

	// velocity (in ecef frame, m/s)
	double ecef_vx, ecef_vy, ecef_vz;

	// accelerometer biases
	double bax, bay, baz;
	// gyro biases
	double bwx, bwy, bwz;

	// ypr covariance matrix
	double cov_ypr[3][3];

	// position covariance matrix
	double cov_pos[3][3];

	// velocity covariance matrix
	double cov_vel[3][3]; 

	// last reported septentrio position
	double gps_px, gps_py, gps_pz;
	// trimble ecef position
	double hp_px, hp_py, hp_pz;
	// septentrio heading/pitch/roll
	double sep_heading, sep_pitch, sep_roll;
};

#pragma pack(pop)

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif
