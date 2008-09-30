using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// The perimeter of a zone
	/// </summary>
	[Serializable]
	public class ArbiterPerimeter : IDisplayObject, INetworkObject
	{
		#region Perimeter Members

		/// <summary>
		/// Zone this is the perimeter of
		/// </summary>
		public ArbiterZone Zone;

		/// <summary>
		/// Unique identifier of the perimeter
		/// </summary>
		public ArbiterPerimeterId PerimeterId;

		/// <summary>
		/// Perimeter Points of the Perimeter
		/// </summary>
		public Dictionary<ArbiterPerimeterWaypointId, ArbiterPerimeterWaypoint> PerimeterPoints;

		/// <summary>
		/// Polygon of the perimeter
		/// </summary>
		public Polygon PerimeterPolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="PerimeterId"></param>
		/// <param name="perimeterPoints"></param>
		public ArbiterPerimeter(ArbiterPerimeterId perimeterId, List<ArbiterPerimeterWaypoint> perimeterPoints)
		{
			this.PerimeterId = perimeterId;
			this.PerimeterPoints = new Dictionary<ArbiterPerimeterWaypointId,ArbiterPerimeterWaypoint>();

			List<Coordinates> perCoords = new List<Coordinates>();

			// set perimeter points
			foreach (ArbiterPerimeterWaypoint apw in perimeterPoints)
			{
				this.PerimeterPoints.Add(apw.WaypointId, apw);
				perCoords.Add(apw.Position);
			}

			this.PerimeterPolygon = new Polygon(perCoords, CoordinateMode.AbsoluteProjected);
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
			if (obj is ArbiterPerimeter)
			{
				// check if the numbers are equal
				return ((ArbiterPerimeter)obj).PerimeterId.Equals(this.PerimeterId);
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
			return this.PerimeterId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.PerimeterId.ToString();
		}

		#endregion

		#region IDisplayObject Members		

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Performs hit test but can never actually selsect
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="tol"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		/// <summary>
		/// Display the perimeter
		/// </summary>
		/// <param name="g"></param>
		/// <param name="t"></param>
		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			foreach (ArbiterPerimeterWaypoint apw in this.PerimeterPoints.Values)
			{
				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterPerimiter, System.Drawing.Drawing2D.DashStyle.Dash, apw.Position, apw.NextPerimeterPoint.Position, g, t);
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
				return SelectionType.NotSelected;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { return this.Zone; }
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
			return DrawingUtility.DrawArbiterPerimeter;
		}

		#endregion
	}
}
