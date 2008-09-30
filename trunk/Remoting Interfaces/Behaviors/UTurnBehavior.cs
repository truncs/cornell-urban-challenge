using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class UTurnBehavior : Behavior {
		private Polygon boundary;
		private LinePath endingPath;
		private ArbiterLaneId endingLane;
		private SpeedCommand endingSpeedCommand;
		private List<Polygon> stayOutPolygons;
		private bool stopOnEndingPath;

		public UTurnBehavior(Polygon boundary, LinePath endingPath, ArbiterLaneId endingLane, SpeedCommand endingSpeedCommand) {
			this.boundary = boundary;
			this.endingPath = endingPath;
			this.endingLane = endingLane;
			this.endingSpeedCommand = endingSpeedCommand;
			this.stayOutPolygons = new List<Polygon>();
		}

		public UTurnBehavior(Polygon boundary, LinePath endingPath, ArbiterLaneId endingLane, SpeedCommand endingSpeedCommand, List<Polygon> stayOutPolygons)
		{
			this.boundary = boundary;
			this.endingPath = endingPath;
			this.endingLane = endingLane;
			this.endingSpeedCommand = endingSpeedCommand;
			this.stayOutPolygons = stayOutPolygons;
			this.stopOnEndingPath = true;
		}

		public bool StopOnEndingPath {
			get { return stopOnEndingPath; }
		}

		public List<Polygon> StayOutPolygons {
			get { return stayOutPolygons; }			
		}

		public Polygon Boundary {
			get { return boundary; }
		}

		public LinePath EndingPath {
			get { return endingPath; }
		}

		public ArbiterLaneId EndingLane {
			get { return endingLane; }
		}

		public SpeedCommand EndingSpeedCommand {
			get { return endingSpeedCommand; }
		}

		public override string ToString() {
			return string.Format("UTurnBehavior: ending lane {0}, ending speed {1}", endingLane, endingSpeedCommand);
		}

    public override string ToShortString()
    {
      return("uTurnBehavior");
    }

    public override string ShortBehaviorInformation()
    {
      return (endingLane == null ? "null" : endingLane.ToString());
    }

		public override string SpeedCommandString()
		{
			return this.endingSpeedCommand.ToString();
		}
	}
}
