using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class StayInLaneBehavior : Behavior {
		private SpeedCommand speedCommand;
		private ArbiterLaneId targetLane;
		private List<int> ignorableObstacles;
		private LinePath backupPath;
		private double laneWidth;
		private int numLanesLeft;
		private int numLanesRight;

		public StayInLaneBehavior(ArbiterLaneId targetLane, SpeedCommand speedCommand, IEnumerable<int> ignorableObstacles)
		{
			this.targetLane = targetLane;
			this.speedCommand = speedCommand;
			if (ignorableObstacles != null)
			{
				this.ignorableObstacles = new List<int>(ignorableObstacles);
			}
			else
			{
				this.ignorableObstacles = new List<int>();
			}
		}

		public StayInLaneBehavior(ArbiterLaneId targetLane, SpeedCommand speedCommand, IEnumerable<int> ignorableObstacles, LinePath backupPath, double laneWidth, int numLanesLeft, int numLanesRight) {
			this.targetLane = targetLane;
			this.speedCommand = speedCommand;
			if (ignorableObstacles != null) {
				this.ignorableObstacles = new List<int>(ignorableObstacles);
			}
			else {
				this.ignorableObstacles = new List<int>();
			}
			this.backupPath = backupPath;
			this.laneWidth = laneWidth;
			this.numLanesLeft = numLanesLeft;
			this.numLanesRight = numLanesRight;
		}

		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
			set { speedCommand = value; }
		}

		public ArbiterLaneId TargetLane {
			get { return targetLane; }
		}

		public List<int> IgnorableObstacles {
			get { return ignorableObstacles; }
			set { this.ignorableObstacles = value; }
		}

		public LinePath BackupPath {	
			get { return backupPath; }
		}

		public double LaneWidth {
			get { return laneWidth; }
			set { laneWidth = value; }
		}

		public int NumLaneLeft {
			get { return numLanesLeft; }
		}

		public int NumLanesRight {
			get { return numLanesRight; }
		}

		public override string ToString() {
			return string.Format("StayInLaneBehavior: {0}, speed command {1}", targetLane, speedCommand);
		}

    public override string ToShortString() {
      return ("StayInLaneBehavior");
    }

    public override string ShortBehaviorInformation()
    {
			return targetLane != null ? targetLane.ToString() : "NONE";
    }

		public override string SpeedCommandString()
		{
			return this.speedCommand.ToString();
		}

		public override string UniqueId()
		{
			return this.targetLane != null ? this.targetLane.ToString() : this.ToShortString();
		}
	}
}
