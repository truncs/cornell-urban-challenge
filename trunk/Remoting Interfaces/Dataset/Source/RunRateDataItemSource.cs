using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Utility;

namespace Dataset.Source {
	public class RunRateDataItemSource : DataItemSource<double> {
		// default window size in seconds if none is specified
		private const int DefaultWindowSize = 5;
		// minimum window size in seconds
		private const int MinWindowSize = 1;
		// default rate to use when figuring out how big to make run time queue
		private const int DefaultRate = 20; 
		// number of deletes we're allowed to do before to doing a full recalculation for sumPeriod, sumPeriodSq
		private const int DeleteRecalcThreshold = 100;

		private struct RunTimeEntry {
			public double period;
			public DateTime timestamp;

			public RunTimeEntry(double period, DateTime timestamp) {
				this.period = period;
				this.timestamp = timestamp;
			}
		}

		private DataItemSource<double> periodMaxDataItem;
		private DataItemSource<double> periodStdDevDataItem;

		// size of run time queue to keep around in seconds
		private int windowSize;

		// circular queue of run time data
		private RunTimeEntry[] runTimeQueue;
		private int runTimeQueueHead;
		private int runTimeQueueTail;
		private int runTimeQueueCount;

		// timestamp of the oldest entry in the queue, only valid if runTimeQueueCount > 0
		private DateTime oldestQueueTime;

		// number of entries deleted from the queue since the last full recalculation
		private int numDeleted;

		// tracker variables
		// sum of the periods of the entries in the queue
		private double sumPeriod;
		// sum of the squared periods of the entries in the queue
		private double sumPeriodSq;
		// maximum period of entries in the queue
		private double maxPeriod;

		// last timestamp of getting mark operations
		private DateTime lastTimestamp;
		// flag indicating if we've set the lastTime field
		private bool setLastTimestamp;

		// global locking object
		private object lockobj = new object();

		public RunRateDataItemSource(DataItemDescriptor desc, List<KeyValuePair<string, string>> attributes)
			: base(desc) {

			// initialize the default settings
			windowSize = DefaultWindowSize;
			int nominalRate = DefaultRate;

			// process the attributes
			foreach (KeyValuePair<string, string> attrib in attributes) {
				string attribName = attrib.Key.ToLower();
				if (attribName == "windowsize") {
					if (!int.TryParse(attrib.Value, out windowSize)) {
						windowSize = DefaultWindowSize;
					}
				}
				else if (attribName == "nominalrate") {
					if (!int.TryParse(attrib.Value, out nominalRate)) {
						nominalRate = DefaultRate;
					}
				}
			}

			// bound the window size below 
			if (windowSize < MinWindowSize) {
				windowSize = MinWindowSize;
			}

			// don't let the nominal rate be less that the default space -- allocating a bit more storage isn't too bad
			if (nominalRate < DefaultRate) {
				nominalRate = DefaultRate;
			}

			// construct the sub items
			DataItemDescriptor periodMaxDesc = new DataItemDescriptor(desc.Name + " (max period)", typeof(double), desc.Description, "s", desc.Capacity);
			DataItemDescriptor periodStdDevDesc = new DataItemDescriptor(desc.Name + " (period σ)", typeof(double), desc.Description, "s", desc.Capacity);

			// create the data items
			periodMaxDataItem = new DataItemSource<double>(periodMaxDesc);
			periodStdDevDataItem = new DataItemSource<double>(periodStdDevDesc);

			// create the queue array using the window size and default rate
			runTimeQueue = new RunTimeEntry[windowSize*nominalRate];
		}

		public override DatasetSource Parent {
			get {
				return base.Parent;
			}
			set {
				base.Parent = value;

				lock (lockobj) {
					// if the parent is valid, add the sub-items to it (the parent will assign the sub-items' parent fields)
					if (value != null) {
						value.Add(periodMaxDataItem.Name, periodMaxDataItem);
						value.Add(periodStdDevDataItem.Name, periodStdDevDataItem);
					}
				}
			}
		}

		public override void Add(double value, CarTimestamp t) {
			throw new NotSupportedException();
		}

		public void Mark(CarTimestamp ts) {
			lock (lockobj) {
				// check if we've ever set the last time
				if (!setLastTimestamp) {
					// if not, get the current time and mark that we set it 
					lastTimestamp = HighResDateTime.Now;
					setLastTimestamp = true;
				}
				else {
					// get the current timestamp
					DateTime timestamp = HighResDateTime.Now;
					// calculate dt between this and last invocation
					double period = (timestamp - lastTimestamp).TotalSeconds;
					// update the last timestamp
					lastTimestamp = timestamp;

					// add the entry to the queue
					AddToQueue(timestamp, period);

					// run queue maintenance
					MaintainQueue(timestamp);

					// send the values if we have valid data and we've filled up at least half the window
					if (runTimeQueueCount > 0 && sumPeriod > 0 && timestamp - oldestQueueTime > TimeSpan.FromSeconds(windowSize/2.0)) {
						// calculate values
						double averagePeriod = sumPeriod / runTimeQueueCount;
						double averageRate = 1/averagePeriod;

						double varPeriod = sumPeriodSq / runTimeQueueCount - averagePeriod*averagePeriod;
						double stdDevPeriod = (varPeriod > 0) ? Math.Sqrt(varPeriod) : 0;

						// output values
						// publish the rate here
						base.Add(averageRate, ts);

						// publish the std dev and max period on the appropriate items
						periodStdDevDataItem.Add(stdDevPeriod, ts);
						periodMaxDataItem.Add(maxPeriod, ts);
					}
				}
			}
		}

