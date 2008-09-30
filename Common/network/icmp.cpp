#include "icmp.h"

icmp::icmp() {
	// initialize windows sockets
  if (WSAStartup(MAKEWORD(2,2), &wsadata) != NO_ERROR) {
    this->sock = INVALID_SOCKET;
		throw std::exception("error initializing Winsock");
  }
	
	// Create the socket
	sd = WSASocket(AF_INET, SOCK_RAW, IPPROTO_ICMP, NULL, 0, WSA_FLAG_OVERLAPPED);
  if (sd == INVALID_SOCKET) {
		throw std::exception("error creating socket");
  }

  

  // Initialize the destination host info block
  memset(&dest, 0, sizeof(dest));

  // Turn first passed parameter into an IP address to ping
  unsigned int addr = inet_addr(host);
  if (addr != INADDR_NONE) {
      // It was a dotted quad number, so save result
      dest.sin_addr.s_addr = addr;
      dest.sin_family = AF_INET;
  }
  else {
      // Not in dotted quad form, so try and look it up
      hostent* hp = gethostbyname(host);
      if (hp != 0) {
          // Found an address for that host, so save it
          memcpy(&(dest.sin_addr), hp->h_addr, hp->h_length);
          dest.sin_family = hp->h_addrtype;
      }
      else {
          // Not a recognized hostname either!
          cerr << "Failed to resolve " << host << endl;
          return -1;
      }
  }

  return 0;

}

icmp::~icmp() {
}

bool icmp::send_ping(int addr, int ttl, int size) {
	if (setsockopt(sd, IPPROTO_IP, IP_TTL, (const char*)&ttl, sizeof(ttl)) == SOCKET_ERROR) {
		return false;
  }

	sockaddr_in dest;
	dest.sin_family = AF_INET;
	dest.sin_addr.s_addr = addr;
	dest.sin_port = 0;

	size_t packet_size = sizeof(icmp_header) + size;
	char* buf = new char[packet_size];
	icmp_header* icmp_msg = (icmp_header*)buf;
	icmp_msg->type = ICMP_ECHO_REQUEST;
	icmp_msg->code = 0;
	icmp_msg->checksum = 0;
	icmp_msg->id = GetProcessId();
	icmp_msg->seq = ++seq_no;
	icmp_msg->timestamp = GetTickCount();

	char* data = buf + sizeof(icmp_header);
	memset(data, 0xDA, size);
	icmp_msg->checksum = ip_checksum((unsigned short*)buf, packet_size);

	if (sendto(sock, buf, packet_size, 0, (sockaddr*)&dest, sizeof(dest)) == SOCKET_ERROR) {
		return false;
	}

	return true;
}

bool icmp::send_ping(const char* addr, int ttl, int size) {
	return send_ping(inet_addr(), ttl, size);
}

timespan icmp::get_ping_reply(int addr, timespan timeout) {

}

timespan icmp::get_ping_reply(const char* addr, timespan timeout) {
}

unsigned short icmp::ip_checksum(unsigned short* buf, int size) {
}