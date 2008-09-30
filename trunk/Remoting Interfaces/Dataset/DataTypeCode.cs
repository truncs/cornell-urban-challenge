using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset {
	[Serializable]
	public enum DataTypeCode {
		Double,
		Single,
		Int8,
		Int16,
		Int32,
		Int64,
		UInt8,
		UInt16,
		UInt32,
		UInt64,
		Boolean,
		DateTime,
		TimeSpan,
		Coordinates,
		Circle,
		Line,
		LineSegment,
		Polygon,
		Bezier,
		LineList,
		BinarySerialized,
		CoordinatesArray,
		PolygonArray,
		LineListArray,
		ObstacleArray
	}
}
