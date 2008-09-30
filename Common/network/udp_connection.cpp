#include "udp_connection.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#pragma comment(lib,"WS2_32.lib")

const FLOWSPEC udp_params::def_flowspec = {QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED, QOS_NOT_SPECIFIED};

bool operator == (const FLOWSPEC& lhs, const FLOWSPEC& rhs) {
	return ((lhs.DelayVariation == rhs.DelayVariation) &&
		(lhs.Latency == rhs.Latency) && 
		(lhs.MaxSduSize == rhs.MaxSduSize) &&
		(lhs.MinimumPolicedSize == rhs.MinimumPolicedSize) && 
		(lhs.PeakBandwidth == rhs.PeakBandwidth) &&
		(lhs.ServiceType == rhs.PeakBandwidth) && 
		(lhs.TokenBucketSize == rhs.TokenBucketSize) &&
		(lhs.TokenRate == rhs.TokenRate));
}

bool operator != (const FLOWSPEC& lhs, const FLOWSPEC& rhs) {
	return !(lhs == rhs);
}

udp_params::udp_params() {
	init();
}

udp_params::udp_params(unsigned long local_ip, unsigned short local_port, bool find_qos, FLOWSPEC flowspec) {
	init(local_ip, local_port, find_qos, flowspec);
}

udp_params::udp_params(const char* local_ip, unsigned short local_port, bool find_qos, FLOWSPEC flowspec) {
	init(inet_addr(local_ip), local_port, find_qos, flowspec);
}

udp_params::udp_params(unsigned short local_port, bool find_qos, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, find_qos, flowspec);
}

udp_params::udp_params(unsigned long local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(local_ip, local_port, protocol_info, flowspec);
}

udp_params::udp_params(const char* local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(inet_addr(local_ip), local_port, protocol_info, flowspec);
}

udp_params::udp_params(unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, protocol_info, flowspec);
}

udp_params::udp_params(unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, remote_ip, remote_port, find_qos, flowspec);
}

udp_params::udp_params(unsigned short local_port, const char* remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, inet_addr(remote_ip), remote_port, find_qos, flowspec);
}

udp_params::udp_params(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec) {
	init(local_ip, local_port, remote_ip, remote_port, find_qos, flowspec);
}

udp_params::udp_params(const char* local_ip, unsigned short local_port, const char* remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec) {
	init(inet_addr(local_ip), local_port, inet_addr(remote_ip), remote_port, find_qos, flowspec);
}

udp_params::udp_params(unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, remote_ip, remote_port, protocol_info, flowspec);
}

udp_params::udp_params(unsigned short local_port, const char* remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(INADDR_ANY, local_port, inet_addr(remote_ip), remote_port, protocol_info, flowspec);
}

udp_params::udp_params(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(local_ip, local_port, remote_ip, remote_port, protocol_info, flowspec);
}

udp_params::udp_params(const char* local_ip, unsigned short local_port, const char* remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init(inet_addr(local_ip), local_port, inet_addr(remote_ip), remote_port, protocol_info, flowspec);
}

void udp_params::init() {
	this->local_ip = ADDR_ANY;
	this->local_port = 0;

	this->remote_ip = ADDR_ANY;
	this->remote_port = 0;
	this->do_connect = false;

	this->use_protocol_info = false;
	this->find_qos = false;

	this->send_flowspec = def_flowspec;
	this->recv_flowspec = def_flowspec;
	this->use_flowspec = false;

  this->listener_thread_priority = THREAD_PRIORITY_NORMAL;
	this->listener_thread_affinity = 0;

	this->multicast_ttl = -1;
	this->multicast_loopback = true;
	this->reuse_addr = -1;

	this->no_listen = false;
}

void udp_params::init(unsigned long local_ip, unsigned short local_port, bool find_qos, FLOWSPEC flowspec) {
	init();

	this->local_ip = local_ip;
	this->local_port = local_port;

	this->use_protocol_info = false;
	this->find_qos = find_qos;

	this->send_flowspec = flowspec;
	this->recv_flowspec = flowspec;
	this->use_flowspec = (flowspec != def_flowspec);
}

