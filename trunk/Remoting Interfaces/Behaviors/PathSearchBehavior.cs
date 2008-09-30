using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;

namespace UrbanChallenge.Behaviors {
	public enum PathSearchType {
		OnRoadSearch,
		GetOnRoadSearch,
		EnterParkingSpace,
		ExitParkingSpace
	}

	[Serializable]
	public class PathSearchBehavior : Behavior {
		private LineList leftBound;
		private LineList rightBound;
		private Coordinates endOrientation;
		private double endHeading;
		private double endSpeed;
		private double maxSpeed;
		private PathSearchType searchType;

		public PathSearchBehavior(LineList leftBound, LineList rightBound, Coordinates endOrientation, double endHeading, double endSpeed, double maxSpeed, PathSearchType searchType) {
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.endOrientation = endOrientation;
			this.endHeading = endHeading;
			this.endSpeed = endSpeed;
			this.maxSpeed = maxSpeed;
			this.searchType = searchType;
		}

		public LineList LeftBound {
			get { return leftBound; }
		}

		public LineList RightBound {
			get { return rightBound; }
		}

		public Coordinates EndOrientation {
			get { return endOrientation; }
		}

		public double EndHeading {
			get { return endHeading; }
		}

		public double EndSpeed {
			get { return endSpeed; }
		}

		public double MaxSpeed {
			get { return maxSpeed; }
		}

		public PathSearchType SearchType {
			get { return searchType; }
		}

		public override string ToShortString()
		{
			return ("PathSearchBehavior");
		}

		public override string ShortBehaviorInformation()
		{
			return ("");
		}

		public override string SpeedCommandString()
		{
			return "";
		}
	}
}
