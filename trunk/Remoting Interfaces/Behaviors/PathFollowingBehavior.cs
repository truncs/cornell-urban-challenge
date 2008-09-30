using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class PathFollowingBehavior : Behavior {
		private Path basePath;
		private LineList leftBound;
		private LineList rightBound;
		private SpeedCommand speedCommand;

		public PathFollowingBehavior(Path basePath, LineList leftBound, LineList rightBound, SpeedCommand speedCommand) {
			this.basePath = basePath;
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.speedCommand = speedCommand;
		}

		public Path BasePath {
			get { return basePath; }
		}

		public LineList LeftBound {
			get { return leftBound; }
		}

		public LineList RightBound {
			get { return rightBound; }
		}

		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public override string ToShortString()
		{
			return ("PathFollowingBehavior");
		}

		public override string ShortBehaviorInformation()
		{
			return ("");
		}

		public override string SpeedCommandString()
		{
			return speedCommand.ToString();
		}
	}
}
