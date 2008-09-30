#include "poseclient.h"
#ifndef UDP_MSVS6_COMPAT
#include "..\network\net_utility.h"
#endif
#include <cassert>

#define POSE_SUBNET "192.168.1.0"
#define POSE_ADDR "239.132.1.33"
#define POSE_PORT 4839

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

pose_client::pose_client() {
	// initialize the socket
	udp_params p(POSE_PORT,POSE_ADDR,0);
	// we don't want to connect
	p.do_connect = false;

#ifndef UDP_MSVS6_COMPAT
  unsigned int local_addr;
  // figure out an appropriate interface to listen on is the pose address is multicast
	if (IN_MULTICAST(ntohl(p.remote_ip)) && find_subnet_adapter(inet_addr(POSE_SUBNET), local_addr)) {
		// we found a local address on the correct port
		// p.local_ip = local_addr;		
		// indicate that we want to allow address reuse (supported for multicast data)
		p.reuse_addr = 1;
	}
#else
  p.local_ip = inet_addr("0.0.0.0");
  p.reuse_addr = 1;
#endif

	//abs_cbk = NULL;
	//rel_cbk = NULL;

	// create the udp connection
	conn = new udp_connection(p);
	packetCount=0;
	dropCount=0;
	sequenceNumber=0;

#ifndef UDP_MSVS6_COMPAT
	conn->set_callback(MakeDelegate(this,&pose_client::msg_recv), NULL);
#else
  conn->set_callback(&(pose_client::msg_recv),this);
#endif
}

pose_client::~pose_client() {
	// kill the udp connection
	delete conn;
	conn = NULL;
}

void pose_client::register_rel_callback(pose_rel_packet_cbk cbk, void* arg) {
	rel_cbks.push_back (cbk);
	rel_args.push_back (arg);
	//rel_arg = arg;
	//rel_cbk = cbk;
}

void pose_client::unregister_rel_callback() {
	rel_cbks.empty();
	rel_args.empty();
	//rel_cbk = NULL;
	//rel_arg = NULL;
}

void pose_client::register_abs_callback(pose_abs_packet_cbk cbk, void* arg) {
	abs_cbks.push_back (cbk);
	abs_args.push_back (arg);
	//abs_arg = arg;
	//abs_cbk = cbk;
}

void pose_client::unregister_abs_callback() {
	abs_cbks.empty();
	abs_args.empty();
	//abs_cbk = NULL;
	//abs_arg = NULL;
}

void pose_client::msg_recv(udp_message& msg, udp_connection* conn, void* arg) {
  pose_client* pc;
#ifndef UDP_MSVS6_COMPAT
  pc = this;
#else
  pc = (pose_client*)arg;
#endif
	
	packetCount++;

	pose_rel_msg rel_msg;
	pose_abs_msg abs_msg;

	// figure out what type of message it is
	switch (msg.data[0]) {
		case POSE_REL_PACKET_V1:
			// this is the unambiguous case--can't be a new message in disguise
			if (msg.len == sizeof(pose_rel_msg_v1)) {
				map_v1_rel_packet((pose_rel_msg_v1*)msg.data, &rel_msg);
				pc->on_rel_msg(&rel_msg);
			}
			break;

		case POSE_MSG_VERSION:
			// this is the ambiguous case--it could be a V1 absolute message, or it could be a V2 rel/abs message
			// check the second byte, should be 0 for a V1 absolute message
			if (msg.data[1] == 0 && msg.len == sizeof(pose_abs_msg_v1)) {
				map_v1_abs_packet((pose_abs_msg_v1*)msg.data, &abs_msg);
				pc->on_abs_msg(&abs_msg);
			}
			else if (msg.data[1] == POSE_REL_PACKET_V2 && msg.len == sizeof(pose_rel_msg)) {
				pc->on_rel_msg((pose_rel_msg*)msg.data);
			}
			else if (msg.data[1] == POSE_ABS_PACKET_V2 && msg.len == sizeof(pose_abs_msg)) {
				pc->on_abs_msg((pose_abs_msg*)msg.data);
			}
			break;
	}
}

