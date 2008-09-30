#ifndef _POSE_CLIENT_H
#define _POSE_CLIENT_H

#include <vector>
#include "pose_message.h"
#include "..\network\udp_connection.h"

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

typedef void (*pose_rel_packet_cbk)(const pose_rel_msg&, void*);
typedef void (*pose_abs_packet_cbk)(const pose_abs_msg&, void*);

class pose_client {
public:
	pose_client();
	~pose_client();

	void register_rel_callback(pose_rel_packet_cbk cbk, void* arg);
	void unregister_rel_callback();

	void register_abs_callback(pose_abs_packet_cbk cbk, void* arg);
	void unregister_abs_callback();

private:
#ifndef UDP_MSVS6_COMPAT
	void msg_recv(udp_message&, udp_connection*, void*);
#else
  static void msg_recv(udp_message&, udp_connection*, void*);
#endif

	udp_connection *conn;
	
	//pose_rel_packet_cbk rel_cbk;
	//void* rel_arg;
	vector<pose_rel_packet_cbk> rel_cbks;
	vector<void*>	rel_args;

	//pose_abs_packet_cbk abs_cbk;
	//void* abs_arg;
	vector<pose_abs_packet_cbk> abs_cbks;
	vector<void*>	abs_args;

	int packetCount;
	int dropCount;
	int sequenceNumber;

	static void map_v1_abs_packet(pose_abs_msg_v1 *v1_msg, pose_abs_msg *v2_msg);
	static void map_v1_rel_packet(pose_rel_msg_v1 *v1_msg, pose_rel_msg *v2_msg);

	void on_rel_msg(pose_rel_msg* msg);
	void on_abs_msg(pose_abs_msg* msg);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif
