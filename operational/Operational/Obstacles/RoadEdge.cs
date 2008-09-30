using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Obstacles {
	static class RoadEdge {
		static SideRoadEdge lastLeftEdge;
		static SideRoadEdge lastRightEdge;

		public static void OnRoadEdge(SideRoadEdge edge) {
			if (edge == null)
				return;

			if (edge.side == SideRoadEdgeSide.Driver) {
				lastLeftEdge = edge;
			}
			else {
				lastRightEdge = edge;
			}
		}

		public static LinePath GetLeftEdgeLine() {
			return GetEdgeLine(lastLeftEdge);
		}

		public static LinePath GetRightEdgeLine() {
			return GetEdgeLine(lastRightEdge);
		}

		public static LinePath GetEdgeLine(SideRoadEdge edge) {
			if (edge == null || !edge.isValid || Math.Abs(edge.curbDistance) < TahoeParams.T/2.0 + 0.1) {
				return null;
			}

			LinePath ret = new LinePath();
			Coordinates pt1 = new Coordinates(2, 0).Rotate(edge.curbHeading) + new Coordinates(0, edge.curbDistance);
			Coordinates pt2 = new Coordinates(10 + TahoeParams.FL, 0).Rotate(edge.curbHeading) + new Coordinates(0, edge.curbDistance);

			ret.Add(pt1);
			ret.Add(pt2);

			return ret;
		}
	}
}
