using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class PlanningGrid : ISerializable {
		private float[][] data;
		private int sizeX, sizeY;
		private float min, max;
		private float spacing;
		private float offsetX, offsetY;

		private bool windowEnabled;
		private int windowStartX, windowStartY;
		private int windowEndX, windowEndY;

		public PlanningGrid(int sizeX, int sizeY, float[][] data, float spacing, float offsetX, float offsetY) {
			this.sizeX = sizeX;
			this.sizeY = sizeY;
			this.data = data;
			this.spacing = spacing;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
		}

		public void SetWindow(int windowStartX, int windowStartY, int windowEndX, int windowEndY) {
			this.windowStartX = windowStartX;
			this.windowStartY = windowStartY;
			this.windowEndX = windowEndX;
			this.windowEndY = windowEndY;
			this.windowEnabled = true;
		}

		protected PlanningGrid(SerializationInfo info, StreamingContext context) {
			sizeX = info.GetInt32("sizeX");
			sizeY = info.GetInt32("sizeY");

			spacing = info.GetSingle("spacing");
			offsetX = info.GetSingle("offsetX");
			offsetY = info.GetSingle("offsetY");

			byte[] compData = (byte[])info.GetValue("data", typeof(byte[]));
			MemoryStream ms = new MemoryStream(compData, false);
			DeflateStream compStream = new DeflateStream(ms, CompressionMode.Decompress);
			BinaryReader reader = new BinaryReader(compStream);

			min = float.MaxValue;
			max = float.MinValue;

			data = new float[sizeX][];
			for (int x = 0; x < sizeX; x++) {
				data[x] = new float[sizeY];
				for (int y = 0; y < sizeY; y++) {
					float value = reader.ReadSingle();
					data[x][y] = value;

					if (value < min) min = value;
					if (value > max) max = value;
				}
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			if (windowEnabled) {
				info.AddValue("sizeX", windowEndX-windowStartX+1);
				info.AddValue("sizeY", windowEndY-windowStartY+1);
				info.AddValue("spacing", spacing);
				info.AddValue("offsetX", offsetX + spacing*windowStartX);
				info.AddValue("offsetY", offsetY + spacing*windowStartY);
			}
			else {
				info.AddValue("sizeX", sizeX);
				info.AddValue("sizeY", sizeY);
				info.AddValue("spacing", spacing);
				info.AddValue("offsetX", offsetX);
				info.AddValue("offsetY", offsetY);
			}

			// allocate with a big size
			MemoryStream ms = new MemoryStream(1 << 16);
			DeflateStream compStream = new DeflateStream(ms, CompressionMode.Compress);
			BinaryWriter writer = new BinaryWriter(compStream);

			if (windowEnabled) {
				for (int x = windowStartX; x <= windowEndX; x++) {
					for (int y = windowStartY; y <= windowEndY; y++) {
						writer.Write(data[x][y]);
					}
				}
			}
			else {
				for (int x = 0; x < sizeX; x++) {
					for (int y = 0; y < sizeY; y++) {
						writer.Write(data[x][y]);
					}
				}
			}

			compStream.Close();

			info.AddValue("data", ms.ToArray());
		}

		public int SizeX {
			get { return sizeX; }
		}

		public int SizeY {
			get { return sizeY; }
		}

		public float[][] Data {
			get { return data; }
		}

		public float this[int x, int y] {
			get { return data[x][y]; }
		}

		public float MinValue {
			get { return min; }
		}

		public float MaxValue {
			get { return max; }
		}

		public float OffsetX {
			get { return offsetX; }
		}

		public float OffsetY {
			get { return offsetY; }
		}

		public float Spacing {
			get { return spacing; }
		}
	}
}
