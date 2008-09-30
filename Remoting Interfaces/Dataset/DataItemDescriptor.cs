using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset {
	[Serializable]
	public class DataItemDescriptor {
		private string name;
		private Type dataType;
		private string description;
		private string units;
		private DataTypeCode typeCode;
		private int capacity;

		public DataItemDescriptor(string name, Type dataType, string description, string units, int capacity) {
			this.name = name;
			this.dataType = dataType;
			this.description = description;
			this.units = units;
			this.capacity = capacity;
			this.typeCode = DetermineDataTypeCode();
		}

		private DataTypeCode DetermineDataTypeCode() {
			if (dataType == typeof(double))
				return DataTypeCode.Double;
			else if (dataType == typeof(float))
				return DataTypeCode.Single;
			else if (dataType == typeof(sbyte))
				return DataTypeCode.Int8;
			else if (dataType == typeof(Int16))
				return DataTypeCode.Int16;
			else if (dataType == typeof(Int32))
				return DataTypeCode.Int32;
			else if (dataType == typeof(Int64))
				return DataTypeCode.Int64;
			else if (dataType == typeof(byte))
				return DataTypeCode.UInt8;
			else if (dataType == typeof(UInt16))
				return DataTypeCode.UInt16;
			else if (dataType == typeof(UInt32))
				return DataTypeCode.UInt32;
			else if (dataType == typeof(UInt64))
				return DataTypeCode.UInt64;
			else if (dataType == typeof(bool))
				return DataTypeCode.Boolean;
			else if (dataType == typeof(DateTime))
				return DataTypeCode.DateTime;
			else if (dataType == typeof(TimeSpan))
				return DataTypeCode.TimeSpan;
			else if (dataType == typeof(UrbanChallenge.Common.Coordinates))
				return DataTypeCode.Coordinates;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.Circle))
				return DataTypeCode.Circle;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.Line))
				return DataTypeCode.Line;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.LineSegment))
				return DataTypeCode.LineSegment;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.Polygon))
				return DataTypeCode.Polygon;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.LineList))
				return DataTypeCode.LineList;
			else if (dataType == typeof(UrbanChallenge.Common.Splines.CubicBezier))
				return DataTypeCode.Bezier;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.Polygon[]))
				return DataTypeCode.PolygonArray;
			else if (dataType == typeof(UrbanChallenge.Common.Shapes.LineList[]))
				return DataTypeCode.LineListArray;
			else if (dataType == typeof(UrbanChallenge.Common.Coordinates[]))
				return DataTypeCode.CoordinatesArray;
			else if (dataType == typeof(UrbanChallenge.Common.Sensors.OperationalObstacle[]))
				return DataTypeCode.ObstacleArray;
			else
				return DataTypeCode.BinarySerialized;
		}

		public string Name {
			get { return name; }
		}

		public Type DataType {
			get { return dataType; }
		}

		public DataTypeCode DataTypeCode {
			get { return typeCode; }
		}

		public string Description {
			get { return description; }
		}

		public string Units {
			get { return units; }
		}

		public int Capacity {
			get { return capacity; }
		}
	}
}
