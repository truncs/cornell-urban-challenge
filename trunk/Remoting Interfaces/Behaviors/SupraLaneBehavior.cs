using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class SupraLaneBehavior : Behavior {
		private ArbiterLaneId startingLaneId;
		private LinePath startingLanePath;
		private double startingLaneWidth;
		private int startingNumLanesLeft;
		private int startingNumLanesRight;

		private ArbiterLaneId endingLaneId;
		private LinePath endingLanePath;
		private double endingLaneWidth;
		private int endingNumLanesLeft;
		private int endingNumLanesRight;

		private Polygon intersectionPolygon;

		private SpeedCommand speedCommand;

		private List<int> ignorableObstacles;

		/*public SupraLaneBehavior(
			ArbiterLaneId startingLaneId, LinePath startingLanePath, double startingLaneWidth, int startingNumLanesLeft, int startingNumLanesRight,
			ArbiterLaneId endingLaneId, LinePath endingLanePath, double endingLaneWidth, int endingNumLanesLeft, int endingNumLanesRight,
			SpeedCommand speedCommand, List<int> ignorableObstacles) {

			this.startingLaneId = startingLaneId;
			this.startingLanePath = startingLanePath;
			this.startingLaneWidth = startingLaneWidth;
			this.startingNumLanesLeft = startingNumLanesLeft;
			this.startingNumLanesRight = startingNumLanesRight;

			this.endingLaneId = endingLaneId;
			this.endingLanePath = endingLanePath;
			this.endingLaneWidth = endingLaneWidth;
			this.endingNumLanesLeft = endingNumLanesLeft;
			this.endingNumLanesRight = endingNumLanesRight;

			this.speedCommand = speedCommand;
			this.ignorableObstacles = ignorableObstacles;
		}*/

		public SupraLaneBehavior(
			ArbiterLaneId startingLaneId, LinePath startingLanePath, double startingLaneWidth, int startingNumLanesLeft, int startingNumLanesRight,
			ArbiterLaneId endingLaneId, LinePath endingLanePath, double endingLaneWidth, int endingNumLanesLeft, int endingNumLanesRight,
			SpeedCommand speedCommand, List<int> ignorableObstacles, Polygon intersectionPolygon) {

			this.startingLaneId = startingLaneId;
			this.startingLanePath = startingLanePath;
			this.startingLaneWidth = startingLaneWidth;
			this.startingNumLanesLeft = startingNumLanesLeft;
			this.startingNumLanesRight = startingNumLanesRight;

			this.endingLaneId = endingLaneId;
			this.endingLanePath = endingLanePath;
			this.endingLaneWidth = endingLaneWidth;
			this.endingNumLanesLeft = endingNumLanesLeft;
			this.endingNumLanesRight = endingNumLanesRight;

			this.intersectionPolygon = intersectionPolygon;

			this.speedCommand = speedCommand;
			this.ignorableObstacles = ignorableObstacles;
		}

		public ArbiterLaneId StartingLaneId {
			get { return startingLaneId; }
		}

		public LinePath StartingLanePath {
			get { return startingLanePath; }
		}

		public double StartingLaneWidth {
			get { return startingLaneWidth; }
		}

		public int StartingNumLanesLeft {
			get { return startingNumLanesLeft; }
		}

		public int StartingNumLanesRight {
			get { return startingNumLanesRight; }
		}

		public ArbiterLaneId EndingLaneId {
			get { return endingLaneId; }
		}

		public LinePath EndingLanePath {
			get { return endingLanePath; }
		}

		public double EndingLaneWidth {
			get { return endingLaneWidth; }
		}

		public int EndingNumLanesLeft {
			get { return endingNumLanesLeft; }
		}

		public int EndingNumLanesRight {
			get { return endingNumLanesRight; }
		}

		public Polygon IntersectionPolygon {
			get { return intersectionPolygon; }
		}

		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
			set { speedCommand = value; }
		}

		public List<int> IgnorableObstacles {
			get { return ignorableObstacles; }
		}

		public override string ToShortString() {
			return "SupraLane";
		}

		public override string ShortBehaviorInformation() {
			return this.startingLaneId.ToString() + " -> " + this.endingLaneId.ToString();
		}

		public override string SpeedCommandString() {
			return speedCommand.ToString();
		}
	}
}
