using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.EarthModel;

namespace RndfToolkit
{
	/// <summary>
	/// Class to help with generating the rndf network
	/// </summary>
	public class RndfNetworkGenerator
	{
		/// <summary>
		/// Creates an rndf network from an xy rndf
		/// </summary>
		/// <returns></returns>
		public RndfNetwork CreateRndfNetwork(IRndf xyRndf, PlanarProjection projection)
		{
			return null;
		}

		/// <summary>
		/// Determines the lane adjacency
		/// </summary>
		/// <param name="rndf"></param>
		/// <returns></returns>
		private RndfNetwork DetermineLaneAdjacency(RndfNetwork rndf)
		{
			// loop over segment
			foreach (Segment segment in rndf.Segments.Values)
			{
				// make sure both ways valid
				if (segment.Way1.IsValid && segment.Way2.IsValid)
				{
					// dictionary of lanes in the segment
					Dictionary<LaneID, Lane> segmentLanes = new Dictionary<LaneID, Lane>();

					// construct dictionary
					foreach (Way way in segment.Ways.Values)
					{
						foreach (Lane lane in way.Lanes.Values)
						{
							segmentLanes.Add(lane.LaneID, lane);
						}
					}

					// check sample lane in way1	
					Lane way1SampleLane = null;
					foreach (Lane tmp in segment.Way1.Lanes.Values)
					{
						way1SampleLane = tmp;
					}

					// check sample lane in way2
					Lane way2SampleLane = null;
					foreach (Lane tmp in segment.Way2.Lanes.Values)
					{
						way2SampleLane = tmp;
					}

					// modifies to denote increasing or decreasing form way1 to way 2
					int modifier = 1;

					// check if way2 has lower numbers
					if (way1SampleLane.LaneID.LaneNumber > way2SampleLane.LaneID.LaneNumber)
						modifier = -1;

					int i = 1;
					LaneID currentLaneID = new LaneID(way1SampleLane.LaneID.WayID, i);

					// loop over lanes
					while (segmentLanes.ContainsKey(currentLaneID))
					{
						Lane currentLane = segmentLanes[currentLaneID];

						// increasing lane
						LaneID increasingLaneID1 = new LaneID(segment.Way1.WayID, i + (modifier * 1));
						LaneID increasingLaneID2 = new LaneID(segment.Way2.WayID, i + (modifier * 1));
						if (segmentLanes.ContainsKey(increasingLaneID1))
						{
							if (currentLaneID.WayID.WayNumber == 1)
								currentLane.OnLeft = segmentLanes[increasingLaneID1];
							else
								currentLane.OnRight = segmentLanes[increasingLaneID1];
						}
						else if (segmentLanes.ContainsKey(increasingLaneID2))
						{
							if (currentLaneID.WayID.WayNumber == 1)
								currentLane.OnLeft = segmentLanes[increasingLaneID2];
							else
								currentLane.OnRight = segmentLanes[increasingLaneID2];
						}

						// check for decreasing
						// increasing lane
						increasingLaneID1 = new LaneID(segment.Way1.WayID, i - (modifier * 1));
						increasingLaneID2 = new LaneID(segment.Way2.WayID, i - (modifier * 1));
						if (segmentLanes.ContainsKey(increasingLaneID1))
						{
							if (currentLaneID.WayID.WayNumber == 1)
								currentLane.OnRight = segmentLanes[increasingLaneID1];
							else
								currentLane.OnLeft = segmentLanes[increasingLaneID1];
						}
						else if (segmentLanes.ContainsKey(increasingLaneID2))
						{
							if (currentLaneID.WayID.WayNumber == 1)
								currentLane.OnRight = segmentLanes[increasingLaneID2];
							else
								currentLane.OnLeft = segmentLanes[increasingLaneID2];
						}
						
						if (currentLane.OnLeft != null)
						{
							Console.WriteLine("Lane: " + currentLane.LaneID.ToString() + ". On Left: " + currentLane.OnLeft.LaneID.ToString());
						}

						i++;
						currentLaneID = new LaneID(segment.Way1.WayID, i);

						if (segmentLanes.ContainsKey(new LaneID(segment.Way2.WayID, i)))
							currentLaneID = new LaneID(segment.Way2.WayID, i);
					}
				}
				else
				{
					// HACK
					//throw new Exception("single way lane!");
				}
			}

			return rndf;
		}

		/// <summary>
		/// Determines exit adjacency maps, i.e. where on each lane is an entry adjacent to (with some penalty for number of lane switches)
		/// </summary>
		/// <param name="rndf"></param>
		/// <returns></returns>
		private RndfNetwork DetermineEntryAdjacency(RndfNetwork rndf)
		{
			return rndf;
		}

		/// <summary>
		/// Generates the zone graph for travel in the zone
		/// </summary>
		/// <param name="rndf"></param>
		/// <returns></returns>
		private RndfNetwork CreateZoneGraph(RndfNetwork rndf)
		{
			return rndf;
		}

		/// <summary>
		/// Modifies the zones into the rndf network format
		/// </summary>
		/// <param name="rndf"></param>
		/// <returns></returns>
		private RndfNetwork GenerateRndfNetworkZones(RndfNetwork rndf)
		{
			return rndf;
		}
	}
}
