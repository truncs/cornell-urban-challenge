using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Pose;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Common;
using OperationalLayer.Tracing;
using UrbanChallenge.Common.Pose;

namespace OperationalLayer.Pose {
	class AbsolutePoseQueue {
		private RollingQueue<AbsolutePose> queue;
		private object lockobj = new object();

		public AbsolutePoseQueue(int capacity) {
			this.queue = new RollingQueue<AbsolutePose>(capacity);
		}

		public void PushAbsolutePose(AbsolutePose pose) {
			lock (lockobj) {
				queue.Add(pose);

				if (!queue.VerifySort(delegate(AbsolutePose l, AbsolutePose r) { return l.timestamp.CompareTo(r.timestamp); })) {
					OperationalTrace.WriteError("absolute sort is donzoed, flushing queue");
					queue.Clear();
				}
			}
		}

		public AbsolutePose GetAbsolutePose(CarTimestamp timestamp) {
			lock (lockobj) {
				//OperationalTrace.WriteVerbose("tride to find absolute pose for {0}", timestamp);
				//return queue.FindClosest(delegate(AbsolutePose p) { return p.timestamp.CompareTo(timestamp); });
				int i = queue.Count-1;
				while (i >= 0 && queue[i].timestamp > timestamp) {
					i--;
				}

				CarTimestamp ts = CarTimestamp.Invalid;
				if (i >= 0 && i < queue.Count) {
					ts = queue[i].timestamp;
				}
				OperationalTrace.WriteVerbose("tried to find absolute pose for {0}, found index {1} timestamp {2}", timestamp, i, ts);

				if (i < 0) {
					throw new TransformationNotFoundException(timestamp);
				}
				return queue[i];
			}
		}

		public AbsoluteTransformer GetAbsoluteTransformer(CarTimestamp timestamp) {
			return new AbsoluteTransformer(GetAbsolutePose(timestamp));
		}

		public AbsolutePose Current {
			get {
				lock (lockobj) {
					if (queue.Count == 0)
						return default(AbsolutePose);
					else
						return queue[queue.Count-1];
				}
			}
		}
	}
}