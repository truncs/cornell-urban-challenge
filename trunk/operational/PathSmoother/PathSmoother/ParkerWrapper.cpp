#include "..\..\..\Sensors\ParkingMcGhee\ParkingMcGhee\Parker.h"

#ifdef __cplusplus_cli
#pragma managed(on)
#endif

using namespace System;
using namespace UrbanChallenge::Common;
using namespace UrbanChallenge::Common::Vehicle;

namespace UrbanChallenge { namespace Parking  {

public ref class ParkingSpaceParams {
public:
	Coordinates BackWallPoint;
	Coordinates FrontWallPoint;
	Coordinates ParkPoint;
	Coordinates ParkVector;
	Coordinates PulloutPoint;
};

public ref class ParkerVehicleState {
public:
	Coordinates Loc;
	Coordinates Heading;

	ParkerVehicleState(Coordinates loc, Coordinates heading){
		this->Loc = loc;
		this->Heading = heading;
	}
};

public ref class ParkerMovingOrder {
public:
	bool Forward;
	double TurningRadius;
	bool Completed;
	Coordinates CenterPoint;

	ParkerVehicleState^ DestState;
};

public ref class ParkerWrapper {
private:
	Parker* parker;

public:
	ParkerWrapper(ParkingSpaceParams^ params) {
		ParkingSpaceParameters parkerParams;
		parkerParams.corner_xs[0] = params->BackWallPoint.X;
		parkerParams.corner_xs[1] = params->FrontWallPoint.X;
		parkerParams.corner_ys[0] = params->BackWallPoint.Y;
		parkerParams.corner_ys[1] = params->FrontWallPoint.Y;
		Coordinates endingVec = params->ParkVector;
		parkerParams.park_point.x = params->ParkPoint.X;
		parkerParams.park_point.y = params->ParkPoint.Y;
		parkerParams.park_point.hx = endingVec.X;
		parkerParams.park_point.hy = endingVec.Y;
		parkerParams.pullout_point_x = params->PulloutPoint.X;
		parkerParams.pullout_point_y = params->PulloutPoint.Y;

		VehicleParameters vehicleParams;
		vehicleParams.back_bumper = TahoeParams::RL;
		vehicleParams.front_bumper = TahoeParams::FL;
		vehicleParams.width = TahoeParams::T;
		vehicleParams.min_turning_rad = 1.0/std::min(abs(TahoeParams::CalculateCurvature(TahoeParams::SW_max, 2)), abs(TahoeParams::CalculateCurvature(-TahoeParams::SW_max, 2)));

		parker = new Parker(vehicleParams, parkerParams);
	}

	~ParkerWrapper() {
		OnDispose(true);
		GC::SuppressFinalize(this);
	}

	!ParkerWrapper() {
		if (parker != NULL) {
			OnDispose(false);
		}
	}

	ParkerMovingOrder^ GetNextParkingOrder(ParkerVehicleState^ currentState) {
		::VehicleState state;
		state.x = currentState->Loc.X;
		state.y = currentState->Loc.Y;
		state.hx = currentState->Heading.X;
		state.hy = currentState->Heading.Y;

		MovingOrder order;
		bool completed = !parker->GetNextParkingOrder(order, state);
		ParkerMovingOrder^ retOrder = gcnew ParkerMovingOrder();
		retOrder->Completed = completed;
		if (!completed){
			retOrder->Forward = order.forward;
			retOrder->TurningRadius = order.turning_radius;
			retOrder->CenterPoint = Coordinates(order.center_x, order.center_y);
			retOrder->DestState = gcnew ParkerVehicleState(Coordinates(order.dest_state.x, order.dest_state.y), Coordinates(order.dest_state.hx, order.dest_state.hy));
		}

		return retOrder;
	}

	ParkerMovingOrder^ GetNextPulloutOrder(ParkerVehicleState^ currentState) {
		::VehicleState state;
		state.x = currentState->Loc.X;
		state.y = currentState->Loc.Y;
		state.hx = currentState->Heading.X;
		state.hy = currentState->Heading.Y;

		MovingOrder order;
		bool completed = !parker->GetNextPulloutOrder(order, state);
		ParkerMovingOrder^ retOrder = gcnew ParkerMovingOrder();
		retOrder->Completed = completed;
		if (!completed){
			retOrder->Forward = order.forward;
			retOrder->TurningRadius = order.turning_radius;
			retOrder->CenterPoint = Coordinates(order.center_x, order.center_y);
			retOrder->DestState = gcnew ParkerVehicleState(Coordinates(order.dest_state.x, order.dest_state.y), Coordinates(order.dest_state.hx, order.dest_state.hy));
		}

		return retOrder;
	}

protected:
	void OnDispose(bool disposing){
		delete parker;
		parker = NULL;
	}
};

} }