#ifndef QUEUEMONITOR_H
#define QUEUEMONITOR_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct QueueMonitor
{
	//A structure for monitoring queue sizes

	//the number of times the queue has been checked
	int QueueChecks;
	//the total number of packets found in the queue across all checks
	int QueuePackets;
	//the sum of squares of packets found in the queue
	int QueuePackets2;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //QUEUEMONITOR_H
