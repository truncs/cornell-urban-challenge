using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;

namespace OperationalLayer.RoadModel {
	class CombinedRoadModelProvider {
		enum WaypointRejectionResult {
			Unknown,
			AllWaypointsAccepted,
			SomeWaypointsAccepted,
			AllWaypointsRejected,
			NoWaypoints
		}

		class LaneModelTestResult {
			public double rndf_deviation = double.NaN;
			public int forward_match_count = 0;
			public int forward_rejection_count = 0;
		}

		private const double lane_deviation_reject_threshold = 1;
		private const double lane_model_max_dist = 30;
		private const double lane_probability_reject_threshold = 0.65;
		private const double y_var_reject_threshold = 1*1;

		private const double road_deviation_reject_threshold = 2;
		private const double road_model_max_dist = 20;

		private const double model_probability_reject_threshold = 0.5;
		private const double model_probability_accept_threshold = 0.65;

		private const double extra_lane_probability = 0.1;

		private const int existanceVoteStep = 1;
		private const int deviationVoteStep = 5;
		private const int matchVoteStep = 1;
		private const int rejectVoteStep = 1;

		private const double rndf_angle_threshold = 30*Math.PI/180.0;
		private const double rndf_dist_min = 4;
		private const double rndf_dist_max = 15;
		private const double rndf_dist_step = 1;

		private LocalRoadModel localRoadModel;

		private bool didRejectLast = true;

		//private double prevOfftrack;
		//private double prevHeadingOffset;
		//private CarTimestamp prevTime = CarTimestamp.Invalid;
		//private double[] prevProb = new double[3];

		public CombinedRoadModelProvider() {
		}

		public LocalRoadModel LocalRoadModel {
			get { return localRoadModel; }
			set { localRoadModel = value; }
		}

		public void GetLaneChangeModels(LinePath startingRndfPath, double startingRndfPathWidth, int startingNumLanesLeft, int startingNumLanesRight,
			LinePath endingRndfPath, double endingRndfPathWidth, bool changeLeft, CarTimestamp rndfPathTimestamp,
			out ILaneModel startingLaneModel, out ILaneModel endingLaneModel) {

			LocalRoadModel localRoadModel = this.localRoadModel;

			// get the selected lane for the starting path
			int startingSelectedLane = SelectLane(localRoadModel, startingRndfPath, startingRndfPathWidth, rndfPathTimestamp, startingNumLanesLeft, startingNumLanesRight);

			// calculate the number of lanes left and right for the ending lane
			int endingNumLanesLeft = startingNumLanesLeft;
			int endingNumLanesRight = startingNumLanesRight;

			if (changeLeft) {
				endingNumLanesLeft--;
				endingNumLanesRight++;
			}
			else {
				endingNumLanesLeft++;
				endingNumLanesRight--;
			}

			// get the selected lane for the ending path
			int endingSelectedLane = SelectLane(localRoadModel, endingRndfPath, endingRndfPathWidth, rndfPathTimestamp, endingNumLanesLeft, endingNumLanesRight);

			// check if either is invalid or the difference does not line up with the change direction
			int deltaExpected = changeLeft ? 1 : -1; // starting - ending
			if (startingSelectedLane < 0 || endingSelectedLane < 0 || (startingSelectedLane - endingSelectedLane) != deltaExpected) {
				// this is some invalid stuff
				// we won't use either of them since we're not sure which one is truly valid
				startingLaneModel = GetLaneModel(localRoadModel, -1, startingRndfPath, startingRndfPathWidth, rndfPathTimestamp);
				endingLaneModel = GetLaneModel(localRoadModel, -1, endingRndfPath, endingRndfPathWidth, rndfPathTimestamp);

				// mark that we did reject 
				didRejectLast = true;
			}
			else {
				// looks like the lane model selection was valid
				startingLaneModel = GetLaneModel(localRoadModel, startingSelectedLane, startingRndfPath, startingRndfPathWidth, rndfPathTimestamp);
				endingLaneModel = GetLaneModel(localRoadModel, endingSelectedLane, endingRndfPath, endingRndfPathWidth, rndfPathTimestamp);

				// mark that we did not reject
				didRejectLast = false;
			}

			// figure out what we want to send to the ui
			// for now, just send starting lane model
			SendLaneModelToUI(startingLaneModel, rndfPathTimestamp);
		}

