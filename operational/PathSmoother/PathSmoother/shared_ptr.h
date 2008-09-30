#ifndef _SHARED_PTR_H
#define _SHARED_PTR_H

#include "ref_obj.h"

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

template<class T>
class shared_ptr 
{
public:
  // default constructor, initialize to NULL
  shared_ptr();
	shared_ptr(const shared_ptr<T>& copy);

  // constructor - initialize from T ptr
  shared_ptr(T* ptr);

  // destructor, automatically decrements the
  //  reference count, deletes the object if
  //  necessary
  ~shared_ptr();

  // overloaded arrow operator, allows the user to call
  // methods using the contained pointer.
  T* operator->() const;

  // overloaded dereference operator, allows the user
  // to dereference the contained pointer.
  T& operator*() const;

  /** Overloaded equals operator, allows the user to
   * set the value of the SmartPtr from a raw pointer */
  shared_ptr<T>& operator=(T* rhs);

  /** Overloaded equals operator, allows the user to
   * set the value of the SmartPtr from another 
   * SmartPtr */
  shared_ptr<T>& operator=(const shared_ptr<T>& rhs);

  /** Overloaded equality comparison operator, allows the
   * user to compare the value of two SmartPtrs */
  template <class U1, class U2>
  friend
  bool operator==(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs);

  /** Overloaded equality comparison operator, allows the
   * user to compare the value of a SmartPtr with a raw pointer. */
  template <class U1, class U2>
  friend
  bool operator==(const shared_ptr<U1>& lhs, U2* raw_rhs);

  /** Overloaded equality comparison operator, allows the
   * user to compare the value of a raw pointer with a SmartPtr. */
  template <class U1, class U2>
  friend
  bool operator==(U1* lhs, const shared_ptr<U2>& raw_rhs);

  /** Overloaded in-equality comparison operator, allows the
   * user to compare the value of two SmartPtrs */
  template <class U1, class U2>
  friend
  bool operator!=(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs);

  /** Overloaded in-equality comparison operator, allows the
   * user to compare the value of a SmartPtr with a raw pointer. */
  template <class U1, class U2>
  friend
  bool operator!=(const shared_ptr<U1>& lhs, U2* raw_rhs);

  /** Overloaded in-equality comparison operator, allows the
   * user to compare the value of a SmartPtr with a raw pointer. */
  template <class U1, class U2>
  friend
  bool operator!=(U1* lhs, const shared_ptr<U2>& raw_rhs);
  //@}

  /**@name friend method declarations. */
  //@{
  /** Returns the raw pointer contained.
   * Use to get the value of
   * the raw ptr (i.e. to pass to other
   * methods/functions, etc.)
   * Note: This method does NOT copy, 
   * therefore, modifications using this
   * value modify the underlying object 
   * contained by the SmartPtr,
   * NEVER delete this returned value.
   */
  template <class U>
  friend
  U* get_raw_ptr(const shared_ptr<U>& ptr);

  /** Returns a const pointer */
  template <class U>
  friend
  shared_ptr<const U> const_ptr(const shared_ptr<U>& ptr);

  /** Returns true if the SmartPtr is NOT NULL.
   * Use this to check if the SmartPtr is not null
   * This is preferred to if(GetRawPtr(sp) != NULL)
   */
  template <class U>
  friend
  bool is_valid(const shared_ptr<U>& ptr);

  /** Returns true if the SmartPtr is NULL.
   * Use this to check if the SmartPtr IsNull.
   * This is preferred to if(GetRawPtr(sp) == NULL)
   */
  template <class U>
  friend
  bool is_null(const shared_ptr<U>& ptr);
  //@}

private:
  /**@name Private Data/Methods */
  //@{
  /** Actual raw pointer to the object. */
  T* ptr_;

  /** Set the value of the internal raw pointer
   * from another raw pointer, releasing the 
   * previously referenced object if necessary. */
  shared_ptr<T>& set_from_raw_ptr(T* rhs);

  /** Set the value of the internal raw pointer
   * from a SmartPtr, releasing the previously referenced
   * object if necessary. */
  shared_ptr<T>& set_from_shared_ptr(const shared_ptr<T>& rhs);

  /** Release the currently referenced object. */
  void release_ptr();
  //@}
};

/**@name SmartPtr friend function declarations.*/
//@{
template <class U>
U* get_raw_ptr(const shared_ptr<U>& ptr);

template <class U>
shared_ptr<const U> const_ptr(const shared_ptr<U>& ptr);

template <class U>
bool is_null(const shared_ptr<U>& ptr);

template <class U>
bool is_valid(const shared_ptr<U>& ptr);

template <class U1, class U2>
bool operator==(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs);

