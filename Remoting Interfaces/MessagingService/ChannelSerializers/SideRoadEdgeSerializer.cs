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
	/// Deserializes incoming messages from the side road edge sensor.	
	/// </summary>
	public static class SideRoadEdgeSerializer 
	{

		public static void Serialize(Stream stream, Object o) 
		{
			throw new NotImplementedException("You cannot send to the side road edge over this channel.");
		}

		public static Object Deserialize(Stream stream, string channelName) 
		{
			BinaryReader br = new BinaryReader(stream);

			SideRoadEdgeMsgID msgtype = (SideRoadEdgeMsgID)br.ReadInt32();
			switch (msgtype) 
			{
				case SideRoadEdgeMsgID.Info:
					Console.WriteLine("SRE Info:");
					break;
				case SideRoadEdgeMsgID.Bad:
					Console.WriteLine("SRE BAD:");
					break;
				case SideRoadEdgeMsgID.RoadEdgeMsg: {
						SideRoadEdge roadEdge = new SideRoadEdge();
						roadEdge.side = (SideRoadEdgeSide)br.ReadInt32();
						roadEdge.timestamp = br.ReadDouble();
						roadEdge.curbHeading = br.ReadDouble();
						roadEdge.curbDistance = br.ReadDouble();
						roadEdge.isValid = br.ReadBoolean();
						roadEdge.probabilityValid = br.ReadDouble();

						return roadEdge;
					}
				default:
					throw new InvalidDataException("Invalid SideRoadEdgeSerializer Message Received: " + msgtype);
			}
			return null;

		}
	}
}
