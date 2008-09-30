using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class Boundary {
		public IList<Coordinates> Coords;
		public bool Polygon;
		public double MinSpacing;
		public double DesiredSpacing;
		public double AlphaS;
		public bool CheckSmallObstacle;
		public bool CheckFrontBumper;
		public int Index;

		public Boundary() {
			MinSpacing = 0.1;
			DesiredSpacing = 1;
			AlphaS = -1;
			Polygon = false;
			CheckSmallObstacle = true;
			CheckFrontBumper = false;
			Index = -2;
		}

		public Boundary(IList<Coordinates> coords, double alpha_w) : this() {
			this.Coords = coords;
			this.AlphaS = alpha_w;
			this.MinSpacing = 0;
			this.DesiredSpacing = 0;
		}

		public Boundary(IList<Coordinates> coords, double minSpacing, double desiredSpacing):this() {
			Coords = coords;
			MinSpacing = minSpacing;
			DesiredSpacing = desiredSpacing;
		}

		public Boundary(IList<Coordinates> coords, double minSpacing, double desiredSpacing, double alpha_s):this() {
			Coords = coords;
			MinSpacing = minSpacing;
			DesiredSpacing = desiredSpacing;
			AlphaS = alpha_s;
		}

		public Boundary(IList<Coordinates> coords, double minSpacing, double desiredSpacing, double alpha_s, bool checkSmallObs)
			: this() {
			this.Coords = coords;
			this.MinSpacing = minSpacing;
			this.DesiredSpacing = desiredSpacing;
			this.AlphaS = alpha_s;
			this.CheckSmallObstacle = checkSmallObs;
		}
	}

}
