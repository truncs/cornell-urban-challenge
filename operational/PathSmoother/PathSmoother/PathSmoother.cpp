#ifndef _PATH_SMOOTHER_H
#define _PATH_SMOOTHER_H

#include "smoother.h"
#include "loqo_smoother.h"
//#include "ipopt_smoother.h"
#include "polygon.h"

#ifdef __cplusplus_cli
#pragma managed(push,on)

using namespace System;
using namespace System::Collections::Generic;

using namespace UrbanChallenge::Common;
using namespace UrbanChallenge::Common::Shapes;
using namespace UrbanChallenge::Common::Path;
using namespace UrbanChallenge::Operational::Common;

namespace UrbanChallenge {
	namespace PathSmoothing {

		public value class PathPoint {
		public:
			double x, y, v;

			PathPoint(double x, double y, double v) {
				this->x = x;
				this->y = y;
				this->v = v;
			}
		};

		public enum class SmoothingAlgorithm {
			Ipopt,
			Loqo
		};

		public ref class SmootherOptions {
		public:
			// weighting coefficients
			// curvature penalty
			double alpha_c;
			// change in curvature penalty
			double alpha_d;
			// deviation from base path penalty
			double alpha_w;
			// spacing violation penalty
			double alpha_s;
			// velocity benefit
			double alpha_v;
			// forward-back acceleration penalty
			double alpha_a;

			// maximum lateral acceleration
			double a_lat_max;
			// maximum braking acceleration
			double a_brake_max;
			// maximum steering
			double k_max;
			// offset difference bound
			double w_diff;

			// initial heading constraints
			bool set_init_heading;
			double init_heading;

			// final heading constraints
			bool set_final_heading;
			double final_heading;

			// final offset constraints
			// if true, will not allow the final point to move
			bool set_final_offset;
			double final_offset_min;
			double final_offset_max;

			// initial steering constraints
			bool set_init_steering;
			double init_steer_min;
			double init_steer_max;

			// initial velocity constraints
			bool set_min_init_velocity;
			double min_init_velocity;
			bool set_max_init_velocity;
			double max_init_velocity;

			// overall velocity constraints
			double max_velocity;
			double min_velocity;

			// final velocity constraints
			bool set_final_velocity_max;
			double final_velocity_max;

			// number of passes
			int num_passes;

			// what algorithm to use
			SmoothingAlgorithm alg;

			// determines if we want to generate path smoothing details
			bool generate_details;

			// reverse planning flag
			bool reverse;
		};

		public enum class SmoothResult {
			Sucess,
			Infeasible,
			FailedToConverge,
			Error
		};