		public ILaneModel GetLaneModel(LinePath rndfPath, double rndfPathWidth, CarTimestamp rndfPathTimestamp, int numLanesLeft, int numLanesRight) {
			LocalRoadModel localRoadModel = this.localRoadModel;

			int selectedLane = SelectLane(localRoadModel, rndfPath, rndfPathWidth, rndfPathTimestamp, numLanesLeft, numLanesRight);

			// send the selected lane index (or rejection code) out
			Services.Dataset.ItemAs<double>("selected lane").Add(selectedLane, rndfPathTimestamp);

			// build the road model from the selected lane
			ILaneModel finalLaneModel = GetLaneModel(localRoadModel, selectedLane, rndfPath, rndfPathWidth, rndfPathTimestamp);

			// send to UI for display
			SendLaneModelToUI(finalLaneModel, rndfPathTimestamp);

			// return back to center
			return finalLaneModel;
		}

		private int SelectLane(LocalRoadModel localRoadModel, LinePath rndfPath, double rndfPathWidth, CarTimestamp rndfPathTimestamp, int numLanesLeft, int numLanesRight) {
			// check if we don't have a road model or it should be rejected
			bool nullRejection = localRoadModel == null;
			bool probRejection1 = false, probRejection2 = false, shapeRejection = false;
			if (localRoadModel != null) {
				probRejection1 = (didRejectLast && localRoadModel.ModelProbability < model_probability_accept_threshold);
				probRejection2 = (!didRejectLast && localRoadModel.ModelProbability < model_probability_reject_threshold);
				shapeRejection = !CheckGeneralShape(rndfPath, rndfPathTimestamp, localRoadModel.CenterLaneModel);
			}

			if (nullRejection || probRejection1 || probRejection2 || shapeRejection) {
				int rejectionCode = -4;
				if (nullRejection) {
					rejectionCode = -2;
				}
				else if (probRejection1 || probRejection2) {
					rejectionCode = -3;
				}
				else if (shapeRejection) {
					rejectionCode = -4;
				}

				return rejectionCode;
			}

			// we decided stuff isn't completely stuff
			// we need to pick a lane that is the best fit

			// TODO: hysterysis factor--want to be in the "same lane" as last time

			// get the test results for each lane
			LaneModelTestResult leftTest = TestLane(rndfPath, rndfPathTimestamp, localRoadModel.LeftLane);
			LaneModelTestResult centerTest = TestLane(rndfPath, rndfPathTimestamp, localRoadModel.CenterLaneModel);
			LaneModelTestResult rightTest = TestLane(rndfPath, rndfPathTimestamp, localRoadModel.RightLane);

			// compute the lane existance probability values for left, center and right
			bool hasLeft = numLanesLeft > 0;
			bool hasRight = numLanesRight > 0;

			double probLeftExists = hasLeft ? 1 : extra_lane_probability;
			double probNoLeftExists = hasLeft ? 0 : (1-extra_lane_probability);
			double probRightExists = hasRight ? 1 : extra_lane_probability;
			double probNoRightExists = hasRight ? 0 : (1-extra_lane_probability);

			// calculate center lane probability
			double centerLaneExistanceProb = (probLeftExists*localRoadModel.LeftLane.Probability + probNoLeftExists*(1-localRoadModel.LeftLane.Probability))*(probRightExists*localRoadModel.RightLane.Probability + probNoRightExists*(1-localRoadModel.RightLane.Probability));

			// calculate left lane probability
			double leftLaneExistanceProb = 0.5*(probRightExists*localRoadModel.CenterLaneModel.Probability + probNoRightExists*(1-localRoadModel.CenterLaneModel.Probability));

			// calculate right lane probability
			double rightLaneExistanceProb = 0.5*(probLeftExists*localRoadModel.CenterLaneModel.Probability + probNoLeftExists*(1-localRoadModel.CenterLaneModel.Probability));

			// now figure out what to do!!

			// create rankings for each duder
			// 0 - left lane
			// 1 - center lane
			// 2 - right lane

			// existance vote order
			int[] existanceVotes = GetVoteCounts(false, existanceVoteStep, double.NaN, leftLaneExistanceProb, centerLaneExistanceProb, rightLaneExistanceProb);
			// deviation vote order
			int[] deviatationVotes = GetVoteCounts(true, deviationVoteStep, double.NaN, leftTest.rndf_deviation, centerTest.rndf_deviation, rightTest.rndf_deviation);
			// number agreeing vote order
			int[] numAgreeVotes = GetVoteCounts(false, matchVoteStep, 0, leftTest.forward_match_count, centerTest.forward_match_count, rightTest.forward_match_count);
			// number rejected vote order
			int[] numRejectedVotes = GetVoteCounts(true, rejectVoteStep, 0, leftTest.forward_rejection_count, centerTest.forward_rejection_count, rightTest.forward_rejection_count);

			// vote on the stuff
			int selectedLane = DoVoting(3, existanceVotes, deviatationVotes, numAgreeVotes, numRejectedVotes);

			return selectedLane;
		}

