#include "Parker.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include <math.h>
#include <float.h>
#include <windows.h>

#include <assert.h>

#include <iostream>
#include <list>
using namespace std;

const double PI = 3.1415926535897932384626433832795 ;

Parker::Parker(){
}

/*void Parker::Show(VehicleState s1, VehicleState s2, VehicleState s3, VehicleState s4, char* name){
	assert(0);
	cvNamedWindow(name);
	IplImage* img = cvCreateImage(cvSize(1000,1000), IPL_DEPTH_8U, 3);
	cvZero(img);
	const int shift = 500;
	cvLine(img, cvPoint(0, shift), cvPoint(999,shift), cvScalarAll(128), 3);
	cvLine(img, cvPoint(shift, 0), cvPoint(shift, 999), cvScalarAll(128), 3);
	s1.x += shift;
	s1.y += shift;
	s2.x += shift;
	s2.y += shift;
	s3.x += shift;
	s3.y += shift;
	s4.x += shift;
	s4.y += shift;
	Car(img, s1, vehicle);
	Car(img, s2, vehicle);
	Car(img, s3, vehicle);
	Car(img, s4, vehicle);
	cvFlip(img);
	cvShowImage(name, img);
	cvReleaseImage(&img);
}*/

const double Parker::slot_definition_safety_fraction = 0.02;

Parker::DestinationSlot::DestinationSlot(VehicleState vs){
	state = vs;
}

Parker::DestinationSlot::DestinationSlot(){}

Parker::Parker(VehicleParameters vehicle, ParkingSpaceParameters space){
	Parker::vehicle = vehicle;

	//Parker::space = space;
	space.park_point.Normalize();
	front_wall_px = -space.park_point.hx ;
	front_wall_py = -space.park_point.hy ;
	double finish_dot = space.park_point.x*front_wall_px + space.park_point.y*front_wall_py ;
	double back_dot = -DBL_MAX;
	double front_dot=  DBL_MAX;
	for(int i=0; i<2; i++){
		double dot = space.corner_xs[i]*front_wall_px + space.corner_ys[i]*front_wall_py ;
		 back_dot = max( back_dot, dot);
		front_dot = min(front_dot, dot);
	}
	wall_sep = back_dot - front_dot ;
	forward_park_position = finish_dot - front_dot ;
	
	front_wall_x = space.park_point.x - forward_park_position*front_wall_px ;
	front_wall_y = space.park_point.y - forward_park_position*front_wall_py ;

	double side_x =  front_wall_py ;
	double side_y = -front_wall_px ;
	double middle_dot = space.park_point.x*side_x + space.park_point.y*side_y ;
	double pos_x_dot = -DBL_MAX ;
	double neg_x_dot =  DBL_MAX ;
	for(int i=0; i<2; i++){
		double dot = space.corner_xs[i]*side_x + space.corner_ys[i]*side_y ;
		pos_x_dot = max(pos_x_dot, dot);
		neg_x_dot = min(neg_x_dot, dot);
	}
	pos_x_bound = pos_x_dot - middle_dot;
	neg_x_bound = neg_x_dot - middle_dot;


	space.pullout_point_x -= front_wall_x;
	space.pullout_point_y -= front_wall_y;
	pullout_to_plus_x = ( space.pullout_point_x*side_x + space.pullout_point_y*side_y ) > 0 ;

	list<DestinationSlot> slots_pre_prune;
	num_slots = 0;

	double safe_distance = slot_definition_safety_fraction * vehicle.min_turning_rad ;

	double fudge = vehicle.min_turning_rad / 10.0 ;

	//do front and back walls.
	for(int angle_i=0; angle_i<angular_resolution; angle_i++){
		for(int width_i=0; width_i<width_resolution; width_i++){
			double angle = double(angle_i+1)/double(angular_resolution+1);
			angle = angle*2.0 - 1.0;
			angle = angle*PI*0.5;

			double horiz = double(width_i)/double(width_resolution-1);
			horiz = horiz*(pos_x_bound-neg_x_bound) + neg_x_bound ;

			slots_pre_prune.push_back( GetXWallHits(angle, horiz, fudge, true, true)  );
			slots_pre_prune.push_back( GetXWallHits(angle, horiz, fudge, true, false) );
			
			slots_pre_prune.push_back( GetXWallHits(angle, horiz, wall_sep - fudge, false, true)  );
			slots_pre_prune.push_back( GetXWallHits(angle, horiz, wall_sep - fudge, false, false)  );
		}
	}

	//do side walls
	for(int angle_i=0; angle_i<angular_resolution; angle_i++){
		for(int width_i=0; width_i<width_resolution; width_i++){
			double angle = double(angle_i+1)/double(angular_resolution+1);
			angle = angle*2.0 - 1.0;
			angle = angle*PI*0.5;

			double horiz = double(width_i)/double(width_resolution-1);
			horiz = horiz*wall_sep ;

			slots_pre_prune.push_back( GetYWallHits(angle, horiz, neg_x_bound + fudge, false,  true)  );
			slots_pre_prune.push_back( GetYWallHits(angle, horiz, neg_x_bound + fudge, false,  false) );
			
			slots_pre_prune.push_back( GetYWallHits(angle, horiz, pos_x_bound - fudge, true, true)  );
			slots_pre_prune.push_back( GetYWallHits(angle, horiz, pos_x_bound - fudge, true, false) );
		}
	}

	slots = new DestinationSlot[slots_pre_prune.size()];
	num_slots = 0 ;
	for(list<DestinationSlot>::iterator i=slots_pre_prune.begin(); i != slots_pre_prune.end(); i++){
		if(QueryStaticViolation(i->state)==0){
			slots[num_slots++] = *i;
		}
	}

	ideal_back.x = 0;
	ideal_back.y = wall_sep - vehicle.back_bumper;
	ideal_back.hx = 0;
	ideal_back.hy = -1;

	ideal_front.x = 0;
	ideal_front.y = vehicle.front_bumper;
	ideal_front.hx = 0;
	ideal_front.hy = -1;

	bafto_idx_loc_conv = max(abs(pos_x_bound), abs(neg_x_bound)) * 1.2 / width_resolution ;
	bafto_y_sample = wall_sep - fudge - sqrt( vehicle.back_bumper*vehicle.back_bumper + vehicle.width*vehicle.width/4.0 );
	best_angle_for_top_offset = new double[width_resolution];
	double angle_max = PI/2.0;
	for(int i=0; i<width_resolution; i++){
		double loc = double(i) * bafto_idx_loc_conv ;

		MovingOrder thing = ConstructOneMoveTraversal(ideal_front, loc, bafto_y_sample , false);
		double best_angle = atan2( thing.dest_state.hy, thing.dest_state.hx );
		if(AbsAngleDiff(best_angle, -PI/2.0)>angle_max){
			if(best_angle<-PI/2.0 || best_angle>PI/2.0) best_angle = -PI/2.0 - angle_max;
			else                                        best_angle = -PI/2.0 + angle_max;
		}
		best_angle_for_top_offset[i] = best_angle ;
	}

	for(int i=0; i<num_slots; i++){
		slots[i].utility = ReflectionPointUtilityMeasure(slots[i].state);
	}
	delete[] best_angle_for_top_offset;
}

