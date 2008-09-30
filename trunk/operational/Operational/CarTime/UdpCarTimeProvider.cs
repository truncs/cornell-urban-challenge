using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.CarTime {
	class UdpCarTimeProvider : ICarTimeProvider {
		private const double alpha = 0.8;
		private double offset;

		private DateTime start;

		private UdpClient client;
		private Thread listenerThread;

		public UdpCarTimeProvider() {
			// set the start time
			start = HighResDateTime.Now;

			// set the offset to NaN
			offset = double.NaN;

			// create the udp client to listen on port 30
			client = new UdpClient(30);
			// enable receiving of broadcast packets
			client.EnableBroadcast = true;

			// create the reader thread
			listenerThread = new Thread(ListenerProc);
			// set to the highest priority so that it keeps things real time and stuff
			listenerThread.Priority = ThreadPriority.Highest;
			// put a useful name on it for debugging
			listenerThread.Name = "UdpCarTimeProvider - Listener";
			// set it as a background thread so it doesn't keep the application alive
			listenerThread.IsBackground = true;

			// set the thread running
			listenerThread.Start();
		}

		#region ICarTimeProvider Members

		public CarTimestamp Now {
			get {  
				// get the elapsed seconds since the start of the program
				TimeSpan elapsed = HighResDateTime.Now-start;
				// add in the offset
				return new CarTimestamp(elapsed.TotalSeconds+offset);
			}
		}

		#endregion

		private void ListenerProc() {
			while (true) {
				IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.Receive(ref ep);

				// get the current time
				DateTime now = HighResDateTime.Now;

				if (data.Length == 5) {
					// parse the data
					BigEndianBinaryReader reader = new BigEndianBinaryReader(data);
					// check that the first byte is a 0
					if (reader.ReadByte() == 0) {
						// read the seconds
						ushort secs = reader.ReadUInt16();
						// read the ticks
						ushort ticks = reader.ReadUInt16();

						// get the car timestamp
						CarTimestamp ct = new CarTimestamp(secs, ticks);

						// get the elapsed seconds since start
						TimeSpan elapsed = now-start;
						double localElapsedSecs = elapsed.TotalSeconds;
						
						// update the diff
						double curDiff = ct.ts-localElapsedSecs;
						double newDiff;
						// check if we haven't initialized the offset or the offset deviates from the just-measured offset by more than a second
						if (double.IsNaN(offset) || Math.Abs(offset-curDiff) >= 1) {
							newDiff = curDiff;
						}
						else {
							// smooth out the offset values using an exponential filter
							newDiff = curDiff*alpha + offset*(1-alpha);
						}

						// swap in the new value
						// probably don't need an interlocked operation for this, but using it so that 
						//	we swap the entire double at once and don't get any weird problems
						Interlocked.Exchange(ref offset, newDiff);
					}
				}
			}
		}
	}
}
