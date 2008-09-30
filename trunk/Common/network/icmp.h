#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef _WIN32_WINNT 
#define _WIN32_WINNT 0x0500
#endif
#include <winsock2.h>
#include <windows.h>
#include <exception>

#include "../time/timestamp.h"

#ifndef _ICMP_H
#define _ICMP_H

// ICMP packet types
#define ICMP_ECHO_REPLY 0
#define ICMP_DEST_UNREACH 3
#define ICMP_TTL_EXPIRE 11
#define ICMP_ECHO_REQUEST 8

// Minimum ICMP packet size, in bytes
#define ICMP_MIN 8

#ifdef _MSC_VER
// The following two structures need to be packed tightly, but unlike
// Borland C++, Microsoft C++ does not do this by default.
#pragma pack(1)
#endif

// The IP header
struct ip_header {
	BYTE h_len:4;           // Length of the header in dwords
	BYTE version:4;         // Version of IP
	BYTE tos;               // Type of service
	USHORT total_len;       // Length of the packet in dwords
	USHORT ident;           // unique identifier
	USHORT flags;           // Flags
	BYTE ttl;               // Time to live
	BYTE proto;             // Protocol number (TCP, UDP etc)
	USHORT checksum;        // IP checksum
	ULONG source_ip;
	ULONG dest_ip;
};

// ICMP header
struct icmp_header {
	BYTE type;          // ICMP packet type
	BYTE code;          // Type sub code
	USHORT checksum;
	USHORT id;
	USHORT seq;
	ULONG timestamp;    // not part of ICMP, but we need it
};

#ifdef _MSC_VER
#pragma pack()
#endif

class icmp {
private:
	static int seq_no = 0;

	SOCKET sock;
	HANDLE read_event;
	WSAOVERLAPPED ov;
	WSADATA wsadata;

	unsigned short ip_checksum(unsigned short* buf, int size);

public:
	icmp();
	~icmp();

	bool send_ping(int addr, int ttl = 128, int size = 64);
	bool send_ping(const char* addr, int ttl = 128, int size = 64);
	timespan get_ping_reply(int addr, timespan timeout = timespan::invalid());
	timespan get_ping_reply(const char* addr, timespan timeout = timespan::invalid());

	HANDLE begin_get_ping_reply(const char* addr);
	HANDLE begin_get_ping_reply(int addr);
	timespan end_get_ping_reply();
};


#endif