void Parker::GetFourCorners(double* xs, double* ys){
	for(int i=0; i<4; i++){
		VehicleState v;
		v.x = (i<2) ? neg_x_bound : pos_x_bound ;
		v.y = (i==1 || i==2) ? wall_sep : 0 ;

		Trans_I_O(v);

		xs[i] = v.x;
		ys[i] = v.y;
	}
}

double Parker::GetFinalAlignmentUtility(double finish_y, double turning_rad, double arc_len){
	double wall_proximity = min(finish_y-vehicle.front_bumper, wall_sep-(finish_y+vehicle.back_bumper));

	double hard_turning_rad = vehicle.min_turning_rad*1.2;
	double hard_wall_proximity = vehicle.width;
	
	double turning_rad_discomfort;// = vehicle.min_turning_rad*1.2 - turning_rad ;
	double wall_proximity_discomfort;// = vehicle.width - wall_proximity;

	if( turning_rad < hard_turning_rad ){
		turning_rad_discomfort = ( hard_turning_rad - turning_rad )/( hard_turning_rad - vehicle.min_turning_rad );
	}else{
		turning_rad_discomfort = 0.0;
	}

	if( wall_proximity < hard_wall_proximity ){
		wall_proximity_discomfort = ( hard_wall_proximity - wall_proximity )/( hard_wall_proximity );
	}else{
		wall_proximity_discomfort = 0;
	}

	if( turning_rad_discomfort>0 || wall_proximity_discomfort>0 ){
		return 3.0 - turning_rad_discomfort - wall_proximity_discomfort ;
	}

	return 15.0 - (arc_len+finish_y)/( 10.0*(wall_sep+pos_x_bound-neg_x_bound) ) ;
}

