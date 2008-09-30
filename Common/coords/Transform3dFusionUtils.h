#ifndef TRANSFORM_3D_FUSION_UTILS_H_OCT_3_2007_SVL5
#define TRANSFORM_3D_FUSION_UTILS_H_OCT_3_2007_SVL5

#include "Transform3d.h"
#include "../Fusion/Sensor.h"

inline Transform3d sensor2vehicleXForm(Sensor& s){
  Transform3d ret;
  ret.Translate(s.SensorX,s.SensorY,s.SensorZ);
  ret.Rotate(s.SensorYaw,s.SensorPitch,s.SensorRoll);
  return ret;
}

inline Transform3d veh2SensorXForm(Sensor& s){
  return sensor2vehicleXForm(s).Inverse();
}

#endif //TRANSFORM_3D_FUSION_UTILS_H_OCT_3_2007_SVL5

