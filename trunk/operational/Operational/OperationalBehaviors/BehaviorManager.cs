using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using OperationalLayer.Tracking;
using UrbanChallenge.OperationalUIService.Behaviors;
using UrbanChallenge.Common.Utility;
using System.Threading;
using System.Diagnostics;
using OperationalLayer.Tracing;
using OperationalLayer.CarTime;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors.CompletionReport;

namespace OperationalLayer.OperationalBehaviors {
	class BehaviorManager {
		private const string behavior_watchdog = "op_behavior_watchdog";
		private volatile IOperationalBehavior currentBehavior;
		private volatile IOperationalBehavior queuedBehavior;
		private volatile Behavior queuedOrigBehavior;
		private volatile object queuedParam;

		private volatile Type currentBehaviorType;

		private object queueLock = new object();

		private delegate IOperationalBehavior BehaviorBuidler();
		private Dictionary<Type, BehaviorBuidler> behaviorMap;

		private Thread behaviorThread;

		public static TraceSource TraceSource = new TraceSource("behaviors");

		private TimeWindowQueue<double> cycleTimeQueue = new TimeWindowQueue<double>(3);

		private EventWaitHandle watchdogEvent;

		private bool testMode;
		private volatile CompletionReport cachedCompletionReport;
		private SAUDILevel saudiLevel;

		public BehaviorManager(bool testMode) {
			if (Services.TrackingManager == null) {
				throw new InvalidOperationException("TrackingManager must be initialized before building the BehaviorManager");
			}

			this.testMode = testMode;

			try {
				watchdogEvent = new EventWaitHandle(true, EventResetMode.AutoReset, behavior_watchdog);
			}
			catch (Exception ex) {
				Console.WriteLine("could not create watchdog event: {0}", ex);
			}

			// construct the behavior mapping
			BuildBehaviorMap();

			if (!testMode) {
				// subscribe the tracking manager notification
				Services.TrackingManager.TrackingCompleted += TrackingManager_TrackingCompleted;
				if (Services.CommandTransport != null) {
					//Services.CommandTransport.CarModeChanged += CommandTransport_CarModeChanged;
				}

				AsyncFlowControl? flowControl = null;
				if (!ExecutionContext.IsFlowSuppressed()) {
					flowControl = ExecutionContext.SuppressFlow();
				}

				// create the behavior thread
				behaviorThread = new Thread(BehaviorProc);
				// set it as a background thread
				behaviorThread.IsBackground = true;
				// set the priority to below normal to prevent other stuff from getting messdd mwith
				behaviorThread.Priority = ThreadPriority.BelowNormal;
				// give it a useful name
				behaviorThread.Name = "Behavior Thread";
				// start it running
				behaviorThread.Start();

				if (flowControl.HasValue) {
					flowControl.Value.Undo();
				}
			}
		}

		void CommandTransport_CarModeChanged(object sender, CarModeChangedEventArgs e) {
			OnCarModeChanged(e.Mode);
		}

		void TrackingManager_TrackingCompleted(object sender, TrackingCompletedEventArgs e) {
			if (currentBehavior != null) {
				currentBehavior.OnTrackingCompleted(e);
			}
		}

		public Type CurrentBehaviorType {
			get { return currentBehaviorType; }
		}

		public void OnCarModeChanged(CarMode newMode) {
			if (!testMode) {
				try {
					if (newMode == CarMode.Human) {
						// clear out the current behavior
						Execute(new HoldBrake(), null, false);
					}
				}
				catch (Exception ex) {
					TraceSource.TraceEvent(TraceEventType.Error, 1, "exception when handling new car mode: {0}", ex);
				}
			}
		}

