using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common.Shapes;
using System.Drawing;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Zone area in an rndf network
	/// </summary>
	[Serializable]
	public class ArbiterZone : IDisplayObject, INetworkObject, IVehicleArea
	{
		#region Zone Members

		/// <summary>
		/// Unique identifier of the zone
		/// </summary>
		public ArbiterZoneId ZoneId;

		/// <summary>
		/// Perimeter of the Zone
		/// </summary>
		public ArbiterPerimeter Perimeter;

		/// <summary>
		/// List of parking spots
		/// </summary>
		public List<ArbiterParkingSpot> ParkingSpots;

		/// <summary>
		/// Parking spot waypoints
		/// </summary>
		public Dictionary<ArbiterParkingSpotWaypointId, ArbiterParkingSpotWaypoint> ParkingSpotWaypoints;

		/// <summary>
		/// Exits out of the zone
		/// </summary>
		public List<ArbiterPerimeterWaypoint> ZoneExits;

		/// <summary>
		/// Navigational Coarse Cost Map
		/// </summary>
		public ArbiterZoneCostMap CostMap;

		/// <summary>
		/// Speed limts of the zone
		/// </summary>
		public ArbiterSpeedLimit SpeedLimits;

		/// <summary>
		/// Areas to stay out of
		/// </summary>
		public List<Polygon> StayOutAreas;
		public List<INavigableNode> NavigationNodes;
		public List<NavigableEdge> NavigableEdges;
		public ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zoneId"></param>
		/// <param name="perimter"></param>
		/// <param name="parkingSpots"></param>
		public ArbiterZone(ArbiterZoneId zoneId, ArbiterPerimeter perimter,
			List<ArbiterParkingSpot> parkingSpots, ArbiterRoadNetwork roadNetwork)
		{
			this.ZoneId = zoneId;
			this.Perimeter = perimter;
			this.ParkingSpots = parkingSpots;
			this.ParkingSpotWaypoints = new Dictionary<ArbiterParkingSpotWaypointId, ArbiterParkingSpotWaypoint>();
			this.ZoneExits = new List<ArbiterPerimeterWaypoint>();
			this.StayOutAreas = new List<Polygon>();
			this.NavigationNodes = new List<INavigableNode>();
			this.NavigableEdges = new List<NavigableEdge>();
			this.RoadNetwork = roadNetwork;

			// set zone
			this.Perimeter.Zone = this;

			// create waypoint lookup
			foreach (ArbiterParkingSpot aps in parkingSpots)
			{
				// set zone
				aps.Zone = this;

				foreach (ArbiterParkingSpotWaypoint apsw in aps.Waypoints.Values)
				{
					ParkingSpotWaypoints.Add(apsw.WaypointId, apsw);
				}
			}

			// create exits
			foreach (ArbiterPerimeterWaypoint apw in perimter.PerimeterPoints.Values)
			{
				ZoneExits.Add(apw);
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
			if (obj is ArbiterZone)
			{
				// check if the numbers are equal
				return ((ArbiterZone)obj).ZoneId.Equals(this.ZoneId);
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
			return this.ZoneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.ZoneId.ToString();
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
			if (DrawingUtility.DrawArbiterZoneMap)
			{
				foreach (Polygon p in this.StayOutAreas)
				{
					DrawingUtility.DrawControlPolygon(p, Color.SteelBlue, System.Drawing.Drawing2D.DashStyle.Solid, g, t);
					foreach (Coordinates c in p)
						DrawingUtility.DrawControlPoint(c, Color.SteelBlue, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}

				foreach (INavigableNode nn in this.NavigationNodes)
				{
					DrawingUtility.DrawControlPoint(nn.Position, Color.DarkOrange, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}

				foreach (NavigableEdge ne in this.NavigableEdges)
				{
					DrawingUtility.DrawColoredArrowControlLine(Color.DarkBlue, System.Drawing.Drawing2D.DashStyle.Solid, ne.Start.Position, ne.End.Position, g, t);
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

		public SelectionType Selected
		{
			get
			{
				return  SelectionType.NotSelected;
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
			return DrawingUtility.DisplayArbiterZone;
		}

		#endregion

		#region IVehicleArea Members

		public double DistanceTo(UrbanChallenge.Common.Coordinates loc)
		{
			return this.Perimeter.PerimeterPolygon.IsInside(loc) ? 0.0 : Double.MaxValue;
		}

		#endregion

		#region IVehicleArea Members

		public bool ContainsVehicle(UrbanChallenge.Common.Coordinates center, double length, double width, UrbanChallenge.Common.Coordinates heading)
		{
			return this.DistanceTo(center) == 0.0 ? true : false;
		}

		public string DefaultAreaId()
		{
			return this.ToString();
		}

		#endregion
	}
}
