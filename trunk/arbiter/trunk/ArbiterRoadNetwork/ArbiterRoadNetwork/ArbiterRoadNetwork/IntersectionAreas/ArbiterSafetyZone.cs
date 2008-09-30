using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Path;
using System.Drawing;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents a safety zone
	/// </summary>
	[Serializable]
	public class ArbiterSafetyZone : IDisplayObject, INetworkObject
	{
		#region Safety Zone Members

		// lane the safety zone is a part of
		public ArbiterLane lane;

		// these on the partition path
		public LinePath.PointOnPath safetyZoneEnd;
		public LinePath.PointOnPath safetyZoneBegin;

		/// <summary>
		/// If the safety zone represents an exit
		/// </summary>
		public bool isExit;

		/// <summary>
		/// the exit this safety zone is associated with
		/// </summary>
		public ArbiterWaypoint Exit;

		public Polygon SafetyPolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="safetyZoneEnd"></param>
		/// <param name="safetyZoneBegin"></param>
		public ArbiterSafetyZone(ArbiterLane lane, LinePath.PointOnPath safetyZoneEnd, LinePath.PointOnPath safetyZoneBegin)
		{
			this.safetyZoneBegin = safetyZoneBegin;
			this.safetyZoneEnd = safetyZoneEnd;
			this.lane = lane;
			this.GenerateSafetyZone();
		}

		private void GenerateSafetyZone()
		{
			if (!safetyZoneBegin.Location.Equals(safetyZoneEnd.Location))
			{
				LinePath lp = new LinePath(new Coordinates[] { safetyZoneBegin.Location, safetyZoneEnd.Location });
				LinePath lp1 = lp.ShiftLateral(-lane.Width / 2.0);
				LinePath lp2 = lp.ShiftLateral(lane.Width / 2.0);
				List<Coordinates> aszCoords = lp2;
				aszCoords.AddRange(lp1);
				this.SafetyPolygon = Polygon.GrahamScan(aszCoords);
			}
			else
			{
			}
		}

		/// <summary>
		/// Checks if a coordinate is in a safety zone
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool IsInSafety(Coordinates c)
		{
			LinePath.PointOnPath pop = lane.GetClosestPoint(c);
			return this.IsInSafety(pop);
		}

		/// <summary>
		/// checks if a pointon path is in a safety zone
		/// </summary>
		/// <param name="pop"></param>
		/// <returns></returns>
		public bool IsInSafety(LinePath.PointOnPath pop)
		{
			double distIn = lane.LanePath().DistanceBetween(safetyZoneBegin, pop);

			if (distIn < 30 && distIn >= 0)
				return true;
			else
				return false;
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			if (this.SafetyPolygon == null)
			{
				/*if (this.safetyZoneBegin.Equals(this.safetyZoneEnd))
				{
					Console.WriteLine("dupilicate safety zone changed" + this.safetyZoneBegin.Location.ToString());
					this.safetyZoneBegin = this.lane.LanePath().StartPoint;
				}*/

				this.GenerateSafetyZone();
			}

			if (this.SafetyPolygon.IsInside(loc) && DrawingUtility.DrawArbiterSafetyZones)
			{
				Coordinates center = new Coordinates((this.safetyZoneBegin.Location.X + this.safetyZoneEnd.Location.X) / 2.0,
					(this.safetyZoneEnd.Location.Y + this.safetyZoneBegin.Location.Y) / 2.0);
				return new HitTestResult(this, true, (float)loc.DistanceTo(center));
			}
			else
				return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{			
			Color c = DrawingUtility.ColorArbiterSafetyZone;
			HatchBrush hb = new HatchBrush(HatchStyle.ForwardDiagonal, c, Color.White);

			using (Pen p = new Pen(hb, 3))
			{
				g.DrawLine(p, 
					DrawingUtility.ToPointF(safetyZoneBegin.Location),
					DrawingUtility.ToPointF(safetyZoneEnd.Location));
			}
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
			return DrawingUtility.DrawArbiterSafetyZones;
		}

		#endregion
	}
}