		public ref class PathSmoother {
		private:
			smoother* sm;
			SmoothingDetails^ details;

			line_list* create_line_list(IList<Coordinates>^ pts) {
				int n = pts->Count;
				vector<vec2> pt_vec;
				pt_vec.reserve(n);

				for (int i = 0; i < n; i++) {
					pt_vec.push_back(vec2(pts[i].X, pts[i].Y));
				}

				return new line_list(pt_vec);
			}

			line_list* create_polygon(IList<Coordinates>^ pts) {
				int n = pts->Count+1;
				vector<vec2> pt_vec;
				pt_vec.reserve(n+1);

				for (int i = 0; i < n; i++) {
					pt_vec.push_back(vec2(pts[i].X, pts[i].Y));
				}

				if (n > 1) {
					pt_vec.push_back(vec2(pts[0].X, pts[0].Y));
				}

				return new line_list(pt_vec);
			}

		public:
			static SmootherOptions^ GetDefaultOptions() {
				SmootherOptions^ opt = gcnew SmootherOptions();

				// weighting coefficients
				opt->alpha_c = 10;
				opt->alpha_d = 100;
				opt->alpha_w = 0.00025;
				opt->alpha_a = 10;
				opt->alpha_v = 10;
				opt->alpha_s = 10000;

				// maximum accelerations
				opt->a_brake_max = 6;
				opt->a_lat_max = 4;

				// velocity limits
				opt->min_velocity = 1;
				opt->max_velocity = 13.3;

				opt->w_diff = 1;

				// steering limits
				opt->k_max = K_MAX;

				// number of passes
				opt->num_passes = 3;

				// mark that we're not setting any optional stuff
				opt->set_final_heading = false;
				opt->set_final_offset = false;
				opt->set_init_heading = false;
				opt->set_init_heading = false;
				opt->set_init_steering = false;
				opt->set_min_init_velocity = false;
				opt->set_max_init_velocity = false;
				opt->set_final_velocity_max = false;

				// set the algorithm
				opt->alg = SmoothingAlgorithm::Loqo;

				// flag that we don't want to generate smoothing details by default
				opt->generate_details = false;
				opt->reverse = false;
				
				return opt;
			}

			void Cancel() {
				if (sm != NULL) {
					//sm->cancel();
				}
			}

			SmoothResult SmoothPath(LineList^ base_path, List<Boundary^>^ target_paths, List<Boundary^>^ ub_list, List<Boundary^>^ lb_list, SmootherOptions^ opt, List<PathPoint>^ ret_path) {
				smoother_engine* engine = NULL;
				sm = NULL;

				solve_result sr = sr_error;

				try {
					// convert the base path
					shared_ptr<line_list> path = create_line_list(base_path);

					vector<boundary> target_path_bounds;
					target_path_bounds.reserve(target_paths->Count);
					for each (Boundary^ b in target_paths) {
						shared_ptr<shape> shape_ptr = create_line_list(b->Coords);

						target_path_bounds.push_back(boundary(shape_ptr, side_path, 0, 0, b->AlphaS, b->Index, b->CheckSmallObstacle));
					}

					// convert each boundary
					vector<boundary> ub_bounds;
					ub_bounds.reserve(ub_list->Count);
					for each (Boundary^ b in ub_list) {
						shared_ptr<shape> shape_ptr;
						if (b->Polygon) {
							shape_ptr = create_polygon(b->Coords);
						}
						else {
							shape_ptr = create_line_list(b->Coords);
						}

						ub_bounds.push_back(boundary(shape_ptr, side_left, b->DesiredSpacing, b->MinSpacing, b->AlphaS, b->Index, b->CheckSmallObstacle));
					}

					vector<boundary> lb_bounds;
					lb_bounds.reserve(lb_list->Count);
					for each (Boundary^ b in lb_list) {
						shared_ptr<shape> shape_ptr;
						if (b->Polygon) {
							shape_ptr = create_polygon(b->Coords);
						}
						else {
							shape_ptr = create_line_list(b->Coords);
						}

						lb_bounds.push_back(boundary(shape_ptr, side_right, b->DesiredSpacing, b->MinSpacing, b->AlphaS, b->Index, b->CheckSmallObstacle));
					}

					// create the bound finder
					shared_ptr<bound_finder> target_path_finder = new bound_finder(target_path_bounds, side_path);
					shared_ptr<bound_finder> ub_finder = new bound_finder(ub_bounds, side_left);
					shared_ptr<bound_finder> lb_finder = new bound_finder(lb_bounds, side_right);

					// create the path smoother
					if (opt->alg == SmoothingAlgorithm::Loqo)
						engine = new loqo_smoother();
						//throw gcnew InvalidOperationException();
					else
						throw gcnew InvalidOperationException();
						//sm = new ipopt_smoother(path, ub_finder, lb_finder);

					sm = new smoother(path, target_path_finder, ub_finder, lb_finder, engine);

					// assign the options
					sm->options.a_brake_max = opt->a_brake_max;
					sm->options.a_lat_max = opt->a_lat_max;
					sm->options.alpha_a = opt->alpha_a;
					sm->options.alpha_c = opt->alpha_c;
					sm->options.alpha_d = opt->alpha_d;
					sm->options.alpha_s = opt->alpha_s;
					sm->options.alpha_v = opt->alpha_v;
					sm->options.alpha_w = opt->alpha_w;
					sm->options.final_heading = opt->final_heading;
					sm->options.final_offset_min = opt->final_offset_min;
					sm->options.final_offset_max = opt->final_offset_max;
					sm->options.init_heading = opt->init_heading;
					sm->options.init_steer_max = opt->init_steer_max;
					sm->options.init_steer_min = opt->init_steer_min;
					sm->options.min_init_velocity = opt->min_init_velocity;
					sm->options.max_init_velocity = opt->max_init_velocity;
					sm->options.k_max = opt->k_max;
					sm->options.w_diff = opt->w_diff;
					sm->options.max_velocity = opt->max_velocity;
					sm->options.min_velocity = opt->min_velocity;
					sm->options.num_passes = opt->num_passes;
					sm->options.set_final_heading = opt->set_final_heading;
					sm->options.set_final_offset = opt->set_final_offset;
					sm->options.set_init_heading = opt->set_init_heading;
					sm->options.set_init_steering = opt->set_init_steering;
					sm->options.set_min_init_velocity = opt->set_min_init_velocity;
					sm->options.set_max_init_velocity = opt->set_max_init_velocity;
					sm->options.set_final_velocity_max = opt->set_final_velocity_max;
					sm->options.final_velocity_max = opt->final_velocity_max;
					sm->options.reverse = opt->reverse;

					// smooth the path
					vector<path_pt> ret;
					sr = sm->smooth_path(ret);

					// convert to the desired type
					ret_path->Clear();
					ret_path->Capacity = (int)ret.size();
					for (int i = 0; i < (int)ret.size(); i++) {
						ret_path->Add(PathPoint(ret[i].x, ret[i].y, ret[i].v));
					}

					// check if we want to build out the smoothing details
					if (opt->generate_details) {
						details = gcnew SmoothingDetails();
						
						int n_pts = sm->n_pts;
						// get the curvature constraint lamda terms
						details->k_constraint_lamda = gcnew array<double>(n_pts-2);
						for (int i = 1; i < n_pts-1; i++){
							details->k_constraint_lamda[i-1] = sm->lamda[CT_K_MAX(i)];
						}

	#ifdef OPT_VEL
						// get the acceleration constraint lamda terms
						details->a_constraint_lamda = gcnew array<double>(n_pts-1);
						for (int i = 0; i < n_pts-1; i++) {
							details->a_constraint_lamda[i] = sm->lamda[CT_A_LIM(i)];
						}
	#endif
						
						// get the bound information
						details->leftBounds = gcnew array<BoundInformation>(n_pts);
						details->rightBounds = gcnew array<BoundInformation>(n_pts);
						for (int i = 0; i < n_pts; i++) {
							details->rightBounds[i].boundaryHitIndex = sm->hit_index_l[i];
							details->rightBounds[i].deviation = sm->lb[i];
							details->rightBounds[i].spacing = sm->s_l[i];
							details->rightBounds[i].alpha_s = sm->alpha_s_l[i];
							details->rightBounds[i].spacing_violation = sm->q_l[i];

							details->leftBounds[i].boundaryHitIndex = sm->hit_index_u[i];
							details->leftBounds[i].deviation = sm->ub[i];
							details->leftBounds[i].spacing = sm->s_u[i];
							details->leftBounds[i].alpha_s = sm->alpha_s_u[i];
							details->leftBounds[i].spacing_violation = sm->q_u[i];
						}

						vector<vec2> bound_points;
						sm->get_lb_points(bound_points);
						for (int i = 0; i < n_pts; i++) {
							details->rightBounds[i].point.X = bound_points[i].x;
							details->rightBounds[i].point.Y = bound_points[i].y;
						}

						sm->get_ub_points(bound_points);
						for (int i = 0; i < n_pts; i++) {
							details->leftBounds[i].point.X = bound_points[i].x;
							details->leftBounds[i].point.Y = bound_points[i].y;
						}
					}
				}
				finally {
					delete sm;
					delete engine;
					sm = NULL;
					engine = NULL;
				}

				if (shape::shape_count > 0) {
					char buffer[1024];
					sprintf_s(buffer, "shape count: %d", shape::shape_count);
					Console::WriteLine(gcnew String(buffer));
				}

				switch (sr) {
					case sr_success:
						return SmoothResult::Sucess;

					case sr_failed_to_converge:
						return SmoothResult::FailedToConverge;

					case sr_infeasible:
						return SmoothResult::Infeasible;

					default:
						return SmoothResult::Error;
				}

				
			};

			SmoothingDetails^ GetSmoothingDetails() {
				return details;
			}

		};
	}
}

#pragma managed(pop)
#endif

#endif
