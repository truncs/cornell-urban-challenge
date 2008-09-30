#ifndef LOCALMAPCONSTANTS_H
#define LOCALMAPCONSTANTS_H

//File of constants, thresholds, and other tuning parameters for LocalMap

#define PI 3.1415926535897932384626433832795
#define TWOPI 6.283185307179586476925286766559
#define PIOTWO 1.5707963267948966192313216916398
#define PIOFOUR 0.78539816339744830961566084581988
#define LNTWOPI 1.8378770664093454835606594728112

//***LOCAL MAP OPTIONS***
//whether to write incoming sensor data to a file
//#define LM_PRINTLOGS
//whether to write the targets to a file (automatically defined in *WithLogs configurations)
//#define LM_PRINTTARGETS
//whether to write the local road model to a file (automatically defined in *WithLogs configurations)
//#define LM_PRINTROAD
//define this to send LocalMap and LocalRoad to Scene estimator and Hammox (automatically defined in real sensors configurations)
//#define LM_TRANSMITMSGS
//define this to print common debug messages
//#define LM_COMMONDEBUGMSGS
//define this to short circuit local map: no tracking is done.
//#define LM_NOTRACKING
//define this to perform duplication reasoning to eliminate duplicate targets
//#define LM_NODUPLICATION
//define this to confirm targets using existence/occupancy grids
#define LM_CONFIRMTARGETS

//***LOCAL MAP CONSTANTS***
//timeout period for localmap and localroad event queues
#define LM_EVENTTIMEOUT 2000
//delay time (sec.) to wait before processing queue data in localmap
#define LM_QUEUEDELAY 0.2
//maximum number of events in the localmap queue before it is emptied
#define LM_QUEUEMAXEVENTS 70
//delay time (sec.) to wait before processing queue data in localroad
#define LR_QUEUEDELAY 0.5
//maximum number of events in the localroad queue before it is emptied
#define LR_QUEUEMAXEVENTS 50
//number of particles to use in LocalMap
#define LM_NUMPARTICLES 4
//effective number of particles threshold for resample
#define LM_NPEFFRESAMPLE 2.0
//size of the relative pose queue (number of pose packets to save)
#define LM_RPPQUEUESIZE 200
//frequency at which timing is received (ms)
#define LM_TIMINGPERIOD 10.0
//how long to wait before terminating a thread (ms)
#define LM_THREADTIMEOUT 10000

//number of columns in each packet type
#define LM_DELPHIPACKETSIZE 17
#define LM_CLUSTEREDSICKPACKETSIZE 10
#define LM_CLUSTEREDIBEOPACKETSIZE 10
#define LM_JASONROADPACKETSIZE 15
#define LM_MOBILEYEOBSTACLEPACKETSIZE 11
#define LM_MOBILEYEROADPACKETSIZE 25
#define LM_ODOMPACKETSIZE 14
#define LM_SIDESICKPACKETSIZE 5

//***LOCALROAD SETTINGS***
//LocalRoad process noises (cts time)
#define LR_ROADOFFSETVAR 0.694444444444445
#define LR_ROADHEADINGVAR 0.0171347298630024
#define LR_ROADCURVATUREVAR 1e-006
//time to lose 95% of initial information in road and lane model
#define LR_T95PCT 4.0
//uniform measurement likelihood for the background noise road model comparison
//set at: 1/20 * 1/(pi/2) * 1/0.4
#define LR_UNIFORMMODEL 0.0795774715459477
//minimum allowed road model correctness probability before it is reset
#define LR_MINROADCONFIDENCE 0.1
//lane width process noise (cts time)
#define LR_LANEWIDTHVAR 0.01
//lane existence threshold for considering the lane there
#define LR_MINLANEEXISTENCEPROB 0.6
//default model parameters (for initializing)
#define LR_DEFAULTMODELPROB 0.5
#define LR_DEFAULTOFFSET 0.0
#define LR_DEFAULTOFFSETVAR 100.0
#define LR_DEFAULTHEADING 0.0
#define LR_DEFAULTHEADINGVAR 9.86960440108936
#define LR_DEFAULTCURVATURE 0.0
#define LR_DEFAULTCURVATUREVAR 0.01
//default lane parameters (for initializing)
#define LR_DEFAULTLANEPROB 0.5
#define LR_DEFAULTLANEWIDTH 3.6576
#define LR_DEFAULTLANEVAR 4.0
//distance to which the road model is generated when transmitting points (m)
#define LR_GENDISTANCE 50.0

