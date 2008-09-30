using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class ArcVotingResults {
		public List<ArcResults> arcResults;
		public ArcResults selectedArc;
	}

	[Serializable]
	public class ArcResults {
		public double curvature;
		public bool vetoed;
		public double totalUtility;
		public double obstacleHitDistance;
		public double obstacleClearanceDistance;

		public double obstacleUtility;
		public double hysteresisUtility;
		public double straightUtility;
		public double goalUtility;
		public double sideObstacleUtility;
		public double rollUtility;
		public double roadUtility;
	}
}
