using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using System.Drawing;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Classifier of a waypoint
	/// </summary>
	public enum WaypointType
	{
		Exit,
		Entry,
		Stop,
		End,
		Start
	}

	/// <summary>
	/// Defines a waypoint in a segment
	/// </summary>
	[Serializable]
	public class ArbiterWaypoint : IArbiterWaypoint, INavigableNode, IDisplayObject, IGenericWaypoint, ITraversableWaypoint, INetworkObject
	{
		#region Waypoint Members

		private Coordinates position;
		private bool isCheckpoint;
		private int checkpointId;
		private bool isExit;
		private bool isEntry;
		private List<ArbiterInterconnect> exits;
		private List<ArbiterInterconnect> entries;

		/// <summary>
		/// Id of the waypoint
		/// </summary>
		public ArbiterWaypointId WaypointId;

		/// <summary>
		/// Flag if this waypoint is a stop
		/// </summary>
		private bool isStop;

		/// <summary>
		/// Next Partition
		/// </summary>
		public ArbiterLanePartition NextPartition;

		/// <summary>
		/// Previous Partition
		/// </summary>
		public ArbiterLanePartition PreviousPartition;

		/// <summary>
		/// Lane the waypoint is in
		/// </summary>
		public ArbiterLane Lane;

		public ArbiterWaypoint(Coordinates position, ArbiterWaypointId waypointId)
		{
			this.position = position;
			this.WaypointId = waypointId;
			this.outgoingConnections = new List<NavigableEdge>();
		}

		#endregion

		#region IArbiterWaypoint Members

		public IAreaSubtypeWaypointId AreaSubtypeWaypointId
		{
			get { return this.WaypointId; }
		}

		public Coordinates Position
		{
			get
			{
				return position;
			}
			set
			{
				this.position = value;
			}
		}

		public bool IsCheckpoint
		{
			get
			{
				return this.isCheckpoint;
			}
			set
			{
				this.isCheckpoint = value;
			}
		}

		public int CheckpointId
		{
			get
			{
				return this.checkpointId;
			}
			set
			{
				this.checkpointId = value;
			}
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
			if (filter(this))
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
			//if (t.WorldLowerLeft.X < this.Position.X && t.WorldLowerLeft.Y < this.position.Y && t.WorldUpperRight.X > this.position.X && t.WorldUpperRight.Y > this.position.Y)
			if (t.ShouldDraw(this.GetBoundingBox(t)))
			{
				Color c;

				if (this.isCheckpoint && this.IsStop)
				{
					c = DrawingUtility.ColorArbiterWaypointStopCheckpoint;
				}
				else if (this.isCheckpoint)
				{
					c = DrawingUtility.ColorArbiterWaypointCheckpoint;
				}
				else if (this.IsStop)
				{
					c = DrawingUtility.ColorArbiterWaypointStop;
				}
				else
				{
					c = DrawingUtility.ColorArbiterWaypoint;
				}

				if (this.isCheckpoint && DrawingUtility.DisplayArbiterWaypointCheckpointId)
				{
					DrawingUtility.DrawControlPoint(this.position, DrawingUtility.ColorDisplayArbiterCheckpoint, this.checkpointId.ToString(),
						ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
				}

				if (DrawingUtility.DisplayArbiterWaypointId)
				{
					DrawingUtility.DrawControlPoint(this.position, c, this.WaypointId.ToString(),
						ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
				}
				else
				{
					DrawingUtility.DrawControlPoint(this.position, c, null,
						ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
				}
			}
		}

		public bool MoveAllowed
		{
			get { return true; }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			this.position = orig + offset;
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			this.position = orig + offset;
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
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
			return DrawingUtility.DrawArbiterWaypoint;
		}

		#endregion

		#region IGenericWaypoint Members

		public object GenericId
		{
			get { return this.WaypointId; }
		}

		#endregion

		#region ITraversableWaypoint Members

		/// <summary>
		/// if this is an exit
		/// </summary>
		public bool IsExit
		{
			get
			{
				return this.isExit;
			}
			set
			{
				this.isExit = value;
			}
		}

		/// <summary>
		/// if thsi is an entry
		/// </summary>
		public bool IsEntry
		{
			get
			{
				return this.isEntry;
			}
			set
			{
				this.isEntry = value;
			}
		}

		/// <summary>
		/// exits from this waypoint
		/// </summary>
		public List<ArbiterInterconnect> Exits
		{
			get
			{
				return this.exits;
			}
			set
			{
				this.exits = value;
			}
		}

		/// <summary>
		/// entries into this waypoint
		/// </summary>
		public List<ArbiterInterconnect> Entries
		{
			get
			{
				return this.entries;
			}
			set
			{
				this.entries = value;
			}
		}

		/// <summary>
		/// Checks if this is stop or not
		/// </summary>
		public bool IsStop
		{
			get
			{
				return this.isStop;
			}
			set
			{
				this.isStop = value;
			}
		}

		/// <summary>
		/// Vehicle area this waypoint is associated with
		/// </summary>
		public IVehicleArea VehicleArea
		{
			get
			{
				return this.Lane;
			}
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two zones are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterWaypoint)
			{
				// check if the numbers are equal
				return ((ArbiterWaypoint)obj).WaypointId.Equals(this.WaypointId);
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
			return this.WaypointId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.WaypointId.ToString();
		}

		#endregion

		#region INavigableNode Members

		private double timeToHere;
		private double timeToGoal;
		private double time;
		private INavigableNode previous;
		List<NavigableEdge> outgoingConnections;

		public double TimeToHere
		{
			get
			{
				return this.timeToHere;
			}
			set
			{
				this.timeToHere = value;
			}
		}

		public double TimeToGoal
		{
			get
			{
				return this.timeToGoal;
			}
			set
			{
				this.timeToGoal = value;
			}
		}

		public double Time
		{
			get
			{
				return this.time;
			}
			set
			{
				this.time = value;
			}
		}

		public INavigableNode Previous
		{
			get
			{
				return this.previous;
			}
			set
			{
				this.previous = value;
			}
		}

		public List<NavigableEdge> OutgoingConnections
		{
			get
			{
				return this.outgoingConnections;
			}
			set
			{
				this.outgoingConnections = value;
			}
		}

		public double TimeThroughAdjacent(NavigableEdge edge)
		{
			return edge.TimeCost();
		}

		public double TimeTo(INavigableNode node)
		{
			// avg speed of 10mph from place to place
			return this.position.DistanceTo(node.Position) / (this.Lane.Way.Segment.RoadNetwork.MissionAverageMaxSpeed / 2.0);
		}

		public void ResetPlanningCosts()
		{
			this.timeToGoal = 0;
			this.timeToHere = 0;
			this.time = 0;
		}

		public double ExtraTimeCost
		{
			get
			{
				return this.IsStop ? NavigationPenalties.StopWaypoint : 0.0;
			}
		}

		public bool EqualsNode(INavigableNode node)
		{
			return this.Equals(node);
		}

		public string Name
		{
			get
			{
				return this.ToString();
			}
		}

		public double Value
		{
			get
			{
				return this.time;
			}
		}

		#endregion

		/// <summary>
		/// Type off a waypoint
		/// </summary>
		/// <param name="wt"></param>
		/// <returns></returns>
		public bool WaypointTypeEquals(WaypointType wt)
		{
			if (wt == WaypointType.End)
			{
				return this.NextPartition == null ? true : false;
			}
			else if (wt == WaypointType.Entry)
			{
				return this.isEntry;
			}
			else if (wt == WaypointType.Exit)
			{
				return this.isExit;
			}
			else if (wt == WaypointType.Stop)
			{
				return this.IsStop;
			}
			else
				return this.PreviousPartition == null;
		}
	}
}