		private void AddToQueue(DateTime timestamp, double period) {
			// check if we're out of space and resize the queue if necessary
			if (runTimeQueueCount == runTimeQueue.Length) {
				ResizeQueue();
			}

			// add to the tail location
			runTimeQueue[runTimeQueueTail] = new RunTimeEntry(period, timestamp);
			// increment the tail location
			runTimeQueueTail = (runTimeQueueTail+1)%runTimeQueue.Length;
			// increment the size
			runTimeQueueCount++;

			// update the tracker variables
			sumPeriod += period;
			sumPeriodSq += period*period;
			if (period > maxPeriod) {
				maxPeriod = period;
			}

			// check if we just added the first element
			if (runTimeQueueCount == 1) {
				// update the oldest queue time 
				oldestQueueTime = timestamp;
			}
		}

		private void ResizeQueue() {
			int averageRunRate = 0;
			if (sumPeriod > 0) {
				// calculate average mark rate, round up to next integer
				averageRunRate = (int)Math.Ceiling(runTimeQueueCount / sumPeriod);
			}

			// check if the run rate is below the default
			if (averageRunRate < DefaultRate) {
				averageRunRate = DefaultRate;
			}

			// calculate the new target size
			int newQueueSize = averageRunRate*windowSize;
			
			// if we're resizing, we might as well do a big resize
			// we'll enforce the rule that we at least make the queue 1.5x as big
			int minQueueSize = 3*runTimeQueue.Length / 2;
			if (newQueueSize < minQueueSize) {
				newQueueSize = minQueueSize;
			}

			// create the new queue
			RunTimeEntry[] newRunTimeQueue = new RunTimeEntry[newQueueSize];

			// copy the old values into the new queue
			if (runTimeQueueCount > 0) {
				if (runTimeQueueHead < runTimeQueueTail) {
					// we can copy directly, no wrap around at the moment
					Array.Copy(runTimeQueue, runTimeQueueHead, newRunTimeQueue, 0, runTimeQueueCount);
				}
				else {
					// we need to copy in two parts
					// 1) head to end of array
					Array.Copy(runTimeQueue, runTimeQueueHead, newRunTimeQueue, 0, runTimeQueue.Length - runTimeQueueHead);
					// 2) start of array to tail
					Array.Copy(runTimeQueue, 0, newRunTimeQueue, runTimeQueue.Length - runTimeQueueHead, runTimeQueueTail);
				}
			}

			// set the new queue as the current queue
			runTimeQueue = newRunTimeQueue;
			// adjust the head/tail indicies
			runTimeQueueHead = 0;
			runTimeQueueTail = runTimeQueueCount;
		}

		private void MaintainQueue(DateTime currentTime) {
			// cutoff time for removing samples
			DateTime cutoff = currentTime - TimeSpan.FromTicks(TimeSpan.TicksPerSecond*windowSize);

			// flag indicating if we deleted the max period value
			bool maxWasDeleted = false;

			// iterate while there are elements in the queue
			while (runTimeQueueCount > 0 && runTimeQueue[runTimeQueueHead].timestamp < cutoff) {
				double period = runTimeQueue[runTimeQueueHead].period;
				
				// we want to delete this entry
				// subtract off from sumPeriod, sumPeriod2
				sumPeriod -= period;
				sumPeriodSq -= period*period;

				// increment the number we've deleted
				numDeleted++;

				// check if we're deleting the max period
				if (period >= maxPeriod) {
					// flag that we did delete the max period value
					maxWasDeleted = true;
				}

				// move the head index forward
				runTimeQueueHead = (runTimeQueueHead+1)%runTimeQueue.Length;
				// decrement the size
				runTimeQueueCount--;
			}

			// check if we need to do a full recalculation
			if (maxWasDeleted || numDeleted >= DeleteRecalcThreshold || runTimeQueueCount == 0) {
				// reset the tracker variables
				sumPeriod = 0;
				sumPeriodSq = 0;
				maxPeriod = 0;

				// iterate through the entries
				for (int i = 0; i < runTimeQueueCount; i++) {
					// get the entry at index i
					double period = runTimeQueue[(runTimeQueueHead+i)%runTimeQueue.Length].period;
					// update the tracker variables
					sumPeriod += period;
					sumPeriodSq += period*period;
					if (period > maxPeriod) {
						maxPeriod = period;
					}
				}

				// reset the deleted count
				numDeleted = 0;
			}
			else {
				// make sure we don't have weird accumulated error problems due to floating point rounding
				if (sumPeriod < 0) {
					sumPeriod = 0;
				}

				if (sumPeriodSq < 0) {
					sumPeriodSq = 0;
				}
			}

			// update the oldest time in the queue
			// note that this timestamp may not be valid if runTimeQueueCount == 0, but in that case
			// oldestQueueTime isn't used
			oldestQueueTime = runTimeQueue[runTimeQueueHead].timestamp;
		}
	}
}
