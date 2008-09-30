#ifndef _BLOCKINGQUEUE_H
#define _BLOCKINGQUEUE_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <windows.h>
#include <deque>
#include <string>

using namespace std;

// Provides a thread safe queue that blocks callers to pull_item if there are no entries.
// 
// Example usage:
//
// blocking_queue<int> num_queue;
//
// DWORD WINAPI filler_thread(LPVOID lpparam);
//
// void main() {
//   CreateThread(NULL, 0, filler_thread, NULL, 0, NULL);
//
//   while (true) {
//     int the_num;
//     if (num_queue.pull_item(the_num, 50)) {
//       printf("the number is %d\n", the_num);
//     }
//     else {
//       printf("timed out\n");
//     }
//   }
// }
//  
// DWORD WINAPI filler_thread(LPVOID lpparam) {
//   while (true) {
//		 int rand_sleep = rand() % 100;
//     Sleep(rand_sleep);
//     int rand_num = rand();
//     num_queue.push_back(rand_num);
//   }
// }

#if defined(QUEUE_COLLECT_STATS)
#include "../time/timestamp.h"

template <class T>
struct __queue_entry 
{
public:
	T value;
	timestamp ts;

	__queue_entry(const T& val, const timestamp& time) : value(val), ts(time) {}
};
#endif

template <class T>
class blocking_queue
{
private:
#if defined(QUEUE_COLLECT_STATS)
	typedef deque<__queue_entry<T> > queue_type;

	// last time an entry was added or removed
	timestamp last_entry_time;
	// time-average length running sum
	double sum_ta_length;
	// sample-average length running sum
	size_t sum_sa_length;
	// sample-average delay time running sum
	double sum_delay_time;
	// number of samples for sample-average length/delay time
	int n_samp;
	// time stats were last computer
	timestamp stats_start_time;
#else
	typedef deque<T> queue_type;
#endif

	queue_type queue;
	mutable CRITICAL_SECTION cs;
	HANDLE availableEvent;

	volatile long waitCount;

public:
	blocking_queue(void);
	// constructs the blocking queue with a named wait event
	blocking_queue(const string& wait_event_name);
	blocking_queue(const wstring& wait_event_name);

	~blocking_queue(void);

	// Pushes the specified item onto the queue and free a thread if one is blocked waiting for data.
	// Note: this function is thread safe, so any thread may call it at any time.
	void push_item(const T& item);

	// Attempts to remove an items from the queue. If there are no items, this method will block for 
	// timeout milliseconds. If an item is successfully removed, then the function will return true. 
	// If no item is present before the timeout, then the function will return false. The timeout 
	// value may be 0 if you do not want to block.
	// Note: this function is thread safe, meaning that multiple threads may try to call this at once
	// and it will only return an object to one of them.
	bool pull_item(T& ret, unsigned long timeout = INFINITE);
	
	// Gets the number of items in the queue. This function is thread safe.
	size_t size() const;

	// Returns a Windows Event that can be used to wait for data. Useful for calls to WaitForMultipleObjects
	HANDLE wait_handle() const;

	// clears all elements from the queue
	void clear();

	// acquire the queue lock
	void lock() const;
	// release the queue lock
	void unlock() const;

#if defined(QUEUE_COLLECT_STATS)
	double time_average_length() const;
	double samp_average_length() const;

	double samp_average_delay() const;

	void reset_stats();
#endif
};

template <class T>
blocking_queue<T>::blocking_queue(void)
{
	InitializeCriticalSection(&cs);
	availableEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	waitCount = 0;

#if defined(QUEUE_COLLECT_STATS)
	reset_stats();
#endif
}

template <class T>
blocking_queue<T>::blocking_queue(const string& wait_event_name)
{
	InitializeCriticalSection(&cs);
	availableEvent = CreateEventA(NULL, FALSE, FALSE, wait_event_name.c_str());
	waitCount = 0;

#if defined(QUEUE_COLLECT_STATS)
	reset_stats();
#endif
}

template <class T>
blocking_queue<T>::blocking_queue(const wstring& wait_event_name)
{
	InitializeCriticalSection(&cs);
	availableEvent = CreateEventW(NULL, FALSE, FALSE, wait_event_name.c_str());
	waitCount = 0;

#if defined(QUEUE_COLLECT_STATS)
	reset_stats();
#endif
}

template <class T>
blocking_queue<T>::~blocking_queue(void)
{
	// allow each waiting thread to be removed from the tx queue
	while (waitCount > 0) 
	{
		// Enter the critical section, trigger the event, leave the 
		// section to allow work to continue
		lock();
		SetEvent(availableEvent);
		unlock();
	}

	DeleteCriticalSection(&cs);
	CloseHandle(availableEvent);
}

