using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.Obstacles {
	static class ObstacleUtilitiesStuff {
		//private class ObstacleData {
		//  public Polygon poly;
		//  public Coordinates[] leftHitPoints;
		//  public Coordinates[] rightHitPoints;
		//}

		//public static double MinDistToLaneBlockage(LinePath centerLine, LinePath leftBound, LinePath rightBound, IList<Polygon> obstacles) {
		//  // 1) create a bounding box around left and right bound
		//  // 2) filter all obstacles by their bounding boxes to get the ones way outside the lane
		//  // 3) get the closest point of each polygon to the center line
		//  // 4) intersect the line formed by the closest point and obstacle point with the lane boundaries

		//  Rect leftBoundingBox = leftBound.GetBoundingBox();
		//  Rect rightBoundingBox = rightBound.GetBoundingBox();
		//  Rect totalBoundingBox = Rect.Union(leftBoundingBox, rightBoundingBox);

		//  double laneBlockageDist = double.MaxValue;

		//  Circle tahoeCircle = new Circle(TahoeParams.T + 0.2, Coordinates.Zero);
		//  Polygon tahoePolygon = tahoeCircle.ToPolygon(32);

		//  List<ObstacleData> convObstacles = new List<ObstacleData>();

		//  foreach (Polygon obstacle in obstacles) {
		//    // inflate the obstacle by the vehicle width 
		//    // TODO: incorporate obstacle spacing

		//    // this will break down if the obstacle is a rectangle with only 4 points on the corners for example
		//    //obstacle = obstacle.Inflate(TahoeParams.T);

		//    // get the bounding box of the polygon
		//    Rect obstacleRect = obstacle.CalculateBoundingRectangle();
		//    // check if it overlaps the lane bounding box
		//    if (!totalBoundingBox.Overlaps(obstacleRect))
		//      continue;

		//    ObstacleData data = new ObstacleData();

		//    // convolve the obstacle polygon with the tahoe "polygon" by which i mean circle
		//    data.poly = obstacle = Polygon.ConvexMinkowskiConvolution(tahoePolygon, obstacle);

		//    // go through the edges on the left boundary and see if any intersect this polygon
		//    int leftIntersectionSeg = -1;
		//    Coordinates[] leftIntersectionPoints = null;
		//    for (int i = 0; i < leftBound.Count-1; i++) {
		//      if (obstacle.Intersect(leftBound.GetSegment(i), out leftIntersectionPoints)) {
		//        leftIntersectionSeg = i;
		//        break;
		//      }
		//    }

		//    // the obstacle does not intersect the left boundary, so we don't have to check the right boundary as it can't
		//    // be blocking the lane
		//    if (leftIntersectionSeg != -1) {
		//      data.leftHitPoints = leftIntersectionPoints;
		//    }

		//    // we have a left hit, now see if there is a right hit
		//    int rightIntersectionSeg = -1;
		//    Coordinates[] rightInterectionPoints = null;
		//    for (int i = 0; i < rightBound.Count-1; i++) {
		//      if (obstacle.Intersect(rightBound.GetSegment(i), out rightInterectionPoints)) {
		//        rightIntersectionSeg = i;
		//        break;
		//      }
		//    }

		//    // did we find a right hit? if not, this obstacle doesn't block stuff
		//    if (rightIntersectionSeg != -1) {
		//      data.rightHitPoints = rightInterectionPoints;
		//    }

		//    if (leftIntersectionSeg != -1 && rightIntersectionSeg != -1) {
		//      // this is already a blockage, figure stuff out

		//      // there is both a left and right hit, figure out which one is closer
		//      // enumerate all the left and right hits, getting the closest point on the center line
		//      // calculate the distance between this and the starting point of the lane
		//      foreach (Coordinates hitPoint in MultiEnumerator.GetEnumerator(leftIntersectionPoints, rightInterectionPoints)) {
		//        LinePath.PointOnPath centerLinePoint = centerLine.GetClosestPoint(hitPoint);
		//        double dist = centerLine.DistanceBetween(centerLine.StartPoint, hitPoint);

		//        if (dist < laneBlockageDist) {
		//          laneBlockageDist = dist;
		//        }
		//      }
		//    }
		//    else {
		//      convObstacles.Add(data);
		//    }
		//  }
		//}
	}
}
