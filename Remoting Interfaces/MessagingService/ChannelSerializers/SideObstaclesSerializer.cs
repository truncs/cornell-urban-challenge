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
	/// Deserializes incoming messages from the side sick stuff.	
	/// </summary>
	public static class SideObstaclesSerializer 
	{
		public static void Serialize(Stream stream, Object o) 
		{
			throw new NotImplementedException("You cannot send to the side sick over this channel.");
		}

		public static Object Deserialize(Stream stream, string channelName) 
		{
			BinaryReader br = new BinaryReader(stream);

			SideObstacleMsgID msgtype = (SideObstacleMsgID)br.ReadInt32();
			switch (msgtype) 
			{
				case SideObstacleMsgID.Info:
					Console.WriteLine("SO Info:");
					break;
				case SideObstacleMsgID.Bad:
					Console.WriteLine("SO BAD:");
					break;
				case SideObstacleMsgID.ScanMsg:
					{
						SideObstacles sideObstacles = new SideObstacles();

						sideObstacles.side = (SideObstacleSide)br.ReadInt32();
						sideObstacles.timestamp = br.ReadDouble();
						int numobstacles = br.ReadInt32();
						sideObstacles.obstacles = new List<SideObstacle>();
						for (int i = 0; i < numobstacles; i++)
						{
							SideObstacle obstacle = new SideObstacle();
							obstacle.distance = br.ReadSingle();
							int points = br.ReadInt32();
							obstacle.height = br.ReadSingle();
							sideObstacles.obstacles.Add(obstacle);
						}
						for (int i = numobstacles; i < 10; i++)
						{
							br.ReadSingle();
							br.ReadInt32();
							br.ReadSingle();
						}
						if (br.BaseStream.Position != br.BaseStream.Length)
							Console.WriteLine("WARNING: Incomplete read of side sick msg.");
						
						return sideObstacles;
					}
				default:
					throw new InvalidDataException("Invalid SideObstaclesSerializer Message Received: " + msgtype);
			}
			return null;

		}
	}
}