template <class U1, class U2>
bool operator==(const shared_ptr<U1>& lhs, U2* raw_rhs);

template <class U1, class U2>
bool operator==(U1* lhs, const shared_ptr<U2>& raw_rhs);

template <class U1, class U2>
bool operator!=(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs);

template <class U1, class U2>
bool operator!=(const shared_ptr<U1>& lhs, U2* raw_rhs);

template <class U1, class U2>
bool operator!=(U1* lhs, const shared_ptr<U2>& raw_rhs);

//@}


template <class T>
shared_ptr<T>::shared_ptr()
    : ptr_(NULL) {}

template <class T>
shared_ptr<T>::shared_ptr(const shared_ptr<T>& copy) : ptr_(NULL) {
	set_from_shared_ptr(copy);
}

template <class T>
shared_ptr<T>::shared_ptr(T* ptr) : ptr_(NULL) {
	set_from_raw_ptr(ptr);
}

template <class T>
shared_ptr<T>::~shared_ptr() {
	release_ptr();
}

template <class T>
T* shared_ptr<T>::operator->() const {
  return ptr_;
}

template <class T>
T& shared_ptr<T>::operator*() const {
  return *ptr_;
}

template <class T>
shared_ptr<T>& shared_ptr<T>::operator=(T* rhs) {
	return set_from_raw_ptr(rhs);
}

template <class T>
shared_ptr<T>& shared_ptr<T>::operator=(const shared_ptr<T>& rhs) {
	return set_from_shared_ptr(rhs);
}

template <class T>
shared_ptr<T>& shared_ptr<T>::set_from_raw_ptr(T* rhs) {

  // Release any old pointer
  release_ptr();

  if (rhs != NULL) {
    rhs->add_ref();
    ptr_ = rhs;
  }

  return *this;
}

template <class T>
shared_ptr<T>& shared_ptr<T>::set_from_shared_ptr(const shared_ptr<T>& rhs)
{
  T* ptr = get_raw_ptr(rhs);
	set_from_raw_ptr(ptr);

  return (*this);
}

template <class T>
void shared_ptr<T>::release_ptr() {
  if (ptr_) {
    if (ptr_->release_ref() == 0) {
      delete ptr_;
    }
    ptr_ = NULL;
  }
}

template <class U>
U* get_raw_ptr(const shared_ptr<U>& ptr){
  return ptr.ptr_;
}

template <class U>
shared_ptr<const U> const_ptr(const shared_ptr<U>& ptr) {
  // compiler should implicitly cast
	return get_raw_ptr(ptr);
}

template <class U>
bool is_valid(const shared_ptr<U>& ptr) {
  return !is_null(ptr);
}

template <class U>
bool is_null(const shared_ptr<U>& ptr) {
  return (ptr.ptr_ == NULL);
}

template <class U1, class U2>
bool compare_ptrs(const U1* lhs, const U2* rhs) {
  if (lhs == rhs) {
    return true;
  }

  // Even if lhs and rhs point to the same object
  // with different interfaces U1 and U2, we cannot guarantee that
  // the value of the pointers will be equivalent. We can
  // guarantee this if we convert to void*
  const void* v_lhs = static_cast<const void*>(lhs);
  const void* v_rhs = static_cast<const void*>(rhs);
  if (v_lhs == v_rhs) {
    return true;
  }

  // They must not be the same
  return false;
}

template <class U1, class U2>
bool operator==(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs) {
  U1* raw_lhs = get_raw_ptr(lhs);
  U2* raw_rhs = get_raw_ptr(rhs);
  return compare_ptrs(raw_lhs, raw_rhs);
}

template <class U1, class U2>
bool operator==(const shared_ptr<U1>& lhs, U2* raw_rhs) {
  U1* raw_lhs = get_raw_ptr(lhs);
	return compare_ptrs(raw_lhs, raw_rhs);
}

template <class U1, class U2>
bool operator==(U1* raw_lhs, const shared_ptr<U2>& rhs) {
  const U2* raw_rhs = get_raw_ptr(rhs);
	return compare_ptrs(raw_lhs, raw_rhs);
}

template <class U1, class U2>
bool operator!=(const shared_ptr<U1>& lhs, const shared_ptr<U2>& rhs)
{
  bool retValue = operator==(lhs, rhs);
  return !retValue;
}

template <class U1, class U2>
bool operator!=(const shared_ptr<U1>& lhs, U2* raw_rhs) {
  bool retValue = operator==(lhs, raw_rhs);
  return !retValue;
}

template <class U1, class U2>
bool operator!=(U1* raw_lhs, const shared_ptr<U2>& rhs) {
  bool retValue = operator==(raw_lhs, rhs);
  return !retValue;
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif