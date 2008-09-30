#pragma once

struct RoadFitterOutputData{	


	//HERE IS ALL THE BASIC DATA:
	enum BorderType{		
		BT_SingleLaneLine=0,
		BT_Edge=1
	};
	static const int max_borders_observed = 4;

	int confidence; //arbitrary confidence scale; higher is more confident.

	float road_heading;  //if one were to take any of the constituent borders, and find the point on it which is closest to the
		//vehicle, and measure the heading of that border, one would obtain road_heading.  In radians, a positive value indicates
		//the road is pointing to the left.
	int borders_observed; //the number of markers of any kind observed
	BorderType observed_border_types[max_borders_observed];
	float border_offsets[max_borders_observed];  //If one draws a line perpendicular to the direction defined by heading, which
		//passes through the origin, this array gives the position along the line that each marker observed lies.  In meters,
		//positive value means to the left.
	float system_curvature;  //This gives the curvature of the entire system.  The best way to accurately describe this is to imagine
		//that the entire system of borders consisted of a set of concentric circles.  The center of all these circles is defined in
		//in exactly the same way the locations of the borders was: a distance along a line that's perpendicular to the heading.  Of 
		//course, this is the curvature, so it will be the inverse of that number.

	//HERE IS ALL THE DATA EXTRACTED, WHICH IS MORE THAN YOU WILL PROBABLY NEED.  All the above parameters, with the exception of
	//borders_observed and observed_border_types, are just computed by best-fitting a model to these numbers.
	static const int num_ctrl_pts = 6;

	struct SpinePoint{
		float x, y, z;
		float dx, dy, dz;
		float border_offsets[max_borders_observed];
	};


	SpinePoint control_points[num_ctrl_pts];
};