template <class T>
void blocking_queue<T>::push_item(const T &item)
{
#if defined(QUEUE_COLLECT_STATS)
	// do this before acquiring the critical section so the delay time calculation
	// is a little more accurate
	timestamp entry_time = timestamp::cur();
#endif

	// enter the critical section to begin work on the queue
	lock();

#if defined(QUEUE_COLLECT_STATS)
	timestamp ts = timestamp::cur();
	if (last_entry_time.is_valid()) 
	{
		sum_ta_length += (ts-last_entry_time).total_secs()*queue.size();
	}
	last_entry_time = ts;

	queue.push_back(__queue_entry<T>(item, entry_time));
#else
	// push it to the back
	queue.push_back(item);
#endif

	// trigger the event to notify that there are items available
	SetEvent(availableEvent);
	// leave the critical section
	unlock();
}

template <class T>
bool blocking_queue<T>::pull_item(T& ret, unsigned long timeout)
{
	// enter the critical section to begin working on the queue
	lock();
	
	// check if there are items in the queue
	if (queue.size() > 0)
	{
#if defined(QUEUE_COLLECT_STATS)
		timestamp ts = timestamp::cur();
		if (last_entry_time.is_valid()) 
		{
			sum_ta_length += (ts-last_entry_time).total_secs()*queue.size();
		}
		last_entry_time = ts;

		// accumulate sample averaged queue length
		sum_sa_length += queue.size();
		// accumulate sample averaged delay time
		sum_delay_time += (ts-queue.front().ts).total_secs();
		// accumulate number of samples
		n_samp++;

		ret = queue.front().value;
		queue.pop_front();
#else
		// if so, get the item and return it
		ret = queue.front();
		queue.pop_front();
#endif

		// return here to make things clean
		unlock();

		// sucessfully retrieved an item
		return true;
	}
	else
	{
		// leave the critical section to allow items to be added
		unlock();

		// increment the wait count (use interlocked increment cause this could be done
		//    across processors, interlocked functions will flush value)
		InterlockedIncrement(&waitCount);
		// wait for items to be put into the queue or the queue to be destructed
		DWORD result = WaitForSingleObject(availableEvent, timeout);
		// decrement the wait count
		InterlockedDecrement(&waitCount);

		// check if we timed out
		if (result == WAIT_TIMEOUT)
		{
			return false;
		}
		else 
		{
			// enter the queue access lock
			lock();

			// get the first item if it exists
			// need to check the size because we could have returned due to the queue
			// being destructed
			if (queue.size() > 0)
			{
#if defined(QUEUE_COLLECT_STATS)
				timestamp ts = timestamp::cur();
				// accumulate time-averaged queue length
				if (last_entry_time.is_valid()) 
				{
					sum_ta_length += (ts-last_entry_time).total_secs()*queue.size();
				}
				last_entry_time = ts;

				// accumulate sample averaged queue length
				sum_sa_length += queue.size();
				// accumulate sample averaged delay time
				sum_delay_time += (ts-queue.front().ts).total_secs();
				// accumulate number of samples
				n_samp++;

				ret = queue.front().value;
				queue.pop_front();
#else
				// if so, get the item and return it
				ret = queue.front();
				queue.pop_front();
#endif
				// leave the critical section, done with queue
				unlock();

				return true;
			}
			else
			{
				// leave the critical section, done with queue
				unlock();

				// unsuccessful
				return false;
			}
		}
	}
}

template <class T>
size_t blocking_queue<T>::size() const
{
	return queue.size();
}

template <class T>
HANDLE blocking_queue<T>::wait_handle() const
{
	return availableEvent;
}

template <class T>
void blocking_queue<T>::clear() 
{
	lock();
	queue.clear();
#if defined(QUEUE_COLLECT_STATS)
	reset_stats();
#endif
	unlock();
}

template <class T>
void blocking_queue<T>::lock() const { EnterCriticalSection(&cs); }

template <class T>
void blocking_queue<T>::unlock() const { LeaveCriticalSection(&cs); }

#if defined(QUEUE_COLLECT_STATS)

template <class T>
double blocking_queue<T>::time_average_length() const {
	lock();
	// get the total time
	double total_time = (timestamp::cur()-stats_start_time).total_secs();

	// return the calculated average
	double avg;
	if (total_time > 0) {
		avg = sum_ta_length/total_time;
	}
	else {
		avg = 0;
	}

	unlock();

	return avg;
}

template <class T>
double blocking_queue<T>::samp_average_length() const {
	lock();
	// return the calculated average
	double avg;
	if (n_samp > 0) {
		avg = sum_sa_length/(double)n_samp;
	}
	else {
		avg = 0;
	}

	unlock();

	return avg;
}

template <class T>
double blocking_queue<T>::samp_average_delay() const {
	lock();
	// return the calculated average
	double avg;
	if (n_samp > 0) {
		avg = sum_delay_time/(double)n_samp;
	}
	else {
		avg = 0;
	}

	unlock();

	return avg;
}

template <class T>
void blocking_queue<T>::reset_stats() {
	lock();
	
	timestamp cur = timestamp::cur();
	last_entry_time = stats_start_time = cur;
	sum_ta_length = sum_delay_time = 0;
	sum_sa_length = n_samp = 0;

	unlock();
}
#endif //defined(QUEUE_COLLECT_STATS)

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //_BLOCKINGQUEUE_H
