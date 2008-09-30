using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Common;
using System.Diagnostics;
using OperationalLayer.Pose;
using System.Threading;
using WatchdogAPI;
using OperationalLayer.Communications;
using UrbanChallenge.Arbiter.Core.Remote;
using UrbanChallenge.Behaviors.CompletionReport;
using OperationalLayer.Tracing;

namespace OperationalLayer {
	enum OperationalDataSource {
		Pose,
		PosteriorPose,
		UntrackedClusters,
		TrackedClusters,
		None
	}

	static class TimeoutMonitor {
		class TimeoutData {
			public TimeSpan timeoutWindow;
			public DateTime lastTime;

			public TimeoutData(TimeSpan window) {
				this.timeoutWindow = window;
				this.lastTime = DateTime.MinValue;
			}

			public bool HasTimedOut(DateTime now) {
				if (lastTime == DateTime.MinValue) {
					return false;
				}
				else {
					return (now - lastTime) > timeoutWindow;
				}
			}
		}

		private static SortedList<OperationalDataSource, TimeoutData> timeouts;
		private static bool forceTimeout = false;

		public static void Initialize() {
			timeouts = new SortedList<OperationalDataSource, TimeoutData>();
			timeouts.Add(OperationalDataSource.Pose, new TimeoutData(TimeSpan.FromMilliseconds(500)));
			timeouts.Add(OperationalDataSource.TrackedClusters, new TimeoutData(TimeSpan.FromSeconds(1)));
			timeouts.Add(OperationalDataSource.UntrackedClusters, new TimeoutData(TimeSpan.FromSeconds(1)));
			timeouts.Add(OperationalDataSource.PosteriorPose, new TimeoutData(TimeSpan.FromSeconds(1)));

			if (!Settings.TestMode) {
				watchdogResetEvent = new AutoResetEvent(false);
				watchdogResetThread = new Thread(ResetThread);
				watchdogResetThread.IsBackground = true;
				watchdogResetThread.Name = "Reset thread";
				watchdogResetThread.Start();
			}
		}

		public static void MarkData(OperationalDataSource dataSource) {
			timeouts[dataSource].lastTime = HighResDateTime.Now;
		}

		public static bool AnyTimedOut(ref OperationalDataSource timeout) {
			if (forceTimeout) {
				timeout = OperationalDataSource.TrackedClusters;
				return true;
			}

			DateTime now = HighResDateTime.Now;
			foreach (KeyValuePair<OperationalDataSource, TimeoutData> kvp in timeouts) {
				if (kvp.Value.HasTimedOut(now)) {
					timeout = kvp.Key;
					return true;
				}
			}

			return false;
		}

		private const double distanceTolerance = 3;

		// initialize to a very large position so we reset immediately
		private static Coordinates lastPosition = new Coordinates(1e10, 1e10);
		private static Stopwatch stopTimer = new Stopwatch();

		private static Stopwatch stage1Timer;

		private static Stopwatch stage2Timer;

		private static AutoResetEvent watchdogResetEvent;
		private static Thread watchdogResetThread;
		private static bool watchdogResetComplete;

		private static int resetStage = 0;

		public static bool HandleRecoveryState() {
			if (Settings.TestMode)
				return false;

			UpdatePosition();

			int desiredResetStage = GetResetStage();

			bool initialize = false;
			if (desiredResetStage != resetStage) {
				initialize = true;
				resetStage = desiredResetStage;

				OperationalTrace.WriteWarning("executing recovery stage {0}", resetStage);
			}

			bool cancelCurrentBehavior;

			switch (resetStage) {
				case 0:
				default:
					cancelCurrentBehavior = false;
					break;

				case 1:
					cancelCurrentBehavior = false;
					HandleResetStage1(initialize);
					break;

				case 2:
					cancelCurrentBehavior = false;
					HandleResetStage2(initialize);
					break;
			}

			return cancelCurrentBehavior;
		}