		public void OnBehaviorReceived(Behavior b) {
			Trace.CorrelationManager.StartLogicalOperation("OnBehaviorReceived");

			Services.Dataset.MarkOperation("behavior rate", LocalCarTimeProvider.LocalNow);

			try {
				// write the trace output
				if (TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) {
					// write a verbose output
					TraceSource.TraceEvent(TraceEventType.Verbose, 1, "recevied behavior: {0}", b);
				}
				else {
					TraceSource.TraceEvent(TraceEventType.Information, 1, "received behavior: {0}", b.GetType());
				}

				if (b != null) {
					HandleDecorators(b.Decorators);
				}

				if (b is NullBehavior)
					return;

				if (currentBehavior != null) {
					try {
						currentBehavior.OnBehaviorReceived(b);
					}
					catch (Exception ex) {
						TraceSource.TraceEvent(TraceEventType.Warning, 2, "current behavior ({0}) threw exception in OnBehaviorReceived: {1}", currentBehavior, ex);
						throw;
					}
				}
				else {
					Execute(b, null, false);
				}
			}
			finally {
				Trace.CorrelationManager.StopLogicalOperation();
			}
		}

		public void Execute(Behavior b, object param, bool immediate) {
			lock (queueLock) {
				Execute(MapBehavior(b), param, immediate);
				queuedOrigBehavior = b;
			}
		}

		public void Execute(IOperationalBehavior b, object param, bool immediate) {
			lock (queueLock) {
				// if we want to immediately execute the next behavior, then we need to cancel the current one
				if (immediate && currentBehavior != null) {
					TraceSource.TraceEvent(TraceEventType.Verbose, 9, "in Execute, perfoming cancel on current behavior");
					try {
						currentBehavior.Cancel();
					}
					catch (Exception ex) {
						TraceSource.TraceEvent(TraceEventType.Error, 3, "cancelling the current behavior threw an exception: {0}", ex);
						throw;
					}
				}

				// queue up the new behavior
				queuedBehavior = b;
				queuedParam = param;
				queuedOrigBehavior = null;
			}
		}

		public void QueueParam(object param) {
			TraceSource.TraceEvent(TraceEventType.Verbose, 10, "queuing parameter ({0}) for current behavior ({1})", param, currentBehavior);
			lock (queueLock) {
				if (queuedBehavior == null) {
					queuedParam = param;
					TraceSource.TraceEvent(TraceEventType.Verbose, 10, "queued parameter");
				}
				else {
					//TraceSource.TraceEvent(TraceEventType.Warning, 10, "request to queue parameter when queued behavior is not null ({0})", queuedBehavior);
				}
			}
		}

		private void BuildBehaviorMap() {
			behaviorMap = new Dictionary<Type, BehaviorBuidler>();

			behaviorMap.Add(typeof(HoldBrakeBehavior), delegate() { return new HoldBrake(); });
			behaviorMap.Add(typeof(SimpleStayInLaneBehavior), delegate() { return new SimpleStayInLane(); });
			behaviorMap.Add(typeof(StayInLaneBehavior), delegate() { return new StayInLane(); });
			behaviorMap.Add(typeof(ChangeLaneBehavior), delegate() { return new ChangeLanes(); });
			behaviorMap.Add(typeof(UTurnBehavior), delegate() { return new UTurn(); });
			behaviorMap.Add(typeof(TurnBehavior), delegate() { return new Turn(); });
			behaviorMap.Add(typeof(SupraLaneBehavior), delegate() { return new SupraStayInLane(); });
			behaviorMap.Add(typeof(ZoneParkingPullOutBehavior), delegate() { return new ZoneParkingBase(); });
			behaviorMap.Add(typeof(ZoneParkingBehavior), delegate() { return new ZoneParkingBase(); });
			behaviorMap.Add(typeof(ZoneTravelingBehavior), delegate() { return new ZoneTravel(); });
		}

		private IOperationalBehavior MapBehavior(Behavior b) {
			BehaviorBuidler builder;
			if (behaviorMap.TryGetValue(b.GetType(), out builder)) {
				return builder();
			}
			else {
				TraceSource.TraceEvent(TraceEventType.Warning, 2, "behavior type {0} does not have a mapping", b.GetType());
				throw new ArgumentException("Behavior type " + b.GetType().Name + " does not have a mapping");
			}
		}

