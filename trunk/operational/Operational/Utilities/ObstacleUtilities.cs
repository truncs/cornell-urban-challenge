using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Obstacles;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using System.Diagnostics;

namespace OperationalLayer.Utilities {
	static class ObstacleUtilities {

    // returns true if the line segment is not completely inside of the perimeter or if it touches one of the obstacles
    private static bool ExpansionViolatesConstraints(LineSegment ls, Polygon pcPerimeter, List<Polygon> pcObstacles)
    {
      if (!pcPerimeter.IsInside(ls)) return true;
      for (int i = 0; i < pcObstacles.Count; i++)
      {
        if (pcObstacles[i].DoesIntersect(ls))
        {
          return true;
        }
      }
      return false;
    }

    // obstacles - list of obstacles to avoid near the parking spot
    // projectionLine - P1 is the place where the front bumper of the vehicle should end up; P0 is where the rear bumper of the vehicle should end up
    // zonePerimiter - list of points specifying the perimiter of the parking zone
    // vehiclePosition - current position of the vehicle
    // vehicleHeading - heading of the car, in radians

    // corner1 and corner2 are always set to the corners of the rectangle, however if the function returns FALSE then
    // the rectangle is the minimum 8x8 box containing the vehicle's back axis whether or not the box contains obstacles or crosses the zone perimiter
    public static bool FindParkingInterval(IList<Obstacle> obstacles, LineSegment projectionLine, Polygon zonePerimeter, Coordinates vehiclePosition, double vehicleHeading, ref Coordinates[] corners)
    {
      const double expansion_step = .25;
      bool ret = true;

      // transform everything into "parking coordinates":
      // 0,0 is where the center of the rear bumper should end up when parking is finished
      // the vehicle should be parallel to (and on top of) the Y axis when parking has finished (positive Y)
      List<Polygon> pcObstacles = new List<Polygon>(obstacles.Count);
      for(int i=0;i<obstacles.Count;i++){
        Polygon p = obstacles[i].AvoidancePolygon.Inflate(Math.Max(obstacles[i].minSpacing,.5));
        for(int j=0;j<p.points.Count;j++){
          p.points[j] -= projectionLine.P0;
          p.points[j] = p.points[j].Rotate(-projectionLine.UnitVector.RotateM90().ArcTan);
        }
				pcObstacles.Add(p);
      }

      Polygon pcPerimeter = new Polygon(zonePerimeter);
      for (int i = 0; i < pcPerimeter.points.Count; i++)
      {
        pcPerimeter.points[i] -= projectionLine.P0;
        pcPerimeter.points[i] = pcPerimeter.points[i].Rotate(-projectionLine.UnitVector.RotateM90().ArcTan);
      }

      Coordinates pcVehPos = vehiclePosition;
      pcVehPos -= projectionLine.P0;
      pcVehPos = pcVehPos.Rotate(-projectionLine.UnitVector.RotateM90().ArcTan);

      double leftBound, rightBound;
      double lowBound; // highbound is always 0
      const double highBound = 0;

      leftBound = Math.Min(-TahoeParams.T / 2.0 - 1, pcVehPos.X - TahoeParams.T / 2.0 * 1.2);
      rightBound = Math.Max(TahoeParams.T / 2.0 + 1, pcVehPos.X + TahoeParams.T / 2.0 * 1.2);
      lowBound = Math.Min(0, pcVehPos.Y - TahoeParams.T / 2.0 * 1.2);

      bool canExpandDown = true;
      bool canExpandLeft = true;
      bool canExpandRight = true;

      // initial expansion... lower boundary
      while (lowBound > -8 && canExpandDown)
      {
        LineSegment l = new LineSegment(new Coordinates(leftBound, lowBound), new Coordinates(rightBound, lowBound));
        if (ExpansionViolatesConstraints(l,pcPerimeter,pcObstacles))
        {
          canExpandDown = false;
          break;
        }
        lowBound -= expansion_step;
      }

      // initial expansion... left & right boundary
      while ((rightBound-leftBound) < 8 && (canExpandRight || canExpandLeft))
      {
        LineSegment ls;
        
        ls = new LineSegment(new Coordinates(leftBound, lowBound), new Coordinates(leftBound, highBound));
        if (ExpansionViolatesConstraints(ls, pcPerimeter, pcObstacles))
        {
          canExpandLeft = false;
        }
        else
        {
          leftBound -= expansion_step;
        }
        
        ls = new LineSegment(new Coordinates(rightBound, lowBound), new Coordinates(rightBound, highBound));
        if (ExpansionViolatesConstraints(ls, pcPerimeter, pcObstacles))
        {
          canExpandRight = false;
        }
        else
        {
          rightBound += expansion_step;
        }
      }

      // move boundaries out to make at least an 8x8 m box, even if this violates the perimiter/includes obstacles
      if(rightBound-leftBound < 8){
        double tmp = (rightBound+leftBound)/2;
        leftBound = tmp - 4;
        rightBound = tmp + 4;
        ret = false;
      }
      if (lowBound > -8)
      {
        ret = false;
        lowBound = -8;
      }

      bool lastExpandLeft = false;

      while((canExpandDown || canExpandLeft || canExpandRight) && (lowBound>-30) && (rightBound-leftBound)<30){
        if(!(canExpandLeft || canExpandRight) || (-lowBound < (rightBound-leftBound) && canExpandDown)){
          LineSegment l = new LineSegment(new Coordinates(leftBound, lowBound - .5), new Coordinates(rightBound, lowBound - .5));
          if (ExpansionViolatesConstraints(l, pcPerimeter, pcObstacles))
          {
            canExpandDown = false;
          }else{
            lowBound -= expansion_step;
          }
        }else if(!(canExpandDown || canExpandRight) || (canExpandLeft && !(lastExpandLeft && canExpandRight))){
          LineSegment l = new LineSegment(new Coordinates(leftBound - .5, lowBound), new Coordinates(leftBound - .5, highBound));
          if (ExpansionViolatesConstraints(l, pcPerimeter, pcObstacles))
          {
            canExpandLeft = false;
          }else{
            leftBound -= expansion_step;
          }
          lastExpandLeft = true;
        }else{  // canExpandRight
          LineSegment l = new LineSegment(new Coordinates(rightBound + .5, lowBound), new Coordinates(rightBound + .5, highBound));
          if (ExpansionViolatesConstraints(l, pcPerimeter, pcObstacles))
          {
            canExpandRight = false;
          }
          else{
            rightBound += expansion_step;
          }
          lastExpandLeft = false;
        }
      }

      corners = new Coordinates[4];

      corners[0] = new Coordinates(leftBound, lowBound);
      corners[1] = new Coordinates(leftBound, highBound);
      corners[2] = new Coordinates(rightBound, highBound);
      corners[3] = new Coordinates(rightBound, lowBound);

			Random r = new Random();
      for (int i = 0; i < 4; i++)
      {
        corners[i] = corners[i].Rotate(projectionLine.UnitVector.RotateM90().ArcTan);
        corners[i] += projectionLine.P0;
      }

      return ret;

      /*Line pll = new Line(projectionLine.P0,projectionLine.P1);
      Coordinates offVec = projectionLine.UnitVector.Rotate90();
      // find the leftmost boundary (looking into the parking spot)
      for (int i = 0; i < 20; i++)
      {
        Line tl = new Line(pll);
        tl.P0 = tl.P0 + offVec * i;
        tl.P1 = tl.P1 + offVec * i;
        
        Coordinates[] ipts;
        zonePerimeter.Intersect(tl, out ipts);

        obstacles[0].AvoidancePolygon

        if (ipts.Length < 2) break; // the test line is completely outside of the zone perimiter
        if (ipts.Length == 2)
        {

        }
      }*/

		}
    public static void FindParkingInterval(IList<Obstacle> obstacles, LineSegment projectionLine, double offDistMax, double offDistMin, double maxBottomDist, double maxTopDist, ref Coordinates topPoint, ref Coordinates bottomPoint)
    {
			List<double> projectedDists = new List<double>(1000);

			Coordinates vec = projectionLine.UnitVector;
			Coordinates startPt = projectionLine.P0;
			double len = projectionLine.Length;

			foreach (Obstacle obs in obstacles) {
				foreach (Coordinates pt in obs.AvoidancePolygon) {
					double projDist = vec.Dot(pt - startPt);
					double offDist = vec.Cross(pt - startPt);

					if (projDist < 0 || projDist > len)
						continue;

					if (offDist < offDistMin || offDist > offDistMax)
						continue;

					// add to the list
					projectedDists.Add(projDist);
				}
			}

			// sort the list
			projectedDists.Sort();

			// find the max dist between two adjacent projected distance
			double bestDist = -1;
			double bestProjBottom = 0;
			double bestProjTop = maxTopDist;
			for (int i = 0; i < projectedDists.Count-1; i++) {
				if (projectedDists[i] > maxBottomDist) {
					if (bestDist < 0) {
						bestDist = bestProjTop = projectedDists[i];
					}
					break;
				}
				double dist = projectedDists[i+1]-projectedDists[i];
				if (dist > bestDist) {
					bestDist = dist;
					bestProjBottom = projectedDists[i];
					bestProjTop = projectedDists[i+1];
				}
			}

			bottomPoint = projectionLine.P0 + vec*bestProjBottom;
			topPoint = projectionLine.P0 + vec*bestProjTop;
		}
	}
}
