using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Logging {
	public struct UdpLogPacketDescriptor {
		public readonly TimeSpan timestamp;
		public readonly uint sourceAddr;
		public readonly uint destAddr;
		public readonly ushort destPort;
		public readonly int length;
		public readonly long filePosition;

		public UdpLogPacketDescriptor(TimeSpan timestamp, uint sourceAddr, uint destAddr, ushort destPort, int length, long filePosition) {
			this.timestamp = timestamp;
			this.sourceAddr = sourceAddr;
			this.destAddr = destAddr;
			this.destPort = destPort;
			this.length = length;
			this.filePosition = filePosition;
		}
	}
}
