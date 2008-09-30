#include "Parker.h"
#include <iostream>
using namespace std;

#include <cxcore.h>
#include <cv.h>
#include <highgui.h>

#pragma comment(lib, "cxcore.lib")
#pragma comment(lib, "cv.lib")
#pragma comment(lib, "highgui.lib")

const int size = 800;

void Display(int wait=0);


bool SameSign(double a, double b){
	return a<0 && b<0 || a>0 && b>0;
}

CvPoint cvPoint(double x, double y){
	return cvPoint(int(x), int(y));
}

int lx, ly;

void Arrow(IplImage* img, VehicleState vs){
	vs.Normalize();
	cvLine(img, cvPoint(vs.x, vs.y), cvPoint(vs.x+20*vs.hx, vs.y+20*vs.hy), cvScalar(0,0,255), 3);
	cvCircle(img, cvPoint(vs.x, vs.y), 3, cvScalar(255,255,255), -1);
}

void Car(IplImage* img, VehicleState vs, VehicleParameters& vp){
	vs.Normalize();
	double tx =   vs.hy * vp.width / 2.0 ;
	double ty = - vs.hx * vp.width / 2.0 ;

	double x[4];
	double y[4];
	for(int i=0; i<4; i++){
		x[i] = vs.x;
		y[i] = vs.y;
		if(i==0 || i==1){
			x[i] += tx;
			y[i] += ty;
		}else{
			x[i] -= tx;
			y[i] -= ty;
		}
		if(i==1 || i==2){
			x[i] += vs.hx*vp.front_bumper;
			y[i] += vs.hy*vp.front_bumper;
		}else{
			x[i] -= vs.hx*vp.back_bumper;
			y[i] -= vs.hy*vp.back_bumper;
		}
	}
	for(int i=0; i<4; i++){
		int n = (i+1)%4;
		cvLine(img, cvPoint(x[i], y[i]), cvPoint(x[n], y[n]), cvScalar(255,128,128), 2); 
	}
	Arrow(img, vs);
}

void Simulate(MovingOrder order);

double meter_scale = 30.0;

IplImage* bg;
IplImage* img;
VehicleParameters vehicle;
ParkingSpaceParameters space;
VehicleState state;
Parker* pp;

void Mouse(int e, int x, int y, int flags, void* param){
	y = size-1-y;

	if(e==CV_EVENT_LBUTTONDOWN){
		lx = x;
		ly = y;
	}else if(e==CV_EVENT_RBUTTONDOWN){
		/*VehicleState to;
		to.x = lx;
		to.y = ly;
		to.hx = x - lx;
		to.hy = y - ly;*/
		/*to.x = -112.3;
		to.y =  188.4;// + space.front_wall_y;
		to.hx =  .51 ;
		to.hy = -.86 ;
		to.Normalize();
		pp->Trans_I_O(to);

		VehicleState from;
		from.x = 101;
		from.y = 115.6;// + space.front_wall_y;
		from.hx = .9;
		from.hy = .43;
		from.Normalize();
		pp->Trans_I_O(from);

		MovingOrder o1, o2;

		VehicleState from_shit = from;
		VehicleState to_shit = to;
		pp->Trans_O_I(from_shit);
		pp->Trans_O_I(to_shit);

		pp->Get_PA_PA_Traversal_Gentile(from_shit, to_shit, o1, o2);
		pp->PolishMovingOrder(o1);
		pp->PolishMovingOrder(o2);
		//VehicleState mid = p->Simulate(from, order, img);
		//p->Simulate(mid, p->GetSecondOrder(order, to), img);
		state = from;
		Display(0);
		Simulate(o1);
		Simulate(o2);

		Arrow(bg, from          );
		Arrow(bg, to            );
		Arrow(bg, o1.dest_state );
		Display(0);*/
	}
}

void Display(int wait){
	cvConvertImage(bg, img);
	Car(img, state, vehicle);
	cvFlip(img);
	cvShowImage("stuff", img);
	cvWaitKey(wait);
}


bool NotDoneYet(MovingOrder order, VehicleState state){
	bool cur_forward = (state.x-order.dest_state.x)*(order.dest_state.hx)+(state.y-order.dest_state.y)*(order.dest_state.hy)>0 ;
	double dx = state.x - order.dest_state.x;
	double dy = state.y - order.dest_state.y;
	double tol = order.turning_radius /2;
	return (cur_forward != order.forward) || (dx*dx+dy*dy>tol*tol) ;
}

double Rand(double min_val, double max_val){
	assert(max_val>min_val);
	double r = double(rand()) / double(RAND_MAX) ;
	return min_val + (max_val-min_val) * r ;
}

/*bool InBounds(VehicleState vs){
	double xs[4];
	double ys[4];

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

	bool bad_front = false;
	bool bad_back = false;
	for(int i=0; i<4; i++){
		if(ys[i]<space.front_wall_y) bad_front = true ;
		if(ys[i]>space.back_wall_y)  bad_back = true ;
	}
	if(abs(vs.x-space.park_point_x)<vehicle.min_turning_rad/10.0&&vs.hy<-.9){
		return !bad_back;
	}else if(bad_front || bad_back) return false;
	return true;
}*/

