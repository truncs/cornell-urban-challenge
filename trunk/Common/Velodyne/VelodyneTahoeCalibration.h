#ifndef VELODYNE_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5
#define VELODYNE_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5

#include "../Math/Angle.h"
#include "../Fusion/Sensor.h"

#define __VELO_SENSOR_CALIBRATION_CHEAT\
  inline Sensor SensorCalibration(){\
      Sensor ret;\
      ret.SensorX = x;\
      ret.SensorY = y;\
      ret.SensorZ = z;\
      ret.SensorYaw = yaw;\
      ret.SensorPitch = pitch;\
      ret.SensorRoll = roll;\
      return ret;\
    }

#define __VELO_CALIBRATION_XYZ_YPR_RAD(X,Y,Z,YAW,PITCH,ROLL)\
  const float x=(float)(X),y=(float)(Y),z=(float)(Z),yaw=(float)(YAW),pitch=(float)(PITCH),roll=(float)(ROLL);\
  __VELO_SENSOR_CALIBRATION_CHEAT;

#define __VELO_CALIBRATION_XYZ_YPR_DEG(X,Y,Z,YAW,PITCH,ROLL)\
  const float x=(float)(X),y=(float)(Y),z=(float)(Z),yaw=Deg2Rad((float)(YAW)),pitch=Deg2Rad((float)(PITCH)),roll=Deg2Rad((float)(ROLL));\
  __VELO_SENSOR_CALIBRATION_CHEAT;

// all parameters IMU-relative
namespace VELODYNE_CALIBRATION{
  namespace SEPT_21_2007{
    __VELO_CALIBRATION_XYZ_YPR_DEG(1.4986,.05,1.2192,-.1,-2.7,.3);
  }

  namespace OCT_2_2007{
    __VELO_CALIBRATION_XYZ_YPR_DEG(1.27,0,1.1176,0,-2.5,.35);
  }

  // OCT 2
  namespace CURRENT{
    __VELO_CALIBRATION_XYZ_YPR_DEG(1.27,0,1.1176,0,-2.5,.35);
  }

}

#endif //VELODYNE_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5
