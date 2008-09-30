using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors
{
	public enum SideRoadEdgeMsgID : int
	{
		Info = 0,
		RoadEdgeMsg = 1,
		Bad = 99
	}

	public enum SideRoadEdgeSide : int
	{
		Driver = 0,
		Passenger = 1
	}

	public class SideRoadEdge
	{
		public SideRoadEdgeSide side;
		public double timestamp;
		public double curbHeading;
		public double curbDistance;
		public bool isValid;
		public double probabilityValid;
		
	}
}
