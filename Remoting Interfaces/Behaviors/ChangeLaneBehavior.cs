using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class ChangeLaneBehavior : Behavior {
		private ArbiterLaneId startLane;
		private ArbiterLaneId targetLane;
		private double maxDist;
		private SpeedCommand speedCommand;
		private List<int> ignorableObstacles;
		private bool changeLeft;
		private LinePath backupStartLanePath;
		private LinePath backupTargetLanePath;
		private double startWidth;
		private double targetWidth;
		private int startingNumLanesLeft;
		private int startingNumLanesRight;

		public ChangeLaneBehavior(ArbiterLaneId startLane, ArbiterLaneId targetLane, bool changeLeft, double maxDist, SpeedCommand speedCommand,
			IEnumerable<int> ignorableObstacles)
		{
			this.startLane = startLane;
			this.targetLane = targetLane;
			this.maxDist = maxDist;
			this.speedCommand = speedCommand;
			this.changeLeft = changeLeft;

			if (ignorableObstacles != null)
			{
				this.ignorableObstacles = new List<int>(ignorableObstacles);
			}
			else
			{
				this.ignorableObstacles = new List<int>();
			}
		}

		public ChangeLaneBehavior(ArbiterLaneId startLane, ArbiterLaneId targetLane, bool changeLeft, double maxDist, SpeedCommand speedCommand,
			IEnumerable<int> ignorableObstacles, LinePath backupStartLanePath, LinePath backupTargetLanePath, double startWidth, double targetWidth,
			int startingNumLanesLeft, int startingNumLanesRight) {
			this.startLane = startLane;
			this.targetLane = targetLane;
			this.maxDist = maxDist;
			this.speedCommand = speedCommand;
			this.changeLeft = changeLeft;
			this.backupStartLanePath = backupStartLanePath;
			this.backupTargetLanePath = backupTargetLanePath;
			this.startWidth = startWidth;
			this.targetWidth = targetWidth;
			this.startingNumLanesLeft = startingNumLanesLeft;
			this.startingNumLanesRight = startingNumLanesRight;

			if (ignorableObstacles != null) {
				this.ignorableObstacles = new List<int>(ignorableObstacles);
			}
			else {
				this.ignorableObstacles = new List<int>();
			}
		}

		public ArbiterLaneId StartLane {
			get { return startLane; }
		}

		public ArbiterLaneId TargetLane {
			get { return targetLane; }
		}

		public double MaxDist {
			get { return maxDist; }
		}

		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public List<int> IgnorableObstacles {
			get { return ignorableObstacles; }
		}

		public bool ChangeLeft {
			get { return changeLeft; }
		}

		public LinePath BackupStartLanePath {
			get { return backupStartLanePath; }
		}

		public LinePath BackupTargetLanePath {
			get { return backupTargetLanePath; }
		}

		public double StartLaneWidth {
			get { return startWidth; }
		}

		public double TargetLaneWidth {
			get { return targetWidth; }
		}

		public int StartingNumLanesLeft {
			get { return startingNumLanesLeft; }
		}

		public int StartingNumLaneRights {
			get { return startingNumLanesRight; }
		}

		public override string ToString() {
			return string.Format("ChangeLangeBehavior: {0}->{1}, max dist {2}, change left {3}, speed command {4}", startLane, targetLane, maxDist, changeLeft, speedCommand);
		}

    public override string ToShortString()
    {
      return "ChangeLanesBehavior";
    }

		public override string ShortBehaviorInformation()
		{
			return startLane.ToString() + " -> " + targetLane.ToString() + ", d: " + this.maxDist.ToString("f2");
		}

		public override string SpeedCommandString()
		{
			return speedCommand.ToString();
		}
  }
}
