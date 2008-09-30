using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Involved area and point (if exists) in an intersection
	/// </summary>
	[Serializable]
	public class IntersectionInvolved : IComparable
	{
		/// <summary>
		/// Specific Point involved
		/// </summary>
		public ITraversableWaypoint Exit;

		/// <summary>
		/// Turn direction from the exit
		/// </summary>
		public ArbiterTurnDirection TurnDirection;

		/// <summary>
		/// Area involved
		/// </summary>
		public IVehicleArea Area;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exit"></param>
		/// <param name="area"></param>
		/// <param name="turnDirection"></param>
		public IntersectionInvolved(ITraversableWaypoint exit, IVehicleArea area, ArbiterTurnDirection turnDirection)
		{
			this.Exit = exit;
			this.Area = area;
			this.TurnDirection = turnDirection;
		}

		public IntersectionInvolved(IVehicleArea area)
		{
			this.Exit = null;
			this.Area = area;
			this.TurnDirection = ArbiterTurnDirection.Unknown;
		}

		public override string ToString()
		{
			if(Exit == null)
				return Area.ToString();
			else
				return Area.ToString() + ": " + Exit.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is IntersectionInvolved)
			{
				IntersectionInvolved other = (IntersectionInvolved)obj;
				return other.Area.Equals(this.Area);
			}

			return false;
		}

		public override int GetHashCode()
		{
			if (Exit != null)
				return this.Exit.GetHashCode();
			else
				return this.Area.GetHashCode();
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is IntersectionInvolved)
			{
				IntersectionInvolved ii = (IntersectionInvolved)obj;

				if (this.TurnDirection == ii.TurnDirection)
				{
					if (this.Exit != null && ii.Exit == null)
						return -1;
					else if (ii.Exit != null && ii.Exit != null)
						return 0;
					else if (ii.Exit == null && ii.Exit != null)
						return 1;
					else
						return 0;
				}
				else
					return this.TurnDirection.CompareTo(ii.TurnDirection);
			}
			else
				return -1;
		}

		#endregion
	}

	/// <summary>
	/// Represents an intersection
	/// </summary>
	[Serializable]
	public class ArbiterIntersection : IDisplayObject, INetworkObject, IVehicleArea
	{
		#region Intersection Members

		/// <summary>
		/// Id of the intersection
		/// </summary>
		public ArbiterIntersectionId IntersectionId;

		/// <summary>
		/// Road network the intersection is a part of
		/// </summary>
		public ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Center of the intersection
		/// </summary>
		public Coordinates Center;

		/// <summary>
		/// polygon representing the intersection
		/// </summary>
		public Polygon IntersectionPolygon;

		/// <summary>
		/// exits that have stop signs at them
		/// </summary>
		public List<ArbiterStoppedExit> StoppedExits;

		/// <summary>
		/// all exits in the intersection
		/// </summary>
		public Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> AllExits;

		/// <summary>
		/// all entries in an intersection
		/// </summary>
		public Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> AllEntries;

		/// <summary>
		/// Determines for an interconnect, which lanes have priority over it
		/// </summary>
		public Dictionary<ArbiterInterconnect, List<IntersectionInvolved>> PriorityLanes;

		/// <summary>
		/// The points at which lanes enter the intersection polygon
		/// </summary>
		public Dictionary<ArbiterLane, LinePath.PointOnPath> IncomingLanePoints;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="p"></param>
		/// <param name="exits"></param>
		/// <param name="involved"></param>
		/// <param name="incoming"></param>
		public ArbiterIntersection(Polygon p, List<ArbiterStoppedExit> exits, Dictionary<ArbiterInterconnect, List<IntersectionInvolved>> priority,
			Dictionary<ArbiterLane, LinePath.PointOnPath> incoming, Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> allExits, Coordinates center,
			ArbiterIntersectionId id, ArbiterRoadNetwork network, Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> entries)
		{
			this.IntersectionPolygon = p;
			this.StoppedExits = exits;
			this.PriorityLanes = priority;
			this.IncomingLanePoints = incoming;
			this.AllExits = allExits;
			this.Center = center;
			this.IntersectionId = id;
			this.RoadNetwork = network;
			this.AllEntries = entries;
		}

		/// <summary>
		/// Gets the stopped exit represented by this id
		/// </summary>
		/// <param name="awi"></param>
		/// <returns></returns>
		public ArbiterStoppedExit GetStoppedExit(ArbiterWaypointId awi)
		{
			foreach (ArbiterStoppedExit ase in StoppedExits)
			{
				if (ase.Waypoint.WaypointId.Equals(awi))
				{
					return ase;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks if this intersection contains a certain exit
		/// </summary>
		/// <param name="awi"></param>
		/// <returns></returns>
		public bool ContainsExit(ArbiterWaypointId awi)
		{
			return this.AllExits.ContainsKey(awi);
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			// Determine size of bounding box
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = DrawingUtility.cp_large_size;

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)this.Center.X - scaled_size / 2, (float)this.Center.Y - scaled_size / 2, scaled_size, scaled_size);

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
					return new HitTestResult(this, true, (float)loc.DistanceTo(this.Center));
				}
			}

			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			// show intersection safetyzone if supposed to show safety zone (polygon red hatch)
			if (DrawingUtility.DrawArbiterSafetyZones)
			{
				// show intersection polygon
				HatchBrush hBrush1 = new HatchBrush(HatchStyle.ForwardDiagonal, DrawingUtility.ColorArbiterSafetyZone, Color.White);

				// populate polygon
				List<PointF> polyPoints = new List<PointF>();
				foreach (Coordinates c in this.IntersectionPolygon.points)
				{
					polyPoints.Add(DrawingUtility.ToPointF(c));
				}

				// draw poly and fill
				g.FillPolygon(hBrush1, polyPoints.ToArray());
			}

			// render stopped exits
			foreach (ArbiterStoppedExit ase in this.StoppedExits)
			{
				ase.Render(g, t);
			}

			// draw intersection polygon
			DrawingUtility.DrawControlPolygon(this.IntersectionPolygon,
				DrawingUtility.ColorArbiterIntersection,
				DashStyle.DashDotDot,
				g, t);			

			// show incoming lane points (disjoint from exits)
			foreach (KeyValuePair<ArbiterLane, LinePath.PointOnPath> pop in this.IncomingLanePoints)
			{
				DrawingUtility.DrawControlPoint(pop.Key.LanePath().GetPoint(pop.Value), DrawingUtility.ColorArbiterIntersectionIncomingLanePoints, null,
					ContentAlignment.MiddleCenter, ControlPointStyle.SmallX, g, t);
			}

			// show all entries
			foreach (ITraversableWaypoint aw in this.AllEntries.Values)
			{
				DrawingUtility.DrawControlPoint(aw.Position, DrawingUtility.ColorArbiterIntersectionEntries, null,
					ContentAlignment.MiddleCenter, ControlPointStyle.LargeCircle, g, t);
			}

			// show all exits
			foreach (ITraversableWaypoint aw in this.AllExits.Values)
			{
				DrawingUtility.DrawControlPoint(aw.Position, DrawingUtility.ColorArbiterIntersectionExits, null,
					ContentAlignment.MiddleCenter, ControlPointStyle.LargeCircle, g, t);
			}			

			// draw center point
			DrawingUtility.DrawControlPoint(this.Center, DrawingUtility.ColorArbiterIntersection, null, ContentAlignment.MiddleCenter, ControlPointStyle.LargeX, g, t);
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
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
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool CanDelete
		{
			get { throw new Exception("The method or operation is not implemented."); }
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
			return DrawingUtility.DrawArbiterIntersections;
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is ArbiterIntersection)
			{
				return ((ArbiterIntersection)obj).IntersectionId.Equals(this.IntersectionId);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return this.IntersectionId.GetHashCode();
		}

		public override string ToString()
		{
			return this.IntersectionId.ToString();
		}

		#endregion

		#region IVehicleArea Members

		public double DistanceTo(Coordinates loc)
		{
			return this.IntersectionPolygon.IsInside(loc) ? 0.0 : Double.MaxValue;
		}

		public bool ContainsVehicle(Coordinates center, double length, double width, Coordinates heading)
		{
			return this.DistanceTo(center) == 0.0 ? true : false;
		}

		public string DefaultAreaId()
		{
			return this.IntersectionId.ToString();
		}

		#endregion
	}
}
