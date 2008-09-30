using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors
{
	public enum SceneEstimatorMessageID : int
	{
		SCENE_EST_Info = 0,
		SCENE_EST_PositionEstimate = 1,
		SCENE_EST_Stopline = 2,
		SCENE_EST_ParticleRender = 3,
		SCENE_EST_TrackedClusters = 4,
		SCENE_EST_UntrackedClusters = 5,		
		SCENE_EST_Bad = 99
	}

	public static class SceneEstimatorObstacleChannelNames {
		public const string TrackedClusterChannelName = "ObservedVehicleChannel";
		public const string UntrackedClusterChannelName = "ObservedObstacleChannel";
		public const string AnyClusterChannelName = "SceneEstimatorObstacleChannel";
	}

	#region Tracked/Untracked Obstacles
	/// <summary>
	/// Indicates the current state of the target being active or deleted
	/// </summary>
	public enum SceneEstimatorTargetStatusFlag : int
	{
		/// <summary>
		/// Target is actively being tracked so cluster ids will be consistant
		/// </summary>
		TARGET_STATE_ACTIVE = 1,
		/// <summary>
		/// Marks that this target should be deleted so that its id may be recycled
		/// </summary>
		TARGET_STATE_DELETED = 2,
		/// <summary>
		/// Track is occluded by some other track on both sides
		/// </summary>
		TARGET_STATE_OCCLUDED_FULL = 3,
        /// <summary>
        /// Track is occluded by some track on only one side
        /// </summary>
        TARGET_STATE_OCCLUDED_PART = 4

	}

	/// <summary>
	/// Indicates the type of target
	/// </summary>
	public enum SceneEstimatorTargetClass : int
	{
		TARGET_CLASS_UNKNOWN = -1,
		TARGET_CLASS_CARLIKE = 1,
		TARGET_CLASS_NOTCARLIKE = 2
	}

	public enum SceneEstimatorClusterClass : int
	{
		SCENE_EST_LowObstacle = 0,
		SCENE_EST_HighObstacle = 1
	}

	/// <summary>
	/// Represents the partition a cluster is on
	/// </summary>
	/// <remarks>Aaron says: "don't serialize this stuff on the car</remarks>
	[Serializable]
	public struct SceneEstimatorClusterPartition
	{
		/// <summary>
		/// A unique identifier for this partition in the RNDF
		/// </summary>
		public string partitionID;
		/// <summary>
		/// Probability the cluster is on this partition (0 to 1)
		/// </summary>
		public float probability;
	}

	/// <summary>
	/// Clusters that are currently being tracked by the Scene Estimator. IDs are consistant.
	/// </summary>
	/// <remarks>Aaron says: "don't serialize this stuff on the car</remarks>
	[Serializable]
	public class SceneEstimatorTrackedCluster
	{
		/// <summary>
		/// RNDF coordinates of the point with closest range
		/// </summary>
		public Coordinates closestPoint;
		/// <summary>
		/// Indicates the tracked cluster is not moving
		/// </summary>
		public bool isStopped;
		/// <summary>
		/// Absolute Speed of the tracked cluster 
		/// </summary>
		public float speed;
		/// <summary>
		/// Absolute Heading in RADIANS of the tracked cluster. Check validity with the headingValid flag.
		/// </summary>
		public bool speedValid;
		/// <summary>
		/// Indicates the speed is valid and concurrently shows that Frank is stuff.
		/// </summary>
		public float absoluteHeading;
		/// <summary>
		/// Relative Heading in RADIANS of the tracked cluster. Check validity with the headingValid flag.
		/// </summary>
		public float relativeheading;
		/// <summary>
		/// Indicates when heading is valid.
		/// </summary>
		public bool headingValid;
		/// <summary>
		/// class of the tracked cluster
		/// </summary>
		public SceneEstimatorTargetClass targetClass;
		/// <summary>
		/// Consistant ID of tracked clusters across frames
		/// </summary>
		public int id;
		/// <summary>
		/// Indicates the location of the vehicle on the RNDF
		/// </summary>
		public SceneEstimatorClusterPartition[] closestPartitions;
		/// <summary>
		/// State of the tracked cluster
		/// </summary>
		public SceneEstimatorTargetStatusFlag statusFlag;
		/// <summary>
		/// The raw points coming from the sensors (fake and real) RELATIVE
		/// </summary>
		public Coordinates[] relativePoints;
		

		/// <summary>
		/// Gets absolute points from the relative points
		/// </summary>
		public Coordinates[] AbsolutePoints
		{
			get
			{
				throw new NotImplementedException("Someone else can write this");				
			}
		}

		/// <summary>
		/// String representation of id of tracked cluster
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return id.ToString();
		}
	}

	/// <summary>
	/// Loose Clusters not being tracked by the Scene Estimator. There are no IDs.
	/// </summary>
	/// <remarks>Aaron says: "don't serialize this stuff on the car</remarks>
	[Serializable]
	public struct SceneEstimatorUntrackedCluster
	{
		public SceneEstimatorClusterClass clusterClass;
		public Coordinates[] points;		
	}

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>Aaron says: "don't  this stuff on the car</remarks>
	[Serializable]
	public class SceneEstimatorUntrackedClusterCollection
	{
		public double timestamp;
		public SceneEstimatorUntrackedCluster[] clusters;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>Aaron says: "don't  serialize this stuff on the car</remarks>
	[Serializable]
	public class SceneEstimatorTrackedClusterCollection
	{
		public double timestamp;
		public SceneEstimatorTrackedCluster[] clusters;
	}
	#endregion
}
