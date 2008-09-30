using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.PathPlanning {
	struct PathPoint {
		public double X;
		public double Y;
		public double speed;

		public PathPoint(double X, double Y, double speed) {
			this.X = X;
			this.Y = Y;
			this.speed = speed;
		}

		public PathPoint(Coordinates pt, double speed) {
			this.X = pt.X;
			this.Y = pt.Y;
			this.speed = speed;
		}

		public Coordinates Point {
			get { return new Coordinates(X, Y); }
			set { X = value.X; Y = value.Y; }
		}
	}
}
