using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common;
using System.Diagnostics;

namespace UrbanChallenge.MessagingService.ChannelSerializers
{
	/// <summary>
	/// Deserializes incoming messages from the Scene (skeet) Estimator.	
	/// </summary>
	public static class SceneEstimatorSerializer
	{
		private static BinaryFormatter bf;
		private static MemoryStream trackedClusterStorage = new MemoryStream();
		private static int expChunkNum = 0;
		private static object trackStorageLock = new object();
		public static int badPackets = 0;
		public static int goodPackets = 0;
		public static int totPackets = 0;
		static SceneEstimatorSerializer()
		{
			bf = new BinaryFormatter();
		}
		public static void Serialize(Stream stream, Object o)
		{
			throw new NotImplementedException("You cannot send to the scene estimator over this channel.");
		}

		public static Object Deserialize(Stream stream, string channelName)
		{
			BinaryReader br = new BinaryReader(stream);

			SceneEstimatorMessageID msgtype = (SceneEstimatorMessageID)br.ReadInt32();
			switch (msgtype)
			{
				case SceneEstimatorMessageID.SCENE_EST_Info:
					Console.WriteLine("SE Info:");
					break;
				case SceneEstimatorMessageID.SCENE_EST_Bad:
					Console.WriteLine("SE BAD:");
					break;
				case SceneEstimatorMessageID.SCENE_EST_ParticleRender:
					break;
				case SceneEstimatorMessageID.SCENE_EST_PositionEstimate:
					break;
				case SceneEstimatorMessageID.SCENE_EST_Stopline:
					break;
				case SceneEstimatorMessageID.SCENE_EST_TrackedClusters:
					{
						if (channelName != SceneEstimatorObstacleChannelNames.TrackedClusterChannelName &&
							channelName != SceneEstimatorObstacleChannelNames.AnyClusterChannelName)
						{
							break;
						}
						int chunkNum = (int)br.ReadByte();
						int numTotChunks = (int)br.ReadByte();
						totPackets++;
						if (chunkNum==0) //first packet...
						{
							if (expChunkNum != chunkNum) badPackets++;
							int lenToRead =(int)( br.BaseStream.Length - br.BaseStream.Position);
							lock (trackStorageLock)
							{
								trackedClusterStorage = new MemoryStream(65000);
								trackedClusterStorage.Write(br.ReadBytes(lenToRead), 0, lenToRead);
							}
							if (chunkNum == numTotChunks-1) //we are done
							{
								expChunkNum = 0;
								//Console.WriteLine("Got Single Frame Packet OK " + " of " + numTotChunks);
								Object o = null;
								lock (trackStorageLock)
								{
									try
									{
										o = ProcessTrackedClusterMsg(new BinaryReader(trackedClusterStorage));
									}
									catch (Exception ex)
									{
										Console.WriteLine("EXCEPTION: " + ex.Message);
									}
									goodPackets++;
								}
								if (o != null)	return o;
							}
							else
							{								
								expChunkNum = 1;
								Console.WriteLine("Got Mulii Frame Packet " + expChunkNum + " of " + numTotChunks);
							}
						}
						else if (chunkNum == expChunkNum) //nth packet....
						{
							lock (trackStorageLock)
							{
								int lenToRead = (int)(br.BaseStream.Length - br.BaseStream.Position);
								trackedClusterStorage.Write(br.ReadBytes(lenToRead), 0, lenToRead);
							}
							if (chunkNum == numTotChunks-1) //we are done
							{
								expChunkNum = 0;
								Object o = null;
								lock (trackStorageLock)
								{
									Console.WriteLine("Got Mulii Frame Packet OK Len:" + trackedClusterStorage.Length + " Chunks " + numTotChunks);
									try
									{
										o = ProcessTrackedClusterMsg(new BinaryReader(trackedClusterStorage));
									}
									catch (Exception ex)
									{
										Console.WriteLine("EXCEPTION: " + ex.Message);
									}
									trackedClusterStorage = new MemoryStream(65000);
									goodPackets++;
								}
								return o;
							}
							else
							{								
								expChunkNum++;
								Console.WriteLine("Got Mulii Frame Packet " + expChunkNum + " of " + numTotChunks);
							}
						}
						else //misaligned packet! yikes
						{
							lock (trackStorageLock)
							{
								trackedClusterStorage = new MemoryStream();
							}
							expChunkNum = 0;
							badPackets++;
							Console.WriteLine("Bad Packet! ChunkNum: " + chunkNum + " ExpChunkNum: " + expChunkNum + " Total: " + numTotChunks);
						}

						if (totPackets % 100 == 0)
						{
							Console.WriteLine("PACKET STATS: GOOD: " + goodPackets + " BAD: " + badPackets);
						}
						break;
					}
				case SceneEstimatorMessageID.SCENE_EST_UntrackedClusters:
					{
						if (channelName != SceneEstimatorObstacleChannelNames.UntrackedClusterChannelName &&
								channelName != SceneEstimatorObstacleChannelNames.AnyClusterChannelName)
						{
							break;
						}

						SceneEstimatorUntrackedClusterCollection ucc = new SceneEstimatorUntrackedClusterCollection();
						int numClusters = br.ReadInt32();


						ucc.clusters = new SceneEstimatorUntrackedCluster[numClusters];
						ucc.timestamp = br.ReadDouble();
						for (int i = 0; i < numClusters; i++)
						{
							int numPoints = br.ReadInt32();
							ucc.clusters[i].clusterClass = (SceneEstimatorClusterClass)br.ReadInt32();
							ucc.clusters[i].points = new Coordinates[numPoints];
							for (int j = 0; j < numPoints; j++)
							{
								Int16 tmpx = br.ReadInt16();
								Int16 tmpy = br.ReadInt16();
								ucc.clusters[i].points[j].X = (double)tmpx / 100.0;
								ucc.clusters[i].points[j].Y = (double)tmpy / 100.0;
							}
						}
						if (br.BaseStream.Position != br.BaseStream.Length)
						{
							Console.WriteLine("Warning: Incomplete parse of received untracked cluster message. length is " + br.BaseStream.Length + ", go to " + br.BaseStream.Position + ".");
						}
						return ucc;
					}
				case (SceneEstimatorMessageID)LocalRoadModelMessageID.LOCAL_ROAD_MODEL:
					if ((channelName != LocalRoadModelChannelNames.LocalRoadModelChannelName) && (channelName != LocalRoadModelChannelNames.LMLocalRoadModelChannelName))
						break;

					LocalRoadModel lrm = new LocalRoadModel();
					lrm.timestamp = br.ReadDouble();
					lrm.probabilityRoadModelValid = br.ReadSingle();
					lrm.probabilityCenterLaneExists = br.ReadSingle();
					lrm.probabilityLeftLaneExists = br.ReadSingle();
					lrm.probabilityRightLaneExists = br.ReadSingle();

					lrm.laneWidthCenter = br.ReadSingle();
					lrm.laneWidthCenterVariance = br.ReadSingle();
					lrm.laneWidthLeft = br.ReadSingle();
					lrm.laneWidthLeftVariance = br.ReadSingle();
					lrm.laneWidthRight = br.ReadSingle();
					lrm.laneWidthRightVariance = br.ReadSingle();

					int numCenterPoints = br.ReadInt32();
					int numLeftPoints = br.ReadInt32();
					int numRightPoints = br.ReadInt32();

					lrm.LanePointsCenter = new Coordinates[numCenterPoints];
					lrm.LanePointsCenterVariance = new float[numCenterPoints];
					lrm.LanePointsLeft = new Coordinates[numLeftPoints];
					lrm.LanePointsLeftVariance = new float[numLeftPoints];
					lrm.LanePointsRight = new Coordinates[numRightPoints];
					lrm.LanePointsRightVariance = new float[numRightPoints];

					for (int i = 0; i < numCenterPoints; i++)
					{
						//fixed point conversion!
						Int16 x = br.ReadInt16();
						Int16 y = br.ReadInt16();
						UInt16 var = br.ReadUInt16();
						lrm.LanePointsCenter[i] = new Coordinates((double)x / 100.0, (double)y / 100.0);
						lrm.LanePointsCenterVariance[i] = ((float)var / 100.0f) * ((float)var / 100.0f);
					}
					for (int i = 0; i < numLeftPoints; i++)
					{
						//fixed point conversion!
						Int16 x = br.ReadInt16();
						Int16 y = br.ReadInt16();
						UInt16 var = br.ReadUInt16();
						lrm.LanePointsLeft[i] = new Coordinates((double)x / 100.0, (double)y / 100.0);
						lrm.LanePointsLeftVariance[i] = ((float)var / 100.0f) * ((float)var / 100.0f);
					}
					for (int i = 0; i < numRightPoints; i++)
					{
						//fixed point conversion!
						Int16 x = br.ReadInt16();
						Int16 y = br.ReadInt16();
						UInt16 var = br.ReadUInt16();
						lrm.LanePointsRight[i] = new Coordinates((double)x / 100.0, (double)y / 100.0);
						lrm.LanePointsRightVariance[i] = ((float)var / 100.0f) * ((float)var / 100.0f);
					}
					int offset = (LocalRoadModel.MAX_LANE_POINTS * 3) - (numCenterPoints + numRightPoints + numLeftPoints);
					offset *= 6; //this is just the size of each "point"
					if ((br.BaseStream.Position + offset) != br.BaseStream.Length)
					{
						Console.WriteLine("Warning: Incomplete parse of received local road model message. length is " + br.BaseStream.Length + ", go to " + br.BaseStream.Position + ".");
					}
					return lrm;
				default:
					throw new InvalidDataException("Invalid Scene Estimator Message Received: " + msgtype);
			}
			return null;

		}

