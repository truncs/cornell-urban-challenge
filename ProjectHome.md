http://4.bp.blogspot.com/_F7aJZWIx2R0/RyuY8jGAhqI/AAAAAAAAAHQ/qIGrqUAfq0Q/s400/IMG_1405.JPG

## Cornell's DARPA Urban Challenge Vehicle ##

This is the repository for Cornell's 2007 DARPA Urban Challenge Vehicle, Skynet. The full source code for all relevant components of the vehicle is available here. The code is written in Visual Studio 2005/2008 with a mixture of C++ and C#. All the libraries used are for Windows 32 bit with the exception of the pose estimator, which can run in either 32 or 64 bit mode.

### Major Components ###

  * Arbiter - High level route planning
  * Operational - Low level vehicle control and obstacle avoidance
  * Local Map - Low level sensor fusion
  * Scene Estimator - Posterior positioning and high level target tracking
  * Pose Estimator - a tightly coupled pose estimator fusing wheel speed, IMU measurements, HP corrections and raw GPS psuedoranges
  * Sensors - These are the low level sensing engines which feed to either the local map or scene estimator. They include lidar clustering, road and lane finding in camera data, and other lidar based feature detectors.
  * Utilities - Data playback engine, Messaging and Name services, Other utilities
  * Simulation - Allows for simulation of the AI components (Arbiter and Operational)

Further documentation will be available in the wiki.

### Major Change Log ###
October 1, 2008 - Initial snapshot of repository deployed.

