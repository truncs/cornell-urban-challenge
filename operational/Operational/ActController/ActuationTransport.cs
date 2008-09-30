using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;
using System.Net;
using System.Net.Sockets;
using UrbanChallenge.Common;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Actuation;

namespace OperationalLayer.ActController {
	class ActuationTransport : ICommandTransport {
		private readonly IPAddress multicastAddr = IPAddress.Parse("239.132.1.6");
		private const int receivePort = 30006;

		/// <summary>
		/// pedal position 0% value for determining commanded throttle value
		/// </summary>
		const double pedal_map_min = 900;
		/// <summary>
		/// pedal position 100% value for determining commanded throttle value
		/// </summary>
		const double pedal_map_max = 3200;

		/// <summary>
		/// maximum allowed commanded torque
		/// </summary>
		const double torque_max = 420;

		public event EventHandler<CarModeChangedEventArgs> CarModeChanged; 

		private FeedbackMapper mapper;
		private UdpClient client;

		private CarMode lastCarMode = CarMode.Unknown;

		private bool useWheelspeed = true;
		private int ws_count = 0;

		private volatile bool running = true;
		private Thread messageThread = null;

		private byte lastTurnSignal = ActuationCommandInterface.ACT_SIG_NONE;
		private short? lastSteeringValue;
		private byte? lastBrakeValue;
		private ushort? lastThrottleValue;
		private byte? lastTransmissionValue;

		// last received transmission gear
		// set to neutral as we'll never have a good reason to be in neutral (i.e. we're shifting out of it immediately)
		private TransmissionGear lastRecvTransmissionGear = TransmissionGear.Neutral;

		public ActuationTransport(bool listen) {
			// determine if the wheelspeeds should be used as the speed values
			useWheelspeed = Settings.UseWheelSpeed;

			if (listen) {
				// create the feedback mapper
				using (Stream configStream = typeof(ActuationTransport).Assembly.GetManifestResourceStream(typeof(ActuationTransport), "FeedbackMapping.xml")) {
					mapper = new FeedbackMapper(configStream, typeof(ActuationTransport), null);
				}
			}
		}

		public void SetCommand(double? engineTorque, double? brakePressue, double? steeringAngle, TransmissionGear? gear) {
			// break out if we're in test mode
			if (Settings.TestMode) {
				return;
			}

			if (steeringAngle.HasValue) {
				int tempSteeringValue = (int)((steeringAngle.Value * 180 / Math.PI) / 0.0625);
				lastSteeringValue = (short)tempSteeringValue;
			}
			else {
				lastSteeringValue = null;
			}

			if (brakePressue.HasValue) {
				// do a check on brake values
				if (brakePressue.Value < 22)
					brakePressue = 22;
				if (brakePressue.Value > 65)
					brakePressue = 65;

				lastBrakeValue = (byte)Math.Round(brakePressue.Value, 0);
			}
			else {
				lastBrakeValue = null;
			}

			if (engineTorque.HasValue) {
				// map the value of throttle to torque
				lastThrottleValue = MapThrottle(engineTorque.Value);
			}
			else {
				lastThrottleValue = null;
			}

			if (gear.HasValue) {
				switch (gear.Value) {
					case TransmissionGear.First:
					case TransmissionGear.Second:
					case TransmissionGear.Third:
					case TransmissionGear.Fourth:
						lastTransmissionValue = ActuationCommandInterface.TRANS_GEAR_DRIVE;
						break;

					case TransmissionGear.Reverse:
						lastTransmissionValue = ActuationCommandInterface.TRANS_GEAR_REV;
						break;

					case TransmissionGear.Park:
						lastTransmissionValue = ActuationCommandInterface.TRANS_GEAR_PARK;
						break;

					case TransmissionGear.Neutral:
						lastTransmissionValue = ActuationCommandInterface.TRANS_GEAR_NEUTRAL;
						break;

					default:
						lastTransmissionValue = null;
						break;
				}
			}
			else {
				lastTransmissionValue = null;
			}
		}

		public void SetTurnSignal(TurnSignal signal) {
			if (Settings.TestMode)
				return;

			Services.Dataset.ItemAs<TurnSignal>("turn signal").Add(signal, Services.RelativePose.CurrentTimestamp);
			byte sigValue = ActuationCommandInterface.ACT_SIG_NONE;
			switch (signal) {
				case TurnSignal.Off:
					sigValue = ActuationCommandInterface.ACT_SIG_NONE;
					break;

				case TurnSignal.Left:
					sigValue = ActuationCommandInterface.ACT_SIG_LEFT;
					break;

				case TurnSignal.Right:
					sigValue = ActuationCommandInterface.ACT_SIG_RIGHT;
					break;

				case TurnSignal.Hazard:
					sigValue = ActuationCommandInterface.ACT_SIG_HAZARDS;
					break;
			}

			lastTurnSignal = sigValue;
		}

		public void Flush() {
			if (Settings.TestMode)
				return;

			// send all command message
			if (client != null) {
				ActuationCommandInterface.SendAllCommand(client.Client, lastSteeringValue, lastBrakeValue, lastThrottleValue, lastTransmissionValue, lastTurnSignal);
			}
		}