void udp_params::init(unsigned long local_ip, unsigned short local_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init();

	this->local_ip = local_ip;
	this->local_port = local_port;

	this->protocol_info = protocol_info;
	this->use_protocol_info = true;
	this->find_qos = false;

	this->send_flowspec = flowspec;
	this->recv_flowspec = flowspec;
	this->use_flowspec = (flowspec != def_flowspec);
}

void udp_params::init(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, bool find_qos, FLOWSPEC flowspec) {
	init();

	this->local_ip = local_ip;
	this->local_port = local_port;

	this->remote_ip = remote_ip;
	this->remote_port = remote_port;
	this->do_connect = false;

	this->use_protocol_info = false;
	this->find_qos = find_qos;

	this->send_flowspec = flowspec;
	this->recv_flowspec = flowspec;
	this->use_flowspec = (flowspec != def_flowspec);
}

void udp_params::init(unsigned long local_ip, unsigned short local_port, unsigned long remote_ip, unsigned short remote_port, WSAPROTOCOL_INFO& protocol_info, FLOWSPEC flowspec) {
	init();

	this->local_ip = local_ip;
	this->local_port = local_port;

	this->remote_ip = remote_ip;
	this->remote_port = remote_port;
	this->do_connect = false;

	this->protocol_info = protocol_info;
	this->use_protocol_info = true;
	this->find_qos = false;

	this->send_flowspec = flowspec;
	this->recv_flowspec = flowspec;
	this->use_flowspec = (flowspec != def_flowspec);
}

udp_connection::udp_connection(const udp_params& cp) : create_params(cp)
{
	// make a local copy of the create params
	//this->create_params = create_params;
#ifdef UDP_MSVS6_COMPAT
  this->cbk = NULL;
#endif
	
	bool success = init_wsa();
	if (success && create_params.find_qos)	success = find_QOS_protocol();
	if (success) success = init_socket();
	if (success) success = connect_remote();
	if (success) success = create_listener();

	if (success) {
		in_addr addr;
		addr.s_addr = create_params.local_ip;

		if (!create_params.no_listen) {
			printf("Listening on %s:%hu\r\n", inet_ntoa(addr), create_params.local_port);
		}
	}
	else {
		throw exception("udp_connection did not initialize properly\r\n");
	}
}

udp_connection::~udp_connection(void) {
	printf("deconstructing udp_connection port %hu\r\n", create_params.local_port);

	this->running = false;

	// clean up the data
	if (WaitForSingleObject(listener_thread, 200) == WAIT_TIMEOUT) TerminateThread(listener_thread, 10);

	// cleanup the sockets
	if (sock != INVALID_SOCKET) {
    closesocket(sock);
    WSACleanup();
  }
}

bool udp_connection::init_failed() const { 
	return (sock == INVALID_SOCKET); 
}

bool udp_connection::init_wsa(void) {
	int result = WSAStartup( MAKEWORD(2,2), &wsadata );
  if (result != NO_ERROR) {
		printf("error during winsock startup: %d\r\n", result);;
    this->sock = INVALID_SOCKET;
    return false;
  }
	else {
		return true;
	}
}

bool udp_connection::find_QOS_protocol(void) {
	create_params.use_protocol_info = true;

	DWORD buf_length = 0;
	int proto = IPPROTO_UDP;
	WSAEnumProtocols(&proto, NULL, &buf_length);

	char* buffer = new char[buf_length];

	LPWSAPROTOCOL_INFO proto_info = (LPWSAPROTOCOL_INFO)buffer;

	int result = WSAEnumProtocols(&proto, proto_info, &buf_length);

	if (result == SOCKET_ERROR)	{
		printf("error enumerating UDP protocols: %d\r\n", WSAGetLastError());
		delete [] buffer;
		return false;
	}
	else {
		bool found = false;
		for (int i = 0; i < result; i++) {
			if ((proto_info[i].dwServiceFlags1 & XP1_QOS_SUPPORTED) == XP1_QOS_SUPPORTED) {
				create_params.protocol_info = proto_info[i];
				found = true;
				break;
			}
		}

		delete [] buffer;

		return found;
	}
}

