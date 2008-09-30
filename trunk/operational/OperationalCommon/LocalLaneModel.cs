using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class LocalLaneModel {
		private LinePath lanePath;
		private double[] laneYVariance;
		private double width, widthVariance;
		private double probability;

		public LocalLaneModel(LinePath lanePath, double[] laneYVariance, double width, double widthVariance, double probability) {
			// do some checking to make sure stuff is value
			if (probability > 0) {
				if (lanePath == null) {
					throw new ArgumentNullException("lanePath");
				}
				if (laneYVariance == null) {
					throw new ArgumentNullException("laneYVariance");
				}
				if (laneYVariance.Length != lanePath.Count) {
					throw new ArgumentException("Number of lane points and length of lane point variance must match");
				}
			}

			this.lanePath = lanePath;
			this.laneYVariance = laneYVariance;
			this.width = width;
			this.widthVariance = widthVariance;
			this.probability = probability;
		}

		public LinePath LanePath {
			get { return lanePath; }
		}

		public double[] LaneYVariance {
			get { return laneYVariance; }
		}

		public double Width {
			get { return width; }
		}

		public double WidthVariance {
			get { return widthVariance; }
		}

		public double Probability {
			get { return probability; }
		}

		public bool IsValid {
			get { return probability > 0; }
		}

		public LocalLaneModel Clone() {
			double[] varianceClone = new double[laneYVariance.Length];
			Array.Copy(laneYVariance, varianceClone, laneYVariance.Length);
			return new LocalLaneModel(lanePath.Clone(), varianceClone, width, widthVariance, probability);
		}
	}
}
