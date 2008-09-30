using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors.CompletionReport {
	[Serializable]
	public class TrajectoryBlockedReport : CompletionReport {
		private CompletionResult result;
		private double distToBlockage;
		private BlockageType blockageType;
		private int trackID;
		private bool reverseRecommended;
		private SAUDILevel saudiLevel;
		private bool badStuffFlag;

		public TrajectoryBlockedReport(CompletionResult result, double distToBlockage, BlockageType blockageType, int trackID, bool reverseRecommended, Type behaviorType)
			: base(behaviorType) {
			this.result = result;
			this.distToBlockage = distToBlockage;
			this.blockageType = blockageType;
			this.trackID = trackID;
			this.reverseRecommended = reverseRecommended;
			this.saudiLevel = SAUDILevel.None;
		}

		public TrajectoryBlockedReport(CompletionResult result, double distToBlockage, BlockageType blockageType, int trackID, bool reverseRecommended, Type behaviorType, string behaviorId)
			: base(behaviorType, behaviorId)
		{
			this.result = result;
			this.distToBlockage = distToBlockage;
			this.blockageType = blockageType;
			this.trackID = trackID;
			this.reverseRecommended = reverseRecommended;
			this.saudiLevel = SAUDILevel.None;
		}

		public TrajectoryBlockedReport(Type behaviorType)
			: base(behaviorType) {
			this.badStuffFlag = true;
			this.trackID = -1;
			this.blockageType = BlockageType.Static;
			this.reverseRecommended = true;
			this.distToBlockage = double.NaN;
			this.saudiLevel = SAUDILevel.L1;
			this.result = CompletionResult.Stopped;
		}

		public override CompletionResult Result {
			get { return result; }
		}

		public SAUDILevel SAUDILevel {
			get { return saudiLevel; }
			set { saudiLevel = value; }
		}

		public double DistanceToBlockage {
			get { return distToBlockage; }
		}

		public BlockageType BlockageType {
			get { return blockageType; }
		}

		public int TrackID {
			get { return trackID; }
		}

		public bool ReverseRecommended {
			get { return reverseRecommended; }
		}

		public override string ToString() {
			return string.Format("TrajectorBlockedReport -- type {0}, result {1}, blockage type {2}, dist {3}, track id {4}, reverse recommended {5}, saudi level {6}", base.BehaviorType, result, blockageType, distToBlockage, trackID, reverseRecommended, saudiLevel);
		}
	}
}
