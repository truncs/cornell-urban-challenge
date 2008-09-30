using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using RndfEditor.Display.Utilities;
using System.Drawing.Drawing2D;
using System.Drawing;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents an intersection exit that has a stop sign attached
	/// </summary>
	[Serializable]
	public class ArbiterStoppedExit : IDisplayObject, INetworkObject
	{
		#region Stopped Exit Members

		/// <summary>
		/// Waypoint represeting the stopped exit
		/// </summary>
		public ArbiterWaypoint Waypoint;

		/// <summary>
		/// Polygon representing absolute area we care about the exit
		/// </summary>
		public Polygon ExitPolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="aw"></param>
		/// <param name="ep"></param>
		public ArbiterStoppedExit(ArbiterWaypoint aw, Polygon ep)
		{
			this.Waypoint = aw;
			this.ExitPolygon = ep;
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
			// show stopped exits (blue diags)
			HatchBrush hBrush1 = new HatchBrush(HatchStyle.ForwardDiagonal, DrawingUtility.ColorArbiterIntersectionStoppedExit, Color.White);

			// populate polygon
			List<PointF> polyPoints = new List<PointF>();
			foreach (Coordinates c in this.ExitPolygon.points)
			{
				polyPoints.Add(DrawingUtility.ToPointF(c));
			}

			// draw poly and fill
			g.FillPolygon(hBrush1, polyPoints.ToArray());
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
			if (obj is ArbiterStoppedExit)
			{
				return ((ArbiterStoppedExit)obj).Waypoint.Equals(this.Waypoint);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return this.Waypoint.ToString();
		}

		#endregion
	}
}
