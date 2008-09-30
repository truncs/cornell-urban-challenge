using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using System.Drawing;
using UrbanChallenge.Common.Shapes;
using RemoraAdvanced.Common;

namespace RemoraAdvanced.Display.DisplayObjects
{
	/// <summary>
	/// Dispays arbiter information
	/// </summary>
	public class ArbiterInformationDisplay : IDisplayObject
	{
		public ArbiterInformation information = null;

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
			ArbiterInformation tmpInfo = information;

			if (tmpInfo != null)
			{
				lock (tmpInfo)
				{
					if (tmpInfo.Route1 != null && tmpInfo.Route1.RoutePlan != null && DrawingUtility.DrawNavigationBestRoute)
					{
						Color c = DrawingUtility.ColorNavigationBest;

						for (int j = 0; j < tmpInfo.Route1.RoutePlan.Count; j++)
						{
							if (j == 0 && RemoraCommon.Communicator.GetVehicleState() != null)
							{
								DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid, RemoraCommon.Communicator.GetVehicleState().Position, tmpInfo.Route1.RoutePlan[j], g, t);
							}
							else if(RemoraCommon.Communicator.GetVehicleState() != null)
							{
								DrawingUtility.DrawColoredControlLine(c, System.Drawing.Drawing2D.DashStyle.Solid, tmpInfo.Route1.RoutePlan[j - 1], tmpInfo.Route1.RoutePlan[j], g, t);
							}
						}
					}

					
					foreach (ArbiterInformationDisplayObject aido in tmpInfo.DisplayObjects)
					{
						if (aido.Type == ArbiterInformationDisplayObjectType.uTurnPolygon)
						{
							Polygon p = (Polygon)aido.DisplayObject;
							if (p.Count > 1)
							{
								for (int i = 1; i < p.Count; i++)
								{
									DrawingUtility.DrawColoredControlLine(Color.Orange, System.Drawing.Drawing2D.DashStyle.Solid, p[i - 1], p[i], g, t);
								}

								DrawingUtility.DrawColoredControlLine(Color.Orange, System.Drawing.Drawing2D.DashStyle.Solid, p[0], p[p.Count - 1], g, t);
							}
						}
						else if (aido.Type == ArbiterInformationDisplayObjectType.leftBound)
						{
							LineList ll = (LineList)aido.DisplayObject;
							if (ll.Count > 1)
							{
								for (int i = 1; i < ll.Count; i++)
								{
									DrawingUtility.DrawColoredControlLine(Color.Blue, System.Drawing.Drawing2D.DashStyle.Solid, ll[i - 1], ll[i], g, t);
								}
							}
						}
						else if (aido.Type == ArbiterInformationDisplayObjectType.rightBound)
						{
							LineList ll = (LineList)aido.DisplayObject;
							if (ll.Count > 1)
							{
								for (int i = 1; i < ll.Count; i++)
								{
									DrawingUtility.DrawColoredControlLine(Color.Red, System.Drawing.Drawing2D.DashStyle.Solid, ll[i - 1], ll[i], g, t);
								}
							}
						}
					}
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
				return SelectionType.NotSelected;
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
			return DrawingUtility.DrawNavigationRoutes;
		}

		#endregion
	}
}
