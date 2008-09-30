#ifndef POSTERIORPOSEPOSITION_H
#define POSTERIORPOSEPOSITION_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define PPP_NUMSTATES 3

struct PosteriorPosePosition
{
	//structure for storing the posterior pose position solution

	//the time the posterior pose solution is valid
	double PosteriorPoseTime;
	//whether the posterior pose solution is valid
	bool IsValid;

	//the ENH MMSE solution
	double EastMMSE;
	double NorthMMSE;
	double HeadingMMSE;

	//the ENH covariance (mean square error) matrix
	double CovarianceMMSE[PPP_NUMSTATES*PPP_NUMSTATES];
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //POSTERIORPOSEPOSITION_H
