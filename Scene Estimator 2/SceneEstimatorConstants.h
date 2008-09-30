#ifndef SCENEESTIMATORCONSTANTS_H
#define SCENEESTIMATORCONSTANTS_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//File of constants, thresholds, and other tuning parameters for SceneEstimator

#define PI 3.1415926535897932384626433832795
#define TWOPI 6.283185307179586476925286766559
#define PIOTWO 1.5707963267948966192313216916398
#define LNTWOPI 1.8378770664093454835606594728112
#define SQRTTWO 1.4142135623730950488016887242097

//***SCENE ESTIMATOR OPTIONS***
//whether to write incoming sensor data to a file
//#define SE_PRINTLOGS
//whether to write the particles to a file (automatically defined in *WithLogs configurations)
//#define SE_PRINTPARTICLES
//whether to write the tracks to a file (automatically defined in *WithLogs configurations)
//#define SE_PRINTTRACKS
//whether to write the loose points to a file (automatically defined in *WithLogs configurations)
//#define SE_PRINTPOINTS
//define this to send particles to Hammox (WARNING: kills bandwidth)
//#define SE_TRANSMITMSGS
//define this to print common debug messages
//#define SE_COMMONDEBUGMSGS
//define this to do fast deletion of tracks (i.e. delete when a target not assigned)
#define SE_FASTDELETE
//define this to use occlusion reasoning in tracks
#define SE_TRACKOCCLUSION
//define this to calculate stats on the road graph
//#define SE_ROADGRAPHTEST

//POSTERIOR POSE CONSTANTS
//define this to use GPS biases
//#define PP_GPSBIASES
//timeout period for ppose event queue
#define PP_EVENTTIMEOUT 2000
//delay time (sec.) to wait before processing queue data
#define PP_QUEUEDELAY 0.5
//maximum number of events in the event queue before it is emptied
#define PP_QUEUEMAXEVENTS 100
//number of particles to use in PosteriorPose
#define PP_NUMPARTICLES 2000
//effective number of particles threshold for resample
#define PP_NPEFFRESAMPLE 1000
//size of the posterior pose queue (number of posterior pose packets to save)
#define PP_PPQUEUESIZE 30
//size of the relative pose queue (number of pose packets to save)
#define PP_RPPQUEUESIZE 200
//number of rows in the road graph road cache
#define PP_NUMCACHEROWS 250
//number of columns in the road graph road cache
#define PP_NUMCACHECOLS 250
//how long to wait before terminating a thread (ms)
#define PP_THREADTIMEOUT 10000

//TRACK GENERATOR CONSTANTS
//timeout period for tgen event queue
#define TG_EVENTTIMEOUT 2000
//track generator queue delay before transmit
#define TG_QUEUEDELAY 0.2
//maximum number of events in the event queue before it is emptied
#define TG_QUEUEMAXEVENTS 30

//SCENE ESTIMATOR CONSTANTS
//frequency at which timing is received (ms)
#define SE_TIMINGPERIOD 10.0

//number of columns in each packet type
#define SE_JASONROADPACKETSIZE 15
#define SE_LMTHEADERPACKETSIZE 11
#define SE_LMTCOVARIANCEPACKETSIZE 24
#define SE_LMTPOINTSPACKETSIZE 7
#define SE_LRHEADERPACKETSIZE 14
#define SE_LRPOINTSPACKETSIZE 7
#define SE_LOCALPOINTSPACKETSIZE 7
#define SE_MOBILEYEROADPACKETSIZE 25
#define SE_ODOMPACKETSIZE 14
#define SE_POSEPACKETSIZE 29
#define SE_STOPLINEPACKETSIZE 5

