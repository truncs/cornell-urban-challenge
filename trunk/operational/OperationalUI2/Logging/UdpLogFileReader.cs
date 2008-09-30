using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UrbanChallenge.Logging {
	public class UdpLogFileReader {
		private Stream stream;
		private BinaryReader reader;
		private byte[] tempBuffer = new byte[ushort.MaxValue];

		private long tickConv = TimeSpan.TicksPerSecond / 10000;

		public UdpLogFileReader(Stream stream) {
			this.stream = stream;
			this.reader = new BinaryReader(stream);
		}

		public Stream Stream {
			get { return stream; }
		}

		private void ReadPacketInternal(bool readData, out UdpLogPacketDescriptor desc, byte[] data) {
			long filePosition = stream.Position;
			long ticks = reader.ReadUInt32();
			uint sourceIP = reader.ReadUInt32();
			uint destIP = reader.ReadUInt32();
			ushort destPort = reader.ReadUInt16();
			int len = reader.ReadInt32();

			if (readData || !stream.CanSeek) {
				if (data.Length < len) {
					throw new ArgumentException("Data buffer is not sufficient length", "data");
				}

				int offset = 0;
				int count = len;
				int bytesRead = 0;
				do {
					bytesRead = stream.Read(data, offset, count);
					if (bytesRead == 0) {
						throw new EndOfStreamException();
					}
					count -= bytesRead;
					offset += bytesRead;
				} while (count > 0);
			}
			else {
				data = null;
				long targetPosition = stream.Position + len;
				long newPosition = stream.Seek(len, SeekOrigin.Current);
				if (targetPosition != newPosition) {
					throw new EndOfStreamException();
				}
			}

			TimeSpan timestamp = TimeSpan.FromTicks(ticks * tickConv);
			desc = new UdpLogPacketDescriptor(timestamp, sourceIP, destIP, destPort, len, filePosition);
		}

		public UdpLogPacketDescriptor ReadPacketDescriptor() {
			UdpLogPacketDescriptor desc;
			ReadPacketInternal(false, out desc, tempBuffer);
			return desc;
		}

		public void ReadPacket(out UdpLogPacketDescriptor desc, byte[] data) {
			ReadPacketInternal(true, out desc, data);
		}
	}
}
