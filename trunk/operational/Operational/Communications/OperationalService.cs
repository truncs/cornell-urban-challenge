using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;
using UrbanChallenge.Common.EarthModel;
using OperationalLayer.Tracking;
using UrbanChallenge.MessagingService;
using OperationalLayer.Tracing;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UrbanChallenge.Behaviors.CompletionReport;

namespace OperationalLayer.Communications {
	class OperationalService : OperationalFacade, IPingable {

		private class TestHelper : OperationalTestComponentFacade, IPingable {
			public override UrbanChallenge.Behaviors.CompletionReport.CompletionReport TestExecuteBehavior(Behavior b) {
				return Services.BehaviorManager.TestBehavior(b);
			}

			[Obsolete]
			public override void SetProjection(PlanarProjection proj) {
				Services.Operational.SetProjection(proj);
			}

			public override void SetRoadNetwork(UrbanChallenge.Arbiter.ArbiterRoads.ArbiterRoadNetwork roadNetwork) {
				Services.Operational.SetRoadNetwork(roadNetwork);
			}

			public override void Ping() {
				
			}

			public override object InitializeLifetimeService() {
				return null;
			}

			#region IPingable Members

			void IPingable.Ping() {
				
			}

			#endregion
		}

		private const string CarModeChannelName = "CarMode";

		private bool started;

		private CarMode carMode;
		private CarMode forcedCarMode = CarMode.Unknown;
		private IChannel carModeChannel;

		private OperationalListener listener;

		private TestHelper helper;

		public OperationalService(ICommandTransport transport, bool forwardPackets, bool listenPackets) {
			if (transport == null) {
				throw new ArgumentNullException("transport");
			}

			if (forwardPackets || listenPackets) {
				InitializePacketForwarding(listenPackets, forwardPackets);
			}

			// don't really need to initialize anything
			if (Settings.TestMode) {
				helper = new TestHelper();
				CommBuilder.BindObject(OperationalTestComponentFacade.ServiceName, helper);
			}
			else {
				CommBuilder.BindObject(OperationalFacade.ServiceName, this);
			}

			// set up the car mode channel
			carModeChannel = CommBuilder.GetChannel(CarModeChannelName);

			// register to receive run mode events
			transport.CarModeChanged += new EventHandler<CarModeChangedEventArgs>(transport_CarModeChanged);

			// mark that we haven't started yet
			started = false;
		}

		void transport_CarModeChanged(object sender, CarModeChangedEventArgs e) {
			if (this.carMode != e.Mode && e.Mode != CarMode.Run) {
				// clear out the forced car mode on transition
				forcedCarMode = CarMode.Unknown;
			}

			this.carMode = e.Mode;

			//Console.WriteLine("car mode changed to {0}", e.Mode);

			if (carModeChannel != null)
			{
				carModeChannel.PublishUnreliably(carMode);
			}

			Services.BehaviorManager.OnCarModeChanged(carMode);
		}

		public override void ExecuteBehavior(Behavior b) {
			if (started) {
				Services.BehaviorManager.OnBehaviorReceived(b);

				ForwardBehavior(b);
			}
		}

		public override Type GetCurrentBehaviorType() {
			// return from the behavior manager
			return Services.BehaviorManager.CurrentBehaviorType;
		}

		public void ForceCarMode(CarMode forced) {
			this.forcedCarMode = forced;
		}

		public override CarMode GetCarMode() {
			if (forcedCarMode != CarMode.Unknown) {
				return forcedCarMode;
			}
			else {
				return carMode;
			}
		}

		public CarMode GetRealCarMode() {
			return carMode;
		}

		[Obsolete]
		public override void SetProjection(PlanarProjection proj) {
			Services.Projection = proj;
		}

		public override void SetRoadNetwork(UrbanChallenge.Arbiter.ArbiterRoads.ArbiterRoadNetwork roadNetwork) {
			Services.Projection = roadNetwork.PlanarProjection;
			Services.RoadNetwork = roadNetwork;
		}

		public void Start() {
			started = true;
		}

		public override object InitializeLifetimeService() {
			return null;
		}

		public override void RegisterListener(OperationalListener listener) {
			this.listener = listener;
			CompletionReport report = new SuccessCompletionReport(typeof(OperationalStartupBehavior));
			SendCompletionReport(report);
		}

		public override void UnregisterListener(OperationalListener listener) {
			this.listener = null;
		}

		public void SendCompletionReport(UrbanChallenge.Behaviors.CompletionReport.CompletionReport report) {
			//OperationalTrace.WriteInformation("sending completion report {0}, listener {1}", report, listener == null ? "<null>" : listener.ToString());
			OperationalListener l = listener;
			if (l != null) {
				try {
					l.OnCompletionReport(report);
					OperationalTrace.WriteVerbose("completion report succeeded");
				}
				catch (Exception ex) {
					OperationalTrace.WriteWarning("completion report send failed: {0}", ex);
				}

				ForwardCompReport(report);
			}
		}

		public override void Ping() {
			// nothing to do
		}

		private const short version = 1;
		private const byte packet_type_arbiter_command = 1;
		private const byte packet_type_comp_report = 2;
		private const byte packet_compressed = 1;

