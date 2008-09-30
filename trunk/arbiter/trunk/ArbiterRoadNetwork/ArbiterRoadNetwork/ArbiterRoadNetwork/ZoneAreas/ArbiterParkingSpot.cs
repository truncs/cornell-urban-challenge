using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a parking spot in a zone
	/// </summary>
	[Serializable]
	public class ArbiterParkingSpot : IDisplayObject, INetworkObject
	{
		#region Parking Spot Members

		/// <summary>
		/// Waypoints of the parking spot organized by Id
		/// </summary>
		public Dictionary<ArbiterParkingSpotWaypointId, ArbiterParkingSpotWaypoint> Waypoints;

		/// <summary>
		/// The normal waypoint of the parking spot
		/// </summary>
		public ArbiterParkingSpotWaypoint NormalWaypoint;

		/// <summary>
		/// The checkpoint of the parking spot
		/// </summary>
		public ArbiterParkingSpotWaypoint Checkpoint;

		/// <summary>
		/// Id of the parking spot
		/// </summary>
		public ArbiterParkingSpotId SpotId;

		/// <summary>
		/// Width of the parking spot
		/// </summary>
		public double Width;

		/// <summary>
		/// Zone containing the parking spot
		/// </summary>
		public ArbiterZone Zone;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width"></param>
		/// <param name="spotId"></param>
		public ArbiterParkingSpot(double width, ArbiterParkingSpotId spotId)
		{
			this.SpotId = spotId;
			this.Width = width;
			this.Waypoints = new Dictionary<ArbiterParkingSpotWaypointId, ArbiterParkingSpotWaypoint>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		/// <param name="width"></param>
		/// <param name="spotId"></param>
		/// <param name="checkpoint"></param>
		/// <param name="waypoints"></param>
		public ArbiterParkingSpot(double width,
			ArbiterParkingSpotId spotId, List<ArbiterParkingSpotWaypoint> waypoints)
		{
			this.Width = width;
			this.SpotId = spotId;
			this.Waypoints = new Dictionary<ArbiterParkingSpotWaypointId, ArbiterParkingSpotWaypoint>();

			// set waypoints
			foreach (ArbiterParkingSpotWaypoint apsw in waypoints)
			{
				this.Waypoints.Add(apsw.WaypointId, apsw);

				// set checkpoint
				if (apsw.IsCheckpoint)
				{
					this.Checkpoint = apsw;
				}
				else
				{
					this.NormalWaypoint = apsw;
				}
			}
		}

		public void SetWaypoints(List<ArbiterParkingSpotWaypoint> waypoints)
		{
			// set waypoints
			foreach (ArbiterParkingSpotWaypoint apsw in waypoints)
			{
				this.Waypoints.Add(apsw.WaypointId, apsw);

				// set checkpoint
				if (apsw.IsCheckpoint)
				{
					this.Checkpoint = apsw;
				}
				else
				{
					this.NormalWaypoint = apsw;
				}
			}
		}

		public LinePath GetSpotPath()
		{
			return new LinePath(new Coordinates[] { this.NormalWaypoint.Position, this.Checkpoint.Position });
		}

		public LinePath GetLeftBound()
		{
			return this.GetSpotPath().ShiftLateral(this.Width / 2.0);
		}

		public LinePath GetRightBound()
		{
			return this.GetSpotPath().ShiftLateral(-this.Width / 2.0);
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
			if (obj is ArbiterParkingSpot)
			{
				// check if the numbers are equal
				return ((ArbiterParkingSpot)obj).SpotId.Equals(this.SpotId);
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
			return this.SpotId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.SpotId.ToString();
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
			DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterParkingSpot, System.Drawing.Drawing2D.DashStyle.Solid,
				this.NormalWaypoint.Position, this.Checkpoint.Position, g, t);
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
			return DrawingUtility.DrawArbiterParkingSpot;
		}

		#endregion
	}
}
