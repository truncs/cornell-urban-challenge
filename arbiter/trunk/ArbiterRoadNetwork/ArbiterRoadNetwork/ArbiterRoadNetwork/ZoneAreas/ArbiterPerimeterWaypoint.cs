using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using System.Drawing;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// A waypoint of the perimeter of the zone
	/// </summary>
	[Serializable]
	public class ArbiterPerimeterWaypoint : IArbiterWaypoint, IGenericWaypoint, INavigableNode, IDisplayObject, ITraversableWaypoint, INetworkObject
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
		public ArbiterPerimeterWaypointId WaypointId;

		/// <summary>
		/// Interconnects from the exit
		/// </summary>
		public Dictionary<ArbiterInterconnectId, ArbiterInterconnect> OutgoingInterconnects;

		/// <summary>
		/// The next waypoint in the perimeter
		/// </summary>
		public ArbiterPerimeterWaypoint NextPerimeterPoint;

		/// <summary>
		/// the perimeter this is a part of
		/// </summary>
		public ArbiterPerimeter Perimeter;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position"></param>
		/// <param name="waypointId"></param>
		public ArbiterPerimeterWaypoint(ArbiterPerimeterWaypointId waypointId, Coordinates position)
		{
			this.position = position;
			this.WaypointId = waypointId;
			this.outgoingConnections = new List<NavigableEdge>();
		}

		#endregion

		#region IArbiterWaypoint Members

		/// <summary>
		/// id of the waypoint
		/// </summary>
		public IAreaSubtypeWaypointId AreaSubtypeWaypointId
		{
			get { return WaypointId; }
		}

		/// <summary>
		/// position of the waypoint
		/// </summary>
		public UrbanChallenge.Common.Coordinates Position
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
		/// if this is a checkpoint
		/// </summary>
		public bool IsCheckpoint
		{
			get { return this.isCheckpoint; }
			set { this.isCheckpoint = value; }
		}

		/// <summary>
		/// number of checkpoint it is
		/// </summary>
		public int CheckpointId
		{
			get { return this.checkpointId; }
			set { this.checkpointId = value; }
		}

		#endregion

		#region IDisplayObject Members

		private SelectionType selected;

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

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			// check filter
			if (filter.Target == null || filter.Target is ArbiterPerimeterWaypoint)
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
			Color c = DrawingUtility.ColorArbiterPerimeterWaypoint;

			if (selected == SelectionType.SingleSelected)
				c = DrawingUtility.ColorArbiterPerimeterWaypointSelected;
			else if (this.IsExit)
				c = Color.Red;
			else if (this.IsEntry)
				c = Color.Blue;

			if (DrawingUtility.DisplayArbiterPerimeterWaypointId)
			{
				DrawingUtility.DrawControlPoint(this.position, c, this.WaypointId.ToString(),
					ContentAlignment.BottomCenter, ControlPointStyle.SmallBox, g, t);
			}
			else
			{
				DrawingUtility.DrawControlPoint(this.position, c, null, 
					ContentAlignment.BottomCenter, ControlPointStyle.SmallBox, g, t);
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
			this.position = orig + offset; ;
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			this.position = orig;
		}

		public SelectionType Selected
		{
			get
			{
				return this.selected;
			}
			set
			{
				this.selected = value;
			}
		}

		public IDisplayObject Parent
		{
			get { return this.Perimeter; }
		}

		public bool CanDelete
		{
			get { return false; }
		}

		public List<IDisplayObject> Delete()
		{
			return null;
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			return true;
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawArbiterPerimeterWaypoint;
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
				return false;
			}
			set
			{				
			}
		}

		/// <summary>
		/// Vehicle area this waypoint is associated with
		/// </summary>
		public IVehicleArea VehicleArea
		{
			get
			{
				return this.Perimeter.Zone;
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
			if (obj is ArbiterPerimeterWaypoint)
			{
				return ((ArbiterPerimeterWaypoint)obj).WaypointId.Equals(this.WaypointId);
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
			return this.WaypointId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
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
			return this.position.DistanceTo(node.Position) / 2.24;
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
				return NavigationPenalties.ZoneWaypoint;
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
	}
}