//AARON STOPLINE SENSOR CONSTANTS
//variance on Aaron's stopline distance measurement (m)
#define AARON_STOPLINEVAR 0.01
//maximum angle to approaching stopline for it to be visible (rad.)
#define AARON_MAXVIEWANGLE 0.523598775598299
//confidence threshold for accepting a stopline
#define AARON_MINCONFIDENCE 300.0
//Aaron stopline hypothesis test threshold: Chi2 with 1 dof
#define AARON_STOPLINE_HYPOTEST 10.8275661706628
//maximum allowed probability of false positive to permit a stopline update
#define AARON_STOPLINE_MAXFPPROB 0.1
//probability of correctly detecting the stopline given that it's present
#define AARON_STOPLINE_ACCURACY 0.95
//likelihood of a measurement given that there's no stopline present
#define AARON_STOPLINE_FPLIKELIHOOD 0.1

//CLUSTER CONSTANTS
#define CLUSTER_HIGHOBSTACLE 1
#define CLUSTER_LOWOBSTACLE 0

//GPS BIAS CONSTANTS
//NOTE: no bias is estimated in a blackout.
//SPS GPS
//gps bias correlation time, in seconds
#define GPSBIASSPS_CORRELATIONTIME 600.0
//variance (time constant) of gps biases (m^2)
#define GPSBIASSPS_VARIANCE 4.0
//WAAS GPS
//gps bias correlation time, in seconds
#define GPSBIASWAAS_CORRELATIONTIME 600.0
//variance (time constant) of gps biases (m^2)
#define GPSBIASWAAS_VARIANCE 4.0
//VBS GPS
//gps bias correlation time, in seconds
#define GPSBIASVBS_CORRELATIONTIME 600.0
//variance (time constant) of gps biases (m^2)
#define GPSBIASVBS_VARIANCE 0.0625
//HP GPS
//gps bias correlation time, in seconds
#define GPSBIASHP_CORRELATIONTIME 600.0
//variance (time constant) of gps biases (m^2)
#define GPSBIASHP_VARIANCE 0.0

//JASON SENSOR CONSTANTS
//maximum and minimum tolerable lane widths
#define JASON_MIN_LANEWIDTH 2.0
#define JASON_MAX_LANEWIDTH 5.4864
//minimum confidence for having a valid road segment
#define JASON_MIN_CONFIDENCE 1.0
//the camera's viewpoint: distance at which the camera sees the road (m)
#define JASON_CAMVIEWDIST 3.0
//maximum allowed probability of no road existing to apply an update
#define JASON_NOROAD_THRESH 0.1
//variance of distance from lane center (m^2)
#define JASON_LANEOFST_VAR 0.25
//variance of heading wrt road (rad.^2)
#define JASON_HDGOFST_VAR 0.00761543549466778
//Jason roadfinder hypothesis test threshold: Chi2 with 2 dof
#define JASON_LANE_HYPOTEST 13.8155105579643
//probability that jason measures the correct lane
#define JASON_CORRECTLANE_PROB 0.9
//#define JASON_CORRECTLANE_PROB 0.02
//probability that jason measures an adjacent lane
#define JASON_INCORRECTLANE_PROB 0.025
//probability that jason combines two lanes
#define JASON_TWOLANE_PROB 0.02
//#define JASON_TWOLANE_PROB 0.9
//probability that jason combines three lanes
#define JASON_THREELANE_PROB 0.005
//probability that the jason locks onto a nonexistent lane
#define JASON_FPLANE_PROB 0.005
//jason fp likelihood: modeled as uniform
#define JASON_FPLIKELIHOOD 0.1

//LANE LINE CONSTANTS
//used in comparing mobileye and jason road lines with RNDF lines
#define LL_NOLINE 1
#define LL_DASHEDLINE 2
#define LL_SOLIDLINE 3
#define LL_DOUBLELINE 4