double Parker::ReflectionPointUtilityMeasure(VehicleState vs){
	//double y = is_front ? wall_sep : 0 ;
	double y = wall_sep;
	double intersect_x = vs.hx*(y-vs.y)/vs.hy + vs.x ;
	
	double min_x, max_x, min_y, max_y;
	GetStaticExtents(vs, max_y, min_y, max_x, min_x);
	
	double cur_angle  = atan2( vs.hy , vs.hx );


	//check for direct a-finishing
	MovingOrder fa_order;
	double fa_utility = -DBL_MAX;
	if(FinalAlignment(vs, fa_order)){
		fa_utility = GetFinalAlignmentUtility(fa_order.dest_state.y, abs(fa_order.turning_radius), ArcLen(vs, fa_order)) + 20;
	}

	MovingOrder sfa_order;
	double sfa_utility = -DBL_MAX;
	double su;
	if( SubFinalAlignment(vs, sfa_order, su) ){
		sfa_utility = su;
	}

	double direct_util = max(fa_utility, sfa_utility);
	if( direct_util>-DBL_MAX )return direct_util;
	
	MovingOrder thing = ConstructOneMoveTraversal(ideal_front, vs.x, vs.y, false);
	double best_angle = atan2( thing.dest_state.hy, thing.dest_state.hx );
	double angle_err = AbsAngleDiff(best_angle, cur_angle);
	if( angle_err<PI/4.0 && abs(thing.turning_radius)>vehicle.min_turning_rad && vs.y>wall_sep/2.0 && vs.hy<0 ){
		return -abs(vs.x) - vehicle.min_turning_rad*angle_err*50.0;
	}

	//
	double top_intersect = vs.hx*(bafto_y_sample-vs.y)/vs.hy + vs.x ;
	bool valid = false;
	if( top_intersect>neg_x_bound+vehicle.width && top_intersect<pos_x_bound-vehicle.width ){
		if(top_intersect<0){
			int idx = int( - top_intersect / bafto_idx_loc_conv );
			if(idx<width_resolution){
				valid = true;
				best_angle = -PI - best_angle_for_top_offset[idx];
				}
		}else{
			int idx = int( top_intersect / bafto_idx_loc_conv );
			if(idx<width_resolution){
				valid = true;
				best_angle = best_angle_for_top_offset[idx];
			}
		}
		if(valid && vs.hy < 0){
			double angle_err = AbsAngleDiff(best_angle, cur_angle);
			if( 1/*angle_err < PI/2.0 && ( wall_sep-max_y<vehicle.min_turning_rad/5 || min_y<vehicle.min_turning_rad/5 )*/){
				return -50000 /*-abs(vs.x)*/ - vehicle.min_turning_rad*angle_err*50.0;
			}
		}
	}
	
	//stuff that attempts (and fails) to achieve vertical alignment.
	double horiz_min_wiggle = min( min_x-neg_x_bound , pos_x_bound-max_x );
	if(vs.y>wall_sep-vehicle.back_bumper*1.5 && AbsAngleDiff(cur_angle, -PI/2.0)<PI/8.0 && horiz_min_wiggle>vehicle.width/2.0 ){
		return -100000;
	}
	return -200000 - vs.hy;// - wall_error*50 ;

}

Parker::~Parker(){
	delete[] slots;
}

VehicleState Parker::GetClosestExtent(double angle, double seperation, double horiz){
	double c = cos(angle);
	double s = sin(angle);
	horiz += s*seperation;
	double back = c*seperation + abs(s)*vehicle.width/2.0 ;

	VehicleState ret;
	ret.hx = s ;
	ret.hy = c ;
	ret.x = horiz ;
	ret.y = back ;
	return ret;
}

VehicleState Parker::GetXWallHits(double angle, double horiz, double wall, bool pos, bool front){
	double safe_distance = slot_definition_safety_fraction * vehicle.min_turning_rad ;
	VehicleState ret;
	ret = GetClosestExtent(angle, safe_distance + ( front ? vehicle.front_bumper : vehicle.back_bumper ), horiz);
	if(front){
		ret.hx = -ret.hx;
		ret.hy = -ret.hy;
	}
	if(!pos){
		ret.y = -ret.y;
		ret.hy = -ret.hy;
	}
	ret.y += wall;
	return ret;
}

VehicleState Parker::GetYWallHits(double angle, double horiz, double wall, bool pos, bool front){
	VehicleState x = GetXWallHits(angle, horiz, 0, pos, front);
	VehicleState ret;
	ret.y  =  x.x  ;
	ret.x  = -x.y + wall ;
	ret.hy =  x.hx ;
	ret.hx = -x.hy ;
	return ret;
}

bool Parker::SubFinalAlignment(VehicleState from, MovingOrder& order, double& util ){
	double y_scan_start = vehicle.front_bumper ;
	double y_scan_stop  = wall_sep - vehicle.back_bumper ;
	const int num_y_scans = 15;

	bool possible[num_y_scans];
	double utility[num_y_scans];
	MovingOrder orders[num_y_scans];

	bool at_all_possible = false;
	for(int i=0; i<num_y_scans; i++){
		double t = double(i) / double(num_y_scans-1);
		double y = y_scan_start*(1.0-t) + y_scan_stop*t ;

		VehicleState vs;
		vs.hx = 0;
		vs.hy = -1;
		vs.x = 0;
		vs.y = y;

		MovingOrder o1, o2;
		possible[i] = Get_PA_PA_Traversal_Gentile(from, vs, o1, o2) ;
		if(possible[i]){
			at_all_possible = true;
			utility[i] = GetFinalAlignmentUtility( y, abs(o1.turning_radius), ArcLen(from, o1) + ArcLen(o1.dest_state, o2) );
			orders[i] = o1;
		}
	}

	if(!at_all_possible) return false;

	double max_utility = -DBL_MAX;
	int best_idx = 0;
	for(int i=0; i<num_y_scans; i++){
		if( possible[i] && utility[i] > max_utility ){
			max_utility = utility[i];
			best_idx = i;
		}
	}
	order = orders[best_idx];
	util = utility[best_idx];
	return true;
}

