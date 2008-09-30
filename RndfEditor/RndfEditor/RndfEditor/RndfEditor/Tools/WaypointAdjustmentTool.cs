using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;

namespace RndfEditor.Tools
{
	/// <summary>
	/// Tool for modifying positions of waypoints
	/// </summary>
	public class WaypointAdjustmentTool : IEditorTool, IDisplayObject
	{
		private Coordinates original;
		private IDisplayObject waypoint;
		private WorldTransform t;

		/// <summary>
		/// Constructor
		/// </summary>
		public WaypointAdjustmentTool(WorldTransform wt)
		{
			this.t = wt;
		}

		/// <summary>
		///  lets us know when in move
		/// </summary>
		public bool CheckInMove
		{
			get
			{
				return this.waypoint != null;
			}
		}

		/// <summary>
		/// Sets the waypoint we're using
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="orig"></param>
		public void SetWaypoint(IDisplayObject obj, Coordinates orig)
		{
			this.original = orig;
			this.waypoint = obj;
			obj.BeginMove(orig, t);
		}

		/// <summary>
		/// Move to a new coordinate
		/// </summary>
		/// <param name="update"></param>
		public void Move(Coordinates update)
		{
			Coordinates offset = update - original;
			waypoint.InMove(original, offset, t);
		}
		
		/// <summary>
		/// Cancel the move
		/// </summary>
		public void CancelMove()
		{
			waypoint.CancelMove(original, t);
			this.waypoint = null;			
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void BeginMove(Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
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
			return false;
		}

		#endregion
	}
}
