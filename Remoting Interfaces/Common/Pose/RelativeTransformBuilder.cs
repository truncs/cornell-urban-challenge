using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common.Utility;
using System.Diagnostics;

namespace UrbanChallenge.Common.Pose {
	public class RelativeTransformBuilder {
		private class TransformEntry {
			public CarTimestamp timestamp;
			public Matrix4 transform;

			public TransformEntry(CarTimestamp timestamp, Matrix4 transform) {
				this.timestamp = timestamp;
				this.transform = transform;
			}
		}

		private RollingQueue<TransformEntry> rollingQueue;

		private bool twoD;

		private CarTimestamp recentTimestamp;
		private object lockobj = new object();

		public RelativeTransformBuilder(int capacity, bool twoD) {
			this.twoD = twoD;
			recentTimestamp = new CarTimestamp(double.NaN);
			rollingQueue = new RollingQueue<TransformEntry>(capacity);
		}

		public CarTimestamp CurrentTimestamp {
			get { return recentTimestamp; }
		}

		public void PushTransform(CarTimestamp timestamp, Matrix4 transform) {
			lock (lockobj) {
				TransformEntry te = new TransformEntry(timestamp, transform);
				rollingQueue.Add(te);

				// check if this is the most recent timestamp
				if (!double.IsNaN(recentTimestamp.ts) && timestamp < recentTimestamp) {
					// do a reverse bubble-sort
					int i = rollingQueue.Count-2;
					// find the insertion point
					while (i >= 0 && rollingQueue[i].timestamp > timestamp) {
						// shift the entry up
						rollingQueue[i+1] = rollingQueue[i];
						i--;
					}

					// i+1 contains the empty slot to insert at
					rollingQueue[i+1] = te;
				}
				else {
					// update the recent timestamp
					recentTimestamp = timestamp;
				}

				if (!rollingQueue.VerifySort(delegate(TransformEntry l, TransformEntry r) { return l.timestamp.CompareTo(r.timestamp); })) {
					Trace.TraceError("relative transform sort is donzoed, flushing queue");
					Reset();
				}
			}
		}

		public void Reset() {
			lock (lockobj) {
				rollingQueue.Clear();
				recentTimestamp = double.NaN;
			}
		}

		public RelativeTransform GetTransform(CarTimestamp timestampOrigin, CarTimestamp timestampEnd) {
			lock (lockobj) {
				// find the best origin timestamp
				// this will be the entry just less than timestampOrigin
				int i_origin = rollingQueue.Count-1;
				while (i_origin >= 0 && rollingQueue[i_origin].timestamp > timestampOrigin) {
					i_origin--;
				}

				// fidn the best ending timestamp
				int i_end = rollingQueue.Count-1;
				while (i_end >= 0 && rollingQueue[i_end].timestamp > timestampEnd) {
					i_end--;
				}

				// check if either transform isn't found
				if (i_origin == -1 || i_end == -1) {
					throw new TransformationNotFoundException("Requested transformation does not exist in the queued data: " + timestampOrigin.ts.ToString("F5") + "->" + timestampEnd.ts.ToString("F5"));
				}

				// check that the timestamps we chose make sense
				// it should be the case that the transform chosen at or before the timestamp and the next
				// transform is after the timestamp
				if (rollingQueue[i_origin].timestamp > timestampOrigin) {
					// this is some stuff
					Console.Write("origin index bad: " + i_origin + ", entry timestamp: " + rollingQueue[i_origin].timestamp + ", requested timestamp: " + timestampOrigin);
					Console.WriteLine();
				}

				if (i_origin < rollingQueue.Count-1) {
					Debug.Assert(rollingQueue[i_origin+1].timestamp > timestampOrigin);
				}

				Debug.Assert(rollingQueue[i_end].timestamp <= timestampEnd);
				if (i_end < rollingQueue.Count-1) {
					Debug.Assert(rollingQueue[i_end+1].timestamp > timestampEnd);
				}

				// build the relative transform
				Matrix4 transform = rollingQueue[i_end].transform*rollingQueue[i_origin].transform.Inverse();

				// return the stuff
				return new RelativeTransform(timestampOrigin, timestampEnd, transform);
			}
		}
	}
}