bool udp_connection::init_socket(void) {
	// create a udp socket
	if (create_params.use_protocol_info) {
		this->sock = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, &create_params.protocol_info, 0, WSA_FLAG_OVERLAPPED);
	}
	else {
		this->sock = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED);
	}

	if (sock == INVALID_SOCKET) {
		printf("Error creating socket: %d\r\n", WSAGetLastError());
    sock = INVALID_SOCKET;
		return false;
	}

	// check if we want to allow address reuse
	if (create_params.reuse_addr == 1) {
		DWORD val = 1;
		if (setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (char*)&val, sizeof(val)) == SOCKET_ERROR) {
			printf("error setting reuse address\n");
		}
	}

  // request IP_PKTINFO (packet destination address)
#ifndef UDP_MSVS6_COMPAT
  int optval(1);
  if(setsockopt(sock, IPPROTO_IP, IP_PKTINFO, (char*)&optval, sizeof(int)) == SOCKET_ERROR)
    printf("ERROR: setting IP_PKTINFO option\n");
#endif

	// bind to the local address
	sockaddr_in service;

	service.sin_family = AF_INET;
	service.sin_addr.s_addr = create_params.local_ip;
	service.sin_port = htons(create_params.local_port);

	if (bind(sock, (SOCKADDR*) &service, sizeof(service)) == SOCKET_ERROR) {
		printf("bind() failed, error: %d\r\n", WSAGetLastError());
		closesocket(sock);
		sock = INVALID_SOCKET;
		return false;
	}

	if (create_params.remote_ip == INADDR_BROADCAST) {
		int tval = 1;
		if (setsockopt(sock, SOL_SOCKET, SO_BROADCAST, (char*)&tval, (int)sizeof(tval)) == SOCKET_ERROR) {
			printf("setsockopt() failed, error: %d\r\n", WSAGetLastError());
			closesocket(sock);
			sock = INVALID_SOCKET;
			return false;
		}
	}
	else if (IN_MULTICAST(htonl(create_params.remote_ip))) {
		if (!create_params.no_listen) {
			ip_mreq req;
			req.imr_interface.s_addr = INADDR_ANY;
			req.imr_multiaddr.s_addr = create_params.remote_ip;

			if (setsockopt(sock, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char*)&req, sizeof(req)) == SOCKET_ERROR) {
				printf("could not join multicast group, error: %d\r\n", WSAGetLastError());
				closesocket(sock);
				sock = INVALID_SOCKET;
				return false;
			}

			int loopback = create_params.multicast_loopback ? TRUE : FALSE;
			if (setsockopt(sock, IPPROTO_IP, IP_MULTICAST_LOOP, (char *)&loopback, sizeof(loopback)) == SOCKET_ERROR) {
				printf("couldn't set multicast loopback, error: %d\r\n", WSAGetLastError());
				closesocket(sock);
				sock = INVALID_SOCKET;
				return false;
			}
		}

		if (create_params.multicast_ttl > 0) {
			int ttl = create_params.multicast_ttl; 
			if (setsockopt(sock, IPPROTO_IP, IP_MULTICAST_TTL, (char *)&ttl, sizeof(ttl)) == SOCKET_ERROR) {
				printf("couldn't set multicast TTL, error: %d\r\n", WSAGetLastError());
				closesocket(sock);
				sock = INVALID_SOCKET;
				return false;
			}
		}
	}

	return true;
}

bool udp_connection::connect_remote(void) {
	sockaddr_in remoteaddr;
	// set the remote address from the ip given
  remoteaddr.sin_family = AF_INET;
	remoteaddr.sin_addr.s_addr = create_params.remote_ip;
	remoteaddr.sin_port = htons(create_params.remote_port);

	// set of the QoS data
	QOS qos_data;
	qos_data.SendingFlowspec = create_params.send_flowspec;
	qos_data.ReceivingFlowspec = create_params.recv_flowspec;
	qos_data.ProviderSpecific.buf = NULL;
	qos_data.ProviderSpecific.len = 0;

	did_connect = (create_params.do_connect) && ((create_params.remote_ip != 0) || (create_params.remote_port != 0));

	int result = 0;

	if (did_connect) {
		if (create_params.use_flowspec) {
			result = WSAConnect(sock, (sockaddr*)&remoteaddr, sizeof(remoteaddr), NULL, NULL, &qos_data, NULL);
		}
		else {
			result = WSAConnect(sock, (sockaddr*)&remoteaddr, sizeof(remoteaddr), NULL, NULL, NULL, NULL);
		}
	}
	else if (create_params.use_flowspec) {
		// we don't want to connect, but still want to set flowspec
		DWORD bytes_ret;
		result = WSAIoctl(sock, SIO_SET_QOS, &qos_data, sizeof(qos_data), NULL, 0, &bytes_ret, NULL, NULL);
	}

	if (result == SOCKET_ERROR) {
		printf("error connection to remote host: %d\r\n", WSAGetLastError());

		return false;
	}
	else {
		return true;
	}
}

