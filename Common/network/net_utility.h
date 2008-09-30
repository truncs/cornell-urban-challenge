#ifndef _NET_UTILITY_H
#define _NET_UTILITY_H

#include <winsock2.h>
#include <Iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

bool find_subnet_adapter(unsigned int subnet, unsigned int &addr);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif