using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Common;
using System.Diagnostics;
using OperationalLayer.Tracing;
using OperationalLayer.CarTime;
using OperationalLayer.Pose;
using UrbanChallenge.Common.Vehicle;
using OperationalLayer.Tracking.Steering;

namespace OperationalLayer.Tracking {
	class TrackingManager {
		public event EventHandler<TrackingCompletedEventArgs> TrackingCompleted {
			add {
				lock (this) { trackingCompleted = (EventHandler<TrackingCompletedEventArgs>)Delegate.Combine(trackingCompleted, value); }
			}
			remove {
				lock (this) { trackingCompleted = (EventHandler<TrackingCompletedEventArgs>)Delegate.Remove(trackingCompleted, value); }
			}
		}

		private EventHandler<TrackingCompletedEventArgs> trackingCompleted;

		private CompletionResult currentResult;
		private ITrackingCommand currentCommand;

		private ITrackingCommand queuedCommand;

		private Thread trackingThread;

		private ICommandTransport commandTransport;

		private TimeWindowQueue<double> deltaSteeringQueue;

		private bool testMode;

		public static TraceSource TraceSource = new TraceSource("tracking");

		public TrackingManager(ICommandTransport commandTransport, bool testMode) {
			// check that the command transport is legit
			if (commandTransport == null) {
				throw new ArgumentNullException("commandTransport");
			}

			this.testMode = testMode;

			// create the command queue
			deltaSteeringQueue = new TimeWindowQueue<double>(3);

			// store the command transport
			this.commandTransport = commandTransport;

			// set up a null tracking command initially
			currentCommand = new NullTrackingCommand();
			// mark the current completion result as "working"
			currentResult = CompletionResult.Working;

			if (!testMode) {
				AsyncFlowControl? flowControl = null;
				if (!ExecutionContext.IsFlowSuppressed()) {
					flowControl = ExecutionContext.SuppressFlow();
				}

				// set up the thread
				trackingThread = new Thread(TrackingProc);
				// set it as a background thread
				trackingThread.IsBackground = true;
				// set it as higher priority
				trackingThread.Priority = ThreadPriority.AboveNormal;
				// give it a useful name
				trackingThread.Name = "Tracking Thread";
				// start her up
				trackingThread.Start();

				if (flowControl != null) {
					flowControl.Value.Undo();
				}
			}
		}

		/// <summary>
		/// Predicts forward by the delay time of the tahoe
		/// </summary>
		/// <param name="absPose"></param>
		/// <param name="vehicleState"></param>
		public void ForwardPredict(double planningTime, out AbsolutePose absPose, out OperationalVehicleState vehicleState) {
			// get the current state 
			OperationalVehicleState currentState = Services.StateProvider.GetVehicleState();

			// assume the vehicle will hold it current curvature
			double curvature = TahoeParams.CalculateCurvature(currentState.steeringAngle, currentState.speed);
			
			// simple dynamics
			// xdot = v*cos(theta)
			// ydot = v*sin(theta)
			// thetadot = v*c

			// do euler integration
			double dt = 0.01;

			double T = TahoeParams.actuation_delay + planningTime;
			int n = (int)Math.Round(T/dt);

			double v = currentState.speed;
			double x = 0, y = 0, heading = 0;
			for (int i = 0; i < n; i++) {
				x += v*Math.Cos(heading)*dt;
				y += v*Math.Sin(heading)*dt;
				heading += v*curvature*dt;
			}

			absPose = new AbsolutePose(new Coordinates(x, y), heading, 0);
			vehicleState = currentState;
		}

		public double DeltaSteeringStdDev {
			get {
				lock (deltaSteeringQueue) {
					// update the queue with the current time
					deltaSteeringQueue.Maintain(LocalCarTimeProvider.LocalNow.ts);

					if (deltaSteeringQueue.Count == 0)
						return 0;

					double sum = 0;
					double sum2 = 0;

					for (int i = 0; i < deltaSteeringQueue.Count; i++) {
						double val = deltaSteeringQueue[i].Value;
						sum += val;
						sum2 += val*val;
					}

					double mean = sum/deltaSteeringQueue.Count;
					double var = sum2/deltaSteeringQueue.Count - mean*mean;
					double std_dev = (var > 0) ? Math.Sqrt(var) : 0;

					return std_dev;
				}
			}
		}

		public void QueueCommand(ITrackingCommand command) {
			if (!testMode) {
				// set the queue command
				queuedCommand = command;
			}
		}

		public void SetTurnSignal(UrbanChallenge.Behaviors.TurnSignal signal) {
			// do this "out of band" because it doesn't really fit with anything else
			commandTransport.SetTurnSignal(signal);
		}

		public CompletionResult CurentCompletionResult {
			get { return currentResult; }
		}

		public ITrackingCommand CurrentCommand {
			get { return currentCommand; }
		}