bool Parker::GetNextParkingOrder(MovingOrder& order, VehicleState state){
	Trans_O_I(state);

	double vio = QueryStaticViolation(state);
	if(vio>0.0){
		cout << "Parker: outside rectange by " << vio << endl;
	}

	if(QueryComplete(state)){
		cout << "Parking: Declaring Complete!" << endl;
		return false;
	}

	if(FinishingOrder(state, order)){
		cout << "Parking: Ordering Final Pull in." << endl;
		return true;
	}

	NudgeInBounds(state);

	if(FinalAlignment(state, order)){
		cout << "Parking: Ordering Final Alignment." << endl;
		return true;
	}

	//check for two-pass connection to any place on the centerline.
	double disc;
	if(SubFinalAlignment(state, order, disc)){
		cout << "Parking: Ording sub-final alignment." << endl;
		PolishMovingOrder(order);
		return true;
	}

	//ok, so we try to hit everything, and keep track of the best.
	DestinationSlot best_dest;
	best_dest.utility = -DBL_MAX;
	DestinationSlot best_direct_dest;
	best_direct_dest.utility = -DBL_MAX;
	for(int i=0; i<num_slots; i++){
		DestinationSlot slot = slots[i];
		
		MovingOrder o1, o2;
		if( slot.utility>best_dest.utility && Get_PA_PA_Traversal_Gentile(state, slot.state, o1, o2) ){
			best_dest = slot;
		}
		
		//TODO: reinsert this
		MovingOrder order;
		if( GetOneMoveTraversal(state, slot.state, order) ){
			if( slot.utility>best_direct_dest.utility ){
				best_direct_dest = slot ;
			}
		}
	}

	//best_direct_dest.utility += abs(best_direct_dest.utility)*0.05 ;
	if( best_dest.utility > best_direct_dest.utility ){

		MovingOrder o2;
		Get_PA_PA_Traversal_Gentile(state, best_dest.state, order, o2);
		cout << "Parking: Moving first in two-part heuristic destination with utility " << best_dest.utility << endl;
		PolishMovingOrder(order);
		return true;

	}else{

		GetOneMoveTraversal(state, best_direct_dest.state, order);
		cout << "Parking: Moving  in one-part heuristic destination with utility " << best_direct_dest.utility << endl;
		PolishMovingOrder(order);
		return true;
	}
}


bool Parker::GetNextPulloutOrder(MovingOrder& order, VehicleState state){
	Trans_O_I(state);
	
	double vio = QueryStaticViolation(state);
	if(vio>0.0){
		cout << "Pullout: outside rectange by " << vio << endl;
	}

	if( QueryStaticViolation(state) > vehicle.min_turning_rad/10.0 ){

		double stop_y = vehicle.front_bumper + vehicle.min_turning_rad/10.0 ;// wall_sep - vehicle.back_bumper - vehicle.width/4;
		double stop_x = state.hx*(stop_y-state.y)/state.hy+state.x;

		order = ConstructOneMoveTraversal(state, stop_x, stop_y, false);
		PolishMovingOrder(order);
		cout << "Pullout: Performing initial back up into rectangle." << endl;
		return true;

	}else{
		bool turn_clockwise;
		const double sufficient_angle = 0.95;
		if(pullout_to_plus_x){
			turn_clockwise = state.hy>0;
			if(state.hx> sufficient_angle){
				cout << "Pullout: Declaring Complete!" << endl;
				return false;
			}
		}else{
			turn_clockwise = state.hy<0;
			if(state.hx<-sufficient_angle){
				cout << "Pullout: Declaring Complete!" << endl;
				return false;
			}
		}

		NudgeInBounds(state);

		MovingOrder forw_order, back_order;
		double stop_angle = pullout_to_plus_x ? 0.0 : PI ;
		if(turn_clockwise){
			forw_order = GetMaximumLockExtent(state, true, false,  stop_angle);
			back_order = GetMaximumLockExtent(state, false, true,  stop_angle);
		}else{
			forw_order = GetMaximumLockExtent(state, true, true,   stop_angle);
			back_order = GetMaximumLockExtent(state, false, false, stop_angle);
		}

		if(pullout_to_plus_x) order = forw_order.dest_state.hx>back_order.dest_state.hx ? forw_order : back_order ;
		else                  order = forw_order.dest_state.hx<back_order.dest_state.hx ? forw_order : back_order ;

		PolishMovingOrder(order);
		cout << "Pullout: Doing middle maneuver. clockwise = " << turn_clockwise << endl;
		return true;
	}
}

bool Parker::FinalAlignment(VehicleState vs, MovingOrder& order){
	if(vs.hy>0) return false;

	double join_len = abs(vs.x) / tan(atan2(abs(vs.hx), abs(vs.hy))/2.0);

	bool p_left = vs.x<0;
	bool a_left = vs.hx<0;
	order.dest_state.x = 0;
	order.dest_state.hx = 0;
	order.dest_state.hy = -1;
	if( p_left&&a_left || !p_left&&!a_left ){
		order.forward = false;
		order.dest_state.y = vs.y + join_len;
	}else{
		order.forward = true;
		order.dest_state.y = vs.y - join_len;
	}
	const double fudge = vehicle.min_turning_rad / 30.0 ;
	double bot = order.dest_state.y - vehicle.front_bumper + fudge ;
	double top = order.dest_state.y + vehicle. back_bumper - fudge ;

	if( bot<0 || top>wall_sep ) return false;

	order.turning_radius = abs(vs.hy*join_len/vs.hx) + abs(vs.x) ;
	if(order.turning_radius<vehicle.min_turning_rad) return false;
	if(p_left) order.turning_radius = -order.turning_radius ;
	order.center_x = p_left ? -abs(order.turning_radius) : abs(order.turning_radius) ;
	order.center_y = order.dest_state.y ;

	PolishMovingOrder(order);
	return true;
}

