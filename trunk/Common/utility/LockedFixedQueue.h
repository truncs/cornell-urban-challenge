#ifndef LOCKED_FIXED_QUEUE_H_SEPT_07_2007
#define LOCKED_FIXED_QUEUE_H_SEPT_07_2007

#include "FixedQueue.h"

template<class T>
class LockedFixedQueue : public FixedQueue<T>
{
public:
  LockedFixedQueue(const LockedFixedQueue& b):FixedQueue(b){
    InitializeCriticalSection(&cs);
  }
  LockedFixedQueue(unsigned int _span, T _failret):FixedQueue(_span,_failret){
    InitializeCriticalSection(&cs);
  }
  virtual ~LockedFixedQueue(){
    DeleteCriticalSection(&cs);
  }

  void operator=(const LockedFixedQueue& b){
    FixedQueue::operator=(b);
  }

  void push_threadsafe(const T val){
    lock();
    push(val);
    unlock();
  }

  void lock(){
    EnterCriticalSection(&cs);
  }
  
  void unlock(){
    LeaveCriticalSection(&cs);
  }

protected:
  CRITICAL_SECTION cs;
};

#endif //LOCKED_FIXED_QUEUE_H_SEPT_07_2007