bool udp_connection::create_listener() {
	if (!create_params.no_listen) {
		running = true;

		listener_thread = CreateThread(NULL, 0, udp_connection::udp_listener, this, 0, NULL);
		SetThreadPriority(listener_thread, create_params.listener_thread_priority);
		
		if (create_params.listener_thread_affinity != 0 && listener_thread != NULL)
			SetThreadAffinityMask(listener_thread, create_params.listener_thread_affinity);

		return (listener_thread != NULL);
	}
	else {
		return true;
	}
}

// send a message to the current remote ip
bool udp_connection::send_message(const void* d, size_t len) {
  // if there is no socket, return
  if (init_failed()) {
		printf("init failed, can't complete sendMessage\r\n");
		return false;
	}

	bool success = false;

  // create buffer struct
	WSABUF wsabuf;
	wsabuf.buf = (char*)d;
	wsabuf.len = (u_long)len;

	DWORD bytes_sent = 0;
	int result = 0;
  // send to the remote address
	if (create_params.do_connect) {
		result = WSASend(sock, &wsabuf, 1, &bytes_sent, 0, NULL, NULL);
	}
	else {
		sockaddr_in remoteaddr;

		remoteaddr.sin_family = AF_INET;
		remoteaddr.sin_addr.s_addr = create_params.remote_ip;
		remoteaddr.sin_port = htons(create_params.remote_port);

		result = WSASendTo(sock, &wsabuf, 1, &bytes_sent, 0, (sockaddr*)&remoteaddr, sizeof(remoteaddr), NULL, NULL);
	}

	if (result == SOCKET_ERROR) {
		success = false;
		printf("error sending packet: %d\r\n", WSAGetLastError());
	}
	else {
		success = true;
	}

	return success;
}

bool udp_connection::send_message(const void* d, size_t len, const char* remote_ip, unsigned short remote_port) {
	unsigned long remote = inet_addr(remote_ip);
	return send_message(d, len, remote, remote_port);
}

bool udp_connection::send_message(const void* d, size_t len, unsigned long remote_ip, unsigned short remote_port) {
	// if there is no socket, return
  if (init_failed()) {
		printf("init failed, can't complete sendMessage\r\n");
		return false;
	}

	bool success = false;
	
	// create the WSABUF to hold the data
	WSABUF wsabuf;
	wsabuf.buf = (char*)d;
	wsabuf.len = (u_long)len;

  // fill in the remote address
	sockaddr_in remoteaddr;

	remoteaddr.sin_family = AF_INET;
	remoteaddr.sin_addr.s_addr = remote_ip;
	remoteaddr.sin_port = htons(remote_port);
  // send to the remote address

	DWORD bytes_sent = 0;
	if (WSASendTo(sock, &wsabuf, 1, &bytes_sent, 0, (sockaddr*)&remoteaddr, sizeof(remoteaddr), NULL, NULL) == SOCKET_ERROR) {
		success = false;
		printf("error sending packet: %d\r\n", WSAGetLastError());
	}
	else {
		success = true;
	}

	return success;
}

void udp_connection::set_callback(udp_msg_handler handler, void* arg) {
	cbk = handler;
	cbk_arg = arg;
}

