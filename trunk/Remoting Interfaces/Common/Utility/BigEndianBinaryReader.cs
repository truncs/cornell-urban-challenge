using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UrbanChallenge.Common.Utility {
	public class BigEndianBinaryReader {
		private Stream stream;
		// pre-allocate buffer so we don't re-allocate for each read
		private byte[] buffer = new byte[4];

		public BigEndianBinaryReader(Stream stream) {
			this.stream = stream;
		}

		public BigEndianBinaryReader(byte[] data) {
			this.stream = new MemoryStream(data, false);
		}

		private void ReadBytes(int count) {
			int totalCount = 0;
			while (totalCount < count) {
				int bytesRead = stream.Read(buffer, 0, count - totalCount);
				if (bytesRead == 0)
					throw new EndOfStreamException();
				totalCount += bytesRead;
			}

			// reverse the bytes we just read
			Array.Reverse(buffer, 0, count);
		}

		public byte ReadByte() {
			ReadBytes(1);
			return buffer[0];
		}

		public sbyte ReadSByte() {
			ReadBytes(1);
			return (sbyte)buffer[0];
		}

		public ushort ReadUInt16() {
			ReadBytes(2);
			return BitConverter.ToUInt16(buffer, 0);
		}

		public short ReadInt16() {
			ReadBytes(2);
			return BitConverter.ToInt16(buffer, 0);
		}

		public uint ReadUInt32() {
			ReadBytes(4);
			return BitConverter.ToUInt32(buffer, 0);
		}

		public int ReadInt32() {
			ReadBytes(4);
			return BitConverter.ToInt32(buffer, 0);
		}
	}
}
