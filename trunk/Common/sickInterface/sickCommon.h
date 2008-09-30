#pragma once

#pragma pack(1)
	struct SickXYPoint
	{
		SickXYPoint(double x, double y)
		{
			this-> x = x; this->y = y;
		}
		SickXYPoint()
		{
			x=0; y=0;
		}
		double x;
		double y;

		bool operator == (SickXYPoint other)
		{
			return ((other.x == this->x) && (other.y == this->y));
		}
	};
#pragma pack()