//MOBILEYE SENSOR CONSTANTS
//maximum and minimum tolerable lane widths
#define MOBILEYE_MIN_LANEWIDTH 2.0
#define MOBILEYE_MAX_LANEWIDTH 5.4864
//minimum road confidence for applying a measurement update
#define MOBILEYE_MIN_ROADCONF 3.0
//minimum distance for model validity (m)
#define MOBILEYE_MIN_VALIDDISTANCE 40.0
//the camera's viewpoint: distance at which the camera sees the road (m)
#define MOBILEYE_CAMVIEWDIST 3.0
//variance in the lane offset measurement (m^2)
#define MOBILEYE_LANEOFST_VAR 0.25
//variance in the lane heading measurement (rad^2)
#define MOBILEYE_HDGOFST_VAR 0.007615435494667
//mobileye lane hypothesis test threshold: Chi2 with 2 dof
#define MOBILEYE_LANE_HYPOTEST 13.8155105579643
//probability that mobileye measures the correct lane
#define MOBILEYE_CORRECTLANE_PROB 0.9
//probability that mobileye measures an adjacent lane
#define MOBILEYE_INCORRECTLANE_PROB 0.025
//probability that mobileye combines two lanes
#define MOBILEYE_TWOLANE_PROB 0.02
//probability that mobileye combines three lanes
#define MOBILEYE_THREELANE_PROB 0.005
//probability that the mobileye locks onto a nonexistent lane
#define MOBILEYE_FPLANE_PROB 0.005
//mobileye fp likelihood: modeled as uniform
#define MOBILEYE_FPLIKELIHOOD 0.1

//maximum allowed probability of no road existing to apply an update
#define MOBILEYE_NOROAD_THRESH 0.1
//maximum allowed probability of wrong line measurement to apply an update
#define MOBILEYE_WRONGLINE_THRESH 0.1
//probability of detecting a double line correctly
#define MOBILEYE_DOUBLELINE_ACCURACY 0.9
//probability of detecting a single line correctly
#define MOBILEYE_SINGLELINE_ACCURACY 0.7
//probability of detecting a dashed line correctly
#define MOBILEYE_DASHEDLINE_ACCURACY 0.6
//probability of detecting no line correctly
#define MOBILEYE_NOLINE_ACCURACY 0.6

//EGO-VEHICLE ODOMETRY PROCESS NOISES (CTS TIME)
//velocity and rate of rotation process noise
#define ODOM_QVX 0.0547830131138888
#define ODOM_QVY 0.0547830131138888
#define ODOM_QVZ 0.0547830131138888
#define ODOM_QWX 0.000304617419786709
#define ODOM_QWY 0.000304617419786709
#define ODOM_QWZ 0.000304617419786709

//POSE SENSOR CONSTANTS
//different GPS modes indicating quality of service
#define POSE_GPSNONE 10000
#define POSE_GPSSPS 10001
#define POSE_GPSWAAS 10002
#define POSE_GPSVBS 10003
#define POSE_GPSHP 10004
//additional variance to add to yaw measurement accounting for calibration error (rad.^2)
#define POSE_ADDLHDGVAR 0.00121846967914683
//additional white noise variance to add to pose E & N measurements (m^2)
#define POSE_ADDLENVAR 0.04
//pose hypothesis test threshold: Chi2 with 3 dof
#define POSE_HYPOTEST 16.2662361962381

//POSTERIORPOSE PARAMETERS
//distance threshold (m) for initiating a full RNDF search for partition
#define PP_OFFRNDFDIST 20.0
//the default lanewidth to assume for lanes and interconnects, for determining track membership
#define PP_DEFAULTLANEWIDTH 3.6576

//SIGMA POINT PARAMETERS
//filter closeness to EKF
#define SPF_ALPHA 0.01
//noise closeness to Gaussian
#define SPF_BETA 2.0
//extra scaling factor
#define SPF_KAPPA 0.0

