using System;
using System.Collections.Generic;
using System.Text;
using ArbiterTools.Data;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;

namespace ArbiterTools.Tools
{
	/// <summary>
	/// Analyzes a point relative to an rndf
	/// </summary>
	public static class PointAnalysis
	{
		/// <summary>
		/// Analyzes a lane to find the closest point to the coordinate passed in
		/// </summary>
		/// <param name="coordinate">coordinate to analyze the lane against</param>
		/// <param name="lane">lane to reference the coordinate to</param>
		/// <returns>vector consisting of relative and absolute rndf localization information</returns>
		public static LocationAnalysis ClosestPartitionOnLane(Coordinates coordinate, LaneID laneId, RndfNetwork rndfNetwork)
		{
			// get the lane
			Lane lane = rndfNetwork.Segments[laneId.WayID.SegmentID].Ways[laneId.WayID].Lanes[laneId];

			// set the value to return
			LocationAnalysis closest = null;

			// values for comparing partitions
			double offsetMin = Double.MaxValue;

			// iterate over lanePartitions within the lane
			foreach (LanePartition lanePartition in lane.LanePartitions)
			{
				// if there are user partitions, operate over them
				if (lanePartition.UserPartitions != null && lanePartition.UserPartitions.Count > 1)
				{
					foreach (UserPartition userPartiton in lanePartition.UserPartitions)
					{
						throw new Exception("user partition relation to vehicle absolute coordinates not implemented yet");
					}
				}
				// otherwise look at how close lane partition is
				else
				{
					// analyze the partition
					LocationAnalysis partitionAnalysis = AnalyzePartition(coordinate, (IWaypoint)lanePartition.InitialWaypoint, (IWaypoint)lanePartition.FinalWaypoint);

					// if this partition has less of an offset from the vehicle than the current best, set as current
					if (partitionAnalysis.Offset < offsetMin)
					{
						offsetMin = partitionAnalysis.Offset;
						closest = partitionAnalysis;

						// set partition of result
						closest.Partition = lanePartition;
					}
					// otherwise, if the vehicle is relatively close to this partition 
					// and the closest Coordinates are not those of the initial final waypoitns of the lane
					// and this error is less than the current best offset
					else if (partitionAnalysis.Offset == 1234567
						&& (partitionAnalysis.RelativeRndfPosition != lane.LanePartitions[0].InitialWaypoint.Position)
						&& (partitionAnalysis.RelativeRndfPosition != lane.LanePartitions[lane.LanePartitions.Count - 1].FinalWaypoint.Position)
						&& (partitionAnalysis.Error < offsetMin))
					{
						offsetMin = partitionAnalysis.Error;
						closest = partitionAnalysis;

						// set partition of result
						closest.Partition = lanePartition;
					}
				}
			}

			// return closest value found
			return closest;
		}

		/// <summary>
		/// Gets the closest relative rndf position to any LANE in the rndf
		/// </summary>
		/// <param name="coordinate"></param>
		/// <param name="rndf"></param>
		/// <returns></returns>
		public static LocationAnalysis GetClosestLanePartition(Coordinates coordinate, RndfNetwork rndfNetwork)
		{
			// set the value to return
			LocationAnalysis closest = null;

			// values for comparing partitions
			double offsetMin = Double.MaxValue;

			foreach (Segment s in rndfNetwork.Segments.Values)
			{
				foreach (Way w in s.Ways.Values)
				{
					foreach (Lane lane in w.Lanes.Values)
					{
						// iterate over lanePartitions within the lane
						foreach (LanePartition lanePartition in lane.LanePartitions)
						{
							// if there are user partitions, operate over them
							if (lanePartition.UserPartitions != null && lanePartition.UserPartitions.Count > 1)
							{
								foreach (UserPartition userPartiton in lanePartition.UserPartitions)
								{
									throw new Exception("user partition relation to vehicle absolute coordinates not implemented yet");
								}
							}
							// otherwise look at how close lane partition is
							else
							{
								// analyze the partition
								LocationAnalysis partitionAnalysis = AnalyzePartition(coordinate, (IWaypoint)lanePartition.InitialWaypoint, (IWaypoint)lanePartition.FinalWaypoint);

								// if this partition has less of an offset from the vehicle than the current best, set as current
								if (partitionAnalysis.Offset < offsetMin)
								{
									offsetMin = partitionAnalysis.Offset;
									closest = partitionAnalysis;

									// set partition of result
									closest.Partition = lanePartition;
								}
								// otherwise, if the vehicle is relatively close to this partition 
								// and the closest Coordinates are not those of the initial final waypoitns of the lane
								// and this error is less than the current best offset
								else if (partitionAnalysis.Offset == 1234567
									&& (partitionAnalysis.RelativeRndfPosition != lane.LanePartitions[0].InitialWaypoint.Position)
									&& (partitionAnalysis.RelativeRndfPosition != lane.LanePartitions[lane.LanePartitions.Count - 1].FinalWaypoint.Position)
									&& (partitionAnalysis.Error < offsetMin))
								{
									offsetMin = partitionAnalysis.Error;
									closest = partitionAnalysis;

									// set partition of result
									closest.Partition = lanePartition;
								}
							}
						}
					}
				}
			}

			// return closest value found
			return closest;
		}


