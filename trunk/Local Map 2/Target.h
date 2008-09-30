#ifndef TARGET_H
#define TARGET_H

#include "Cluster.h"
#include "LikelihoodCache.h"
#include "LocalMapConstants.h"
#include "LocalMapFunctions.h"
#include "MatrixIndex.h"
#include "MetaMeasurement.h"
#include "Sensor.h"
#include "TargetOccupancyGrid.h"
#include "VehicleOdometry.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDLIB.H>

#define T_NUMSTATES 6

//pre-declare for internal pointers
class Target;

class Target
{
	//The target class.  Stores one LocalMap target (an obstacle)

private:

	//STATUS FLAGS
	//which dynamics model to use for the target
	int mTargetType;
	//whether the target has been initialized
	bool mIsInitialized;
	//when the target received its last measurements
	double mLastUpdateTime;
	double mLastIbeoUpdateTime;
	double mLastMobileyeUpdateTime;
	double mLastRadarUpdateTime;
	double mLastSideSickUpdateTime;
	//how many measurements the target has received
	int mNumMeasurements;
	int mNumIbeoMeasurements;
	int mNumPartialIbeoMeasurements;
	int mNumMobileyeMeasurements;
	int mNumRadarMeasurements;
	int mNumSideSickMeasurements;

	//STATUS ESTIMATES:
	//probability that the object exists
	double mExistenceProbability;

	//STATE VECTOR:
	//the x-coordinate of the target, in ego-vehicle coordinates (m)
	double mX;
	//the y-coordinate of the target, in ego-vehicle coordinates (m)
	double mY;
	//the orientation angle: angle from ego vehicle coordinates to object storage coordinates
	double mOrientation;
	//the target's absolute ground speed (m/s)
	double mSpeed;
	//the target's heading wrt the ego-vehicle (rad.)
	double mHeading;
	//target width estimate, perpendicular to line of site (m)
	double mWidth;

	//COVARIANCE MATRIX:
	//target state covariance matrix (states are in the order listed above
	double mCovariance[T_NUMSTATES*T_NUMSTATES];

	//TARGET POINT DATA:
	//number of points in the target
	int mNumPoints;
	//array of target points (each row is an x, y pair for a single point)
	double* mTargetPoints;
	//the occupancy grid for the target (for removing duplicate targets)
	TargetOccupancyGrid* mTargetGrid;

	//MEASUREMENT PLACEHOLDER VARIABLES
	int mMeasurementType;
	double mLambda;
	double* nu;
	double* S;
	double* W;

	//LIKELIHOOD CACHE VARIABLES
	//cache for the bearing-bearing-range measurements
	LikelihoodCache mBBRCache;
	//cache for the clockwise bearing measurements
	LikelihoodCache mCWBCache;
	//cache for the counter-clockwise bearing measurements
	LikelihoodCache mCCWBCache;
	//cache for the mobileye measurements
	LikelihoodCache mMobileyeCache;
	//cache for the radar measurements
	LikelihoodCache mRadarCache;
	//cache for the side SICK measurements
	LikelihoodCache mSideSickCache;

	bool CanBeAssociated(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void RepairAnchorPoint();

public:

	//the next and previous targets (for implemeneting a linked list)
	Target* PrevTarget;
	Target* NextTarget;

	//status accessor methods
	bool IsInitialized() {return mIsInitialized;};
	double LastUpdateTime() {return mLastUpdateTime;}
	double LastIbeoUpdateTime() {return mLastIbeoUpdateTime;}
	double LastMobileyeUpdateTime() {return mLastMobileyeUpdateTime;}
	double LastRadarUpdateTime() {return mLastRadarUpdateTime;}
	double LastSideSickUpdateTime() {return mLastSideSickUpdateTime;}
	int NumMeasurements() {return mNumMeasurements;}
	int NumIbeoMeasurements() {return mNumIbeoMeasurements;}
	int NumPartialIbeoMeasurements() {return mNumPartialIbeoMeasurements;}
	int NumMobileyeMeasurements() {return mNumMobileyeMeasurements;}
	int NumRadarMeasurements() {return mNumRadarMeasurements;}
	int NumSideSickMeasurements() {return mNumSideSickMeasurements;}
	int TargetType() {return mTargetType;}

	//estimate accessor methods
	double ExistenceProbability() {return mExistenceProbability;}

	double X() {return mX;}
	double Y() {return mY;}
	double Orientation() {return mOrientation;}
	double Speed() {return mSpeed;}
	double Heading() {return mHeading;}
	double Width() {return mWidth;}

	//covariance accessor method
	double Covariance(int r, int c) {return mCovariance[midx(r, c, T_NUMSTATES)];}

	//target points accessor methods
	int NumPoints() {return mNumPoints;}
	double TargetPoints(int r, int c) {return mTargetPoints[midx(r, c, mNumPoints)];}
	TargetOccupancyGrid* TargetGrid() {return mTargetGrid;}
	inline void DilateOccupancyGrid() {mTargetGrid->DilateOccupancyGrid(TARGET_DILATERANGE); return;}
	inline bool OccupancyGridIsValid() {return mTargetGrid->IsValid();}
	inline double OverlapPercentage(TargetOccupancyGrid* iComparisonGrid) {return mTargetGrid->OverlapPercentage(iComparisonGrid);}
	inline void ResetOccupancyGrid() {mTargetGrid->ResetOccupancyGrid(); return;}
	inline void SetOccupancyGrid() {mTargetGrid->SetOccupancyGrid(mNumPoints, mTargetPoints, mX, mY, mOrientation); return;}

	//measurement accessor method
	double Lambda() {return mLambda;}

	//general methods
	Target();
	Target(Target* iTarget2Copy);
	~Target();

	Cluster TargetPointsCluster();

	void ExtremePoints(double& oCWX, double& oCWY, double& oCCWX, double& oCCWY, double& oCPX, double& oCPY);
	void Initialize(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	bool IsInLineOfSight(Sensor* iSensor);
	void Likelihood(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void MaintainTarget();
	void Predict(double iPredictDt, VehicleOdometry* iVehicleOdometry);
	void PrepareForTransmit();
	void Update(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateExistenceProbability(double ipSgE, double ipSgNE);

};

#endif //TARGET_H
