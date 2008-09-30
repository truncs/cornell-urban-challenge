using System;
using System.Collections.Generic;
using System.Text;


namespace UrbanChallenge.Behaviors.UIBehaviors {
	[Serializable]
	public class CommandBehavior : Behavior {
		private float? steeringCmd;
		private float? brakeCmd;
		private float? throttleCmd;

		public CommandBehavior(float? steeringCmd, float? brakeCmd, float? throttleCmd) {
			this.steeringCmd = steeringCmd;
			this.brakeCmd = brakeCmd;
			this.throttleCmd = throttleCmd;
		}

		public float? SteeringCommand {
			get { return steeringCmd; }
		}

		public float? BrakeCommand {
			get { return brakeCmd; }
		}

		public float? ThrottleCommand {
			get { return throttleCmd; }
		}

		public override Behavior NextBehavior {
			get { return null; }
		}
	}
}
