#ifndef IBEO_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5
#define IBEO_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5

#include "../Math/Angle.h"
#include "../Fusion/Sensor.h"

#define __IBEO_SENSOR_CALIBRATION_CHEAT\
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

#define __IBEO_CALIBRATION_XYZ_YPR_RAD(X,Y,Z,YAW,PITCH,ROLL)\
  const float x=(float)X),y=(float)(Y),z=(float)(Z),yaw=(float)(YAW),pitch=(float)(PITCH),roll=(float)(ROLL);\
  __IBEO_SENSOR_CALIBRATION_CHEAT;

#define __IBEO_CALIBRATION_XYZ_YPR_DEG(X,Y,Z,YAW,PITCH,ROLL)\
  const float x=(float)(X),y=(float)(Y),z=(float)(Z),yaw=Deg2Rad((float)(YAW)),pitch=Deg2Rad((float)(PITCH)),roll=Deg2Rad((float)(ROLL));\
  __IBEO_SENSOR_CALIBRATION_CHEAT;

// all parameters IMU-relative
namespace IBEO_CALIBRATION{

  namespace AUG_2007{
    namespace FRONT_LEFT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,0.8,-0.5118,37.05,-1.1,-0.9);
    }

    namespace FRONT_CENTER{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.6449,0.0f,-0.0508,0,0,0);
    }

    namespace FRONT_RIGHT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,-0.8,-0.5118,-41.5,-.7,1);
    }
  }

  namespace SEPT_21_2007{
    namespace FRONT_LEFT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,0.8,-0.4118,-37.05,1.1,0.4);
    }

    namespace FRONT_CENTER{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.6449,0.0f,-0.0508,0,0,0);
    }

    namespace FRONT_RIGHT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,-0.8,-0.4118,41.5,.7,-.2);
    }
  }

  namespace OCT_2_2007{
     namespace FRONT_LEFT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,0.8,-0.4118,-37.05,1.1,0.4);
    }

    namespace FRONT_CENTER{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.6449,0.0f,-0.0508,0,0,0);
    }

    namespace FRONT_RIGHT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,-0.8,-0.4118,41.5,.9,-.3);
    }
  }
  
  //OCT_2_2007
  namespace CURRENT{
     namespace FRONT_LEFT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,0.8,-0.4118,-37.05,1.1,0.4);
    }

    namespace FRONT_CENTER{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.6449,0.0f,-0.0508,0,0,0);
    }

    namespace FRONT_RIGHT{
      __IBEO_CALIBRATION_XYZ_YPR_DEG(3.5306,-0.8,-0.4118,41.5,1,-.4);
    }
  }
};

#endif //IBEO_TAHOE_CALIBRATION_H_SEPT_28_2007_SVL5
