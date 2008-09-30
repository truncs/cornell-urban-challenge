using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a link between two darpa defined waypoints
	/// </summary>
	[Serializable]
	public class ArbiterLanePartition : NavigableEdge, IConnectAreaWaypoints, IDisplayObject, INetworkObject
	{
		#region Lane Partition Members

		/// <summary>
		/// the blockage contained in this partition
		/// </summary>
		private NavigationBlockage blockage;

		/// <summary>
		/// Unique Id of the partition
		/// </summary>
		public ArbiterLanePartitionId PartitionId;

		/// <summary>
		/// Initial waypoint
		/// </summary>
		public ArbiterWaypoint Initial;

		/// <summary>
		/// Final waypoint
		/// </summary>
		public ArbiterWaypoint Final;

		/// <summary>
		/// Length of the partition
		/// </summary>
		public double Length;

		/// <summary>
		/// By default is a normal partition
		/// </summary>
		public PartitionType Type = PartitionType.Normal;

		/// <summary>
		/// User Partitions of this lane partition
		/// </summary>
		public List<ArbiterUserPartition> Partitions;

		/// <summary>
		/// User waypoints
		/// </summary>
		public List<ArbiterUserWaypoint> UserWaypoints;

		/// <summary>
		/// List of partitions adjacent to this one on an adjacent lane
		/// </summary>
		public List<ArbiterLanePartition> NonLaneAdjacentPartitions;

		/// <summary>
		/// Lane the partition belongs to
		/// </summary>
		public ArbiterLane Lane;

		/// <summary>
		/// Line parht of partitions
		/// </summary>
		public LinePath PartitionPath;

		/// <summary>
		/// Polygon set if this is to be sparse
		/// </summary>
		public Polygon SparsePolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="partitionId"></param>
		public ArbiterLanePartition(ArbiterLanePartitionId partitionId,
			ArbiterWaypoint initial, ArbiterWaypoint final, ArbiterSegment segment)
			: base(false, null, true, segment, new List<IConnectAreaWaypoints>(), initial, final)
		{
			this.PartitionId = partitionId;
			this.Initial = initial;
			this.Final = final;
			this.Length = this.Initial.Position.DistanceTo(this.Final.Position);
			this.UserWaypoints = new List<ArbiterUserWaypoint>();
			this.NonLaneAdjacentPartitions = new List<ArbiterLanePartition>();

			// create a path of the partition and get the closest
			List<Coordinates> coords = new List<Coordinates>();
			coords.Add(initial.Position);
			coords.Add(final.Position);
			PartitionPath = new LinePath(coords);

			// nav edge stuff
			this.Contained.Add(this);
			this.blockage = new NavigationBlockage(0.0);
		}

		public void ReformPath()
		{
			List<Coordinates> coords = new List<Coordinates>();
			foreach (ArbiterUserPartition aup in this.UserPartitions)
			{
				coords.Add(aup.InitialGeneric.Position);
			}
			coords.Add(this.UserPartitions[this.UserPartitions.Count - 1].FinalGeneric.Position);
			this.PartitionPath = new LinePath(coords);
			this.SetDefaultSparsePolygon();
		}

		public List<Coordinates> NotInitialPathCoords()
		{
			List<Coordinates> coords = new List<Coordinates>();
			foreach (ArbiterUserPartition aup in this.UserPartitions)
			{
				coords.Add(aup.FinalGeneric.Position);
			}
			return coords;
		}

		public LinePath UserPartitionPath {
			get {
				int numUserPartitions = 0;
				if (this.UserPartitions != null) {
					numUserPartitions = this.UserPartitions.Count;
				}
				LinePath path = new LinePath(2+numUserPartitions);

				path.Add(Initial.Position);

				if (this.UserPartitions != null && this.UserPartitions.Count > 0) {
					foreach (ArbiterUserPartition partiton in this.UserPartitions) {
						path.Add(partiton.FinalGeneric.Position);
					}
				}
				else {
					path.Add(Final.Position);
				}

				return path;
			}
		}

		#endregion

		#region IConnectAreaWaypoints Members

		/// <summary>
		/// initial generic waypoint of the connection
		/// </summary>
		public IArbiterWaypoint InitialGeneric
		{
			get
			{
				return this.Initial;
			}
		}

		/// <summary>
		/// final generic waypoint of connection
		/// </summary>
		public IArbiterWaypoint FinalGeneric
		{
			get
			{
				return this.Final;
			}
		}

		/// <summary>
		/// Generic Id
		/// </summary>
		public IConnectAreaWaypointsId ConnectionId
		{
			get { return this.PartitionId; }
		}

		/// <summary>
		/// User partitions that make up the interconnect
		/// </summary>
		public List<ArbiterUserPartition> UserPartitions
		{
			get
			{
				return this.Partitions;
			}
			set
			{
				this.Partitions = value;
			}
		}

		/// <summary>
		/// Get distance from partition to a location
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public double DistanceTo(Coordinates loc)
		{
			return(loc.DistanceTo(this.PartitionPath.GetPoint(this.PartitionPath.GetClosestPoint(loc))));
		}

		/// <summary>
		/// Distance to another connect
		/// </summary>
		/// <param name="icaw"></param>
		/// <returns></returns>
		public double DistanceTo(IConnectAreaWaypoints icaw)
		{
			LinePath.PointOnPath current = this.PartitionPath.StartPoint;
			double inc = 1.0;
			double dist = 0;
			double minDist = double.MaxValue;

			while (dist == 0)
			{
				double tmpDist = icaw.DistanceTo(this.PartitionPath.GetPoint(current));
				
				if (tmpDist < minDist)
					minDist = tmpDist;

				dist = inc;
				current = PartitionPath.AdvancePoint(current, ref dist);
			}

			return minDist;
		}

		/// <summary>
		/// Blockage on this partition
		/// </summary>
		public NavigationBlockage Blockage
		{
			get
			{
				return this.blockage;
			}
			set
			{
				this.blockage = value;
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
			if (obj is ArbiterLanePartition)
			{
				// check if the numbers are equal
				return ((ArbiterLanePartition)obj).PartitionId.Equals(this.PartitionId);
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
			PointF[] pts = new PointF[4];
			pts[0] = new PointF((float)this.Initial.Position.X, (float)this.Initial.Position.Y);
			pts[1] = new PointF((float)this.Initial.Position.X, (float)this.Final.Position.Y);
			pts[2] = new PointF((float)this.Final.Position.X, (float)this.Final.Position.Y);
			pts[3] = new PointF((float)this.Final.Position.X, (float)this.Initial.Position.Y);
			return DrawingUtility.CalcBoundingBox(pts);
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			double dist = this.PartitionPath.GetClosestPoint(loc).Location.DistanceTo(loc);

			if (dist < 5)
			{
				return new HitTestResult(this, true, (float)dist);
			}
			else
			{
				return new HitTestResult(this, false, float.MaxValue);
			}
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			//if ((t.WorldLowerLeft.X < this.Final.Position.X && t.WorldLowerLeft.Y < this.Final.Position.Y && t.WorldUpperRight.X > this.Final.Position.X && t.WorldUpperRight.Y > this.Final.Position.Y) ||
			//	(t.WorldLowerLeft.X < this.Initial.Position.X && t.WorldLowerLeft.Y < this.Initial.Position.Y && t.WorldUpperRight.X > this.Initial.Position.X && t.WorldUpperRight.Y > this.Initial.Position.Y))
			if(t.ShouldDraw(this.GetBoundingBox(t)))
			{
				Color c;

				if (DrawingUtility.DrawArbiterLanePartitionWays && this.Lane.Way.WayId.Number == 1)
				{
					c = DrawingUtility.ColorArbiterLanePartitionWay1;

					DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid,
					this.Initial.Position, this.Final.Position, g, t);
				}
				else if (DrawingUtility.DrawArbiterLanePartitionWays && this.Lane.Way.WayId.Number == 2)
				{
					c = DrawingUtility.ColorArbiterLanePartitionWay2;

					DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid,
					this.Initial.Position, this.Final.Position, g, t);
				}
				else if (this.selected == SelectionType.SingleSelected)
				{
					c = Color.Red;

					DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid,
					this.Initial.Position, this.Final.Position, g, t);
				}
				else
				{
					if (this.Type == PartitionType.Normal)
					{
						c = DrawingUtility.ColorArbiterLanePartitionDefault;
						DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid,
						this.Initial.Position, this.Final.Position, g, t);
					}
					else if (this.Type == PartitionType.Sparse)
					{
						c = Color.Black;
						DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Dash,
						this.Initial.Position, this.Final.Position, g, t);
					}
					else
					{
						c = Color.DarkOrange;
						DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Dot,
					this.Initial.Position, this.Final.Position, g, t);
					}
				}
			}
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

		public SelectionType selected = SelectionType.NotSelected;

		public SelectionType Selected
		{
			get
			{
				return selected;
			}
			set
			{
				this.selected = value;
			}
		}

		public IDisplayObject Parent
		{
			get { return this.Lane; }
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
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawArbiterLanePartition;
		}

		#endregion

		#region Partition Functions

		/// <summary>
		/// Gets the closest point on the partition to the input position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Coordinates ClosestPoint(Coordinates position)
		{
			// create a path of the partition and get the closest
			return this.PartitionPath.GetPoint(this.PartitionPath.GetClosestPoint(position));
		}

		/// <summary>
		/// Vector representing the partition
		/// </summary>
		/// <returns></returns>
		public Coordinates Vector()
		{
			Coordinates c = this.Final.Position - this.Initial.Position;
			return c;
		}

		/// <summary>
		/// Checks if the location is within the lane
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public bool IsInside(Coordinates loc)
		{
			LinePath.PointOnPath pop = this.PartitionPath.GetClosestPoint(loc);

			if (pop.Equals(this.PartitionPath.StartPoint))
				return false;
			else if (pop.Equals(this.PartitionPath.EndPoint))
				return false;
			else
				return true;
		}

		#endregion

		#region IConnectAreaWaypoints Members

		public ArbiterInterconnect ToInterconnect
		{
			get 
			{ 
				ArbiterInterconnect ai = new ArbiterInterconnect(this.Initial, this.Final);
				ai.Blockage = this.blockage;
				return ai;
			}
		}

		#endregion

		#region Sparse Polygon

		public void SetDefaultSparsePolygon()
		{
			ArbiterLanePartition alp = this;
			List<Coordinates> polyCoords = new List<Coordinates>();
			polyCoords.Add(alp.Initial.Position);
			polyCoords.AddRange(alp.NotInitialPathCoords());
			LinePath lpr = (new LinePath(polyCoords)).ShiftLateral(-TahoeParams.VL * 3.0);
			LinePath lpl = (new LinePath(polyCoords)).ShiftLateral(TahoeParams.VL * 3.0);
			List<Coordinates> finalCoords = new List<Coordinates>(polyCoords.ToArray());
			finalCoords.AddRange(lpr);
			finalCoords.AddRange(lpl);
			Polygon p = Polygon.GrahamScan(finalCoords);
			this.SparsePolygon = p;
		}

		#endregion
	}

	/// <summary>
	/// Type of arbiter lane partition
	/// </summary>
	[Serializable]
	public enum PartitionType
	{
		Normal = 0,
		Sparse = 1,
		Startup = 2
	}
}