bool Parker::FinishingOrder(VehicleState vs, MovingOrder& order){

	order = ConstructOneMoveTraversal(vs, 0, forward_park_position + vehicle.front_bumper, true);

	if( abs(vs.x)>vehicle.min_turning_rad*complete_distance_fudge
		|| abs(atan2(order.dest_state.hx, -order.dest_state.hy))>complete_angular_fudge ) return false;

	PolishMovingOrder(order);
	return true;
}


MovingOrder Parker::ConstructOneMoveTraversal(VehicleState start, double to_x, double to_y, bool forward){
	start.Normalize();
	double m_x = (start.x + to_x) / 2.0 ;
	double m_y = (start.y + to_y) / 2.0 ;
	double cf_y = start.y - m_y ;
	double cf_x = start.x - m_x ;
	double rad = - (cf_x*cf_x+cf_y*cf_y) / (-start.hy*cf_x+start.hx*cf_y) ;

	MovingOrder ret;

	ret.center_x = start.x - rad*start.hy ;
	ret.center_y = start.y + rad*start.hx ;

	ret.forward = forward;
	ret.turning_radius = rad ;

	ret.dest_state.x = to_x;
	ret.dest_state.y = to_y;

	double cf_len = sqrt(cf_x*cf_x+cf_y*cf_y);
	double flip_x = -cf_y / cf_len ;
	double flip_y =  cf_x / cf_len ;
	double rev = flip_x*start.hx + flip_y*start.hy ;
	ret.dest_state.hx = start.hx - 2.0*rev*flip_x ;
	ret.dest_state.hy = start.hy - 2.0*rev*flip_y ;

	return ret;
}

bool Parker::GetOneMoveTraversal(VehicleState start, VehicleState to, MovingOrder& order){
	MovingOrder forw = ConstructOneMoveTraversal(start, to.x, to.y, true);
	MovingOrder rev  = forw;
	rev.forward = false;

	double angle_error = AbsAngleDiff( atan2(to.hy, to.hx) , atan2(forw.dest_state.hy, forw.dest_state.hx) );
	if( angle_error > PI/double(angular_resolution) ) return false;

	if( abs(forw.turning_radius) < vehicle.min_turning_rad ) return false;

	bool feasible_forw = QueryMovementViolation(start, forw) == 0 ;
	bool feasible_rev  = QueryMovementViolation(start, rev)  == 0 ;

	if( !feasible_forw && !feasible_rev ) return false;

	if( feasible_forw && feasible_rev ){
		if( ArcLen(start, forw) > ArcLen(start, rev) ){
			order = rev;
		}else{
			order = forw;
		}
		return true;
	}

	if(feasible_forw){
		order = forw;
		return true;
	}else{
		order = rev;
		return true;
	}

}


const double Parker::complete_distance_fudge = 0.2;
const double Parker::complete_angular_fudge  = 0.1;
bool Parker::QueryComplete(VehicleState vs){
	double dx = vs.x;
	double dy = vs.y - (forward_park_position + vehicle.front_bumper);
	return sqrt( dx*dx + dy*dy )<vehicle.min_turning_rad*complete_distance_fudge ;
}

void VehicleState::Normalize(){
	double len = sqrt(hx*hx + hy*hy);
	hx /= len;
	hy /= len;
}

/*void Parker::PushBackUnfinishedOrder(MovingOrder mo){
	Trans_I_O(mo.dest_state);
	pending_orders.push_back(mo);
}*/


void Parker::Trans_I_O(VehicleState& vs){
	double x = front_wall_x + vs.x*front_wall_py + vs.y*front_wall_px ;
	double y = front_wall_y - vs.x*front_wall_px + vs.y*front_wall_py ;
	double hx =   vs.hx*front_wall_py + vs.hy*front_wall_px ;
	double hy = - vs.hx*front_wall_px + vs.hy*front_wall_py ;
	vs.x = x; vs.y = y; vs.hx = hx; vs.hy = hy;
}

void Parker::Trans_O_I(VehicleState& vs){
	vs.x -= front_wall_x;
	vs.y -= front_wall_y;
	double x = front_wall_py*vs.x - front_wall_px*vs.y ;
	double y = front_wall_px*vs.x + front_wall_py*vs.y ;
	double hx = front_wall_py*vs.hx - front_wall_px*vs.hy ;
	double hy = front_wall_px*vs.hx + front_wall_py*vs.hy ;
	vs.x = x; vs.y = y; vs.hx = hx; vs.hy = hy;
}

void Parker::PolishMovingOrder(MovingOrder& mo){
	VehicleState center_point;
	center_point.x = mo.center_x;
	center_point.y = mo.center_y;
	Trans_I_O(mo.dest_state);
	Trans_I_O(center_point);
	mo.center_x = center_point.x;
	mo.center_y = center_point.y;
}

