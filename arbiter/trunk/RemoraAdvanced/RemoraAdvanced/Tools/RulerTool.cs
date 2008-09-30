using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace RemoraAdvanced.Tools
{
	/// <summary>
	/// Ruler tool for the display
	/// </summary>
	public class RulerTool : IDisplayObject, IRemoraTool
	{
		public bool snapToWaypoints;
		public ArbiterRoadNetwork roadNetwork;
		private Coordinates? initial = null;
		private Coordinates? current = null;
		private WorldTransform wt;

		public RulerTool(bool snap, ArbiterRoadNetwork arn, WorldTransform wt)
		{
			this.snapToWaypoints = snap;
			this.roadNetwork = arn;
			this.wt = wt;
		}

		/// <summary>
		/// Initial Coordinate
		/// </summary>
		public Coordinates? Initial
		{
			get
			{
				return this.initial;
			}
			set
			{
				if (snapToWaypoints && roadNetwork != null && value != null)
				{
					Coordinates c = value.Value;
					double minDist = Double.MaxValue;
					Coordinates? closest = null;

					foreach (IArbiterWaypoint iaw in this.roadNetwork.ArbiterWaypoints.Values)
					{
						double d = iaw.Position.DistanceTo(c);
						if (d < minDist && ((IDisplayObject)iaw).HitTest(c, (float)0.2, wt, DrawingUtility.DefaultFilter).Hit)
						{
							minDist = d;
							closest = iaw.Position;
						}
					}

					if (closest != null)
						initial = closest;
					else
						initial = c;
				}
				else
				{
					this.initial = value;
				}
			}
		}

		/// <summary>
		/// Final Coordiante
		/// </summary>
		public Coordinates? Current
		{
			get
			{
				return this.current;
			}
			set
			{
				if (snapToWaypoints && roadNetwork != null && value != null)
				{
					Coordinates c = value.Value;
					double minDist = Double.MaxValue;
					Coordinates? closest = null;

					foreach (IArbiterWaypoint iaw in this.roadNetwork.ArbiterWaypoints.Values)
					{
						double d = iaw.Position.DistanceTo(c);
						if (d < minDist && ((IDisplayObject)iaw).HitTest(c, (float)0.2, wt, DrawingUtility.DefaultFilter).Hit)
						{
							minDist = d;
							closest = iaw.Position;
						}
					}

					if (closest != null)
						current = closest;
					else
						current = c;
				}
				else
				{
					this.current = value;
				}
			}
		}

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
			if (this.Initial != null && this.Current != null)
			{
				DrawingUtility.DrawControlPoint(this.Initial.Value, DrawingUtility.ColorToolRuler, null, System.Drawing.ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
				DrawingUtility.DrawControlPoint(this.Current.Value, DrawingUtility.ColorToolRuler, null, System.Drawing.ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);

				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorToolRuler, System.Drawing.Drawing2D.DashStyle.Solid,
					this.Initial.Value, this.Current.Value, g, t);

				Coordinates dir = this.Current.Value - this.Initial.Value;
				Coordinates final = this.Initial.Value + dir.Normalize(dir.Length / 2.0);

				string label = dir.Length.ToString("F6") + " m";

				DrawingUtility.DrawControlPoint(final, DrawingUtility.ColorToolRuler, label, System.Drawing.ContentAlignment.BottomCenter, ControlPointStyle.None, g, t);
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
			return DrawingUtility.DrawToolRuler;
		}

		#endregion
	}
}