DWORD WINAPI udp_connection::udp_listener(LPVOID lp_param) {
	// the handler object is passed in as a parameter
  udp_connection* handler = (udp_connection*)lp_param;
  
	// check if the initialization worked
	if (handler->init_failed()) return 10;

	printf("Entered connection listener thread\r\n");

  	// create a buffer to hold the data 
	WSABUF wsabuf;
	wsabuf.buf = new char[MAX_PACKET_SIZE];
	wsabuf.len = MAX_PACKET_SIZE;

	DWORD bytes_recvd, flags = 0;

  sockaddr_in fromaddr;
  fromaddr.sin_family = AF_INET;
  fromaddr.sin_addr.S_un.S_addr = inet_addr("0.0.0.0");
  fromaddr.sin_port = htons(handler->create_params.local_port);

#ifndef UDP_MSVS6_COMPAT
  // find the farking WSARecvMsg
  LPFN_WSARECVMSG WSARecvMsg = NULL;
  GUID WSARecvMsg_GUID = WSAID_WSARECVMSG;
	DWORD NumberOfBytes;
	string ErrorMessage, Address;
	int nResult;
  nResult = WSAIoctl(handler->sock, SIO_GET_EXTENSION_FUNCTION_POINTER,
		   &WSARecvMsg_GUID, sizeof WSARecvMsg_GUID,
		   &WSARecvMsg, sizeof WSARecvMsg,
		   &NumberOfBytes, NULL, NULL);
  if (nResult == SOCKET_ERROR) {
	  WSARecvMsg = NULL;
	  return SOCKET_ERROR;
  }
  
  // create a WSAMSG stucture
  char controlBuf[1024];
  WSAMSG wsamsg;

	// loop while the run flag is set
	while (handler->running) {
		int result;
		
		bytes_recvd = 0;

    wsamsg.name = (LPSOCKADDR)&fromaddr;
    wsamsg.namelen = sizeof sockaddr_in;
    wsamsg.lpBuffers = &wsabuf;
    wsamsg.dwBufferCount = 1;
    wsamsg.Control.len = sizeof controlBuf;
    wsamsg.Control.buf = controlBuf;
    wsamsg.dwFlags = 0;

    result = WSARecvMsg(handler->sock, &wsamsg, &bytes_recvd, NULL, NULL);

    // proceed if a valid packet was received
    if(result != SOCKET_ERROR) {
			udp_message msg;
      
			msg.len = bytes_recvd;
			msg.data = wsabuf.buf;
      sockaddr_in from = *(sockaddr_in*)(wsamsg.name);
      msg.port = handler->create_params.local_port;
      msg.source_addr = from.sin_addr.S_un.S_addr;

      WSACMSGHDR *pCMsgHdr = WSA_CMSG_FIRSTHDR(&wsamsg);
      while(pCMsgHdr!=NULL){
        if(pCMsgHdr->cmsg_type==IP_PKTINFO) {
		      IN_PKTINFO *pPktInfo;
		      pPktInfo = (IN_PKTINFO *)WSA_CMSG_DATA(pCMsgHdr);
          msg.dest_addr = pPktInfo->ipi_addr.S_un.S_addr;
        }
        pCMsgHdr = WSA_CMSG_NXTHDR(&wsamsg,pCMsgHdr);
      }
      
#ifndef UDP_MSVS6_COMPAT
			if (!handler->cbk.empty()) {
				handler->cbk(msg, handler, handler->cbk_arg);
			}
#else
      if(handler->cbk!=NULL)
        handler->cbk(msg, handler, handler->cbk_arg);
#endif
    }
		else {
			int err = WSAGetLastError();
			if (err != WSAECONNRESET) {
				printf("WSARecvMsg failed: %d\r\n", err);
			}
		}
  }
#else
	// loop while the run flag is set
	while (handler->running) {
		int result;
		
		bytes_recvd = 0;
		
    if (handler->did_connect) {
			result = WSARecv(handler->sock, &wsabuf, 1, &bytes_recvd, &flags, NULL, NULL);
		}
		else {
			int fromsize = sizeof(fromaddr);
			fromaddr.sin_family = AF_INET;
			fromaddr.sin_addr.s_addr = inet_addr("0.0.0.0");
			fromaddr.sin_port = htons(handler->create_params.local_port);

			result = WSARecvFrom(handler->sock, &wsabuf, 1, &bytes_recvd, &flags, (sockaddr*)&fromaddr, &fromsize, NULL, NULL);
		}

    // proceed if a valid packet was received
    if (result != SOCKET_ERROR) {
			udp_message msg;
			
			msg.len = bytes_recvd;
			msg.data = wsabuf.buf;
			msg.port = ntohs(fromaddr.sin_port);
			msg.source_addr = fromaddr.sin_addr.s_addr;

      if(handler->cbk!=NULL)
        handler->cbk(msg, handler, handler->cbk_arg);
    }
		else {
			int err = WSAGetLastError();
			if (err != WSAECONNRESET) {
				printf("WSARecvMsg failed: %d\r\n", err);
			}
		}
  }
#endif
  return 0;
}
