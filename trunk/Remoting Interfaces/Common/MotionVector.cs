using System;
using System.Collections.Generic;

namespace UrbanChallenge.Common {

	[Serializable]
	public struct MotionVector {

		// Constructor.
		public MotionVector(double angle, Coordinates velocity) {
			this.Angle = angle;
			this.Velocity = velocity;
		}

		// Radian.
		public double Angle;

		// Coordinates per millisecond.
		public Coordinates Velocity;

		public override bool Equals(object obj) {
			if (obj is MotionVector) {
				MotionVector other = (MotionVector)obj;
				return this.Angle.Equals(other.Angle) && (this.Velocity.Equals(other.Velocity));
			} else
				return false;
		}

		public override int GetHashCode() {
			return (this.Angle.GetHashCode() << 16) ^ (this.Velocity.GetHashCode());
		}

		public static bool operator ==(MotionVector left, MotionVector right) {
			return left.Angle == right.Angle && left.Velocity == right.Velocity;
		}

		public static bool operator !=(MotionVector left, MotionVector right) {
			return !(left == right);
		}

	}

}