void pose_client::on_rel_msg(pose_rel_msg* msg) {
	for (size_t i = 0; i < rel_cbks.size(); i++)	{
		//make sure the subscribe request finished
		if (rel_cbks.size() == rel_args.size()) {
			rel_cbks[i](*msg, rel_args[i]);
		}
	}
}

void pose_client::on_abs_msg(pose_abs_msg* msg) {
	for (size_t i = 0; i < abs_cbks.size(); i++) {
		//make sure the subscribe request finished
		if (abs_cbks.size() == abs_args.size()) {
			abs_cbks[i](*msg, abs_args[i]);				
		}
	}
}

void pose_client::map_v1_abs_packet(pose_abs_msg_v1 *v1_msg, pose_abs_msg *v2_msg) {
	// map the version for kicks
	v2_msg->version = POSE_MSG_VERSION;
	// map the packet type
	v2_msg->packet_type = POSE_ABS_PACKET_V2;
	// map a 0 sequence number
	v2_msg->seq_num = 0;
	// do a default flag mapping assuming everything is valie
	v2_msg->flags = POSE_MSG_VALID_FLAG | POSE_ABS_CORR_HP | POSE_ABS_SEP_ATT_VALID_FLAG | POSE_ABS_TRIM_POS_VALID_FLAG | POSE_ABS_SEP_POS_VALID_FLAG;
	
	// start copying fields
	v2_msg->car_ts_ticks = v1_msg->car_ts_ticks;
	v2_msg->car_ts_secs = v1_msg->car_ts_secs;

	v2_msg->yaw = v1_msg->yaw;
	v2_msg->pitch = v1_msg->pitch;
	v2_msg->roll = v1_msg->roll;

	v2_msg->px = v1_msg->px;
	v2_msg->py = v1_msg->py;
	v2_msg->pz = v1_msg->pz;

	v2_msg->veh_vx = v1_msg->veh_vx;
	v2_msg->veh_vy = v1_msg->veh_vy;
	v2_msg->veh_vz = v1_msg->veh_vz;

	v2_msg->ecef_vx = v1_msg->ecef_vx;
	v2_msg->ecef_vy = v1_msg->ecef_vy;
	v2_msg->ecef_vz = v1_msg->ecef_vz;

	v2_msg->bax = v1_msg->bax;
	v2_msg->bay = v1_msg->bay;
	v2_msg->baz = v1_msg->baz;

	v2_msg->bwx = v1_msg->bwx;
	v2_msg->bwy = v1_msg->bwy;
	v2_msg->bwz = v1_msg->bwz;

	memcpy(v2_msg->cov_ypr, v1_msg->cov_ypr, 9*sizeof(double));
	memcpy(v2_msg->cov_pos, v1_msg->cov_pos, 9*sizeof(double));
	memcpy(v2_msg->cov_vel, v1_msg->cov_vel, 9*sizeof(double));

	v2_msg->gps_px = v1_msg->gps_px;
	v2_msg->gps_py = v1_msg->gps_py;
	v2_msg->gps_pz = v1_msg->gps_pz;

	v2_msg->hp_px = v1_msg->hp_px;
	v2_msg->hp_py = v1_msg->hp_py;
	v2_msg->hp_pz = v1_msg->hp_pz;

	v2_msg->sep_heading = v1_msg->sep_heading;
	v2_msg->sep_pitch = v1_msg->sep_pitch;
	v2_msg->sep_roll = v1_msg->sep_roll;
}

void pose_client::map_v1_rel_packet(pose_rel_msg_v1 *v1_msg, pose_rel_msg *v2_msg) {
	v2_msg->version = POSE_MSG_VERSION;
	v2_msg->packet_type = POSE_REL_PACKET_V2;
	v2_msg->seq_num = 0;
	v2_msg->car_ts_ticks = v1_msg->car_ts_ticks;
	v2_msg->car_ts_secs = v1_msg->car_ts_secs;
	v2_msg->flags = POSE_MSG_VALID_FLAG;
	v2_msg->dt = v1_msg->dt;
	memcpy(v2_msg->Rinit2veh, v1_msg->Rinit2veh, 16*sizeof(double));
}