		private int[] GetVoteCounts(bool ascending, int voteStep, double rejectionValue, params double[] values) {
			int[] lanes = new int[values.Length];
			int n = 0;
			// prepare the lanes array and adjust the values array for the sorting operation
			for (int i = 0; i < values.Length; i++) {
				// set the lane index to -1, will be set later to something else if needed
				lanes[i] = -1;
				if (values[i] != rejectionValue && !(double.IsNaN(rejectionValue) && double.IsNaN(values[i]))) {
					lanes[n] = i;
					values[n] = ascending ? values[i] : -values[i];
					n++;
				}
			}

			// if there are no accepted entries, return 0 votes for everything
			if (n == 0) {
				return new int[values.Length];
			}

			// sort lanes based on values
			Array.Sort(values, lanes, 0, n);

			// current vote value
			// this way, the votes will proceed as follows
			//   first - 3*voteStep
			//   second - 2*voteStep
			//   third - voteStep
			// if there is a tie, the ones that are tied will both get votes of the highest position
			//   and the one that is next will reduce by the number equal positions*voteStep
			int currentVote = 3*voteStep;
			// create the votes array, all initialized to 0
			int[] votes = new int[values.Length];
			
			// now assign vote values
			for (int i = 0; i < n; i++) {
				// get the current value
				double currentValue = values[i];

				// assign the current votes to the votes array for this lane
				votes[lanes[i]] = currentVote;

				int numEqual;
				for (numEqual = 1; numEqual < n-i; numEqual++) {
					// check if the next value is equal to the current value
					if (values[i+numEqual] == currentValue) {
						// assign the vote value
						votes[lanes[i+numEqual]] = currentVote;
					}
					else {
						// leave loop, the next is not equivalent
						break;
					}
				}

				// reduce number of equal duders
				numEqual--;
				// increment i forward by the number of equal duders
				i += numEqual;
				// decrement current vote
				currentVote -= (numEqual+1)*voteStep;
			}

			// we have the votes assigned, return the array
			return votes;
		}

		private int DoVoting(int numLanes, params int[][] votes) {
			// iterate through each array apply the votes
			int[] totalVotes = new int[numLanes];

			for (int i = 0; i < votes.Length; i++) {
				for (int j = 0; j < votes[i].Length; j++) {
					totalVotes[j] += votes[i][j];
				}
			}

			// we now have all the votes, pick the best one
			int maxVotes = 0;
			int maxVotesInd = -1;
			for (int i = 0; i < totalVotes.Length; i++) {
				if (totalVotes[i] > maxVotes) {
					maxVotes = totalVotes[i];
					maxVotesInd = i;
				}
			}

			// return the max votes index
			return maxVotesInd;
		}