void Simulate(MovingOrder order){
	state.Normalize();

	order.turning_radius += vehicle.min_turning_rad*0.05*Rand(-1, 1);

	cvLine(bg, cvPoint(state.x, state.y), cvPoint(order.center_x, order.center_y), cvScalarAll(128), 2);

	int iter=0;
	while( NotDoneYet(order, state) ){
		double march = vehicle.min_turning_rad / 10000.0;
		if(!order.forward) march = -march;

		VehicleState next;
		next.x = state.x + state.hx*march ;
		next.y = state.y + state.hy*march ;
		double theta = atan2(state.hy, state.hx) + march/order.turning_radius;
		next.hx = cos(theta);
		next.hy = sin(theta);
		next.Normalize();

		//if(!InBounds(state)&& iter>50) break;

		cvLine(bg, cvPoint(state.x, state.y), cvPoint(next.x, next.y), cvScalarAll(255), 1);
		state = next;

		iter++;
		if(iter%200==0){
			Display(5);
		}
	}

	cvLine(bg, cvPoint(state.x, state.y), cvPoint(order.center_x, order.center_y), cvScalarAll(128), 2);

	double scoot = vehicle.min_turning_rad/25;
	state.x += Rand(-scoot, scoot);
	state.y += Rand(-scoot, scoot);
	double angle_scoot = .05;
	state.hx += Rand(-angle_scoot, angle_scoot);
	state.hy += Rand(-angle_scoot, angle_scoot);
	state.Normalize();

	//state = order.dest_state;
	//Display(0);

}

void DrawBG(){
	double xs[4];
	double ys[4];
	pp->GetFourCorners(xs, ys);

	CvPoint p[4];
	for(int i=0; i<4; i++){ p[i] = cvPoint(xs[i], ys[i]); }

	cvLine(bg, p[0], p[1], cvScalar(200,0,0), 3);
	cvLine(bg, p[1], p[2], cvScalar(200,0,0), 3);
	cvLine(bg, p[2], p[3], cvScalar(200,0,0), 3);
	cvLine(bg, p[0], p[3], cvScalar(0,0,255), 3);
}

