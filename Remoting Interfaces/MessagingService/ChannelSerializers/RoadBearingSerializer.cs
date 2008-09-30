using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.MessagingService.ChannelSerializers {
	public static class RoadBearingSerializer {
		public static object Deserialize(Stream stream) {
			BinaryReader reader = new BinaryReader(stream);
			double timestamp = reader.ReadDouble();
			float heading = reader.ReadSingle();
			float confidence = reader.ReadSingle();

			return new SparseRoadBearing(timestamp, heading, confidence);
		}
	}
}