bool Parker::Get_PA_PA_Traversal_Gentile(VehicleState s1, VehicleState s2, MovingOrder& o1, MovingOrder& o2){
	
	s1.Normalize();
	s2.Normalize();

	MovingOrder sln1[8], sln2[8];
	double length[8];

	double s1_tx =  s1.hy ;
	double s1_ty = -s1.hx ;
	double s2_tx = -s2.hy ;
	double s2_ty =  s2.hx ;
		
	double dp_x = s1.x - s2.x ;
	double dp_y = s1.y - s2.y ;

	double dt_x = s1_tx - s2_tx ;
	double dt_y = s1_ty - s2_ty ;
		
	double a = dt_x*dt_x + dt_y*dt_y - 4.0 ;
	double b = 2.0*(dp_x*dt_x + dp_y*dt_y) ;
	double c = dp_x*dp_x + dp_y*dp_y       ;

	double sq = sqrt(b*b-4.0*a*c) ;

	double r0 = 2.0*c / ( -b - sq ) ;
	double r1 = 2.0*c / ( -b + sq ) ;

	for(int i=0; i<2; i++){
		double r = i==0 ? r0 : r1 ;

		if(!_finite(r)){
			for(int j=0; j<4; j++){
				int idx = i*4 + j;
				length[idx] = DBL_MAX;
			}
			continue;
		}
			
		double c1_x = s1.x + s1_tx*r ;
		double c1_y = s1.y + s1_ty*r ;
	
		double c2_x = s2.x + s2_tx*r ;
		double c2_y = s2.y + s2_ty*r ;

		VehicleState mid;
		mid.x = ( c1_x + c2_x ) / 2.0 ;
		mid.y = ( c1_y + c2_y ) / 2.0 ;
		mid.hx = c2_y - c1_y ;
		mid.hy = c1_x - c2_x ;
		mid.Normalize();

		if( ( mid.hy*(c1_x-mid.x) - mid.hx*(c1_y-mid.y) )*( s1.hy*(c1_x-s1.x) - s1.hx*(c1_y-s1.y) ) < 0 ){
			mid.hx = -mid.hx;
			mid.hy = -mid.hy;
		}

		for(int j=0; j<4; j++){
			int idx = i*4 + j;
			
			sln1[idx].center_x = c1_x;
			sln1[idx].center_y = c1_y;
			sln1[idx].dest_state = mid;
			sln1[idx].forward = j%2 == 0 ;
			sln1[idx].turning_radius = -r ;
			
			sln2[idx].center_x = c2_x;
			sln2[idx].center_y = c2_y;
			sln2[idx].dest_state = s2;
			sln2[idx].forward = j/2 == 0 ;
			sln2[idx].turning_radius = r ;

			length[idx] = ArcLen(s1, sln1[idx]) + ArcLen(sln1[idx].dest_state, sln2[idx]);
		}
	}

	int best_idx=-1;
	for(int i=0; i<8; i++){
		if( abs(sln1[i].turning_radius) < vehicle.min_turning_rad ) continue;
		if( QueryMovementViolation(s1, sln1[i])!=0.0 || QueryMovementViolation(sln1[i].dest_state, sln2[i])!=0.0 ) continue;
		//this one's bizzarre, but whatever:
		if( ArcLen(s1, sln1[i]) < vehicle.min_turning_rad/15.0 ) continue;
		if( length[i] < length[best_idx] || best_idx==-1 ) best_idx = i;
	}
	if(best_idx==-1){
		//cout << "infeasible" << endl;
		return false;
	}
	/*double v1 = QueryMovementVioloation(s1, sln1[best_idx]) ;
	double v2 = QueryMovementVioloation(sln1[best_idx].dest_state, sln2[best_idx]) ;
	if( v1!=0 || v2!=0 )
		cout << "movement violation, v1=" << v1 << "  v2=" << v2 << endl ;*/

	o1 = sln1[best_idx] ;
	o2 = sln2[best_idx] ;

	return true;
}

void Parker::GetWiggleRoom(VehicleState vs, double& front_wiggle, double& back_wiggle){
	//dist to wall
	double dtw_x_neg;
	double dtw_x_pos;
	double dtw_y_neg;
	double dtw_y_pos;
	GetStaticExtents(vs, dtw_y_pos, dtw_y_neg, dtw_x_pos, dtw_x_neg);
	
	dtw_x_neg = dtw_x_neg - neg_x_bound ;
	dtw_x_pos = pos_x_bound - dtw_x_pos ;
	dtw_y_neg = dtw_y_neg ;
	dtw_y_pos = wall_sep - dtw_y_pos ;

	front_wiggle = back_wiggle = vehicle.min_turning_rad * 100 ;

	//top&bot limitations:
	if(vs.hy<0){
		back_wiggle  = min(back_wiggle,  dtw_y_pos / -vs.hy);
		front_wiggle = min(front_wiggle, dtw_y_neg / -vs.hy);
	}else{
		back_wiggle  = min(back_wiggle,  dtw_y_neg /  vs.hy);
		front_wiggle = min(front_wiggle, dtw_y_pos /  vs.hy);
	}

	//left&right
	if(vs.hx<0){
		back_wiggle  = min(back_wiggle,  dtw_x_pos / -vs.hx);
		front_wiggle = min(front_wiggle, dtw_x_neg / -vs.hx);
	}else{
		back_wiggle  = min(back_wiggle,  dtw_x_neg /  vs.hx);
		front_wiggle = min(front_wiggle, dtw_x_pos /  vs.hx);
	}
}

