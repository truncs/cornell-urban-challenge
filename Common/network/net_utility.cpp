#include "net_utility.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#pragma comment (lib, "WS2_32.lib")
#pragma comment (lib, "Iphlpapi.lib")

bool find_subnet_adapter(unsigned int if_subnet, unsigned int &addr) {
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
			if (subnet == if_subnet) {
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
		addr = if_addr;
		return true;
	}
	else {
		return false;
	}
}