//TRACK PARAMETERS
//maximum number of occluded tracks before track generator starts deleting occluded tracks
#define TG_MAXOCCLUDEDTRACKS 75
//number of particles to use to represent the distribution of each track
#define TG_NUMTRACKPARTICLES 100
//length of time for a track to be predicted before it is deleted (sec.)
#define TG_PREDICTTIME 0.25
//length of time for a track to be marked deleted before it is actually deleted (sec.)
#define TG_DELETETIME 1.0
//length of time for a track to exist without having received a measurement (sec.)
#define TG_MAXTIMESINCEUPDATE 60.0
//minimum time to wait before starting to check measurement update rates (sec.)
#define TG_MINTIMEFORRATECHECK 60.0
//minimum update rate for a track to keep it around (measurements / sec.)
#define TG_MINUPDATERATE 1.0
//maximum distance a target can be to be associated with this track (m)
#define TG_MAXASSOCDIST 5.0
//minimum target speed for a valid heading
#define TG_MINSPDFORHDG 2.2352
//maximum heading variance allowed before heading is marked as unknown (rad.^2)
#define TG_MAXHEADINGVAR 0.698131700797732
//maximum speed variance allowed before speed is marked as unknown (m^2/s^2)
#define TG_MAXSPEEDVAR 4.9961
//maximum range for a track to be maintained (m)
#define TG_MAXTRACKRANGE 150.0
//maximum distance a track can be from the ego vehicle to still occlude other tracks (m)
#define TG_MAXOCCLUDERDIST 40.0
//minimum distance between two tracks for one to occlude the other (m)
#define TG_MINOCCLUSIONDIST 1.0
//angle buffer to apply to occluders to expand their boundaries (rad.)
#define TG_OCCLUDERANGLEBUFFER 0.0872664625997165
//maximum distance an anchor point can be away from a stopline for a track to be considered near the stopline (m)
#define TG_FASTSTOPLINEDIST 10.0
//maximum distance a track point can be from a stopline to be considered on the stopline (m)
#define TG_PRECISESTOPLINEDIST 1.5
//angle match (rad.) for two track angles to be considered the same
#define TG_BEARINGMATCH 0.0523598775598299
//range match (m) for two track ranges to be considered the same
#define TG_RANGEMATCH 0.5
//track process noises (cts time)
#define TG_QX 1.0
#define TG_QY 1.0
#define TG_QORIENT 0.0304617419786709
#define TG_QSPEED 1.0
#define TG_QHEADING 0.0304617419786709
#define TG_QWIDTH 1.0
//atomic time interval for transitions for stopped and carlike state (sec.)
#define TG_HMMDT 0.1
//initial probability of something being stopped
#define TG_INITSTOPPROB 0.75
//probability of remaining stopped / not stopped in one atomic time interval
#define TG_STOPSTAYPROB 0.997692176527023
//number of speed sigmas away from 0 speed to lose 95% of the likelihood of being stopped
#define TG_STOPNS95PCT 2.0
//initial probability 
#define TG_INITCARPROB 0.75
//probability of remaining carlike / not carlike in one atomic time interval
#define TG_CARSTAYPROB 0.986232704493359
//mean width for carlike objects (m)
#define TG_MEANCARWIDTH 4.875
//bell curve width parameter (like standard dev) of car width (m)
#define TG_SCALECARWIDTH 3.5
//initial speed variance when speed is not known
#define TG_INITSPEEDVAR 19.98447616
//initial heading variance when heading is not known
#define TG_INITHEADINGVAR 4.3864908449286
//chi2 threshold (2 dof) applied to auxiliary velocity estimator for resetting
#define TG_AVECHI2THRESH 5.99146454710798
//minimum probability of a track being on a partition in order to send that partition
#define TG_MINPARTITIONPROBABILITY 0.01
//chi2 threshold (3 dof) for position matching
#define TG_CHI2POSTHRESH 7.81472790325116
//chi2 threshold (2 dof) for speed matching
#define TG_CHI2SPDTHRESH 5.99146454710798
//chi2 thershold (5 dof) for overall matching
#define TG_CHI2TOTTHRESH 11.0704976935164

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SCENEESTIMATORCONSTANTS_H