MovingOrder Parker::GetMaximumLockExtent(VehicleState vs, bool direction, bool left, double stop_at_heading){
	MovingOrder ret;
	ret.turning_radius = left ? vehicle.min_turning_rad : -vehicle.min_turning_rad ;
	ret.forward = direction;
	double c_x = vs.x - vs.hy*ret.turning_radius ;
	double c_y = vs.y + vs.hx*ret.turning_radius ;
	ret.center_x = c_x;
	ret.center_y = c_y;

	const int resolution = 1000;

	const double inc = 2*PI / 1000.0;

	double start_angle = atan2(vs.hy, vs.hx);

	for(double t=inc; t<2*PI*vehicle.min_turning_rad; t += inc ){
		double a;
		if(direction && left || !direction && !left){
			a = start_angle + t;
		}else{
			a = start_angle - t;
		}
		double s = sin(a);
		double c = cos(a);
		VehicleState to;
		to.hx = c;
		to.hy = s;
		to.x  = c_x + s*ret.turning_radius ;
		to.y  = c_y - c*ret.turning_radius ;
		ret.dest_state = to;
		if( QueryMovementViolation(vs, ret) > 0 ){
			return ret;
		}
		if( AbsAngleDiff(a, stop_at_heading)<2*inc || AbsAngleDiff(a, stop_at_heading)<2*inc ){
			return ret;
		}
	}
	assert(0);
	return ret;
}


void Parker::GetMovementExtents(VehicleState vs, MovingOrder order, double& max_y, double& min_y, double& max_x, double& min_x){

	VehicleState beg_state = vs;
	VehicleState end_state = order.dest_state;
	
	bool clockwise = order.forward != order.turning_radius>0 ;

	if(!clockwise) swap(beg_state, end_state);

	double beg_xs[4];
	double beg_ys[4];
	
	double end_xs[4];
	double end_ys[4];

	FillVehicleCorners(beg_xs, beg_ys, beg_state);
	FillVehicleCorners(end_xs, end_ys, end_state);

	double beg_t_x = beg_state.x - order.center_x;
	double beg_t_y = beg_state.y - order.center_y;
	
	double end_t_x = beg_state.x - order.center_x;
	double end_t_y = beg_state.y - order.center_y;

	max_y = -DBL_MAX;
	max_x = -DBL_MAX;
	min_y =  DBL_MAX;
	min_x =  DBL_MAX;
	for(int i=0; i<8; i++){
		double x = i%2 ? beg_xs[i/2] : end_xs[i/2];
		double y = i%2 ? beg_ys[i/2] : end_ys[i/2];
		max_y = max(max_y, y);
		max_x = max(max_x, x);
		min_y = min(min_y, y);
		min_x = min(min_x, x);
	}

	//now, optionally add each orthogonal alignment
	for(int i=0; i<4; i++){
		double b_dx = beg_xs[i] - order.center_x;
		double b_dy = beg_ys[i] - order.center_y;
		double e_dx = end_xs[i] - order.center_x;
		double e_dy = end_ys[i] - order.center_y;
		double r = ( sqrt(b_dx*b_dx+b_dy*b_dy) + sqrt(e_dx*e_dx+e_dy*e_dy) ) / 2.0 ;
		double txs[4] = {1,0,-1,0};
		double tys[4] = {0,-1,0,1};

		for(int j=0; j<4; j++){
			double tx = txs[j] * r ;
			double ty = tys[j] * r ;

			if(ClockHitsAFirst(b_dx, b_dy, tx, ty, e_dx, e_dy)){

				tx += order.center_x;
				ty += order.center_y;
				max_y = max( max_y, ty );
				max_x = max( max_x, tx );
				min_y = min( min_y, ty );
				min_x = min( min_x, tx );
			}
		}
	}
}

double Parker::AbsAngleDiff(double a1, double a2){
	while(a1-a2>PI){
		a1 -= 2*PI;
	}
	while(a1-a2<-PI){
		a1 += 2*PI;
	}
	return abs(a1-a2);
}

void Parker::DrawArc(double c_x, double c_y, double x1, double y1, double x2, double y2, bool cw){
	x1 -= c_x;
	x2 -= c_x;
	y1 -= c_y;
	y2 -= c_y;
	double ang_start = atan2(y1, x1);
	double ang_stop  = atan2(y2, x2);
	double r1 = sqrt(x1*x1+y1*y1);
	double r2 = sqrt(x2*x2+y2*y2);
	assert(abs(r1-r2)<5.0);
	double r = (r1+r2)/2.0;

	double ang = ang_start;
	const double inc = PI/100;
	while( AbsAngleDiff(ang, ang_stop) > 2*inc ){
		double next;
		if(cw){
			next = ang - inc;
		}else{
			next = ang + inc;
		}

		double a_x = 400 + c_x + cos(ang)*r;
		double a_y = 400 + c_y + sin(ang)*r;
		double b_x = 400 + c_x + cos(next)*r;
		double b_y = 400 + c_y + sin(next)*r;
		assert(0);

		ang = next;
	}
}

