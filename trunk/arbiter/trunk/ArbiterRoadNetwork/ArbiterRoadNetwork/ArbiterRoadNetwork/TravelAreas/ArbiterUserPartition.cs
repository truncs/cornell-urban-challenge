using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Partition of a Lane partition
	/// </summary>
	[Serializable]
	public class ArbiterUserPartition : IDisplayObject, INetworkObject, IComparable
	{
		#region User Partition Members

		/// <summary>
		/// Unique id of this partition
		/// </summary>
		public ArbiterUserPartitionId PartitionId;

		/// <summary>
		/// Initial Waypoint of the partition
		/// </summary>
		public IGenericWaypoint InitialGeneric;

		/// <summary>
		/// Final waypoint of the partition
		/// </summary>
		public IGenericWaypoint FinalGeneric;

		/// <summary>
		/// Length of the partition
		/// </summary>
		public double Length;

		/// <summary>
		/// Parent lane partition
		/// </summary>
		public IConnectAreaWaypoints Partition;

		public LinePath UserPartitionPath;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		public ArbiterUserPartition(ArbiterUserPartitionId partitionId, IConnectAreaWaypoints partition, IGenericWaypoint initial, IGenericWaypoint final)
		{
			this.PartitionId = partitionId;
			this.InitialGeneric = initial;
			this.FinalGeneric = final;
			this.Length = this.InitialGeneric.Position.DistanceTo(this.FinalGeneric.Position);
			this.Partition = partition;
			this.UserPartitionPath = new LinePath(new Coordinates[] { initial.Position, final.Position });
		}

		public void ReformPath()
		{
			this.UserPartitionPath = new LinePath(new Coordinates[] { this.InitialGeneric.Position, this.FinalGeneric.Position });
		}

		#endregion

		public bool IsInsideClose(Coordinates c)
		{
			if(this.UserPartitionPath == null)
				this.UserPartitionPath = new LinePath(new Coordinates[] { this.InitialGeneric.Position, this.FinalGeneric.Position });

			if (this.Partition is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.Partition;
				LinePath.PointOnPath closest = this.UserPartitionPath.GetClosestPoint(c);
				if (closest.Location.DistanceTo(c) < alp.Lane.Width)
				{
					if (!closest.Equals(this.UserPartitionPath.StartPoint) && !closest.Equals(this.UserPartitionPath.EndPoint))
					{
						return true;
					}
				}
			}

			return false;
		}

		public void InsertUserWaypoint(Coordinates c)
		{			
			if (this.Partition is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.Partition;
				ArbiterUserWaypoint final = null;				

				if (this.InitialGeneric is ArbiterUserWaypoint)
				{
					ArbiterUserWaypoint auw = (ArbiterUserWaypoint)this.InitialGeneric;
					ArbiterUserWaypointId auwi = new ArbiterUserWaypointId(auw.WaypointId.Number + 1, this.Partition.ConnectionId);
					final = new ArbiterUserWaypoint(c, auwi, this.Partition);
				}
				else
				{
					ArbiterUserWaypointId auwi = new ArbiterUserWaypointId(1, this.Partition.ConnectionId);
					final = new ArbiterUserWaypoint(c, auwi, this.Partition);
				}

				foreach (ArbiterUserWaypoint aup in alp.UserWaypoints)
				{
					if (aup.WaypointId.Number >= final.WaypointId.Number)
						aup.WaypointId.Number++;
				}

				ArbiterUserPartition aup1 = new ArbiterUserPartition(
					new ArbiterUserPartitionId(this.Partition.ConnectionId, this.InitialGeneric.GenericId, final.WaypointId),
					this.Partition, this.InitialGeneric, final);
				aup1.FinalGeneric = final;
				aup1.InitialGeneric = this.InitialGeneric;
				final.Previous = aup1;
				if (aup1.InitialGeneric is ArbiterUserWaypoint)
					((ArbiterUserWaypoint)aup1.InitialGeneric).Next = aup1;

				ArbiterUserPartition aup2 = new ArbiterUserPartition(
					new ArbiterUserPartitionId(this.Partition.ConnectionId, final.WaypointId, this.FinalGeneric.GenericId),
					this.Partition, final, this.FinalGeneric);
				aup2.InitialGeneric = final;
				aup2.FinalGeneric = this.FinalGeneric;
				final.Next = aup2;
				if (aup2.FinalGeneric is ArbiterUserWaypoint)
					((ArbiterUserWaypoint)aup2.FinalGeneric).Previous = aup2;

				alp.UserPartitions.Remove(this);
				alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Remove(this);
				alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Add(final);
				alp.UserWaypoints.Add(final);
				alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Add(aup1);
				alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Add(aup2);
				alp.UserPartitions.Add(aup1);
				alp.UserPartitions.Add(aup2);				
				alp.UserPartitions.Sort();
				alp.Lane.ReformPath();
			}
		}

		#region Standard Equalities

		/// <summary>
		/// Check if two zones are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterUserPartition)
			{
				// check if the numbers are equal
				return ((ArbiterUserPartition)obj).PartitionId.Equals(this.PartitionId);
			}

			// otherwise not equal
			return false;
		}

		/// <summary>
		/// Hash code for id
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			// for top levels is just the number
			return this.PartitionId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.PartitionId.ToString();
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterUserPartition, System.Drawing.Drawing2D.DashStyle.Dash, 
				this.InitialGeneric.Position, this.FinalGeneric.Position, g, t); 
		}

		public bool MoveAllowed
		{
			get { return false; }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public SelectionType Selected
		{
			get
			{
				return SelectionType.NotSelected;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { return null; }
		}

		public bool CanDelete
		{
			get { return false; }
		}

		public List<IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			return true;
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawArbiterUserWaypoint &&
				(this.InitialGeneric is ArbiterUserWaypoint || this.FinalGeneric is ArbiterUserWaypoint);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is ArbiterUserPartition)
			{
				ArbiterUserPartition aup = (ArbiterUserPartition)obj;
				if (this.InitialGeneric is ArbiterWaypoint)
					return -1;
				else if (aup.InitialGeneric is ArbiterWaypoint)
					return 1;
				else
				{
					ArbiterUserWaypoint auw1 = (ArbiterUserWaypoint)this.InitialGeneric;
					ArbiterUserWaypoint auw2 = (ArbiterUserWaypoint)aup.InitialGeneric;
					return auw1.WaypointId.Number.CompareTo(auw2.WaypointId.Number);
				}
			}
			else
				return -1;
		}

		#endregion
	}
}