		private static Object ProcessTrackedClusterMsg(BinaryReader br)
		{
			br.BaseStream.Position = 0;
			SceneEstimatorTrackedClusterCollection tcc = new SceneEstimatorTrackedClusterCollection();
			int numClusters = br.ReadInt32();
			tcc.clusters = new SceneEstimatorTrackedCluster[numClusters];
			tcc.timestamp = br.ReadDouble();
			Dictionary<SceneEstimatorTrackedCluster, List<Pair<UInt16, UInt16>>> partitions = new Dictionary<SceneEstimatorTrackedCluster, List<Pair<ushort, ushort>>>();
			for (int i = 0; i < numClusters; i++)
			{
				tcc.clusters[i] = new SceneEstimatorTrackedCluster();
				int numPoints = br.ReadInt32();
				tcc.clusters[i].relativePoints = new Coordinates[numPoints];
				int numClosestPartitions = br.ReadInt32();
				tcc.clusters[i].closestPartitions = new SceneEstimatorClusterPartition[numClosestPartitions];
				double closestX = (double)br.ReadSingle();
				double closestY = (double)br.ReadSingle();
				tcc.clusters[i].closestPoint = new Coordinates(closestX, closestY);
				tcc.clusters[i].speed = br.ReadSingle();
				tcc.clusters[i].speedValid = br.ReadBoolean();
				tcc.clusters[i].relativeheading = br.ReadSingle();
				tcc.clusters[i].absoluteHeading = br.ReadSingle();
				tcc.clusters[i].headingValid = br.ReadBoolean();
				tcc.clusters[i].targetClass = (SceneEstimatorTargetClass)br.ReadInt32();
				tcc.clusters[i].id = br.ReadInt32();
				tcc.clusters[i].statusFlag = (SceneEstimatorTargetStatusFlag)br.ReadInt32();
				tcc.clusters[i].isStopped = br.ReadBoolean();
				List<Pair<UInt16, UInt16>> partition = new List<Pair<ushort, ushort>>();
				//create the partitions (still mapped)
				for (int j = 0; j < numClosestPartitions; j++)
				{
					UInt16 partHashID = br.ReadUInt16();
					UInt16 partProbFP = br.ReadUInt16();
					partition.Add(new Pair<ushort, ushort>(partHashID, partProbFP));
				}
				partitions.Add(tcc.clusters[i], partition);
				//create the points
				for (int j = 0; j < numPoints; j++)
				{
					Int16 tmpx = br.ReadInt16();
					Int16 tmpy = br.ReadInt16();
					tcc.clusters[i].relativePoints[j].X = (double)tmpx / 100.0;
					tcc.clusters[i].relativePoints[j].Y = (double)tmpy / 100.0;
				}
			}
			//now get the map
			int mapsize = br.ReadInt32();
			Dictionary<UInt16, string> map = new Dictionary<ushort, string>();
			for (int i = 0; i < mapsize; i++)
			{
				string s = br.ReadString();
				UInt16 id = br.ReadUInt16();
				map.Add(id, s);
			}
			//now reprocess the clusters and populate their closest partitions from the map we made
			for (int i = 0; i < numClusters; i++)
			{
				if (partitions.ContainsKey(tcc.clusters[i]) == false) continue;
				int numPart = partitions[tcc.clusters[i]].Count;
				tcc.clusters[i].closestPartitions = new SceneEstimatorClusterPartition[numPart];
				int j = 0;
				SceneEstimatorTrackedCluster clust = tcc.clusters[i];
				List<Pair<UInt16, UInt16>> death = partitions[clust];
				foreach (Pair<UInt16, UInt16> entry in death)
				{
					//the pair is HASH ID and PROB(fp)
					if (map.ContainsKey(entry.Left) == false) throw new InvalidDataException("Recieved an Invalid Mapping for Closest Partitions. Fatal.");
					tcc.clusters[i].closestPartitions[j].partitionID = map[entry.Left];
					tcc.clusters[i].closestPartitions[j].probability = (float)entry.Right / 65535.0f;
					if (tcc.clusters[i].closestPartitions[j].probability > 1.0f)
					{
						Console.WriteLine("Warning: Overflow on probability of partition: " + tcc.clusters[i].closestPartitions[j].probability.ToString());
						tcc.clusters[i].closestPartitions[j].probability = 1.0f;
					}
					else if (tcc.clusters[i].closestPartitions[j].probability < 0.0f)
					{
						Console.WriteLine("Warning: Underflow on probability of partition: " + tcc.clusters[i].closestPartitions[j].probability.ToString());
						tcc.clusters[i].closestPartitions[j].probability = 0.0f;
					}
					//if (tcc.clusters[i].closestPartitions[j].probability != 0)
					//	Console.WriteLine("stuff! " + tcc.clusters[i].closestPartitions[j].probability.ToString());
					j++;
				}
			}
			if (br.BaseStream.Position != br.BaseStream.Length)
			{
				Console.WriteLine("Warning: Incomplete parse of received tracked cluster message. length is " + br.BaseStream.Length + ", go to " + br.BaseStream.Position + ".");
			}
			return tcc;
		}
	}
}