		private static void UpdatePosition() {
			if (Services.StateProvider != null) {
				AbsolutePose pose = Services.StateProvider.GetAbsolutePose();

				if ((Services.Operational != null && Services.Operational.GetRealCarMode() != CarMode.Run) || lastPosition.DistanceTo(pose.xy) > distanceTolerance) {
					// reset the timer and position
					lastPosition = pose.xy;
					stopTimer.Reset();
					stopTimer.Start();

					// reset the forced car mode
					if (Services.Operational != null) {
						Services.Operational.ForceCarMode(CarMode.Unknown);
					}
				}
			}
		}

		private static int GetResetStage() {
			TimeSpan elapsed = stopTimer.Elapsed;
			if (elapsed >= TimeSpan.FromMinutes(4)) {
				return 2;
			}
			else if (elapsed >= TimeSpan.FromMinutes(3)) {
				return 1;
			}
			else {
				return 0;
			}
		}

		private static ArbiterAdvancedRemote GetRemote() {
			try {
				return (ArbiterAdvancedRemote)CommBuilder.GetObject("ArbiterAdvancedRemote");
			}
			catch (Exception ex) {
				OperationalTrace.WriteWarning("error getting arbiter remote: {0}", ex);
				return null;
			}
		}

		private static void HandleResetStage1(bool initialize) {
			if (initialize) {
				stage1Timer = Stopwatch.StartNew();

				OperationalTrace.WriteWarning("forcing car mode to human");
				
				// fake transition to human mode
				if (Services.Operational != null) {
					Services.Operational.ForceCarMode(CarMode.Human);
				}
			}
			else if (stage1Timer.Elapsed > TimeSpan.FromSeconds(2)) {
				stage1Timer.Reset();

				OperationalTrace.WriteWarning("clearing forced car mode");

				// switch to an unknown car mode
				if (Services.Operational != null) {
					Services.Operational.ForceCarMode(CarMode.Unknown);
				}
			}
		}

		private static void HandleResetStage2(bool initialize) {
			// if we're initializing, send the stop to both the scene estimator and local map
			// wait until we have confirmation that they're stopped, then restart them
			// wait until we get data
			// restart AI

			if (initialize) {
				forceTimeout = true;
				watchdogResetComplete = false;
				stage2Timer = null;
				watchdogResetEvent.Set();
				OperationalTrace.WriteWarning("signaling restart to sensor fusion");
			}
			else if (watchdogResetComplete) {
				stage2Timer = Stopwatch.StartNew();
				watchdogResetComplete = false;
				OperationalTrace.WriteWarning("sensor fusion restart complete");
			}
			else if (stage2Timer != null && stage2Timer.Elapsed > TimeSpan.FromSeconds(10)) {
				// possibly could wait until we have data to do this, but whatever
				OperationalTrace.WriteWarning("resetting arbiter");

				// time to reset the AI
				stage2Timer.Reset();

				try {
					ArbiterAdvancedRemote remote = GetRemote();
					if (remote != null) {
						remote.Reset();
						OperationalTrace.WriteWarning("arbiter reset complete");
					}
				}
				catch (Exception ex) {
					OperationalTrace.WriteError("Error resetting AI in stage 2: {0}", ex);
				}
			}
		}

		private static void ResetThread() {
			if (OperationalBuilder.BuildMode == BuildMode.Realtime) {
				WatchdogCommAPI.Initialize();
				OperationalTrace.WriteWarning("initialized watchdog api");

				while (true) {
					watchdogResetEvent.WaitOne();

					OperationalTrace.WriteWarning("restarting local map");
					while (!WatchdogCommAPI.RestartLocalMap()) {
					}

					OperationalTrace.WriteWarning("restarting scene estimator");
					while (!WatchdogCommAPI.RestartSkeetEstimator()) {
					}

					watchdogResetComplete = true;
					forceTimeout = false;
				}
			}
			else {
				while (true) {
					watchdogResetEvent.WaitOne();

					Thread.Sleep(100);

					watchdogResetComplete = true;
				}
			}
		}
	}
}