		public CarMode CarMode {
			get { return lastCarMode; }
		}

		private ushort MapThrottle(double cmdTorque) {
			if (cmdTorque > torque_max) {
				cmdTorque = torque_max;
			}

			// 4th-degree polynomial mapping of commanded torque to pedal position (determined from logs)
			// returns a pedal position value in [0,1] 
			double intermed = (1.119809E-10*Math.Pow(cmdTorque, 4) - 7.109911E-8*Math.Pow(cmdTorque, 3) + 0.0000145892*Math.Pow(cmdTorque, 2) - 0.000122394*cmdTorque);
			// mapping from pedal position to commanded values
			double intermed2 = pedal_map_min + (pedal_map_max - pedal_map_min)*intermed;
			
			// put a cap on the max value
			if (intermed2 > pedal_map_max)
				intermed2 = pedal_map_max;
			// put a cap on the min value
			if (intermed2 < pedal_map_min)
				intermed2 = pedal_map_min;
			return (ushort)intermed2;
		}

		public void Start() {
			if (client != null)
				throw new InvalidOperationException("Transport already started");

			// create the client on the specified receive port
			client = new UdpClient(receivePort);
			// register for the multicast group
			client.JoinMulticastGroup(multicastAddr);

			// check if we actually want to listen
			if (mapper != null) {
				// start a listener thread
				messageThread = new Thread(MessageThreadProc);
				// set this to be a background thread so that it won't keep the app alive
				messageThread.IsBackground = true;
				// set the thread priority to be high so that the thread scheduler
				// doesn't introduce any weird delay problems
				Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
				// mark that we're running
				running = true;
				// start yer engines
				messageThread.Start();
			}
		}

		private void MessageThreadProc() {
			while (running) {
				try {
					// make a blocking call to receive a message from the serial bridge
					// say that we want to receive from any address on any port
					IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
					byte[] data = client.Receive(ref remote);

					// dispatch the message via the feedback mapper
					mapper.HandleMessage(data, this, Services.Dataset);
				}
				catch (Exception) {
					// just ignore the error and try again for now
					// TODO: add in diagnostic and recovery functionality
				}
			}
		}

		public void OnActuationStatus(
			CarTimestamp ct,
			int version,
			CarMode carMode,
			bool steeringActive,
			bool steeringBypass,
			bool transActive,
			bool transBypass,
			bool throttleActive,
			bool throttleBypass,
			bool brakeActive,
			bool brakeBypass) {

			Services.Dataset.ItemAs<bool>("trans bypassed").Add(transBypass, ct);
		}

		public void OnAllFast(
			CarTimestamp ct,
			double wheelFL,
			double wheelFR,
			double wheelRR,
			double wheelRL,
			double rpm,
			double steerAngle,
			TransmissionGear transGear,
			int transDir,
			double actTorque,
			double reqTorque,
			int cylinderDeactivation,
			int pedalPos,
			double brakePressure) {
			OnWheelspeed(wheelFL, wheelFR, wheelRR, wheelRL, ct);
			OnBrake(brakePressure, ct);

			if (transGear != lastRecvTransmissionGear) {
				Services.Dataset.ItemAs<DateTime>("trans gear change time").Add(HighResDateTime.Now, ct);
				lastRecvTransmissionGear = transGear;
			}
		}

		public void OnWheelspeed(double fl, double fr, double rr, double rl, CarTimestamp ct) {
			ws_count++;
			if (useWheelspeed) {
				Services.Dataset.ItemAs<double>("speed").Add((rr + rl) / 2.0 * (10.0 / 36.0), ct);
			}
		}

		public void OnBrake(double value, CarTimestamp ct) {
			// filter out bad values (70 chosen as a cutoff for now)
			if (value < 70) {
				// add the item to the dataset
				Services.Dataset.ItemAs<double>("brake pressure").Add(value, ct);
			}
		}

		public void OnCarMode(CarTimestamp ct, CarMode mode) {
			// check if the car mode has changed
			if (mode != lastCarMode) {
				Console.WriteLine("car mode changed to " + mode);
				lastCarMode = mode;
				// invoke the callback asynchronously so we don't block the reading thread
				if (CarModeChanged != null) {
					CarModeChanged.BeginInvoke(this, new CarModeChangedEventArgs(mode, ct), OnCarModeChangedCompleted, CarModeChanged);
				}
			}
		}

		private void OnCarModeChangedCompleted(IAsyncResult ar) {
			// handle the cleanup after the async call
			EventHandler<CarModeChangedEventArgs> del = ar.AsyncState as EventHandler<CarModeChangedEventArgs>;
			try {
				del.EndInvoke(ar);
			}
			catch (Exception) {
			}
		}

		#region IDisposable Members

		public void Dispose() {
			running = false;

			client.Close();
			client = null;

			if (messageThread != null) {
				messageThread.Join();
				messageThread = null;
			}
		}

		#endregion
	}
}
