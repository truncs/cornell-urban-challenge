using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using System.Drawing;

namespace RndfEditor.Tools
{
	/// <summary>
	/// Measures an angle
	/// </summary>
	public class AngleMeasureTool : IEditorTool, IDisplayObject
	{
		private Coordinates? p1;
		private Coordinates? p2;
		private Coordinates? p3;

		public bool SetP1;
		public bool SetP2;
		public bool SetP3;

		/// <summary>
		/// Constructor
		/// </summary>
		public AngleMeasureTool()
		{
		}

		/// <summary>
		/// Accessor for first point
		/// </summary>
		public Coordinates? P1
		{
			get
			{
				return p1;
			}
			set
			{
				p1 = value;
			}
		}

		/// <summary>
		/// Accessor for second point
		/// </summary>
		public Coordinates? P2
		{
			get
			{
				return p2;
			}
			set
			{
				p2 = value;
			}
		}

		/// <summary>
		/// Accessor for 3rd point
		/// </summary>
		public Coordinates? P3
		{
			get
			{
				return p3;
			}
			set
			{
				p3 = value;
			}
		}

		/// <summary>
		/// Sets teh first point
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="secondaryTool"></param>
		public void VSetP1(Coordinates p1, IEditorTool secondaryTool)
		{
			this.SetP1 = true;
			this.p1 = p1;

			if (secondaryTool != null && secondaryTool is PointAnalysisTool)
			{
				PointAnalysisTool pat = (PointAnalysisTool)secondaryTool;

				pat.Save = new List<Coordinates>();
				pat.Save.Add(p1);
			}
		}

		/// <summary>
		/// Sets teh second point
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="secondaryTool"></param>
		public void VSetP2(Coordinates p2, IEditorTool secondaryTool)
		{
			this.p2 = p2;
			this.SetP2 = true;

			if (secondaryTool != null && secondaryTool is PointAnalysisTool)
			{
				PointAnalysisTool pat = (PointAnalysisTool)secondaryTool;
				pat.Save.Add(p2);
			}
		}

		/// <summary>
		/// Sets teh third point
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="secondaryTool"></param>
		public void VSetP3(IEditorTool secondaryTool)
		{
			this.Reset(secondaryTool);
		}

		/// <summary>
		/// Resets values
		/// </summary>
		/// <param name="secondaryTool"></param>
		public void Reset(IEditorTool secondaryTool)
		{
			this.p1 = null;
			this.p2 = null;
			this.p3 = null;

			this.SetP1 = false;
			this.SetP2 = false;
			this.SetP3 = false;

			if (secondaryTool != null && secondaryTool is PointAnalysisTool)
			{
				PointAnalysisTool pat = (PointAnalysisTool)secondaryTool;
				pat.Save = new List<Coordinates>();
			}
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			Color c = DrawingUtility.ColorToolAngle;

			if (this.p1 != null && this.p2 != null && this.p3 != null)
			{	
				Coordinates p2p1 = this.p1.Value - this.p2.Value;
				Coordinates p2p3 = this.p3.Value - this.p2.Value;

				double angleTmp = (p2p1.Dot(p2p3) / (p2p1.Length * p2p3.Length));
				angleTmp = Math.Acos(angleTmp);
				double angleDeg = angleTmp * 180.0 / Math.PI;

				//double angleDeg = p2p1.ToDegrees() - p2p3.ToDegrees();
				string angle = angleDeg.ToString("F6") + " deg";

				DrawingUtility.DrawControlPoint(this.p1.Value, c, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawControlPoint(this.p2.Value, c, angle, ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawControlPoint(this.p3.Value, c, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid, this.p1.Value, this.p2.Value, g, t);
				DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid, this.p2.Value, this.p3.Value, g, t);
			}
			else if (this.p1 != null && this.p2 != null)
			{
				DrawingUtility.DrawControlPoint(this.p1.Value, c, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawControlPoint(this.p2.Value, c, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid, this.p1.Value, this.p2.Value, g, t);
			}
		}

		public bool MoveAllowed
		{
			get { return false; }
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
				return SelectionType.NotSelected;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
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
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawToolAngle;
		}

		#endregion
	}
}
