using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking;
using UrbanChallenge.Behaviors;

namespace OperationalLayer.OperationalBehaviors {
	interface IOperationalBehavior {
		/// <summary>
		/// Called when a behavior is received from the operational layer
		/// </summary>
		/// <param name="b"></param>
		void OnBehaviorReceived(Behavior b);

		/// <summary>
		/// Called when the tracking manager reports that it has completed its tracking command
		/// </summary>
		/// <param name="e"></param>
		void OnTrackingCompleted(TrackingCompletedEventArgs e);

		/// <summary>
		/// Called when the behavior first executes before the first process call
		/// </summary>
		void Initialize(Behavior b);

		/// <summary>
		/// Performs a planning cycle
		/// </summary>
		void Process(object param);

		/// <summary>
		/// Immediately terminates the processing of the behavior
		/// </summary>
		void Cancel();

		string GetName();
	}
}
