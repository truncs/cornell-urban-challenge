#ifndef UDP_MESSAGE_H_JULY_25_2007_SVL5
#define UDP_MESSAGE_H_JULY_25_2007_SVL5

typedef unsigned long ulong;

struct udp_message {
	unsigned short port;    
	int source_addr;
  int dest_addr;

	size_t len;
	char* data;
};

#endif //UDP_MESSAGE_H_JULY_25_2007_SVL5