		private bool CheckGeneralShape(LinePath rndfPath, CarTimestamp rndfPathTimestamp, LocalLaneModel centerLaneModel) {
			// bad newz bears
			if (centerLaneModel.LanePath == null || centerLaneModel.LanePath.Count < 2) {
				return false;
			}

			// project the lane model's path into the rndf path's timestamp
			RelativeTransform relTransform = Services.RelativePose.GetTransform(localRoadModel.Timestamp, rndfPathTimestamp);
			LinePath laneModelPath = centerLaneModel.LanePath.Transform(relTransform);

			// get the zero point of the lane model path
			LinePath.PointOnPath laneZeroPoint = laneModelPath.ZeroPoint;

			// get the first waypoint on the RNDF path
			LinePath.PointOnPath rndfZeroPoint = rndfPath.ZeroPoint;
			Coordinates firstWaypoint = rndfPath[rndfZeroPoint.Index+1];

			// get the projection of the first waypoint onto the lane path
			LinePath.PointOnPath laneFirstWaypoint = laneModelPath.GetClosestPoint(firstWaypoint);

			// get the offset vector
			Coordinates offsetVector = laneFirstWaypoint.Location - firstWaypoint;

			// start iterating through the waypoints forward and project them onto the rndf path
			for (int i = rndfZeroPoint.Index+1; i < rndfPath.Count; i++) {
				// get the waypoint
				Coordinates waypoint = rndfPath[i];

				// adjust by the first waypoint offset
				waypoint += offsetVector;

				// project onto the lane model 
				LinePath.PointOnPath laneWaypoint = laneModelPath.GetClosestPoint(waypoint);

				// check the distance from the zero point on the lane model
				if (laneModelPath.DistanceBetween(laneZeroPoint, laneWaypoint) > road_model_max_dist) {
					break;
				}

				// check the devation from the rndf
				double deviation = waypoint.DistanceTo(laneWaypoint.Location);

				// if the deviation is over some threshold, then we reject the model
				if (deviation > road_deviation_reject_threshold) {
					return false;
				}
			}

			// we got this far, so this stuff is OK
			return true;
		}

		private LaneModelTestResult TestLane(LinePath rndfPath, CarTimestamp rndfPathTimestamp, LocalLaneModel laneModel) {
			// construct the result object to hold stuff
			LaneModelTestResult result = new LaneModelTestResult();

			// project the lane model's path into the rndf path's timestamp
			RelativeTransform relTransform = Services.RelativePose.GetTransform(localRoadModel.Timestamp, rndfPathTimestamp);
			LinePath laneModelPath = laneModel.LanePath.Transform(relTransform);

			// get the zero point of the lane model path
			LinePath.PointOnPath laneZeroPoint = laneModelPath.ZeroPoint;

			// get the first waypoint on the RNDF path
			LinePath.PointOnPath rndfZeroPoint = rndfPath.ZeroPoint;

			// get the heading of the rndf path at its zero point and the heading of the lane model at 
			// the rndf's zero point
			LinePath.PointOnPath laneStartPoint = laneModelPath.GetClosestPoint(rndfZeroPoint.Location);
			Coordinates laneModelHeading = laneModelPath.GetSegment(laneStartPoint.Index).UnitVector;
			Coordinates rndfHeading = rndfPath.GetSegment(rndfZeroPoint.Index).UnitVector;
			double angle = Math.Acos(laneModelHeading.Dot(rndfHeading));

			// check if the angle is within limits for comparing offset
			if (angle < 30*Math.PI/180.0) {
				// get the deviation between lane zero point and rndf zero point
				result.rndf_deviation = rndfZeroPoint.Location.DistanceTo(laneZeroPoint.Location);
			}

			// now start check for how many waypoints are accepted
			for (int i = rndfZeroPoint.Index + 1; i < rndfPath.Count; i++) {
				// check the distance along the rndf path
				double rndfDistAlong = rndfPath.DistanceBetween(rndfZeroPoint, rndfPath.GetPointOnPath(i));
				// break out if we're too far along the rndf
				if (rndfDistAlong > 50) {
					break;
				}

				// get the waypoint
				Coordinates waypoint = rndfPath[i];

				// project on to lane path
				LinePath.PointOnPath laneWaypoint = laneModelPath.GetClosestPoint(waypoint);

				// check if we're too far along the lane path
				double distAlong = laneModelPath.DistanceBetween(laneZeroPoint, laneWaypoint);
				if (distAlong > lane_model_max_dist || distAlong < 0) {
					break;
				}

				// check if the deviation
				double dist = waypoint.DistanceTo(laneWaypoint.Location);

				// increment appropriate counts
				if (dist < lane_deviation_reject_threshold) {
					result.forward_match_count++;
				}
				else {
					result.forward_rejection_count++;
				}
			}

			// return the result
			return result;
		}