void Parker::FillVehicleCorners(double* xs, double* ys, VehicleState vs){
	for(int i=0; i<4; i++){
		xs[i] = vs.x;
		ys[i] = vs.y;
	}
	double sx = -vs.hy*vehicle.width/2.0;
	double sy =  vs.hx*vehicle.width/2.0;
	xs[0] += sx; ys[0] += sy;
	xs[1] += sx; ys[1] += sy;
	xs[2] -= sx; ys[2] -= sy;
	xs[3] -= sx; ys[3] -= sy;
	
	xs[0] += vs.hx*vehicle.front_bumper; ys[0] += vs.hy*vehicle.front_bumper;
	xs[2] += vs.hx*vehicle.front_bumper; ys[2] += vs.hy*vehicle.front_bumper;
	xs[1] -= vs.hx*vehicle.back_bumper;  ys[1] -= vs.hy*vehicle.back_bumper;
	xs[3] -= vs.hx*vehicle.back_bumper;  ys[3] -= vs.hy*vehicle.back_bumper;
}

void Parker::GetStaticExtents(VehicleState vs, double& max_y, double& min_y, double& max_x, double& min_x){
	double fx[4];
	double fy[4];
	FillVehicleCorners(fx, fy, vs);
	min_y = max_y = vs.y;
	min_x = max_x = vs.x;
	for(int i=0; i<4; i++){
		min_y = min(min_y, fy[i]);
		max_y = max(max_y, fy[i]);
		min_x = min(min_x, fx[i]);
		max_x = max(max_x, fx[i]);
	}
}

double Parker::QueryStaticViolation(VehicleState vs){
	double max_y, min_y, max_x, min_x;
	GetStaticExtents(vs, max_y, min_y, max_x, min_x);

	min_y = -min_y;
	max_y = max_y - wall_sep;
	min_x = neg_x_bound - min_x;
	max_x = max_x - pos_x_bound;
	double ret = max( max(min_y, max_y), max(min_x, max_x) );
	ret = max(ret, 0);
	if(ret==-0) ret = 0;
	return ret;
}

bool Parker::InBounds(VehicleState vs){
	Trans_O_I(vs);
	return QueryStaticViolation(vs) == 0 ;
}

bool Parker::QueryStateCompletesMovement(VehicleState vs, MovingOrder order){
	vs.x -= order.dest_state.x;
	vs.y -= order.dest_state.y;

	if( sqrt(vs.x*vs.x+vs.y*vs.y) > vehicle.min_turning_rad/4 ) return false;

	double t = order.dest_state.hx*vs.x + order.dest_state.hy*vs.y ;

	return t<0 == order.forward ;
}


double Parker::QueryMovementViolation(VehicleState start, MovingOrder order){
	double max_y, min_y, max_x, min_x;
	GetMovementExtents(start, order, max_y, min_y, max_x, min_x);
	
	min_y = -min_y;
	max_y = max_y - wall_sep;
	min_x = neg_x_bound - min_x;
	max_x = max_x - pos_x_bound;
	double ret = max( max(min_y, max_y), max(min_x, max_x) );
	ret = max(ret, 0);

	if(ret==-0) ret = 0;

	return ret;
}

void Parker::NudgeInBounds(VehicleState& vs){
	
	double fx[4];
	double fy[4];
	FillVehicleCorners(fx, fy, vs);
	double min_y=0, max_y=0;
	double min_x=0, max_x=0;
	for(int i=0; i<4; i++){
		min_y = min(min_y, fy[i]);
		max_y = max(max_y, fy[i]);
		min_x = min(min_x, fx[i]);
		max_x = max(max_x, fx[i]);
	}
	const double s_nudge = vehicle.min_turning_rad / 1000;
	min_y -= s_nudge;
	max_y += s_nudge;
	min_x -= s_nudge;
	max_x += s_nudge;

	double x_move=0;
	double y_move=0;
	if(min_x <= neg_x_bound){
		x_move = neg_x_bound - min_x;
	}
	if(max_x >= pos_x_bound){
		x_move = pos_x_bound - max_x;
	}
	if(min_y<=0){
		y_move = -min_y;
	}
	if(max_y>=wall_sep){
		y_move = wall_sep - max_y;
	}

	vs.x += x_move;
	vs.y += y_move;
}

bool Parker::ClockHitsAFirst(double start_x, double start_y, double a_x, double a_y, double b_x, double b_y){
	double start = atan2(start_y, start_x);
	double a = atan2(a_y, a_x);
	double b = atan2(b_y, b_x);

	a -= start;
	b -= start;

	if(a<0) a+= 2*PI;
	if(b<0) b+= 2*PI;

	return a>b;
}

double Parker::ArcLen(VehicleState start, MovingOrder order){
	double dir = (order.dest_state.x-start.x)*start.hx + (order.dest_state.y-start.y)*start.hy ;
	bool sel_small = dir>0 == order.forward ;
	double ang_beg = atan2( start.y            - order.center_y , start.x            - order.center_x );
	double ang_end = atan2( order.dest_state.y - order.center_y , order.dest_state.x - order.center_x );
	double diff = abs(ang_end - ang_beg) ;
	double sm = min( diff, 2.0*PI - diff );
	double la = 2.0*PI - sm ;
	double ang = sel_small ? sm : la ;
	return abs( ang * order.turning_radius );
}
