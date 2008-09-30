

namespace UrbanChallenge.MessagingService
{
	/// <summary>
	/// Defines how the message on this channel will be deserialized. By default we use the BinarySerializer.
	/// </summary>
	public enum ChannelSerializerInfo : byte
	{
		BinarySerializer = 0,
		PoseSerializer = 1,
		LocalMapPointsSerializer = 2,
		SceneEstimatorSerializer = 3,
		PoseRelativeSerializer = 4,
		PoseAbsoluteSerializer = 5,
		SideObstacleSerializer = 6,
		SideRoadEdgeSerializer = 7,
		RoadBearing = 8,
		ExplicitlyUnsupported = 100,
		TestSerializer = 99
	}
}