		private Socket forwardingSocket;
		private EndPoint forwardingEndpoint;
		private MemoryStream behaviorStream;
		private MemoryStream compReportStream;
		private BinaryFormatter behaviorFormatter;
		private BinaryFormatter compReportFormatter;
		private int sequenceNumber;
		private bool listenPackets;
		private bool forwardPackets;

		private byte[] listenBuffer;

		private void InitializePacketForwarding(bool listen, bool forward) {
			this.listenPackets = listen;
			this.forwardPackets = forward;

			// build the command forwarding socket
			forwardingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			forwardingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			forwardingSocket.Bind(new IPEndPoint(IPAddress.Any, 30101));

			// create the stream objects
			behaviorStream = new MemoryStream();
			compReportStream = new MemoryStream();

			// create the binary formatters
			behaviorFormatter = new BinaryFormatter();
			compReportFormatter = new BinaryFormatter();

			// set up the forwarding destination address
			forwardingEndpoint = new IPEndPoint(IPAddress.Parse("239.132.1.101"), 30101);

			// set up the listening
			if (listen) {
				listenBuffer = new byte[ushort.MaxValue];

				forwardingSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.132.1.101")));
				forwardingSocket.BeginReceive(listenBuffer, 0, listenBuffer.Length, SocketFlags.None, ForwardedPacketReceived, null);
			}
		}

		private void ForwardBehavior(Behavior behavior) {
			if (forwardPackets) {
				ForwardObject(behavior, true, behaviorStream);
			}
		}

		private void ForwardCompReport(UrbanChallenge.Behaviors.CompletionReport.CompletionReport report) {
			if (forwardPackets) {
				ForwardObject(report, false, compReportStream);
			}
		}

		private void ForwardObject(object obj, bool isBehavior, MemoryStream stream) {
			lock (stream) {
				// reset the stream length
				stream.SetLength(0);
				
				BinaryWriter writer = new BinaryWriter(stream);
				// write the version
				writer.Write(version);
				// write the sequence number
				writer.Write(Interlocked.Increment(ref sequenceNumber));
				// write the type of message
				writer.Write(isBehavior ? packet_type_arbiter_command : packet_type_comp_report);
				// write whether we're compressing or not
				writer.Write(packet_compressed);

				// create compression stream
				DeflateStream compressionStream = new DeflateStream(stream, CompressionMode.Compress, true);

				// serialize the object to the compression stream
				if (isBehavior) {
					behaviorFormatter.Serialize(compressionStream, obj);
				}
				else {
					compReportFormatter.Serialize(compressionStream, obj);
				}

				// close the compression stream (note: flush doesn't actually do anything, even though the documentation hints it does)
				compressionStream.Dispose();

				// the memory stream now holds the compressed behavior
				// if the length is too big, send a warning and don't forward the packet
				if (stream.Length >= ushort.MaxValue) {
					OperationalTrace.WriteWarning("While forwarding object of type " + obj.GetType().Name + ", result data was too large");
				}
				else {
					forwardingSocket.SendTo(stream.GetBuffer(), (int)stream.Length, SocketFlags.None, forwardingEndpoint);
				}
			}
		}

		private void ForwardedPacketReceived(IAsyncResult ar) {
			int bytesRead = -1;
			try {
				// end the receive operation
				bytesRead = forwardingSocket.EndReceive(ar);
			}
			catch (ObjectDisposedException) {
				// we're donzoed
				return;
			}
			catch (Exception) {
				// try again
				forwardingSocket.BeginReceive(listenBuffer, 0, listenBuffer.Length, SocketFlags.None, ForwardedPacketReceived, null);
				return;
			}

			// get the version in the first two bytes
			short version = BitConverter.ToInt16(listenBuffer, 0);

			// construct a memory stream around the buffer, starting at index 2
			MemoryStream ms = new MemoryStream(listenBuffer, 2, bytesRead, false);

			// break on packet type
			if (version == 1) {
				ReadVersion1Packet(ms);
			}
			else {
				OperationalTrace.WriteWarning("Unknown forwarded packet version: " + version);
			}

			forwardingSocket.BeginReceive(listenBuffer, 0, listenBuffer.Length, SocketFlags.None, ForwardedPacketReceived, null);
		}

		private void ReadVersion1Packet(MemoryStream ms) {
			BinaryReader reader = new BinaryReader(ms);
			// get the sequence number
			int seqenceNumber = reader.ReadInt32();
			// get the message type
			byte messageType = reader.ReadByte();
			// get if we're compressed or not
			byte compressed = reader.ReadByte();

			Stream sourceStream = ms;
			if (compressed == packet_compressed) {
				// make a decompression stream
				sourceStream = new DeflateStream(ms, CompressionMode.Decompress);
			}

			// deserialize the object
			if (messageType == packet_type_arbiter_command) {
				try {
					Behavior b = (Behavior)behaviorFormatter.Deserialize(sourceStream);
					// execute the behavior
					ExecuteBehavior(b);
				}
				catch (Exception ex) {
					OperationalTrace.WriteWarning("error reading behavior packet: " + ex.ToString());
				}
			}
			else if (messageType == packet_type_comp_report) {
				// ignore for now, I messed this up when recording
			}
		}
	}
}
