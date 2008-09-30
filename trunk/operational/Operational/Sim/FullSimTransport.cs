using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using SimOperationalService;
using UrbanChallenge.MessagingService;
using OperationalLayer.Communications;
using UrbanChallenge.Common;
using Dataset.Source;
using UrbanChallenge.Common.Mapack;
using OperationalLayer.Pose;
using System.Diagnostics;
using OperationalLayer.Tracing;
using System.Net.Sockets;
using OperationalLayer.CarTime;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Common.Sensors;

namespace OperationalLayer.Sim {
	class FullSimTransport : ICommandTransport, IChannelListener {
		private DynamicsSimFacade dynamicsSim;
		private IChannel simStateChannel;
		private IChannel obstacleChannel;
		private IChannel vehicleChannel;

		private CarMode lastCarMode;
		private TransmissionGear lastRecvTransGear = TransmissionGear.Neutral;

		private static TraceSource TraceSource = new TraceSource("simstate");

		public FullSimTransport() {
			// initialize the car mode to unknown so that we raised the changed event immediately
			lastCarMode = CarMode.Unknown;

			// get the DynamicsSimFacade
			dynamicsSim = (DynamicsSimFacade)CommBuilder.GetObject(DynamicsSimFacade.ServiceName);

			// set the sim state channel
			simStateChannel = CommBuilder.GetChannel(OperationalSimVehicleState.ChannelName);
			obstacleChannel = CommBuilder.GetChannel(SceneEstimatorObstacleChannelNames.UntrackedClusterChannelName);
			vehicleChannel = CommBuilder.GetChannel(SceneEstimatorObstacleChannelNames.TrackedClusterChannelName);
			// add ourself as a listern
			simStateChannel.Subscribe(this);
			obstacleChannel.Subscribe(this);
			vehicleChannel.Subscribe(this);
		}

		#region ICommandTransport Members

		public event EventHandler<CarModeChangedEventArgs> CarModeChanged;

		public void SetCommand(double? engineTorque, double? brakePressue, double? steering, TransmissionGear? gear) {
			if (Settings.TestMode)
				return;

			if (steering.HasValue && Math.Abs(steering.Value) > TahoeParams.SW_max) {
				OperationalTrace.WriteWarning("steering out of range: received {0}, limit {1}", steering.Value, TahoeParams.SW_max);
			}

			//Console.WriteLine("command--steering: {0}, torque: {1}, brake: {2}, gear: {3}", steering, engineTorque, brakePressue, gear);

			try {
				dynamicsSim.SetSteeringBrakeThrottle(steering, engineTorque, brakePressue);

				if (gear.HasValue) {
					dynamicsSim.SetTransmissionGear(gear.Value);
				}
			}
			catch (SocketException) {
				// get the DynamicsSimFacade
				dynamicsSim = (DynamicsSimFacade)CommBuilder.GetObject(DynamicsSimFacade.ServiceName);
				throw;
			}
		}

		public void SetTurnSignal(TurnSignal signal) {
			// don't do anything with this as the moment
		}

		public void Flush() {
			// we always send immediately
		}

		public CarMode CarMode {
			get { return lastCarMode; }
		}

		#endregion

		#region IChannelListener Members

		void IChannelListener.MessageArrived(string channelName, object message) {

			if (message is OperationalSimVehicleState) {
				OperationalTrace.ThreadTraceSource = TraceSource;
				Trace.CorrelationManager.StartLogicalOperation("simstate callback");
				try {
					OperationalSimVehicleState state = (OperationalSimVehicleState)message;

					DatasetSource ds = Services.Dataset;

					OperationalTrace.WriteVerbose("received operational sim state, t = {0}", state.Timestamp);

					Services.Dataset.MarkOperation("pose rate", LocalCarTimeProvider.LocalNow);

					CarTimestamp now = state.Timestamp;
					ds.ItemAs<Coordinates>("xy").Add(state.Position, now);
					ds.ItemAs<double>("speed").Add(state.Speed, now);
					ds.ItemAs<double>("heading").Add(state.Heading, now);
					ds.ItemAs<double>("actual steering").Add(state.SteeringAngle, now);
					ds.ItemAs<TransmissionGear>("transmission gear").Add(state.TransmissionGear, now);
					ds.ItemAs<double>("engine torque").Add(state.EngineTorque, now);
					ds.ItemAs<double>("brake pressure").Add(state.BrakePressure, now);
					ds.ItemAs<double>("rpm").Add(state.EngineRPM, now);

					if (state.TransmissionGear != lastRecvTransGear) {
						ds.ItemAs<DateTime>("trans gear change time").Add(HighResDateTime.Now, now);
						lastRecvTransGear = state.TransmissionGear;
					}

					lastCarMode = state.CarMode;
					if (CarModeChanged != null) {
						CarModeChanged(this, new CarModeChangedEventArgs(lastCarMode, now));
					}

					// build the current relative pose matrix
					Matrix4 relativePose = Matrix4.Translation(state.Position.X, state.Position.Y, 0)*Matrix4.YPR(state.Heading, 0, 0);
					relativePose = relativePose.Inverse();

					// push on to the relative pose stack
					Services.RelativePose.PushTransform(now, relativePose);

					// push the current absolute pose entry
					AbsolutePose absPose = new AbsolutePose(state.Position, state.Heading, now);
					Services.AbsolutePose.PushAbsolutePose(absPose);
				}
				finally {
					Trace.CorrelationManager.StopLogicalOperation();
					OperationalTrace.ThreadTraceSource = null;
				}
			}
			else if (message is SceneEstimatorUntrackedClusterCollection) {
				// push to obstacle pipeline
				Services.ObstaclePipeline.OnUntrackedClustersReceived(((SceneEstimatorUntrackedClusterCollection)message));
			}
			else if (message is SceneEstimatorTrackedClusterCollection) {
				Services.ObstaclePipeline.OnTrackedClustersReceived((SceneEstimatorTrackedClusterCollection)message);
			}
		}

		#endregion
	}
}
