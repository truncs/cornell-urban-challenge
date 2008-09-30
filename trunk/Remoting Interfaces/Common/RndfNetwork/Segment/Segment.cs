using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Describes speed information for travelling the segment
	/// </summary>
	[Serializable]
	public class SpeedInformation
	{
		private double minSpeed;
		private double maxSpeed;
		private double averageSpeed;
		private bool traveled;
		private SegmentID segmentID;

		/// <summary>
		/// Default constructor
		/// </summary>
		public SpeedInformation()
		{
		}

		/// <summary>
		/// Constructor. For initialization of speeds from the mdf
		/// </summary>
		/// <param name="segmentID">SegmentID of segment this speed limit belongs to</param>
		/// <param name="minSpeed">Minimum speed on this segment by definition</param>
		/// <param name="maxSpeed">Maximum speed on this segment by definition</param>
		public SpeedInformation(SegmentID segmentID, double minSpeed, double maxSpeed)
		{
			this.segmentID = segmentID;
			this.minSpeed = minSpeed;
			this.maxSpeed = maxSpeed;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentID">SegmentID of segment this speed limit belongs to</param>
		/// <param name="minSpeed">Minimum speed on this segment by definition</param>
		/// <param name="maxSpeed">Maximum speed on this segment by definition</param>
		/// <param name="averageSpeed">Average speed on this segment</param>
		/// <param name="traveled">Have we travelled this segment yet</param>
		public SpeedInformation(SegmentID segmentID, double minSpeed, double maxSpeed, double averageSpeed, bool traveled)
		{
			this.segmentID = segmentID;
			this.minSpeed = minSpeed;
			this.maxSpeed = maxSpeed;
			this.averageSpeed = averageSpeed;
			this.traveled = traveled;
		}

		/// <summary>
		/// SegmentID of segment this speed limit belongs to
		/// </summary>
		public SegmentID SegmentID
		{
			get { return segmentID; }
			set { segmentID = value; }
		}

		/// <summary>
		/// Identifier if we've tavelled segment to get a good average speed
		/// </summary>
		public bool Traveled
		{
			get { return traveled; }
			set { traveled = value; }
		}

		/// <summary>
		/// Average speed on this segment.
		/// Check if traveled
		/// </summary>
		public double AverageSpeed
		{
			get 			
			{
				// HACK: return 8.8 if max speed doesn't exist , should return coarse average of section of velocity profile
				if (maxSpeed == 0)
				{
					return 5; // return 5m/s if no max speed
				}
				else
				{
					return maxSpeed;
				}

				/*if (averageSpeed < minSpeed)
					return minSpeed;
				else if (maxSpeed == 0 && averageSpeed == 0)
				{
					return 8.8; // ~20mph
				}
				else
					return (maxSpeed + minSpeed) / 2.0; */
			}
			set { averageSpeed = value; }
		}

		/// <summary>
		/// maximum defined speed on segment
		/// </summary>
		public double MaxSpeed
		{
			get 
			{
				if (maxSpeed == 0)
				{
					return 5; // return 5 if no max speed
				}
				else
					return maxSpeed;
			}
			set { maxSpeed = value; }
		}

		/// <summary>
		/// Minimum defined speed on segment
		/// </summary>
		public double MinSpeed
		{
			get { return minSpeed; }
			set { minSpeed = value; }
		}
	}

	/// <summary>
	/// Road from the Rndf
	/// </summary>
	[Serializable]
	public class Segment
	{
		private SegmentID segmentID;
		private Way way1;
		private Way way2;
		private Dictionary<WayID, Way> ways;
		private int numLanes;
		private SpeedInformation speedInformation;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentID">Identification information about the segment</param>
		public Segment(SegmentID segmentID)
		{
			this.segmentID = segmentID;
			way1 = new Way();
			way2 = new Way();
			ways = new Dictionary<WayID, Way>();

			// initialize speed
			speedInformation = new SpeedInformation();
		}

		/// <summary>
		/// number of lanes in the segment
		/// </summary>
		public int NumLanes
		{
			get { return numLanes; }
			set { numLanes = value; }
		}

		/// <summary>
		/// The first direction of travel in the Segment
		/// </summary>
		/// <remarks>Setting removes old way1, if valid, from ways. Setting adds new value to ways</remarks>
		public Way Way1
		{
			get { return way1; }
			set 
			{
				if (way1.IsValid)
				{
					ways.Remove(way1.WayID);
				}
				way1 = value;
				ways.Add(value.WayID, value);
			}
		}		

		/// <summary>
		/// The second direction of travel in the Segment
		/// </summary>
		/// <remarks>Setting removes old way2, if valid, from ways. Setting adds new value to ways</remarks>
		public Way Way2
		{
			get { return way2; }
			set
			{
				if (way2.IsValid)
				{
					ways.Remove(way2.WayID);
				}
				way2 = value;
				ways.Add(value.WayID, value);
			}
		}

		/// <summary>
		/// Both ways collected for joint operations
		/// </summary>
		public Dictionary<WayID,Way> Ways
		{
			get { return ways; }
		}

		/// <summary>
		/// Identification information about the segment
		/// </summary>
		public SegmentID SegmentID
		{
			get { return segmentID; }
			set { segmentID = value; }
		}

		/// <summary>
		/// Speed information about the segment
		/// </summary>
		public SpeedInformation SpeedInformation
		{
			get { return speedInformation; }
			set { speedInformation = value; }
		}
		
	}
}
