using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using RndfEditor.Display.Utilities;
using System.Drawing;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// User Waypoint
	/// </summary>
	[Serializable]
	public class ArbiterUserWaypoint : IGenericWaypoint, IDisplayObject, INetworkObject
	{
		#region Waypoint Members

		private Coordinates position;

		/// <summary>
		/// Partition the waypoint is a part of
		/// </summary>
		public IConnectAreaWaypoints Partition;

		/// <summary>
		/// Id of the waypoint
		/// </summary>
		public ArbiterUserWaypointId WaypointId;

		/// <summary>
		/// The next user partition
		/// </summary>
		public ArbiterUserPartition Next;

		/// <summary>
		/// Previous user partition
		/// </summary>
		public ArbiterUserPartition Previous;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position"></param>
		/// <param name="waypointId"></param>
		/// <param name="partition"></param>
		public ArbiterUserWaypoint(Coordinates position, ArbiterUserWaypointId waypointId, IConnectAreaWaypoints partition)
		{
			this.position = position;
			this.WaypointId = waypointId;
			this.Partition = partition;
		}

		#endregion

		#region IGenericWaypoint Members

		/// <summary>
		/// Position of user waypoint
		/// </summary>
		public Coordinates Position
		{
			get
			{
				return this.position;
			}
			set
			{
				this.position = value;
			}
		}

		/// <summary>
		/// generic id
		/// </summary>
		public object GenericId
		{
			get { return this.WaypointId; }
		}

		#endregion

		#region IDisplayObject Members

		SelectionType selected = SelectionType.NotSelected;

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			// Determine size of bounding box
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = DrawingUtility.cp_large_size;

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)this.position.X - scaled_size / 2, (float)this.position.Y - scaled_size / 2, scaled_size, scaled_size);

			// return
			return rect;
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			// check filter
			if (filter.Target == null || filter.Target is ArbiterUserWaypoint)
			{
				// get bounding box dependent on tolerance
				RectangleF bounding = this.GetBoundingBox(wt);
				bounding.Inflate(tol, tol);

				// check if contains point
				if (bounding.Contains(DrawingUtility.ToPointF(loc)))
				{
					return new HitTestResult(this, true, (float)loc.DistanceTo(this.Position));
				}
			}

			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			Color c;

			if (this.selected == SelectionType.NotSelected)
			{
				c = DrawingUtility.ColorArbiterUserWaypoint;
			}
			else
			{
				c = DrawingUtility.ColorArbiterUserWaypointSelected;
			}

			if (DrawingUtility.DisplayArbiterWaypointId)
			{
				DrawingUtility.DrawControlPoint(this.position, c, this.WaypointId.ToString(), ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
			}
			else
			{
				DrawingUtility.DrawControlPoint(this.position, c, null, ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
			}
		}

		public bool MoveAllowed
		{
			get { return true; }
		}

		public void BeginMove(Coordinates orig, WorldTransform t)
		{	
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			this.position = orig + offset;
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			this.position = orig + offset;
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
		{
			this.position = orig;
		}

		public SelectionType Selected
		{
			get
			{
				return selected;
			}
			set
			{
				selected = value;
			}
		}

		public IDisplayObject Parent
		{
			get { return null; }
		}

		public bool CanDelete
		{
			get { return true; }
		}

		/// <summary>
		/// Delete this waypoint, should remove all old user partitions from the display objects and reform after rehash
		/// </summary>
		public List<IDisplayObject> Delete()
		{
			return null;
			//this.Previous = new ArbiterUserPartition(
			//	new ArbiterUserPartitionId(this.Partition.ConnectionId, this.Previous.InitialGeneric.GenericId, this.Next.FinalGeneric.GenericId),

		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			return true;
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawArbiterUserWaypoint;
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is ArbiterUserWaypoint)
			{
				ArbiterUserWaypoint other = (ArbiterUserWaypoint)obj;
				return this.WaypointId.Equals(other.WaypointId);
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return this.WaypointId.GetHashCode();
		}

		public override string ToString()
		{
			return this.WaypointId.ToString();
		}

		#endregion
	}
}