		private ILaneModel GetLaneModel(LocalRoadModel localRoadModel, int selectedLane, LinePath rndfPath, double rndfPathWidth, CarTimestamp rndfPathTimestamp) {
			didRejectLast = false;

			switch (selectedLane) {
				case 0:
					return GetLaneModel(localRoadModel.LeftLane, rndfPath, rndfPathWidth, rndfPathTimestamp);

				case 1:
					return GetLaneModel(localRoadModel.CenterLaneModel, rndfPath, rndfPathWidth, rndfPathTimestamp);

				case 2:
					return GetLaneModel(localRoadModel.RightLane, rndfPath, rndfPathWidth, rndfPathTimestamp);
			}

			didRejectLast = true;

			return new PathLaneModel(rndfPathTimestamp, rndfPath, rndfPathWidth);
		}

		private ILaneModel GetLaneModel(LocalLaneModel laneModel, LinePath rndfPath, double rndfPathWidth, CarTimestamp rndfPathTimestamp) {
			// check the lane model probability
			if (laneModel.Probability < lane_probability_reject_threshold) {
				// we're rejecting this, just return a path lane model
				return new PathLaneModel(rndfPathTimestamp, rndfPath, rndfPathWidth);
			}

			// project the lane model's path into the rndf path's timestamp
			RelativeTransform relTransform = Services.RelativePose.GetTransform(localRoadModel.Timestamp, rndfPathTimestamp);
			LinePath laneModelPath = laneModel.LanePath.Transform(relTransform);

			// iterate through the waypoints in the RNDF path and project onto the lane model
			// the first one that is over the threshold, we consider the waypoint before as a potential ending point
			LinePath.PointOnPath laneModelDeviationEndPoint = new LinePath.PointOnPath();
			// flag indicating if any of the waypoint tests failed because of the devation was too high
			bool anyDeviationTooHigh = false;
			// number of waypoints accepted
			int numWaypointsAccepted = 0;

			// get the vehicle's position on the rndf path
			LinePath.PointOnPath rndfZeroPoint = rndfPath.ZeroPoint;

			// get the vehicle's position on the lane model
			LinePath.PointOnPath laneModelZeroPoint = laneModelPath.ZeroPoint;

			// get the last point we want to consider on the lane model
			LinePath.PointOnPath laneModelFarthestPoint = laneModelPath.AdvancePoint(laneModelZeroPoint, lane_model_max_dist);

			// start walking forward through the waypoints on the rndf path
			// this loop will implicitly exit when we're past the end of the lane model as the waypoints
			//		will stop being close to the lane model (GetClosestPoint returns the end point if we're past the 
			//    end of the path)
			for (int i = rndfZeroPoint.Index+1; i < rndfPath.Count; i++) {
				// get the waypoint
				Coordinates rndfWaypoint = rndfPath[i];
				
				// get the closest point on the lane model
				LinePath.PointOnPath laneModelClosestPoint = laneModelPath.GetClosestPoint(rndfWaypoint);

				// compute the distance between the two
				double deviation = rndfWaypoint.DistanceTo(laneModelClosestPoint.Location);

				// if this is above the deviation threshold, leave the loop
				if (deviation > lane_deviation_reject_threshold || laneModelClosestPoint > laneModelFarthestPoint) {
					// if we're at the end of the lane model path, we don't want to consider this a rejection 
					if (laneModelClosestPoint < laneModelFarthestPoint) {
						// mark that at least on deviation was too high
						anyDeviationTooHigh = true;
					}
					break;
				}

				// increment the number of waypoint accepted
				numWaypointsAccepted++;
				
				// update the end point of where we're valid as the local road model was OK up to this point
				laneModelDeviationEndPoint = laneModelClosestPoint;
			}

			// go through and figure out how far out the variance is within tolerance
			LinePath.PointOnPath laneModelVarianceEndPoint = new LinePath.PointOnPath();
			// walk forward from this point until the end of the lane mode path
			for (int i = laneModelZeroPoint.Index+1; i < laneModelPath.Count; i++) {
				// check if we're within the variance toleration
				if (laneModel.LaneYVariance[i] <= y_var_reject_threshold) {
					// we are, update the point on path
					laneModelVarianceEndPoint = laneModelPath.GetPointOnPath(i);
				}
				else {
					// we are out of tolerance, break out of the loop
					break;
				}
			}

			// now figure out everything out
			// determine waypoint rejection status
			WaypointRejectionResult waypointRejectionResult;
			if (laneModelDeviationEndPoint.Valid) {
				// if the point is valid, that we had at least one waypoint that was ok
				// check if any waypoints were rejected
				if (anyDeviationTooHigh) {
					// some waypoint was ok, so we know that at least one waypoint was accepted 
					waypointRejectionResult = WaypointRejectionResult.SomeWaypointsAccepted;
				}
				else {
					// no waypoint triggered a rejection, but at least one was good
					waypointRejectionResult = WaypointRejectionResult.AllWaypointsAccepted;
				}
			}
			else {
				// the point is not valid, so we either had no waypoints or we had all rejections
				if (anyDeviationTooHigh) {
					// the first waypoint was rejected, so all are rejected
					waypointRejectionResult = WaypointRejectionResult.AllWaypointsRejected;
				}
				else {
					// the first waypoint (if any) was past the end of the lane model
					waypointRejectionResult = WaypointRejectionResult.NoWaypoints;
				}
			}

			// criteria for determining if this path is valid:
			//	- if some or all waypoints were accepted, than this is probably a good path
			//		- if some of the waypoints were accepted, we go no farther than the last waypoint that was accepted
			//	- if there were no waypoints, this is a potentially dangerous situation since we can't reject
			//    or confirm the local road model. for now, we'll assume that it is correct but this may need to change
			//    if we start handling intersections in this framework
			//  - if all waypoints were rejected, than we don't use the local road model
			//  - go no farther than the laneModelVarianceEndPoint, which is the last point where the y-variance of 
			//    the lane model was in tolerance
			
			// now build out the lane model
			ILaneModel finalLaneModel;

			// check if we rejected all waypoints or no lane model points satisified the variance threshold
			if (waypointRejectionResult == WaypointRejectionResult.AllWaypointsRejected || !laneModelVarianceEndPoint.Valid) {
				// want to just use the path lane model
				finalLaneModel = new PathLaneModel(rndfPathTimestamp, rndfPath, rndfPathWidth);
			}
			else {
				// we'll use the lane model
				// need to build up the center line as well as left and right bounds
				
				// to build up the center line, use the lane model as far as we feel comfortable (limited by either variance 
				// or by rejections) and then use the rndf lane after that. 
				LinePath centerLine = new LinePath();

				// figure out the max distance
				// if there were no waypoints, set the laneModelDeviationEndPoint to the end of the lane model
				if (waypointRejectionResult == WaypointRejectionResult.NoWaypoints) {
					laneModelDeviationEndPoint = laneModelFarthestPoint;
				}

				// figure out the closer of the end points
				LinePath.PointOnPath laneModelEndPoint = (laneModelDeviationEndPoint < laneModelVarianceEndPoint) ? laneModelDeviationEndPoint : laneModelVarianceEndPoint;
				bool endAtWaypoint = laneModelEndPoint == laneModelDeviationEndPoint;

				// add the lane model to the center line
				centerLine.AddRange(laneModelPath.GetSubpathEnumerator(laneModelZeroPoint, laneModelEndPoint));

				// create a list to hold the width expansion values
				List<double> widthValue = new List<double>();
				
				// make the width expansion values the width of the path plus the 1-sigma values
				for (int i = laneModelZeroPoint.Index; i < laneModelZeroPoint.Index+centerLine.Count; i++) {
					widthValue.Add(laneModel.Width/2.0 + Math.Sqrt(laneModel.LaneYVariance[i]));
				}

				// now figure out how to add the rndf path
				// get the projection of the lane model end point on the rndf path
				LinePath.PointOnPath rndfPathStartPoint = rndfPath.GetClosestPoint(laneModelEndPoint.Location);

				// if the closest point is past the end of rndf path, then we don't want to tack anything on
				if (rndfPathStartPoint != rndfPath.EndPoint) {
					// get the last segment of the new center line
					Coordinates centerLineEndSegmentVec = centerLine.EndSegment.UnitVector;
					// get the last point of the new center line
					Coordinates laneModelEndLoc = laneModelEndPoint.Location;

					// now figure out the distance to the next waypoint
					LinePath.PointOnPath rndfNextPoint = new LinePath.PointOnPath();

					// figure out if we're ending at a waypoint or not
					if (endAtWaypoint) {
						rndfNextPoint = rndfPath.GetPointOnPath(rndfPathStartPoint.Index+1);

						// if the distance from the start point to the next point is less than rndf_dist_min, then 
						// use the waypont after
						double dist = rndfPath.DistanceBetween(rndfPathStartPoint, rndfNextPoint);

						if (dist < rndf_dist_min) {
							if (rndfPathStartPoint.Index < rndfPath.Count-2) {
								rndfNextPoint = rndfPath.GetPointOnPath(rndfPathStartPoint.Index + 2);
							}
							else if (rndfPath.DistanceBetween(rndfPathStartPoint, rndfPath.EndPoint) < rndf_dist_min) {
								rndfNextPoint = LinePath.PointOnPath.Invalid;
							}
							else {
								rndfNextPoint = rndfPath.AdvancePoint(rndfPathStartPoint, rndf_dist_min*2);
							}
						}
					}
					else {
						// track the last angle we had
						double lastAngle = double.NaN;

						// walk down the rndf path until we find a valid point
						for (double dist = rndf_dist_min; dist <= rndf_dist_max; dist += rndf_dist_step) {
							// advance from the start point by dist
							double distTemp = dist;
							rndfNextPoint = rndfPath.AdvancePoint(rndfPathStartPoint, ref distTemp);

							// if the distTemp is > 0, then we're past the end of the path
							if (distTemp > 0) {
								// if we're immediately past the end, we don't want to tack anything on
								if (dist == rndf_dist_min) {
									rndfNextPoint = LinePath.PointOnPath.Invalid;
								}

								break;
							}

							// check the angle made by the last segment of center line and the segment
							// formed between the end point of the center line and this new point
							double angle = Math.Acos(centerLineEndSegmentVec.Dot((rndfNextPoint.Location-laneModelEndLoc).Normalize()));

							// check if the angle satisfies the threshold or we're increasing the angle
							if (Math.Abs(angle) < rndf_angle_threshold || (!double.IsNaN(lastAngle) && angle > lastAngle)) {
								// break out of the loop, we're done searching
								break;
							}

							lastAngle = angle;
						}
					}

					// tack on the rndf starting at next point going to the end
					if (rndfNextPoint.Valid) {
						LinePath subPath = rndfPath.SubPath(rndfNextPoint, rndfPath.EndPoint);
						centerLine.AddRange(subPath);
						
						// insert the lane model end point into the sub path
						subPath.Insert(0, laneModelEndLoc);

						// get the angles
						List<Pair<int, double>> angles = subPath.GetIntersectionAngles(0, subPath.Count-1);

						// add the width of the path inflated by the angles
						for (int i = 0; i < angles.Count; i++) {
							// calculate the width expansion factor
							// 90 deg, 3x width
							// 45 deg, 1.5x width
							// 0 deg, 1x width
							double widthFactor = Math.Pow(angles[i].Right/(Math.PI/2.0), 2)*2 + 1;

							// add the width value
							widthValue.Add(widthFactor*laneModel.Width/2);
						}

						// add the final width
						widthValue.Add(laneModel.Width/2);
					}

					// set the rndf path start point to be the point we used 
					rndfPathStartPoint = rndfNextPoint;
				}

				// for now, calculate the left and right bounds the same way we do for the path lane model
				// TODO: figure out if we want to do this more intelligently using knowledge of the lane model uncertainty
				LinePath leftBound = centerLine.ShiftLateral(widthValue.ToArray());
				// get the next shifts
				for (int i = 0; i < widthValue.Count; i++) { widthValue[i] = -widthValue[i]; }
				LinePath rightBound = centerLine.ShiftLateral(widthValue.ToArray());

				// build the final lane model
				finalLaneModel = new CombinedLaneModel(centerLine, leftBound, rightBound, laneModel.Width, rndfPathTimestamp);
			}

			SendLaneModelToUI(finalLaneModel, rndfPathTimestamp);

			// output the fit result
			return finalLaneModel;
		}

		private void SendLaneModelToUI(ILaneModel laneModel, CarTimestamp ts) {
			// get the center, left and right bounds
			//LinearizationOptions opts = new LinearizationOptions(0, 50, ts);
			//LinePath finalCenter = laneModel.LinearizeCenterLine(opts);
			//LinePath finalLeft = laneModel.LinearizeLeftBound(opts);
			//LinePath finalRight = laneModel.LinearizeRightBound(opts);

			//Services.UIService.PushLineList(finalCenter, ts, "lane center line", true);
			//Services.UIService.PushLineList(finalLeft, ts, "lane left bound", true);
			//Services.UIService.PushLineList(finalRight, ts, "lane right bound", true);
		}
	}
}