//***TARGET TYPES***
//an empty or invalid target
#define T_INVALID -1
//a target initialized only with ibeo / lidar
#define T_IBEO 101
//an ibeo-only target that is stable and matured
#define T_IBEOMATURE 102
//a mobileye-only target
#define T_MOBILEYE 103
//a radar-only target
#define T_RADAR 104
//a quasi-mature target, having both mobileye and radar on it
#define T_QUASIMATURE 105
//a mature target, with a full state vector
#define T_MATURE 106
//a mature static target (couldn't possibly be a car)
//#define T_MATURESTATIC 
//a mature dynamic target (could be a car)
//#define T_MATUREDYNAMIC 

//***SIGMA POINT FILTER PARAMETERS***
//filter closeness to EKF
#define SPF_ALPHA 0.01
//noise closeness to Gaussian
#define SPF_BETA 2.0
//extra scaling factor
#define SPF_KAPPA 0.0

//***TARGET PROCESS NOISES (CTS TIME)***
#define TARGET_QX 1.0
#define TARGET_QY 1.0
#define TARGET_QORIENT 0.0304617419786709
#define TARGET_QSPEED 1.0
#define TARGET_QHEADING 0.0304617419786709
#define TARGET_QWIDTH 1.0

//***TARGET CONSTANTS***
//how far outside the angular bounds of a target the anchor point can get
#define TARGET_ANCHORANGLETOL 0.0
//how far from the minimum range of a target the anchor point can get
#define TARGET_ANCHORRANGETOL 2.0
//minimum range difference between target and sensors for target to lose probability
#define TARGET_ANCHOREXISTENCERANGE 2.0
//minimum range difference between points on the target and sensors for the target to lose probability
#define TARGET_POINTEXISTENCERANGE 0.5
//minimum existence probability before a target is deleted
#define TARGET_MINEXISTENCEPROB 0.05
//time it takes for a target to lose 95% of its initial existence probability (s)
#define TARGET_T95PCT 3.0
//maximum range of a target before deletion (m)
#define TARGET_MAXRANGE 150.0
//initial existence probability of a target
#define TARGET_INITEXISTENCEPROB 0.5
//how many ibeo measurements on target are required before an object picks up speed
//#define TARGET_IBEOMATUREMEASCOUNT 25
#define TARGET_IBEOMATUREMEASCOUNT 5
//how many other measurements a target must receive to become mature
#define TARGET_MATUREMEASCOUNT 1
//the aspect ratio to use to create fake target points when none are available
#define TARGET_ASPECTRATIO 2.55696202531646
//the default width of a target when none is available (m)
#define TARGET_DEFAULTWIDTH 2.0
//the default width variance of a target when none is available (m^2)
#define TARGET_DEFAULTWIDTHVAR 4.0
//farthest away a measurement can be before the target is not associated (m)
#define TARGET_MAXASSOCDIST 5.0
//farthest away a measurement can be in angle before the target is not associated (rad.)
//(used for measurements that can't compute a distance tolerance)
#define TARGET_MAXASSOCANGLE 1.0471975511966
//maximum width of a target before it is deleted
#define TARGET_MAXWIDTH 15.0
//size of each target's occupancy grid
#define TARGET_OGNUMROWS 50
#define TARGET_OGNUMCOLS 50
//maximum range (m) for target anchor points to be separated to be considere for duplication
#define TARGET_MAXDUPERANGE 2.0
//radius of dilation (m) to apply to target occupancy grid for comparison
#define TARGET_DILATERANGE 0.25
//minimum overlap percentage for targets to be considered duplicates of each other
#define TARGET_MINDUPEPCT 0.5
//maximum vehicle speed for allowing targets to be confirmed by existence (m/s)
#define TARGET_MAXCONFIRMSPD 1.0

//***EGO-VEHICLE ODOMETRY PROCESS NOISES (CTS TIME)***
//velocity and rate of rotation process noise
#define ODOM_QVX 0.0547830131138888
#define ODOM_QVY 0.0547830131138888
#define ODOM_QVZ 0.0547830131138888
#define ODOM_QWX 0.000304617419786709
#define ODOM_QWY 0.000304617419786709
#define ODOM_QWZ 0.000304617419786709

//***BIRTH MODEL CONSTANTS***
//range tolerance (m) before deprecating birth likelihood
#define B_RANGETOL 1.0
//distance tolerance (m) before deprecating birth likelihood
#define B_DISTANCETOL 1.0

//***CHI2 THRESHOLD CONSTANTS***
#define CHI2THRESH_1DOF 3.84145882069415
#define CHI2THRESH_2DOF 5.99146454710798
#define CHI2THRESH_3DOF 7.81472790325116
#define CHI2THRESH_4DOF 9.48772903678116
#define CHI2THRESH_5DOF 11.0704976935164

//***CLUSTER CONSTANTS***
//define codes for high and low obstacles
#define CLUSTER_LOWOBSTACLE 0
#define CLUSTER_HIGHOBSTACLE 1
//minimum number of points
#define CLUSTER_MINNUMPOINTS 2

