using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Behaviors.UIBehaviors {
	[Serializable]
	public class SimplePathFollowingBehavior : Behavior {
		protected Path path;
		protected float velocity;
		protected bool constVelocityMode;

		public SimplePathFollowingBehavior(Path path, float vel, bool constVelocityMode) {
			this.path = path;
			this.velocity = vel;
			this.constVelocityMode = constVelocityMode;
		}

		public Path Path {
			get { return path; }
		}

		public float Velocity {
			get { return velocity; }
		}

		public bool ConstVelocityMode {
			get { return constVelocityMode; }
		}

		public override Behavior NextBehavior {
			get { return new PauseBehavior(); }
		}

		public override bool Equals(object obj) {
			SimplePathFollowingBehavior p = obj as SimplePathFollowingBehavior;
			if (p == null)
				return false;

			return p.velocity == velocity && object.Equals(path, p.path);
		}

		public override int GetHashCode() {
			return velocity.GetHashCode() ^ path.GetHashCode();
		}
	}
}
