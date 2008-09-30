#ifndef _XYCOORD_H
#define _XYCOORD_H

class XYCoord {
public:
	typedef float linear_type;
	XYCoord(float x=0, float y=0);
	XYCoord(const XYCoord&);

	XYCoord& operator = (const XYCoord&);
	float x(void) const;
	float y(void) const;
	float dot(const XYCoord&) const;
	float cross(const XYCoord&) const;
	float length(void) const;
	float lengthsq(void) const;
	XYCoord rotate(float) const;
	XYCoord rotate90() const;
	XYCoord rotateM90() const;
	XYCoord rorate180() const;
	XYCoord& normalize(float = 1.0);
	XYCoord normalize(float = 1.0) const;
	float slope() const;
	XYCoord operator - (void) const;
	XYCoord operator + (const XYCoord&) const;
	XYCoord operator - (const XYCoord&) const;
	XYCoord operator / (float) const;
	XYCoord operator * (float) const;
	XYCoord& operator += (const XYCoord&);
	XYCoord& operator -= (const XYCoord&);
	XYCoord& operator /= (float);
	XYCoord& operator *= (float);
	bool operator == (const XYCoord&) const;
	bool operator != (const XYCoord&) const;
public:
	float x;
	float y;
};

ostream& operator << (ostream&, const XYCoord&);

inline XYCoord::XYCoord(float x, float y) {
	this->x = x;
	this->y = y;
}

inline XYCoord::XYCoord(const XYCoord& pt) {
	x = pt.x;
	y = pt.y;
}

inline XYCoord& XYCoord::operator = (const XYCoord& pt) {
	x = pt.x;
	y = pt.y;
	return *this;
}

inline float XYCoord::x(void) const {
	return x;
}

inline float XYCoord::y(void) const {
	return y;
}

inline float XYCoord::dot(const XYCoord& wc) const {
	return (x*wc.x + y*wc.y);
}

inline float XYCoord::cross(const XYCoord& wc) const {
	return (x*wc.y - y*wc.x);
}

inline float XYCoord::length(void) const {
	return sqrt(lengthsq());
}

inline float XYCoord::lengthsq(void) const {
	return (x*x+y*y);
}

inline XYCoord XYCoord::rotate(double theta) const {
	//positive = counter-clockwise, theta in radians
	double newx = cos(theta)*x - sin(theta)*y;
	double newy = cos(theta)*y + sin(theta)*x;
	return XYCoord(newx, newy);
}

inline XYCoord XYCoord::rotate90() const {
	return XYCoord(-y, x);
}

inline XYCoord XYCoord::rotateM90() const {
	return XYCoord(y, -x);
}

inline XYCoord XYCoord::rorate180() const {
	return XYCoord(-x, -y);
}

inline XYCoord& XYCoord::normalize(double l) {
	float mult = l/length();
	(*this) *= mult;
	return *this;
}

inline XYCoord XYCoord::normalize(double l) const {
	return (*this)*l/length();
}

inline float XYCoord::slope() const {
	return y / x;
}

inline XYCoord XYCoord::operator - (void) const {
	return XYCoord(-x, -y);
}

inline XYCoord XYCoord::operator + (const XYCoord& wc) const {
	return XYCoord(x+wc.x, y+wc.y);
}

inline XYCoord XYCoord::operator - (const XYCoord& wc) const {
	return XYCoord(x-wc.x, y-wc.y);
}

inline XYCoord XYCoord::operator / (double d) const {
	return XYCoord(x/d, y/d);
}

inline XYCoord XYCoord::operator * (double d) const {
	return XYCoord(x*d, y*d);
}

inline XYCoord& XYCoord::operator += (const XYCoord& wc) {
	x+=wc.x;
	y+=wc.y;
	return *this;
}

inline XYCoord& XYCoord::operator -= (const XYCoord& wc) {
	x-=wc.x;
	y-=wc.y;
	return *this;
}

inline XYCoord& XYCoord::operator /= (double d) {
	x/=d;
	y/=d;
	return *this;
}

inline XYCoord& XYCoord::operator *= (double d) {
	x*=d;
	y*=d;
	return *this;
}

inline bool XYCoord::operator == (const XYCoord& wc) const {
	return(wc.x==x && wc.y==y);
}

inline bool XYCoord::operator != (const XYCoord& wc) const {
	return(wc.x!=x || wc.y!=y);
}


#endif