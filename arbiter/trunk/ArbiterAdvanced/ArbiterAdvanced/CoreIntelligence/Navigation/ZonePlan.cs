using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Zone plan
	/// </summary>
	public class ZonePlan : INavigationalPlan
	{
		/// <summary>
		/// Initial node we entered upon
		/// </summary>
		public INavigableNode Start;

		/// <summary>
		/// the current zone
		/// </summary>
		public ArbiterZone Zone;

		/// <summary>
		/// Recommended path from start to the goal in this zone
		/// </summary>
		public LinePath RecommendedPath;

		public List<INavigableNode> PathNodes;

		public double Time;

		/// <summary>
		/// Goal in this zone we are going to
		/// </summary>
		public INavigableNode ZoneGoal;

		public NavigableEdge GetClosestNavigableEdge(Coordinates c)
		{
			try
			{
				int i = this.RecommendedPath.GetClosestPoint(c).Index;
				Coordinates rpi = this.RecommendedPath[i];
				for (int j = 0; j < PathNodes.Count; j++)
				{
					INavigableNode inn = PathNodes[j];
					if (inn.Position.Equals(rpi))
					{
						foreach (NavigableEdge ne in inn.OutgoingConnections)
						{
							if (ne.End.Equals(PathNodes[j + 1]))
								return ne;
						}
					}
				}
			}
			catch (Exception)
			{
			}

			return null;
		}
	}
}
