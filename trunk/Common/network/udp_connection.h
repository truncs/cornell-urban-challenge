#ifndef _WIN32_WINNT 
#define _WIN32_WINNT 0x0501
#endif

// this is crazy: MSVS.net needs winsock2 to be included before windows.h so that winsock.h doesn't get included
// while MSVS6 needs the opposite ordering because its winsock2 depends on types defines in windows.h...
// using the common PSDK version doesnt help cause PSDK no longer supports MSVS6...
#ifndef UDP_MSVS6_COMPAT
#include <winsock2.h>
#include <windows.h>
#else
#include <windows.h>
#include <winsock2.h>
#endif
#include <iostream>
#include <map>
#include <list>
#include <ws2tcpip.h>
#include <mswsock.h>

#ifndef UDP_MSVS6_COMPAT
#include "../utility/fastdelegate.h"
using namespace fastdelegate;
#endif

#ifndef _UDP_CONNECTION_H
#define _UDP_CONNECTION_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define MAX_PACKET_SIZE 65467 // UDP protocol max message size

using namespace std;

class udp_connection;

class udp_params
{
private:
	void init();
	void init(unsigned long local_ip, unsigned short local_port, bool find_qos, FLOWSPEC flowspec);
	void init(unsigned long local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec);
	void init(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec);
	void init(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec);

public:
	const static FLOWSPEC def_flowspec;

	// general notes:
	//  - local_ip, remote_ip are in network byte order (same as return by inet_addr)
	//  - local_port, remote_port are in host byte order
	//  - if find_qos is specified and there is no QoS protocol installed, the socket
	//		initialization will fail
	//  - if not specifying a remote_ip, remote_port, you must call sendMessageTo, 
	//		sendMessage will fail
	//  - if the QoS protocol cannot satisy the QoS (flowspec) request, the socket initialization will fail
	//  - both send_flowspec and recv_flowspec are set to the same values by the constructors
	//  - process_sep_thread is always initialized to false by these constructors, you must
	//		set it after calling the constructor if you want it to be true
	//  - for information on QoS, WSAPROTOCOL_INFO, and FLOWSPEC, see the Winsock2 documentation

	udp_params();

	udp_params(unsigned short local_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned long local_ip, unsigned short local_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	udp_params(const char* local_ip, unsigned short local_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	
	udp_params(unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned long local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);
	udp_params(const char* local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);

	udp_params(unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned short local_port, const char* remote_ip, unsigned short remote_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);
	udp_params(const char* local_ip, unsigned short local_port, const char* remote_ip, unsigned short remote_port, bool find_qos = false, FLOWSPEC flowspec = def_flowspec);

	udp_params(unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned short local_port, const char* remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);
	udp_params(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);
	udp_params(const char* local_ip, unsigned short local_port, const char* remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec = def_flowspec);

	unsigned long local_ip;
	unsigned short local_port;

	unsigned long remote_ip;
	unsigned short remote_port;
	bool do_connect;
	int multicast_ttl;
	bool multicast_loopback;

	int reuse_addr;

	WSAPROTOCOL_INFO protocol_info;
	bool use_protocol_info;
	bool find_qos;

	FLOWSPEC send_flowspec;
	FLOWSPEC recv_flowspec;
	bool use_flowspec;

	bool no_listen;
	DWORD listener_thread_affinity;
  int listener_thread_priority; 
};

bool operator == (const FLOWSPEC& lhs, const FLOWSPEC& rhs);
bool operator != (const FLOWSPEC& lhs, const FLOWSPEC& rhs);

#include "udp_message.h"

#ifndef UDP_MSVS6_COMPAT
typedef FastDelegate3<udp_message&, udp_connection*, void*> udp_msg_handler;
#else
typedef void(*udp_msg_handler)(udp_message&, udp_connection*, void*);
#endif

class udp_connection {
  static DWORD WINAPI udp_listener(LPVOID lp_param);

private:
	SOCKET sock;

	udp_params create_params;
	WSADATA wsadata;
	bool did_connect;

  HANDLE listener_thread;
  volatile bool running;

	udp_msg_handler cbk;
	void* cbk_arg;

	bool init_wsa();
	bool find_QOS_protocol();
  bool init_socket();
	bool connect_remote();
	bool create_listener();

public:
	udp_connection(const udp_params& create_params);
	~udp_connection(void);

  bool init_failed() const;

  void set_callback(udp_msg_handler handler, void* arg);

	bool send_message(const void* d, size_t len);
	bool send_message(const void* d, size_t len, const char* remote_ip, unsigned short remote_port) ;
	bool send_message(const void* d, size_t len, unsigned long remote_ip, unsigned short remote_port) ;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //_udp_connection_H
