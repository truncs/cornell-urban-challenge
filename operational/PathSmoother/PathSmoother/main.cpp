#ifndef _USRDLL

#include "ipopt_smoother.h"
#include "loqo_smoother.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include <vector>
using namespace std;

#include "vec2.h"

void main() {
	// run the smoother
	
	vector<vec2> path_pts;
	path_pts.push_back(vec2(0,0));
	path_pts.push_back(vec2(5,0));
	path_pts.push_back(vec2(10,5));

	shared_ptr<line_list> base_path = new line_list(path_pts);

	vector<vec2> ub_pts;
	ub_pts.push_back(vec2(0,3));
	ub_pts.push_back(vec2(5,3));
	ub_pts.push_back(vec2(11,9));

	shared_ptr<shape> ub_list = new line_list(ub_pts);
	vector<boundary> ub_bound_vec;
	ub_bound_vec.push_back(boundary(ub_list, side_left, 1.0, 0.1));
	shared_ptr<bound_finder> ub_finder = new bound_finder(ub_bound_vec, side_left);

	vector<vec2> lb_pts;
	lb_pts.push_back(vec2(0,-3));
	lb_pts.push_back(vec2(5,-3));
	lb_pts.push_back(vec2(12,2));
	
	shared_ptr<shape> lb_list = new line_list(lb_pts);
	vector<boundary> lb_bound_vec;
	lb_bound_vec.push_back(boundary(lb_list, side_right, 0.75, 0.0));
	shared_ptr<bound_finder> lb_finder = new bound_finder(lb_bound_vec, side_right);

	smoother_options opt;
	// weighting coefficients
	opt.alpha_c = 10;
	opt.alpha_d = 100;
	opt.alpha_w = 0.00025;
	opt.alpha_a = 10;
	opt.alpha_v = 10;
	opt.alpha_s = 10000;

	// maximum accelerations
	opt.a_brake_max = 6;
	opt.a_lat_max = 4;

	// velocity limits
	opt.min_velocity = 1;
	opt.max_velocity = 13.3;

	// steering limits
	opt.k_max = K_MAX;

	// number of passes
	opt.num_passes = 2;

	// mark that we're not setting any optional stuff
	opt.set_final_heading = false;
	opt.set_final_offset = false;
	opt.set_init_heading = false;
	opt.set_init_heading = false;
	opt.set_init_steering = false;
	opt.set_init_velocity = false;
	
	
	if (false) {
		smoother* sm = new ipopt_smoother(base_path, ub_finder, lb_finder);

		sm->options = opt;
		
		vector<path_pt> ret;
		sm->smooth_path(ret);

		delete [] sm;

		getchar();
	}

	if (true) {
		smoother* sm = new loqo_smoother(base_path, ub_finder, lb_finder);

		sm->options = opt;

		vector<path_pt> ret;
		sm->smooth_path(ret);

		delete [] sm;
		getchar();
	}
}
#else

//#include <windows.h>
//
//#ifdef _MANAGED
//#pragma managed(push, off)
//#endif
//
//BOOL APIENTRY DllMain( HMODULE hModule,
//                       DWORD  ul_reason_for_call,
//                       LPVOID lpReserved
//					 )
//{
//	switch (ul_reason_for_call)
//	{
//	case DLL_PROCESS_ATTACH:
//	case DLL_THREAD_ATTACH:
//	case DLL_THREAD_DETACH:
//	case DLL_PROCESS_DETACH:
//		break;
//	}
//    return TRUE;
//}
//
//#ifdef _MANAGED
//#pragma managed(pop)
//#endif

#endif
