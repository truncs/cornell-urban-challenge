using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace CompressionChannel {
	internal static class CompressionHelper {
		public const string CompressKey = "duc_compressed";
		public const string CompressRequest = "can_compress?";
		public const string CompressedFlag = "compressed";

		public static Stream CompressStream(Stream instream) {
			MemoryStream outstream = new MemoryStream((int)instream.Length);
			DeflateStream comp = new DeflateStream(outstream, CompressionMode.Compress, true);

			int numBytes;
			byte[] buffer = new byte[4096];
			while ((numBytes = instream.Read(buffer, 0, 4096)) != 0) {
				comp.Write(buffer, 0, numBytes);
			}
			comp.Flush();
			comp.Dispose();

			// return to the beginning of the stream
			outstream.Position = 0;

			//Debug.WriteLine("Compression: " + instream.Length.ToString() + " to " + outstream.Length.ToString());
			return outstream;
		}

		public static Stream DecompressStream(Stream instream) {
			MemoryStream outstream = new MemoryStream();
			DeflateStream comp = new DeflateStream(instream, CompressionMode.Decompress, true);

			int numBytes;
			byte[] buffer = new byte[4096];
			while ((numBytes = comp.Read(buffer, 0, 4096)) != 0) {
				outstream.Write(buffer, 0, numBytes);
			}

			outstream.Position = 0;
			return outstream;
		}
	}
}
