#ifndef VELODYNE_COMMON_SEPT_14_2007_SVL5
#define VELODYNE_COMMON_SEPT_14_2007_SVL5

#include <vector>
using namespace std;

class VelodynePt : public v3f{
public:
  double vts;
  float intensity;
  float range;
  unsigned char laserNum;
  float sensorTheta;
  float blockTheta;
};

struct VelodyneScan{
  vector<VelodynePt> pts;
  double vts;
};


#endif //VELODYNE_COMMON_SEPT_14_2007_SVL5
