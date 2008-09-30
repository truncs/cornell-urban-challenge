using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Edge that links two INavigable nodes
	/// </summary>
	[Serializable]
	public class NavigableEdge
	{
		/// <summary>
		/// Sets default speed for any edge of 5mph
		/// </summary>
		private double defaultSpeed = 2.5;

		/// <summary>
		/// Zone a part of
		/// </summary>
		public ArbiterZone Zone;
		public double ZoneEdgeBlockedCost = 0.0;

		/// <summary>
		/// Segment a part of
		/// </summary>
		public ArbiterSegment Segment;

		/// <summary>
		/// Partitions edge contains
		/// </summary>
		public List<IConnectAreaWaypoints> Contained;
		
		/// <summary>
		/// Many things don't apply in a zone
		/// </summary>
		public bool IsZone;

		/// <summary>
		/// Many things don't apply in a segment
		/// </summary>
		public bool IsSegment;

		/// <summary>
		/// initial node
		/// </summary>
		public INavigableNode Start;

		/// <summary>
		/// final node
		/// </summary>
		public INavigableNode End;

		/// <summary>
		/// Standard distance of edge
		/// </summary>
		private double standardEdgeDistance;

		/// <summary>
		/// The default time cost for the edge
		/// </summary>
		private double defaultTimeCost;

		/// <summary>
		/// Directed edge between two INavigableNodes
		/// </summary>
		/// <remarks>Can be made up of multiple partitions or zones</remarks>
		public NavigableEdge(bool isZone, ArbiterZone zone, bool isSegment, ArbiterSegment segment, List<IConnectAreaWaypoints> contained,
			INavigableNode start, INavigableNode end)
		{
			this.Start = start;
			this.End = end;
			this.Segment = segment;
			this.IsSegment = isSegment;
			this.Zone = zone;
			this.IsZone = isZone;
			this.Contained = contained == null ? new List<IConnectAreaWaypoints>() : contained;
			this.standardEdgeDistance = this.Start.Position.DistanceTo(this.End.Position);
		}

		/// <summary>
		/// time cost of the edge
		/// </summary>
		public double TimeCost()
		{
			this.defaultTimeCost = this.CalculateDefaultTimeCost();

			if (IsZone)
			{
				return this.defaultTimeCost + this.ZoneEdgeBlockedCost;
			}
			else if (IsSegment)
			{
				// default cost
				double cost = this.defaultTimeCost;

				// add blockage costs
				foreach (IConnectAreaWaypoints icaw in Contained)
				{
					cost += icaw.Blockage.BlockageCost;
				}

				// final cost
				return cost;
			}
			else
			{
				// default cost
				double cost = this.defaultTimeCost;

				// add blockage costs
				foreach (IConnectAreaWaypoints icaw in Contained)
				{
					cost += icaw.Blockage.BlockageCost;
				}

				// return 
				return cost;
			}
		}

		/// <summary>
		/// Generates the default time cost for the edge
		/// </summary>
		/// <returns></returns>
		public double CalculateDefaultTimeCost()
		{
			// add extra costs of the end of this edge
			double cost = End.ExtraTimeCost;

			if (IsZone)
			{
				// zone returns justdistance of edge divided by the zone's minimum speed
				cost += this.standardEdgeDistance / Math.Max(NavigationPenalties.ZoneMinSpeedDefault, this.Zone.SpeedLimits.MinimumSpeed);
				return cost;
			}
			else if (IsSegment)
			{
				// default cost of segment edge
				cost += this.standardEdgeDistance / this.Segment.SpeedLimits.MaximumSpeed;

				// time costs of adjacent
				foreach (IConnectAreaWaypoints icaw in this.Contained)
				{
					if (!icaw.Equals(this))
					{
						// add changing lanes cost
						cost += NavigationPenalties.ChangeLanes;

						if (icaw is NavigableEdge)
						{
							cost += ((NavigableEdge)icaw).TimeCost();
						}
					}
				}

				// interconnect if stop
				if (Contained.Count > 0 && Contained[0] is ArbiterLanePartition)
				{
					ArbiterLanePartition alp = (ArbiterLanePartition)Contained[0];

					if (alp.Type == PartitionType.Sparse)
					{
						cost += this.standardEdgeDistance * 2.0;
					}

					if (alp.Initial.IsStop)
					{
						ArbiterInterconnect ai = alp.ToInterconnect;
						cost += ai.ExtraCost - NavigationPenalties.Interconnect;
					}
				} 

				// cost of partition by default
				return cost;
			}
			else
			{
				// default cost
				cost += this.standardEdgeDistance / this.defaultSpeed;

				// interconnect
				if (Contained.Count > 0 && Contained[0] is ArbiterInterconnect)
				{
					ArbiterInterconnect ai = (ArbiterInterconnect)Contained[0];
					cost = this.standardEdgeDistance / ai.MaximumDefaultSpeed + ai.ExtraCost;
				}

				// return 
				return cost;
			}
		}

		/// <summary>
		/// Equality override
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is NavigableEdge)
			{
				NavigableEdge other = (NavigableEdge)obj;
				return other.Start.Equals(this.Start) && other.End.Equals(this.End);
			}
			else
				return false;
		}

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Start.GetHashCode() + End.GetHashCode();
		}

		public NavigableEdge Reverse()
		{
			return new NavigableEdge(this.IsZone, this.Zone, this.IsSegment, this.Segment, this.Contained, this.End, this.Start);
		}

		public override string ToString()
		{
			string s = "";
			foreach (NavigableEdge ne in this.Contained)
			{
				s += ne.ToString();
			}
			return s;
		}
	}
}