		private void TrackingProc() {
			Trace.CorrelationManager.StartLogicalOperation("tracking loop");
			OperationalTrace.ThreadTraceSource = TraceSource;

			double? lastSteeringCommand = null;

			using (MMWaitableTimer timer = new MMWaitableTimer((uint)(Settings.TrackingPeriod*1000))) {
				// loop infinitely
				while (true) {
					if (Services.DebuggingService.StepMode) {
						Services.DebuggingService.WaitOnSequencer(typeof(TrackingManager));
					}
					else {
						// wait for the timer
						timer.WaitEvent.WaitOne();
					}

					Services.Dataset.MarkOperation("tracking rate", LocalCarTimeProvider.LocalNow);

					// check if there's a new command to switch in
					ITrackingCommand newCommand = Interlocked.Exchange(ref queuedCommand, null);
					if (newCommand != null) {
						TraceSource.TraceEvent(TraceEventType.Verbose, 0, "received new command: {0}", newCommand);
						// set the current result to working
						currentResult = CompletionResult.Working;
						// set the current command to the new command
						currentCommand = newCommand;
					}

					// check if we've completed or had a failure
					if (currentResult == CompletionResult.Working) {
						TrackingData td = new TrackingData(null, null, null, null, CompletionResult.Failed);
						try {
							// get the tracking data
							Trace.CorrelationManager.StartLogicalOperation("process");
							td = currentCommand.Process();
						}
						catch (Exception ex) {
							// log this somehow
							// if we get here, the completion result retains the failed label, so we won't output anything
							TraceSource.TraceEvent(TraceEventType.Error, 1, "exception tracking command: {0}", ex);
						}
						finally {
							Trace.CorrelationManager.StopLogicalOperation();
						}

						// check if there is a failure
						if (td.result == CompletionResult.Failed) {
							td = new TrackingData(null, null, null, null, CompletionResult.Failed);
						}

						// output the command
						try {
							Trace.CorrelationManager.StartLogicalOperation("command output");
							commandTransport.SetCommand(td.engineTorque, td.brakePressure, td.steeringAngle, td.gear);
						}
						catch (Exception ex) {
							TraceSource.TraceEvent(TraceEventType.Error, 0, "error setting command on transport: {0}", ex);
						}
						finally {
							Trace.CorrelationManager.StopLogicalOperation();
						}

						// queue the command
						lock (deltaSteeringQueue) {
							if (lastSteeringCommand != null && td.steeringAngle != null) {
								double delta = td.steeringAngle.Value - lastSteeringCommand.Value;
								deltaSteeringQueue.Add(delta, LocalCarTimeProvider.LocalNow.ts);
							}

							Services.Dataset.ItemAs<double>("delta steering sigma").Add(DeltaSteeringStdDev, LocalCarTimeProvider.LocalNow);

							lastSteeringCommand = td.steeringAngle;
						}

						if (td.brakePressure.HasValue && td.brakePressure.Value > 90) {
							td.brakePressure = 90;
						}

						// output the tracking commands to the UI
						CarTimestamp now = Services.RelativePose.CurrentTimestamp;
						if (td.steeringAngle.HasValue) {
							Services.Dataset.ItemAs<double>("commanded steering").Add(td.steeringAngle.Value, now);
						}
						if (td.engineTorque.HasValue) {
							Services.Dataset.ItemAs<double>("commanded engine torque").Add(td.engineTorque.Value, now);
						}
						if (td.brakePressure.HasValue) {
							Services.Dataset.ItemAs<double>("commanded brake pressure").Add(td.brakePressure.Value, now);
						}

						if (td.result != CompletionResult.Working && trackingCompleted != null) {
							Trace.CorrelationManager.StartLogicalOperation("tracking completed");
							TraceSource.TraceEvent(TraceEventType.Information, 0, "tracking completed, invoking event, result {0}", td.result);
							// raise the completed event asynchronously so we can go on processing
							try {
								trackingCompleted.BeginInvoke(this, new TrackingCompletedEventArgs(td.result, null, currentCommand), OnTrackingCompletedFinished, trackingCompleted);
							}
							catch (Exception ex) {
								TraceSource.TraceEvent(TraceEventType.Error, 0, "exception thrown in beging invoke of tracking complete event: {0}", ex);
							}
							finally {
								Trace.CorrelationManager.StopLogicalOperation();
							}
						}

						// update the current result
						currentResult = td.result;
					}

					// flush the command transport every iteration
					commandTransport.Flush();

					if (Services.DebuggingService.StepMode) {
						Services.DebuggingService.SetCompleted(typeof(TrackingManager));
					}
				}
			}
		}

		private void OnTrackingCompletedFinished(IAsyncResult ar) {
			// recover the delegate
			EventHandler<TrackingCompletedEventArgs> del = (EventHandler<TrackingCompletedEventArgs>)ar.AsyncState;

			// call the end invoke method to clean up the call and ignore any errors
			try {
				del.EndInvoke(ar);
			}
			catch (Exception ex) {
				TraceSource.TraceEvent(TraceEventType.Warning, 0, "exception throw in tracking complete event: {0}", ex);
			}
		}
	}
}
