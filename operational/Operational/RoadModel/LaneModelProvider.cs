using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.RoadModel {
	class LaneModelProvider {
		private Dictionary<string, ILaneModel> laneModels;
		private CarTimestamp lastTime = CarTimestamp.Invalid;
		private object lockobj = new object();

		public LaneModelProvider() {
			
		}

		public CarTimestamp LastLaneModelTime {
			get { return lastTime; }
		}

		public bool ModelValid {
			get {
				lock (lockobj) {
					return laneModels != null && laneModels.Count > 0 && lastTime.IsValid;
				}
			}
		}

		public Dictionary<string, ILaneModel> LaneModels {
			get {
				lock (lockobj) {
					return laneModels;
				}
			}
		}

		public void SetLaneModel(IEnumerable<ILaneModel> models) {
			Dictionary<string, ILaneModel> newLanes = new Dictionary<string, ILaneModel>();
			foreach (ILaneModel l in models) {
				lastTime = l.Timestamp;
				newLanes.Add(l.LaneID, l);
			}

			lock (lockobj) {
				laneModels = newLanes;
			}
		}
	}
}
