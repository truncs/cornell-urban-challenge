using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UrbanChallenge.Common.Utility {
	public class BigEndianBinaryWriter {
		private Stream stream;

		public BigEndianBinaryWriter(byte[] buffer) {
			stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
		}

		public BigEndianBinaryWriter(Stream stream) {
			this.stream = stream;
		}

		public void WriteByte(byte data) {
			stream.WriteByte(data);
		}

		public void WriteBytes(byte[] bytes) {
			stream.Write(bytes, 0, bytes.Length);
		}

		public void WriteUInt16(ushort data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			WriteBytes(bytes);
		}

		public void WriteInt16(short data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			WriteBytes(bytes);
		}

		public void WriteUInt32(uint data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			WriteBytes(bytes);
		}

		public void WriteInt32(int data) {
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			WriteBytes(bytes);
		}
	}
}