void main(){
	vehicle.back_bumper     = 1.0 *meter_scale;
	vehicle.front_bumper    = 4.0 *meter_scale;
	vehicle.width           = 2.0 *meter_scale;
	vehicle.min_turning_rad = 5.5*meter_scale;

	space.corner_xs[0] = size/2 - 10*meter_scale;
	space.corner_ys[0] = size/2 - 10.0*meter_scale;
	
	space.corner_xs[1] = size/2 + 10*meter_scale;//2
	space.corner_ys[1] = size/2 - 2*meter_scale;

	/*space.corner_xs[0] = size/2 - 7.0*meter_scale;
	space.corner_ys[0] = size/2 - 8.0*meter_scale;
	
	space.corner_xs[1] = size/2 + 7.0*meter_scale;//2
	space.corner_ys[1] = size/2 + 6.5*meter_scale;*/

	space.park_point.x = size/2 ;
	space.park_point.y = 0;
	space.park_point.hx = 0 ;
	space.park_point.hy = -1 ;
	space.pullout_point_x = 0;
	space.pullout_point_y = 0;

	bg = cvCreateImage(cvSize(size,size), IPL_DEPTH_8U, 3);
	img = cvCreateImage(cvSize(size,size), IPL_DEPTH_8U, 3);
	/*double back_px = space.front_wall_x - space.back_wall_x ;
	double back_py = space.front_wall_y - space.back_wall_y ;
	double len = sqrt(back_px*back_px+back_py*back_py);
	len = 300/len;
	back_px *= len ;
	back_py *= len ;
	double wall_tx = back_py;
	double wall_ty = -back_px;*/
	cvNamedWindow("stuff", CV_WINDOW_AUTOSIZE);
	//cvSetMouseCallback("stuff", Mouse, NULL);

	Parker p = Parker(vehicle, space);
	pp = &p;

	cvZero(bg);
	//cvLine(bg, cvPoint(space.back_wall_x +10*wall_tx, space. back_wall_y+wall_ty), cvPoint(space. back_wall_x-10*wall_tx, space. back_wall_y-wall_ty), cvScalar(0,0,128), 3);
	//cvLine(bg, cvPoint(space.front_wall_x+10*wall_tx, space.front_wall_y+wall_ty), cvPoint(space.front_wall_x-10*wall_tx, space.front_wall_y-wall_ty), cvScalar(128,0,0), 3);
	DrawBG();

	/*while(1){
		int i = rand() % p.num_slots ;
		state = p.slots[i].state ;
		double util = p.slots[i].utility;
		if(util==-1){
		p.Trans_I_O(state);
		//cout << "Kill Jason " << util << endl;
		Display(0);
		}
	}*/


	/*VehicleState vs;
	vs.x = 358.66418888356884;
	vs.y = 558.93212721209568;
	vs.hx= -0.11031979870844336;
	vs.hy= -0.99389614246807934;
	Car(bg, vs, vehicle);
	p.Trans_O_I(vs);
	Display(0);
	for(int idx=0; idx<p.num_slots; idx++){
		//int idx = rand() % p.num_slots ;
		state = p.slots[idx].state;
		MovingOrder o1, o2;
		//if( ! p.Get_PA_PA_Traversal_Gentile(vs, state, o1, o2) ) continue;
		if( ! p.GetOneMoveTraversal(vs, state, o1) ) continue;
		if(p.slots[idx].utility<0) continue;
		//cout << p.slots[idx].utility << endl;
		double disc;
		cout << "sub=" << p.SubFinalAlignment(state, o1, disc);
		cout << "   disc=" << disc << endl;
		p.Trans_I_O(state);
		Display(0);
	}
	Display(0);*/

	srand(1230);

	/*while(1){
		do{
			state.x = Rand(0, size);
			state.y = Rand(0, size);
			state.hx = Rand(-1.0,1.0);
			state.hy = Rand(-1.0,1.0);
			state.Normalize();
			//state = p.GetXWallHits(Rand(-3.14/4.0, 3.14/4.0), 0, vehicle.width/2.0, true, true) ;
			//p.Trans_I_O(state);
		}while(!pp->InBounds(state));

		VehicleState i_state = state;
		p.Trans_O_I(i_state);

		double wiggle = p.GetWiggleRoom(i_state);
		cout << wiggle << endl;
		Display();
	}*/

	int test=0;

	/*while(1){
		VehicleState from;
		VehicleState to;
		MovingOrder order;
		from.x = Rand(0, size);
		from.y = Rand(0, size);
		from.hx = Rand(-1, 1);
		from.hy = Rand(-1, 1);
		from.Normalize();

		to.x = Rand(0, size);
		to.y = Rand(0, size);
		order = p.ConstructOneMoveTraversal(from, to.x, to.y, true);
		double max_y, min_y, max_x, min_x;
		cvZero(bg);
		p.GetMovementExtents(from, order, max_y, min_y, max_x, min_x);

		if(max_y>size || min_y<0 || max_x>size || min_x<0) continue;

		state = from;
		CvPoint ul = cvPoint(min_x, max_y);
		CvPoint ur = cvPoint(max_x, max_y);
		CvPoint ll = cvPoint(min_x, min_y);
		CvPoint lr = cvPoint(max_x, min_y);
		cvLine(bg, ul, ur, cvScalarAll(255), 1);
		cvLine(bg, ur, lr, cvScalarAll(255), 1);
		cvLine(bg, lr, ll, cvScalarAll(255), 1);
		cvLine(bg, ll, ul, cvScalarAll(255), 1);
		Car(bg, order.dest_state, vehicle);

		Display(0);
		Simulate(order);
	}*/

	while(1){

		cvZero(bg);
		DrawBG();

		//Display(5);

		//p.pending_orders.clear();
		
		VehicleState ib_state;
		do{
			state.x = Rand(0, size);
			state.y = Rand(0, size);
			state.hx = Rand(-1.0,1.0);
			state.hy = Rand(-1.0,1.0);
			state.Normalize();
			//state = p.GetXWallHits(Rand(-3.14/4.0, 3.14/4.0), 0, vehicle.width/2.0, true, true) ;
			ib_state = state;
			p.Trans_O_I(ib_state);
			//p.Trans_I_O(state);
		}while(!pp->InBounds(state) || ib_state.y<vehicle.front_bumper+vehicle.width/2.0 );

		test++;
		/*const int skip_list_size = 7;
		int skip_list[skip_list_size] = {8, 12, 19, 25, 26, 32, 33};
		bool skip=false;
		for(int i=0; i<skip_list_size; i++){
			if(skip_list[i] == test){
				skip=true;
			}
		}
		if(!skip)continue;*/
		//if(test<9) continue; //20

		cout << endl;
		cout << endl;
		cout << endl;
		cout << endl;
		cout << "testing " << test << endl;


		/*state.x = size/2 + 50;
		state.y = size/3;
		state.hx = 0;
		state.hy = -1;*/

		Display(1000);

		MovingOrder order;
		order.turning_radius = 0;
		bool work_left = true;
		while(1){

			if(!work_left) break;

			//execute order:
			cout << "begin plan" << endl;
			work_left = p.GetNextParkingOrder(order, state);
			cout << "end plan" << endl;

			//order.turning_radius += Rand(-vehicle.min_turning_rad/10.0, vehicle.min_turning_rad/10.0);
			if(work_left){
				Simulate(order);
				Display(0);
			}
		}

		/*work_left = true;
		while(1){

			if(!work_left) break;

			//order
			cout << "begin plan" << endl;
			work_left = p.GetNextPulloutOrder(order, state);
			cout << "end plan" << endl;

			if(work_left){
				Simulate(order);
				Display(500);
			}
		}*/
	}
}
