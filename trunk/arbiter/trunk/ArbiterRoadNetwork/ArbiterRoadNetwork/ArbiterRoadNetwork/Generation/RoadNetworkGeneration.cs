using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Generates a road network from an IRndf
	/// </summary>
	[Serializable]
	public class RoadNetworkGeneration
	{
		private IRndf xyRndf;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="xyRndf"></param>
		public RoadNetworkGeneration(IRndf xyRndf)
		{
			this.xyRndf = xyRndf;
		}

		/// <summary>
		/// Generate the arbiter road network from the internal xyRndf
		/// </summary>
		/// <returns></returns>
		public ArbiterRoadNetwork GenerateRoadNetwork()
		{
			// the road network we're generating
			ArbiterRoadNetwork arn = new ArbiterRoadNetwork();
			arn.Name = xyRndf.Name;
			arn.CreationDate = xyRndf.CreationDate;

			// if zoens exist
			if (xyRndf.Zones != null)
			{
				// construct zone generator
				ZoneGeneration zg = new ZoneGeneration(xyRndf.Zones);

				// generate zones
				arn = zg.GenerateZones(arn);
			}

			// if segments exist
			if (xyRndf.Segments != null)
			{
				// generate segments
				SegmentGeneration sg = new SegmentGeneration(xyRndf.Segments);

				// generate segments
				arn = sg.GenerateSegments(arn);

			}

			// interconnects
			InterconnectGeneration ig = new InterconnectGeneration(xyRndf);
			arn = ig.GenerateInterconnects(arn);

			// adjacency (lane, partition, entry)
			AdjacencyGeneration ag = new AdjacencyGeneration();
			arn = ag.GenerateAdjacencies(arn);

			// other stuff (vehicle areas)
			arn.GenerateVehicleAreas();

			// return
			return arn;
		}
	}
}
