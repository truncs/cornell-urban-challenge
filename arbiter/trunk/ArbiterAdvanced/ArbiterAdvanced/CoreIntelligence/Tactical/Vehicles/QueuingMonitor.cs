using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles
{
	/// <summary>
	/// Updates the queuing monitor
	/// </summary>
	public enum QueueingUpdate
	{
		NotQueueing,
		Queueing
	}

	/// <summary>
	/// State of vehicle in queue
	/// </summary>
	public enum QueuingState
	{
		Normal,
		Failed
	}

	/// <summary>
	/// Monitors the queuing state of a vehicle
	/// </summary>
	public class QueuingMonitor
	{
		/// <summary>
		/// Current queuing state
		/// </summary>
		public QueuingState Queuing = QueuingState.Normal;

		/// <summary>
		/// Time when first observed the stopped vehicle
		/// </summary>
		public double StoppedTimestamp = 0.0;

		/// <summary>
		/// Time we have been monitoring the stopped vehicle
		/// </summary>
		public double TimeStopped = 0.0;

		/// <summary>
		/// If the queue timer has started
		/// </summary>
		public bool StartedTimer = false;

		/// <summary>
		/// Time we let them stay Queuing until we say they are failed
		/// </summary>
		public double QueuingTime = 7.0;

		/// <summary>
		/// Stopwatch for waiting for hte vehicle
		/// </summary>
		public Stopwatch WaitTimer;

		/// <summary>
		/// Stopwatch to monitor how long we have seen the vehicle not queueing
		/// </summary>
		public Stopwatch NotQueuingTimer;

		/// <summary>
		/// Default queuing time to wait for a forward vehicle
		/// </summary>
		public double DefaultQueueingTime = 8.0;

		/// <summary>
		/// Maximum we will wait for a vehicle
		/// </summary>
		public double MaximumQueuingTime = 30.0;

		/// <summary>
		/// Constructor
		/// </summary>
		public QueuingMonitor()
		{
			this.NotQueuingTimer = new Stopwatch();
			this.WaitTimer = new Stopwatch();
			this.Reset();
		}

		/// <summary>
		/// Updates the monitor with a new state of the vehicle
		/// </summary>
		/// <param name="StateMonitor"></param>
		public void Update(QueueingUpdate update, double ts)
		{
			if (update == QueueingUpdate.NotQueueing)
			{
				if (this.Queuing == QueuingState.Failed && this.NotQueuingTimer.ElapsedMilliseconds / 1000.0 > 7.0)
					this.QueuingTime = Math.Min(this.QueuingTime + 10.0, this.MaximumQueuingTime);
				else if(this.TimeStopped > (3.0/4.0) * this.QueuingTime)
					this.QueuingTime = Math.Min(this.QueuingTime + 5.0, this.MaximumQueuingTime);

				if (!this.NotQueuingTimer.IsRunning)
					this.NotQueuingTimer.Start();
				else if (this.NotQueuingTimer.ElapsedMilliseconds / 1000.0 > QueuingTime)
					this.QueuingTime = Math.Max(this.QueuingTime, Math.Min(this.NotQueuingTimer.ElapsedMilliseconds / 1000.0, this.MaximumQueuingTime / 2.0));

				this.Reset();
			}
			else
			{
				if (this.NotQueuingTimer.IsRunning)
				this.NotQueuingTimer.Stop();

				if (!this.StartedTimer)
				{
					this.StartedTimer = true;
					this.StoppedTimestamp = ts;
				}

				if (Queuing == QueuingState.Normal)
				{
					TimeStopped = ts - StoppedTimestamp;

					if (TimeStopped > QueuingTime)
						Queuing = QueuingState.Failed;
				}
				else
				{
					TimeStopped = ts - StoppedTimestamp;
				}
			}
		}

		/// <summary>
		/// Reset the monitor
		/// </summary>
		public void Reset()
		{
			this.WaitTimer.Stop();
			this.WaitTimer.Reset();
			this.Queuing = QueuingState.Normal;
			this.StoppedTimestamp = 0.0;
			this.TimeStopped = 0.0;
			this.StartedTimer = false;
			this.QueuingTime = this.DefaultQueueingTime;
		}
	}
}
