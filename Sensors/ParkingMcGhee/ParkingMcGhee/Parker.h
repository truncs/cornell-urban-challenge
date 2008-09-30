#ifndef _PARKER_H
#define _PARKER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "math.h"

#include <iostream>
using namespace std;

struct VehicleParameters{
	double back_bumper; // distance from back axle to back bumper
	double front_bumper; // distance from back axle to front bumper
	double min_turning_rad; //the area defined by the width of a tire times the wheel radius
	double width;
	int skat;
};

struct VehicleState{
	double x, y;  //rear diff postion
	double hx, hy;//orientation.  Don't like vector orientation definitions?  kiss my stuff.

	void Normalize();
};

struct ParkingSpaceParameters{

	double corner_xs[2];
	double corner_ys[2];
		//The parking area is defined by a rectangle.  Two opposite corners of the rectangle are nececarry to define it completely.

	VehicleState park_point;
		//this defines the final orientation of the vehicle when it is parked.  It is important that the angle of park_point
		//agree with the angle of the rectangle; it needs to be parallel to one set of edges.

	double pullout_point_x, pullout_point_y;
		//In pulling out mode, the car will pull out of the spot and align itself parallel to the walls.
		//You control which direction it points with pullout_point.  Place pullout_point on the side of the
		// "parking axis" that you want the car to point.
};

struct MovingOrder{
	bool forward; // direction = forward / reverse
	double turning_radius; //the turning radius describes the center of the circle relative to the vehicle.
		//The sign indicates the direction, but is independant from the direction the vehicle is traveling.
		//A positive value indicates that the center of roation is to the left of the vehicle.

	VehicleState dest_state;

	double center_x, center_y;
};

class Parker{
public:
	Parker();
	Parker(VehicleParameters vehicle, ParkingSpaceParameters space);
	~Parker();

	bool GetNextParkingOrder(MovingOrder& order, VehicleState state);
		//give it your current state.  It returns true if there's more work to be done, in which case it fills order with values.
		//return false if its done.

	bool GetNextPulloutOrder(MovingOrder& order, VehicleState state);

	//Debugging stuff that you don't care about
	bool InBounds(VehicleState vs);
	void GetFourCorners(double* xs, double* ys);

//private:

	VehicleParameters vehicle;
	double wall_sep;
	double neg_x_bound;
	double pos_x_bound;
	double front_wall_px;
	double front_wall_py;
	double front_wall_x;
	double front_wall_y;
	double forward_park_position;
	bool pullout_to_plus_x;

	bool Get_PA_PA_Traversal_Gentile(VehicleState s1, VehicleState s2, MovingOrder& m1, MovingOrder& m2); //get point-angle - point-angle traversal

	struct DestinationSlot{
		VehicleState state;
		double utility;

		DestinationSlot(VehicleState vs);
		DestinationSlot();
	};

	void Trans_I_O(VehicleState& vs);
	void Trans_O_I(VehicleState& vs);

	void PolishMovingOrder(MovingOrder& mo);

	static const int angular_resolution = 31;
	static const int width_resolution = 40;
	static const int turning_radius_width_range_multiple = 3;

	DestinationSlot* slots;
	static const double slot_definition_safety_fraction;
	int num_slots;
	
	VehicleState ideal_back;
	VehicleState ideal_front;

	double ReflectionPointUtilityMeasure(VehicleState vs);
	double* best_angle_for_top_offset;
	double bafto_idx_loc_conv;
	double bafto_y_sample;

	bool QueryComplete(VehicleState vs); //todo
	static const double complete_distance_fudge;
	static const double complete_angular_fudge;

	void NudgeInBounds(VehicleState& vs);

	MovingOrder GetMaximumLockExtent(VehicleState vs, bool direction, bool left, double stop_at_heading);

	void GetWiggleRoom(VehicleState vs, double& front_wiggle, double& back_wiggle);

	VehicleState GetClosestExtent(double angle, double seperation, double horiz); //should be removed

	VehicleState GetXWallHits(double angle, double horiz, double wall, bool pos, bool front);
	VehicleState GetYWallHits(double angle, double horiz, double wall, bool pos, bool front);

	bool FinishingOrder(VehicleState vs, MovingOrder& order);
	MovingOrder ConstructOneMoveTraversal(VehicleState start, double to_x, double to_y, bool forward);
	bool GetOneMoveTraversal(VehicleState start, VehicleState to, MovingOrder& order);

	bool FinalAlignment(VehicleState vs, MovingOrder& order);

	bool SubFinalAlignment(VehicleState vs, MovingOrder& order, double& util);

	double GetFinalAlignmentUtility(double finish_y, double turning_rad, double arc_len);

	bool QueryStateCompletesMovement(VehicleState vs, MovingOrder order);

	double QueryStaticViolation(VehicleState vs);
	double QueryMovementViolation(VehicleState start, MovingOrder order);

	void GetStaticExtents(VehicleState vs, double& max_y, double& min_y, double& max_x, double& min_x);
	void GetMovementExtents(VehicleState vs, MovingOrder order, double& max_y, double& min_y, double& max_x, double& min_x);
	void DrawArc(double c_x, double c_y, double x1, double y1, double x2, double y2, bool cw);
	void FillVehicleCorners(double* xs, double* ys, VehicleState vs);

	void Show(VehicleState s1, VehicleState s2, VehicleState s3, VehicleState s4, char* name);

	double AbsAngleDiff(double a1, double a2);

	bool ClockHitsAFirst(double start_x, double start_y, double a_x, double a_y, double b_x, double b_y);
	double ArcLen(VehicleState start, MovingOrder order);

};

//CvPoint cvPoint(double x, double y);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //_PARKER_H
