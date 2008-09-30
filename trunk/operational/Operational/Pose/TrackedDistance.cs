using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using Dataset.Source;
using UrbanChallenge.Common.Utility;
using System.Diagnostics;
using OperationalLayer.Tracing;

namespace OperationalLayer.Pose {
	class TrackedDistance {
		private class TrackedDistanceItem {
			public CarTimestamp timestamp;
			public double distance;

			public TrackedDistanceItem(CarTimestamp timestamp, double distance) {
				this.timestamp = timestamp;
				this.distance = distance;
			}
		}

		private CarTimestamp lastTime;
		private double lastDist;
		private object lockobj;
		private RollingQueue<TrackedDistanceItem> queue;

		public TrackedDistance(int capacity) {
			lastDist = 0;
			lastTime = new CarTimestamp(double.NaN);
			lockobj = new object();
			queue = new RollingQueue<TrackedDistanceItem>(capacity);

			Services.Dataset.ItemAs<double>("speed").DataValueAdded += TrackedDistance_DataValueAdded;
		}

		void TrackedDistance_DataValueAdded(object sender, SourceDataValueAddedEventArgs<double> e) {
			// check if we received something before
			if (double.IsNaN(lastTime.ts)) {
				// we haven't received anything, initialize starting time
				lastTime = e.Time;
			}
			else if (e.Time < lastTime) {
				//OperationalTrace.WriteWarning("resetting tracked distanace: event time {0}, last time {1}", e.Time, lastTime);
				// timestamp rollover/reset
				// clear everything out
				lock (lockobj) {
					queue.Clear();
					lastTime = e.Time;
					lastDist = 0;
				}
			}
			else {
				// calculate dt
				double dt = e.Time.ts - lastTime.ts;

				// calculate delta distance
				double dx = Math.Abs(e.Value)*dt;

				// get the lock
				lock (lockobj) {
					lastDist += dx;
					lastTime = e.Time;

					//OperationalTrace.WriteVerbose("adding dist {0}, time {1}", lastDist, lastTime);
					queue.Add(new TrackedDistanceItem(lastTime, lastDist));
				}
			}
		}

		public double GetDistanceTravelled(CarTimestamp t0, CarTimestamp t1) {
			TrackedDistanceItem item0 = null;
			TrackedDistanceItem item1 = null;
			lock (lockobj) {
				// find the location of the starting and stopping time
				int i0 = queue.Count-1;
				while (i0 >= 0 && queue[i0].timestamp > t0) {
					i0--;
				}
				if (i0 >= 0) {
					item0 = queue[i0];
				}

				int i1 = queue.Count-1;
				while (i1 >= 0 && queue[i1].timestamp > t1) {
					i1--;
				}
				if (i1 >= 0) {
					item1 = queue[i1];
				}
			}

			if (item0 == null || item1 == null)
				throw new InvalidOperationException(string.Format("Requested timestamps {0:F4}->{1:F4} do not exist in queue", t0, t1));

			//OperationalTrace.WriteVerbose("looking for {0}->{1}, got {2}->{3}, distance {4}->{5}, travelled {6}", t0, t1, item0.timestamp, item1.timestamp, item0.distance, item1.distance, item1.distance - item0.distance);

			return item1.distance - item0.distance;
		}
	}
}