		/// <summary>
		/// gets position on a generic partition
		/// </summary>
		/// <param name="coordinate"></param>
		/// <param name="laneParition"></param>
		/// <returns></returns>
		private static LocationAnalysis getPartitionState(Coordinates coordinate, IConnectWaypoints lanePartition)
		{
			// analyze the partition
			LocationAnalysis partitionAnalysis = AnalyzePartition(coordinate, (IWaypoint)lanePartition.InitialWaypoint, (IWaypoint)lanePartition.FinalWaypoint);

			// set the value to return
			LocationAnalysis closest = null;

			// values for comparing partitions
			double offsetMin = Double.MaxValue;

			// if this partition has less of an offset from the vehicle than the current best, set as current
			if (partitionAnalysis.Offset < offsetMin)
			{
				offsetMin = partitionAnalysis.Offset;
				closest = partitionAnalysis;

				// set partition of result
				closest.Partition = lanePartition;
			}
			// otherwise, if the vehicle is relatively close to this partition 
			// and the closest Coordinates are not those of the initial final waypoitns of the lane
			// and this error is less than the current best offset
			else if (partitionAnalysis.Offset == 1234567
				&& (partitionAnalysis.Error < offsetMin))
			{
				offsetMin = partitionAnalysis.Error;
				closest = partitionAnalysis;

				// set partition of result
				closest.Partition = lanePartition;
			}

			// return estimate
			return partitionAnalysis;
		}

		/// <summary>
		/// Analyzes a specific partition to set closeness values
		/// </summary>
		/// <param name="coordinate">coordinate to analyze the lane partition</param>
		/// <param name="initial">initial waypoint of the partition</param>
		/// <param name="final">final waypoint of the partition</param>
		/// <returns>vector consisting of relative and absolute partition localization information</returns>
		/// <remarks>
		/// In analyzing partitions, the nearest point might be one of the endpoints of the
		/// partition, rather than the closest point on the line. Defining the vehicle's absolutePosition as C,
		/// the initial waypoint of the partition as A and the final Waypoint of the partition as B, 
		/// the closest point to C on the line defined by A and B is might not 
		/// be on the segment AB, so the point closest to C is B or A.
		/// While there are a few different ways to check for this special case,
		/// one way is to apply the dot product. 
		/// 
		/// First, check to see if the nearest
		/// point on the line AB is beyond B (as in the example above) by taking
		/// AB dot BC. If this value is greater than 0, it means that the angle
		/// between AB and BC is between -90 and 90, exclusive, and therefore
		/// the nearest point on the segment AB will be B. Similarly, if BA dot AC
		/// is greater than 0, the nearest point is A. If both dot products are
		/// negative, then the nearest point to C is somewhere along the partiton.
		/// </remarks>
		private static LocationAnalysis AnalyzePartition(Coordinates coordinate, IWaypoint initial, IWaypoint final)
		{
			#region Check if coordinates closest absolutePosition on partition is within the partition

			// Check if coordinate is outside of B			
			Coordinates AB = final.Position - initial.Position;
			Coordinates BC = coordinate - final.Position;
			bool closestB = AB.Dot(BC) > 0;

			// Check if coordinate is outside of A
			Coordinates BA = initial.Position - final.Position;
			Coordinates AC = coordinate - initial.Position;
			bool closestA = BA.Dot(AC) > 0;

			// Check if coordinate is inside segment (not outside of A or B)
			bool insidePartition = !closestA && !closestB;

			#endregion

			// if in partition
			if (insidePartition)
			{
				// get unit vector of partition
				Coordinates unitPartition = AB / AB.Length;

				// project coordinate onto the partition
				Coordinates deltaProjection = AC.Dot(unitPartition) * unitPartition;

				// get distance along partition
				double distance = deltaProjection.Length;

				// get final point
				Coordinates closestPoint = initial.Position + deltaProjection;

				// get offset
				Coordinates difference = coordinate - closestPoint;
				double offset = difference.Length;

				// set the return vector
				return (new LocationAnalysis(distance, null, initial, final, offset, closestPoint, 0));
			}
			else if (closestA)
			{
				// closest point is initial
				Coordinates closestPoint = initial.Position;

				// distance along is 0 as initial point
				double distanceAlong = 0;

				// offset is 1234567 as not perfectly offset
				double offset = 1234567;

				// calculate error
				double errorSize = AC.Length;

				// set the return vector
				return (new LocationAnalysis(distanceAlong, null, initial, final, offset, closestPoint, errorSize));
			}
			else if (closestB)
			{
				// closest point is initial
				Coordinates closestPoint = final.Position;

				// distance along is 0 as initial point
				double distanceAlong = 0;

				// offset is 1234567 as not perfectly offset
				double offset = 1234567;

				// calculate error
				double errorSize = BC.Length;

				// set the return vector
				return (new LocationAnalysis(distanceAlong, null, initial, final, offset, closestPoint, errorSize));
			}

			return null;
		}
	}
}