//***CLUSTERED SICK SENSOR CONSTANTS***
//range and bearing spans (lengths of spans, for likelihoods)
#define CLUSTEREDSICK_RSPAN 30.0
#define CLUSTEREDSICK_BSPAN 3.14159265358979
//range and bearing variances
#define CLUSTEREDSICK_RANGEVAR 1.0
#define CLUSTEREDSICK_BEARINGVAR 0.00121846967914683
//number of bins in the clustered SICK radial grid
#define CLUSTEREDSICK_NUMGRIDBINS 100
//clustered SICK radial grid bearings span (rad.)
#define CLUSTEREDSICK_GRIDSPAN PI
//cluster metameasurement bearing variance
#define CLUSTEREDSICK_CLUSTERBEARINGVAR 0.00761543549466771
//cluster metameasurement range variance
#define CLUSTEREDSICK_CLUSTERRANGEVAR 1.0
//probability that the target is detected given it exists
#define CLUSTEREDSICK_ACCURACY IBEO_ACCURACY
//probability that something is detected given no target
#define CLUSTEREDSICK_FPRATE IBEO_FPRATE

//***IBEO SENSOR CONSTANTS***
//ibeo range and bearing spans (lengths of spans, for likelihoods)
#define IBEO_RSPAN 100.0
#define IBEO_BSPAN 3.66519142918809
//ibeo range and bearing variances
#define IBEO_RANGEVAR 1.0
#define IBEO_BEARINGVAR 0.00121846967914683
//number of bins in the ibeo radial grid
#define IBEO_NUMGRIDBINS 100
//ibeo radial grid bearings span (rad.)
#define IBEO_GRIDSPAN PI
//ibeo initial orientation variance
#define IBEO_INITORIENTVAR 0.00761543549466771
//ibeo initial speed variance
#define IBEO_INITSPEEDVAR 19.98447616
//ibeo initial heading variance
#define IBEO_INITHEADINGVAR 4.3864908449286
//ibeo cluster metameasurement bearing variance
#define IBEO_CLUSTERBEARINGVAR 0.00121846967914683
//ibeo cluster metameasurement range variance
#define IBEO_CLUSTERRANGEVAR 1.0
//probability that the target is detected given it exists
#define IBEO_ACCURACY 0.95
//probability that something is detected given no target
#define IBEO_FPRATE 0.1

//***JASON ROADFINDER SENSOR CONSTANTS***
//maximum and minimum tolerable lane widths
#define JASON_MIN_LANEWIDTH 2.0
#define JASON_MAX_LANEWIDTH 5.4864
//minimum confidence for having a valid road segment
#define JASON_MIN_CONFIDENCE 1.0
//chi2 threshold (3 dof) for road model measurement gating
#define JASON_ROADCHI2GATE 7.81472790325116
//chi2 threshold (1 dof) for lane width measurement gating
#define JASON_LANECHI2GATE 3.84145882069415
//variance of distance from lane center (m^2)
#define JASON_LANEOFST_VAR 0.25
//variance of heading wrt road (rad.^2)
//calculated as (arctan(err (pixels) / focal length))^2
#define JASON_HDGOFST_VAR 4.87640586858475e-005
//variance of the curvature measurement (m^-2)
#define JASON_CRVOFST_VAR 6.92520775623268e-008
//probability that a lane is detected when one is present
#define JASON_LANEACCURACY 0.8
//probability that a lane is detected when one is not present
#define JASON_LANEFPRATE 0.1

//***MOBILEYE ROADFINDER SENSOR CONSTANTS***
//maximum and minimum tolerable lane widths
#define MOBILEYE_MIN_LANEWIDTH 2.0
#define MOBILEYE_MAX_LANEWIDTH 5.4864
//minimum road confidence for applying a measurement update
#define MOBILEYE_MIN_ROADCONF 3.0
//minimum lane confidence for considering a lane to exist (NOTE: used for far left and right lanes only)
#define MOBILEYE_MIN_LANECONF 2.0
//minimum distance for model validity (m)
#define MOBILEYE_MIN_VALIDDISTANCE 40.0
//chi2 threshold (3 dof) for road model measurement gating
#define MOBILEYE_ROADCHI2GATE 7.81472790325116
//chi2 threshold (1 dof) for lane width measurement gating
#define MOBILEYE_LANECHI2GATE 3.84145882069415
//variance in the lane offset measurement (m^2)
#define MOBILEYE_LANEOFST_VAR 0.25
//variance in the lane heading measurement (rad^2)
//calculated as (arctan(err (pixels) / focal length))^2
#define MOBILEYE_HDGOFST_VAR 0.000195037213594883
//variance in the curvature measurement (m^-2)
#define MOBILEYE_CRVOFST_VAR 6.92520775623268e-008
//probability that a lane is detected when one is present
#define MOBILEYE_LANEACCURACY 0.6
//probability that a lane is detected when one is not present
#define MOBILEYE_LANEFPRATE 0.01

