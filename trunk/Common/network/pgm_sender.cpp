#include "pgm_sender.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#pragma comment (lib, "WS2_32.lib")
#pragma comment (lib, "Iphlpapi.lib")

pgm_sender::pgm_sender(const pgm_sender_parameters &params) {
	if (!init_wsa()) {
		return;
	}

	if (!init_socket()) {
		close_wsa();
		return;
	}

	if (params.local_if_subnet != 0) {
		if (!attach_adapter(params.local_if_subnet)) {
			close_socket();
			close_wsa();
			return;
		}
	}

	if (params.data_rate != 0 && params.window_time != 0) {
		if (!set_rate(params.data_rate, params.window_time)) {
			close_socket();
			close_wsa();
			return;
		}
	}

	if (!connect_socket(params.group_addr, params.group_port)) {
		close_socket();
		close_wsa();
		return;
	}
}

bool pgm_sender::init_wsa() {
	int result = WSAStartup( MAKEWORD(2,2), &wsa_data );
  if (result != NO_ERROR) {
		printf("error during winsock startup: %d\r\n", result);;
    s = INVALID_SOCKET;
    return false;
  }
	else {
		return true;
	}
}

bool pgm_sender::init_socket() {
	s = socket(AF_INET, SOCK_RDM, IPPROTO_RM);
	if (s == INVALID_SOCKET) {
		printf("error creating socket: %d\n", WSAGetLastError());
		return false;
	}

	sockaddr_in salocal;
	salocal.sin_family = AF_INET;
	salocal.sin_port = htons(0); // Port is ignored by pgm
	salocal.sin_addr.s_addr = htonl(INADDR_ANY); // always bind to ANY address

	if (bind(s, (SOCKADDR *)&salocal, sizeof(salocal)) == SOCKET_ERROR) {
		printf("error binding socket: %d\n", WSAGetLastError());
		close_socket();
		return false;
	}
	
	return true;
}

bool pgm_sender::attach_adapter(unsigned int local_if_subnet) {
	// find the adapter with the appropriate subnet
	PMIB_IPADDRTABLE addr_table;
	DWORD dwSize = 0;

	addr_table = (PMIB_IPADDRTABLE)malloc(sizeof(MIB_IPADDRTABLE));

	// make an initial call to get the appropriate address table size
	if (GetIpAddrTable(addr_table, &dwSize, FALSE) == ERROR_INSUFFICIENT_BUFFER) {
		// free the original buffer
		free(addr_table);
		// make space, reallocate
		addr_table = (PMIB_IPADDRTABLE)malloc(dwSize);
	}

	// assert that we could allocate space
	if (addr_table == NULL) {
		printf("could not allocate space for addr table\n");
		return false;
	}

	// get the real data
	unsigned int if_addr = 0;
	if (GetIpAddrTable(addr_table, &dwSize, FALSE) == NO_ERROR) {
		// iterate through the table and find a matching entry
		for (DWORD i = 0; i < addr_table->dwNumEntries; i++) {
			unsigned int subnet = addr_table->table[i].dwAddr & addr_table->table[i].dwMask;
			if (subnet == local_if_subnet) {
				if_addr = addr_table->table[i].dwAddr;
				break;
			}
		}

		// free the allocated memory
		free(addr_table);
	}
	else {
		printf("error getting ip address table: %d\n", WSAGetLastError());
		// free the allocated memory
		free(addr_table);

		return false;
	}

	// check if we found a match
	if (if_addr != 0) {
		in_addr addr;
		addr.s_addr = if_addr;
		printf("binding to address %s\n", inet_ntoa(addr));
		// set the socket option
		if (setsockopt(s, IPPROTO_RM, RM_SET_SEND_IF, (char*)&if_addr, sizeof(if_addr)) == SOCKET_ERROR) {
			printf("error setting interface options: %d\n", WSAGetLastError());
			return false;
		}

		return true;
	}
	else {
		printf("could not find matching subnet on local address\n");
		return false;
	}
}

bool pgm_sender::set_rate(unsigned int data_rate, unsigned int window_time) {
	RM_SEND_WINDOW snd_wnd;
	snd_wnd.RateKbitsPerSec = data_rate;
	snd_wnd.WindowSizeInMSecs = window_time;
	snd_wnd.WindowSizeInBytes = snd_wnd.WindowSizeInMSecs * snd_wnd.RateKbitsPerSec / 8;
	if (setsockopt(s, IPPROTO_RM, RM_RATE_WINDOW_SIZE, (char*)&snd_wnd, sizeof(snd_wnd)) == SOCKET_ERROR) {
		printf("error setting send rate: %d\n", WSAGetLastError());
		return false;
	}

	return true;
}

bool pgm_sender::connect_socket(unsigned int group_addr, unsigned short session_port) {
	sockaddr_in sasession;
	sasession.sin_family = AF_INET;
	sasession.sin_port = htons(session_port);
	sasession.sin_addr.s_addr = group_addr;

	if (connect(s, (SOCKADDR *)&sasession, sizeof(sasession)) == SOCKET_ERROR) { 
		printf("error connecting to pgm: %d\n", WSAGetLastError());
		return false;
	}

	return true;
}

void pgm_sender::close_socket() {
	closesocket(s);
	s = INVALID_SOCKET;
}

void pgm_sender::close_wsa() {
	WSACleanup();
}

bool pgm_sender::send(const char* buf, unsigned int len) {
	int result = ::send(s, buf, len, 0);

	if (result == SOCKET_ERROR) {
		printf("error sending data on pgm socket: %d\n", WSAGetLastError());
		return false;
	}
	else {
		return true;
	}
}