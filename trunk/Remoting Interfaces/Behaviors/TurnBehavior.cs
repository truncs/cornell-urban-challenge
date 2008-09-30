using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.ArbiterRoads;
using System.Runtime.Serialization;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class TurnBehavior : Behavior {
		private ArbiterLaneId targetLane;
		private LinePath targetLanePath;
		private LineList leftBound;
		private LineList rightBound;
		private SpeedCommand speedCommand;
		private List<int> ignorableObstacles;
		private ArbiterInterconnectId interconnectId;
		public List<int> VehiclesToIgnore;

		[OptionalField]
		private Polygon intersectionPolygon;

		public TurnBehavior(ArbiterLaneId targetLane, LinePath targetLanePath, LineList leftBound, LineList rightBound, SpeedCommand speedCommand, ArbiterInterconnectId interconnectId) {
			this.targetLane = targetLane;
			this.targetLanePath = targetLanePath;
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.speedCommand = speedCommand;
			this.ignorableObstacles = new List<int>();
			this.interconnectId = interconnectId;
			this.VehiclesToIgnore = new List<int>();
			this.decorators = new List<BehaviorDecorator>();
		}

		public TurnBehavior(ArbiterLaneId targetLane, LinePath targetLanePath, LineList leftBound, LineList rightBound, SpeedCommand speedCommand, Polygon intersectionPolygon, ArbiterInterconnectId interconnectId) {
			this.targetLane = targetLane;
			this.targetLanePath = targetLanePath;
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.speedCommand = speedCommand;
			this.intersectionPolygon = intersectionPolygon;
			this.ignorableObstacles = new List<int>();
			this.interconnectId = interconnectId;
			this.VehiclesToIgnore = new List<int>();
			this.decorators = new List<BehaviorDecorator>();
		}

		public TurnBehavior(ArbiterLaneId targetLane, LinePath targetLanePath, LineList leftBound, LineList rightBound, SpeedCommand speedCommand, Polygon intersectionPolygon, IEnumerable<int> ignorableObstacles, ArbiterInterconnectId interconnectId)
		{
			this.targetLane = targetLane;
			this.targetLanePath = targetLanePath;
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.speedCommand = speedCommand;
			this.intersectionPolygon = intersectionPolygon;
			this.ignorableObstacles = ignorableObstacles != null ? new List<int>(ignorableObstacles) : new List<int>();
			this.interconnectId = interconnectId;
			this.VehiclesToIgnore = new List<int>();
			this.decorators = new List<BehaviorDecorator>();
		}

		public ArbiterInterconnectId InterconnectId {
			get { return interconnectId; }
		}

		public List<int> IgnorableObstacles {
			get { return ignorableObstacles; }
		}

		public ArbiterLaneId TargetLane {
			get { return targetLane; }
		}

		public LinePath TargetLanePath {
			get { return targetLanePath; }
		}

		public LineList LeftBound {
			get { return leftBound; }
			set { rightBound = value; }
		}

		public LineList RightBound {
			get { return rightBound; }
			set { leftBound = value; }
		}

		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public Polygon IntersectionPolygon {
			get { return intersectionPolygon; }
		}

		public override string ToString() {
			return string.Format("TurnBehavior: target {0}, speed command {1}", targetLane, speedCommand);
		}

		public override string ToShortString()
		{
			return ("TurnBehavior");
		}

		public override string ShortBehaviorInformation()
		{
			string init = (targetLane != null ? targetLane.ToString() : "To Zone");
			string decs = "";
			foreach (BehaviorDecorator bd in this.Decorators)
			{
				if (bd is ShutUpAndDoItDecorator)
				{
					decs += ", " + ((ShutUpAndDoItDecorator)bd).Level.ToString();
				}
			}
			if (this.leftBound == null)
				decs += ", lBnull";
			if (this.rightBound == null)
				decs += ", rBnull";

			return init + decs;
		}

		public override string SpeedCommandString()
		{
			return speedCommand.ToString();
		}

		public override string UniqueId()
		{
			return this.interconnectId != null ? this.interconnectId.ToString() : this.ToShortString();
		}
	}
}
