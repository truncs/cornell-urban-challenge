// fixed-span circular FIFO buffer
// Cornell DARPA GC - sensors
// sergei lupashin (svl5@cornell.edu)
#pragma once

#pragma warning(push)
#pragma warning(disable : 4244)

#include <assert.h>
#include <stdlib.h>

template<class T>
class FixedQueue{
public:
  FixedQueue(unsigned int _span, T _failret):failret(_failret),span(_span)
  {
    assert(_span>=1);
    data = new T[_span+1];
    assert(data!=NULL);
    reset();
  }
  FixedQueue(const FixedQueue& b){
    span = b.span;
    begin = b.begin;
    end = b.end;
    failret = b.failret;
    data = new T[span+1];
    memcpy(data,b.data,sizeof(T)*(span+1));
  }
  virtual ~FixedQueue(){
    delete [] data;
  }

  void operator=(const FixedQueue& b){
    delete [] data;
    span = b.span;
    begin = b.begin;
    end = b.end;
    failret = b.failret;
    data = new T[span+1];
    memcpy(data,b.data,sizeof(T)*(span+1));
  }

  void push(const T val){
		assert(end <= span);
    data[end] = val;
    end = (end+1)%(span+1);
    if(begin==end)
      begin = (begin+1)%(span+1);
  }

  inline unsigned int n_meas() const{
    if(begin==end)    return 0;
    if(end<begin)     return (begin+span-end)-1;
    else              return end-begin;
  }

  inline bool empty() const{  return begin==end; }
  inline bool full() const {  return n_meas()==span; }
  inline void reset(){  begin = end = 0; }

  // newest pushed data (bottom)
  T newest() const{
    if(begin==end) return failret;
    return data[(end + span)%(span+1)];
  }

  // oldest pushed data (top)
  T oldest() const{
    if(begin==end) return failret;
    return data[begin];
  }

	inline void pop_oldest(){
		if(begin==end) return;
		begin = (begin+1)%(span+1);
	}

  // newest pushed data (bottom)
  T& newest(){
    if(begin==end) return failret;
    return data[(end + span)%(span+1)];
  }

  // oldest pushed data (top)
  T& oldest(){
    if(begin==end) return failret;
    return data[begin];
  }

  // 0 is the beginning (oldest) data
  T& operator[](unsigned int i){
    if((i+1)>n_meas())
      return failret;
    i = (i+begin)%(span+1);
		assert(i <= span);
    return data[i];
  }

	const T& operator[](unsigned int i) const {
		if((i+1)>n_meas())
      return failret;
    i = (i+begin)%(span+1);
		assert(i <= span);
    return data[i];
	}

protected:
  unsigned int begin, end;
  unsigned int span;
  T *data;
  T failret;
};

template<class T, unsigned int S>
class FixedQueueEx : public FixedQueue<T>{
public:
  FixedQueueEx() : FixedQueue(S,T()){}
};

#pragma warning(pop)