//***MOBILEYE OBSTACLE SENSOR CONSTANTS***
//mobileye x, y, s, and w spans (for likelihoods)
#define MOBILEYE_XSPAN 100.0
#define MOBILEYE_YSPAN 50.0
#define MOBILEYE_SSPAN 53.6448
#define MOBILEYE_WSPAN 6.0
//minimum obstacle confidence for applying a measurement update
#define MOBILEYE_MIN_OBSCONF 2.0
//minimum ego-vehicle speed for initializing a mobileye obstacle (m/s)
#define MOBILEYE_MIN_EGOSPEED 2.2352
//minimum range to target for initializing a mobileye obstacle (m)
#define MOBILEYE_MIN_OBSDIST 20.0

//variance on mobileye vertical bumper position (pixels^2)
#define MOBILEYE_BUMPERVAR 100.0
//variance on mobileye lateral position (pixels^2)
#define MOBILEYE_LATERALVAR 400.0
//variance on mobileye width (pixels^2)
#define MOBILEYE_WIDTHVAR 400.0
//variance on mobileye speed measurement (m^2/s^2)
#define MOBILEYE_SPEEDVAR 25.0
//mobileye camera focal length (pixels)
#define MOBILEYE_FOCALLEN 358.0
//height difference between mobileye camera normal bumper height and ground (m)
#define MOBILEYE_BUMPERHEIGHT 0.354
//initial heading variance for targets created from mobileye
#define MOBILEYE_INITHEADINGVAR 0.274155677808038
//probability that an obstacle is detected when one is present
#define MOBILEYE_OBSACCURACY 0.6
//probability that an obstacle is detected when one is not present
#define MOBILEYE_OBSFPRATE 0.01

//***RADAR OBSTACLE SENSOR CONSTANTS***
//minimum power (dBV) for a target to be considered
#define RADAR_MINPOWER -DBL_MAX
//maximum vehicle speed allowed for the side radars to update (m/s)
#define RADAR_MAXSPEEDFORUPDATE 1.0
//radar r, b, rr spans (for likelihoods)
#define RADAR_RSPAN 254.0
#define RADAR_BSPAN 0.261799387799149
#define RADAR_RRSPAN 53.6448
//variance on radar range measurement (m^2)
#define RADAR_RANGEVAR 25.0
//variance on radar bearing measurement (rad.^2)
#define RADAR_BEARINGVAR 0.00761543549466771
//variance on radar range rate measurement (m^2/s^2)
#define RADAR_RANGERATEVAR 1.0
//initial heading variance for targets created from radar
#define RADAR_INITHEADINGVAR 0.274155677808038
//probability that an obstacle is detected when one is present
#define RADAR_OBSACCURACY 0.9
//probability that an obstacle is detected when one is not present
#define RADAR_OBSFPRATE 0.3

//***SIDE SICK OBSTACLE SENSOR CONSTANTS***
//minimum height for a side lidar measurement to be added to the list of loose clusters (m)
#define SIDESICK_MINLOOSECLUSTERHEIGHT 0.1
//minimum height for a side lidar measurement to update a target (m)
#define SIDESICK_MINTARGETHEIGHT 1.0
//default width when a cluster must be created from a side LIDAR
#define SIDESICK_DEFAULTWIDTH 0.1
//maximum range (m) to allow targets to be created from the side SICK (m)
#define SIDESICK_MAXRANGE 10.0
//minimum range (m) to allow targets to be created from the side SICK (m)
#define SIDESICK_MINRANGE 0.25
//total span of range for the side LIDAR for birth (m)
#define SIDESICK_BRSPAN 10.0
//total span of range for the side LIDAR for clutter (m)
#define SIDESICK_CRSPAN 30.0
//variance on the distance measurement made by the side lidar (m^2)
#define SIDESICK_RANGEVAR 0.25
//variance on the side lidar bearing (rad^2)
#define SIDESICK_BEARINGVAR 7.61543549466771e-005
//accuracy and false positive rate of the side lidar measurement
#define SIDESICK_ACCURACY 0.99
#define SIDESICK_FPRATE 0.1

//***VELODYNE OBSTACLE SENSOR CONSTANTS***
//probability of detecting a target given that it exists
#define VELODYNE_ACCURACY 0.99999
//probability of detecting a target given that it doesn't exist
#define VELODYNE_FPRATE 0.00001
//maximum grid age allowed for the grid to still be used (sec.)
#define VELODYNE_MAXGRIDAGE 0.05

#endif //LOCALMAPCONSTANTS_H