		private void BehaviorProc() {
			Trace.CorrelationManager.StartLogicalOperation("Behavior Loop");
			// set the default trace source for this thread
			OperationalTrace.ThreadTraceSource = TraceSource;

			OperationalDataSource previousTimeout = OperationalDataSource.None;

			using (MMWaitableTimer timer = new MMWaitableTimer((uint)(Settings.BehaviorPeriod*1000))) {
				while (true) {
					if (Services.DebuggingService.StepMode) {
						Services.DebuggingService.WaitOnSequencer(typeof(BehaviorManager));
					}
					else {
						// wait for the timer
						timer.WaitEvent.WaitOne();
					}

					if (watchdogEvent != null) {
						try {
							watchdogEvent.Set();
						}
						catch (Exception) {
						}
					}

					Services.Dataset.MarkOperation("planning rate", LocalCarTimeProvider.LocalNow);

					TraceSource.TraceEvent(TraceEventType.Verbose, 0, "starting behavior loop");

					object param = null;
					IOperationalBehavior newBehavior = null;
					Behavior behaviorParam = null;
					OperationalDataSource timeoutSource = OperationalDataSource.Pose;

					bool forceHoldBrake = false;
					try {
						if (OperationalBuilder.BuildMode != BuildMode.Listen && TimeoutMonitor.HandleRecoveryState()) {
							forceHoldBrake = true;
						}
					}
					catch (Exception ex) {
						OperationalTrace.WriteError("error in recovery logic: {0}", ex);
					}

					if (OperationalBuilder.BuildMode == BuildMode.Realtime && TimeoutMonitor.AnyTimedOut(ref timeoutSource)) {
						if (timeoutSource != previousTimeout) {
							OperationalTrace.WriteError("data source {0} has timed out", timeoutSource);
							previousTimeout = timeoutSource;
						}
						// simply queue up a hold brake 
						Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetHoldBrakeCommand(45));
					}
					else {
						if (previousTimeout != OperationalDataSource.None) {
							OperationalTrace.WriteWarning("data source {0} is back online", previousTimeout);
							previousTimeout = OperationalDataSource.None;
						}

						if (forceHoldBrake || (Services.Operational != null && Services.Operational.GetCarMode() == CarMode.Human)) {
							queuedBehavior = null;
							queuedParam = null;
							queuedOrigBehavior = null;
							if (!(currentBehavior is HoldBrake)) {
								newBehavior = new HoldBrake();
								param = null;
								queuedOrigBehavior = new HoldBrakeBehavior();
							}
						}
						else {
							lock (queueLock) {
								// get the current queue param
								param = queuedParam;

								//TraceSource.TraceEvent(TraceEventType.Verbose, 0, "queued param: {0}", queuedParam == null ? "<null>" : queuedParam.ToString());

								// chekc if there is a queued behavior
								newBehavior = queuedBehavior;
								queuedBehavior = null;

								behaviorParam = queuedOrigBehavior;
								queuedOrigBehavior = null;
							}
						}

						if (newBehavior != null) {
							// dispose of the old behavior
							if (currentBehavior != null && currentBehavior is IDisposable) {
								((IDisposable)currentBehavior).Dispose();
							}

							Trace.CorrelationManager.StartLogicalOperation("initialize");
							TraceSource.TraceEvent(TraceEventType.Verbose, 8, "executing initialize on {0}", newBehavior.GetType().Name);
							// swap in the new behavior and initialize
							currentBehavior = newBehavior;
							try {
								currentBehavior.Initialize(behaviorParam);

								TraceSource.TraceEvent(TraceEventType.Verbose, 8, "initialize completed on {0}", currentBehavior.GetType().Name);
							}
							catch (Exception ex) {
								TraceSource.TraceEvent(TraceEventType.Warning, 4, "exception thrown when initializing behavior: {0}", ex);
								//throw;
							}
							finally {
								Trace.CorrelationManager.StopLogicalOperation();
							}

							if (behaviorParam != null) {
								this.currentBehaviorType = behaviorParam.GetType();
							}
							else {
								this.currentBehaviorType = null;
							}
						}
						else {
							//TraceSource.TraceEvent(TraceEventType.Verbose, 11, "queued behavior was null");
						}

						// process the current behavior
						Trace.CorrelationManager.StartLogicalOperation("process");
						try {
							if (currentBehavior != null) {
								Services.Dataset.ItemAs<string>("behavior string").Add(currentBehavior.GetName(), LocalCarTimeProvider.LocalNow);

								DateTime start = HighResDateTime.Now;
								currentBehavior.Process(param);
								TimeSpan diff = HighResDateTime.Now-start;
								lock (cycleTimeQueue) {
									cycleTimeQueue.Add(diff.TotalSeconds, LocalCarTimeProvider.LocalNow.ts);
								}
							}
						}
						catch (Exception ex) {
							// just ignore any exceptions for now
							// should log this somehow
							TraceSource.TraceEvent(TraceEventType.Warning, 5, "exception thrown when processing behavior: {0}", ex);
						}
						finally {
							Trace.CorrelationManager.StopLogicalOperation();
						}
					}

					TraceSource.TraceEvent(TraceEventType.Verbose, 0, "ending behavior loop");

					if (Services.DebuggingService.StepMode) {
						Services.DebuggingService.SetCompleted(typeof(BehaviorManager));
					}
				}
			}
		}

