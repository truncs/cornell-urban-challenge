using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using RndfToolkit;
using UrbanChallenge.Common.EarthModel;
using System.Drawing;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace RndfEditor.Tools
{
	/// <summary>
	/// Analyzes points
	/// </summary>
	public class PointAnalysisTool : IDisplayObject, IEditorTool
	{
		private PlanarProjection projection;
		public bool snapToWaypoints;
		public ArbiterRoadNetwork roadNetwork;
		private WorldTransform wt;

		/// <summary>
		/// Current point
		/// </summary>
		public Coordinates Current;

		/// <summary>
		/// Display these as well, can be set based upon primary tool
		/// </summary>
		public List<Coordinates> Save;

		public PointAnalysisTool(PlanarProjection projection, bool snap, ArbiterRoadNetwork arn, WorldTransform wt)
		{
			this.projection = projection;
			this.snapToWaypoints = snap;
			this.roadNetwork = arn;
			this.wt = wt;
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
			if (!this.snapToWaypoints || this.roadNetwork == null)
			{
				if (this.Current != null)
				{
					LLACoord lla = GpsTools.XyToLlaDegrees(Current, projection);

					string locString = Current.X.ToString("F6") + ", " + Current.Y.ToString("F6") + "\n" +
						lla.lat.ToString("F6") + ", " + lla.lon.ToString("F6") + "\n" + GpsTools.LlaDegreesToArcMinSecs(lla);

					DrawingUtility.DrawControlPoint(this.Current, DrawingUtility.ColorToolPointAnalysis, locString,
						ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
				}

				if (Save != null && Save.Count > 0)
				{
					foreach (Coordinates tmp in Save)
					{
						LLACoord lla = GpsTools.XyToLlaDegrees(tmp, projection);

						string locString = tmp.X.ToString("F6") + ", " + tmp.Y.ToString("F6") + "\n" +
							lla.lat.ToString("F6") + ", " + lla.lon.ToString("F6") + "\n" + GpsTools.LlaDegreesToArcMinSecs(lla);

						DrawingUtility.DrawControlPoint(tmp, DrawingUtility.ColorToolPointAnalysis, locString,
							ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
					}
				}
			}
			else
			{
				if (this.Current != null)
				{
					Coordinates c = this.Current;
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
						c = closest.Value;

					LLACoord lla = GpsTools.XyToLlaDegrees(c, projection);

					string locString = c.X.ToString("F6") + ", " + c.Y.ToString("F6") + "\n" +
						lla.lat.ToString("F6") + ", " + lla.lon.ToString("F6") + "\n" + GpsTools.LlaDegreesToArcMinSecs(lla);

					DrawingUtility.DrawControlPoint(c, DrawingUtility.ColorToolPointAnalysis, locString,
						ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
				}
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
			return DrawingUtility.DrawToolPointAnalysis;
		}

		#endregion
	}
}
