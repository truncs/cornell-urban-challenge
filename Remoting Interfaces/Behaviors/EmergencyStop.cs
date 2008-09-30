using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class EmergencyStop : Behavior
	{
		private bool poseWorking;
		private bool obsDetectionWorking;
		private bool laneDetectionWorking;

		public EmergencyStop(bool poseWorking, bool obsDetectionWorking, bool laneDetectionWorking)
		{
			this.poseWorking = poseWorking;
			this.obsDetectionWorking = obsDetectionWorking;
			this.laneDetectionWorking = laneDetectionWorking;
		}

		public bool PoseWorking
		{
			get { return poseWorking; }
		}

		public bool ObstacleDetectionWorking
		{
			get { return obsDetectionWorking; }
		}

		public bool LaneDetectionWorking
		{
			get { return laneDetectionWorking; }
		}

		public override string ToShortString()
		{
			return ("EmergencyStopBehavior");
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