		private void HandleDecorators(List<BehaviorDecorator> decorators) {
			if (decorators == null)
				return;

			saudiLevel = SAUDILevel.None;
			foreach (BehaviorDecorator dec in decorators) {
				if (dec is TurnSignalDecorator) {
					HandleTurnSignal((TurnSignalDecorator)dec);
				}
				else if (dec is ShutUpAndDoItDecorator) {
					saudiLevel = ((ShutUpAndDoItDecorator)dec).Level;
				}
				else {
					//TraceSource.TraceEvent(TraceEventType.Warning, 7, "unknown decorator: {0}", dec);
				}
			}
		}

		private void HandleTurnSignal(TurnSignalDecorator decorator) {
			TraceSource.TraceEvent(TraceEventType.Verbose, 6, "turn signal on behavior: {0}", decorator.Signal);
			Services.TrackingManager.SetTurnSignal(decorator.Signal);
		}

		public SAUDILevel SAUDILevel {
			get { return saudiLevel; }
		}

		public bool TestMode {
			get { return testMode; }
		}

		public CompletionReport TestBehavior(Behavior b) {
			IOperationalBehavior operBehavior = null;
			try {
				testMode = true;
				Console.WriteLine("Testing behavior " + b.ToString() + " -- type " + b.GetType().Name);

				cachedCompletionReport = new SuccessCompletionReport(b.GetType());
				HandleDecorators(b.Decorators);
				operBehavior = MapBehavior(b);
				operBehavior.Initialize(b);
				operBehavior.Process(null);

				Console.WriteLine("Result: " + cachedCompletionReport.ToString());

				cachedCompletionReport.BehaviorId = b.UniqueId();

				return cachedCompletionReport;
			}
			catch (Exception ex) {
				TraceSource.TraceEvent(TraceEventType.Error, 0, "error testing behavior {0}: {1}", b.GetType(), ex);
				throw;
			}
			finally {
				if (operBehavior != null && operBehavior is IDisposable) {
					((IDisposable)operBehavior).Dispose();
				}
			}
		}

		public void ForwardCompletionReport(CompletionReport report) {
			if (testMode) {
				cachedCompletionReport = report;
				Console.WriteLine("forward comp report: " + report.ToString());
			}
			else {
				if (report is TrajectoryBlockedReport) {
					((TrajectoryBlockedReport)report).SAUDILevel = saudiLevel;
				}
				if (Services.Operational != null) {
					Services.Operational.SendCompletionReport(report);
				}
			}
		}

		public double AverageCycleTime {
			get {
				lock (cycleTimeQueue) {
					if (cycleTimeQueue.Count == 0) return 0;

					double sum = 0;
					for (int i = 0; i < cycleTimeQueue.Count; i++) {
						sum += cycleTimeQueue[i].Value;
					}

					return sum/cycleTimeQueue.Count;
				}
			}
		}

		public void LockBehaviorLoop() {
			Monitor.Enter(queueLock);
		}

		public void UnlockBehaviorLoop() {
			Monitor.Exit(queueLock);
		}
	}
}
