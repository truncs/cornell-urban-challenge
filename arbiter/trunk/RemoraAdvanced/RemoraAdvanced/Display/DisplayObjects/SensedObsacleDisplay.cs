using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;
using RemoraAdvanced.Common;

namespace RemoraAdvanced.Display.DisplayObjects
{
	public class SensedObsacleDisplay : IDisplayObject
	{
		public SceneEstimatorUntrackedClusterCollection untrackedClusters;
		public SceneEstimatorTrackedClusterCollection trackedClusters;

		public SensedObsacleDisplay()
		{
			untrackedClusters = new SceneEstimatorUntrackedClusterCollection();
			untrackedClusters.clusters = new SceneEstimatorUntrackedCluster[] { };

			trackedClusters = new SceneEstimatorTrackedClusterCollection();
			trackedClusters.clusters = new SceneEstimatorTrackedCluster[] { };
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			return new System.Drawing.RectangleF(0, 0, 1, 1);
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{			
			Polygon p = new Polygon();
			Coordinates tCoord = new Coordinates();
			Coordinates loc = RemoraCommon.Communicator.GetVehicleState().Position;

			// body transformation matrix
			Matrix bodyTrans = new Matrix();
			bodyTrans.Rotate((float)(RemoraCommon.Communicator.GetVehicleState().Heading.ToDegrees()));
			bodyTrans.Translate((float)loc.X, (float)loc.Y, MatrixOrder.Append);

			// save original world transformation matrix
			Matrix origTrans = g.Transform.Clone();
			bodyTrans.Multiply(g.Transform, MatrixOrder.Append);

			// set the new transform
			g.Transform = bodyTrans;

			if (DrawingUtility.DrawSimObstacles)
			{
				if (untrackedClusters != null && untrackedClusters.clusters != null)
				{
					for (int i = 0; i < untrackedClusters.clusters.Length; i++)
					{
						if (untrackedClusters.clusters[i].points.Length > 4)
						{
							tCoord = untrackedClusters.clusters[i].points[0];
							if (Math.Abs(tCoord.X) < 60 && Math.Abs(tCoord.Y) < 60)
							{
								p = Polygon.GrahamScan(new List<Coordinates>(untrackedClusters.clusters[i].points));
								DrawingUtility.DrawControlPolygon(p, Color.Purple, DashStyle.Solid, g, t);
							}
						}
					}
				}
			}

			if (DrawingUtility.DrawSimCars)
			{
				if (trackedClusters != null && trackedClusters.clusters != null)
				{
					for (int i = 0; i < trackedClusters.clusters.Length; i++)
					{
						Color c;
						
						if(this.trackedClusters.clusters[i].statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE)
						{
							if(this.trackedClusters.clusters[i].targetClass == SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE)
							{
								if(this.trackedClusters.clusters[i].isStopped)
									c = Color.Red;
								else
									c =DrawingUtility.ColorSimTrafficCar;
							}
							else
								c = Color.Pink;
						}
						else
							c = DrawingUtility.ColorSimDeletedCar;

						bool draw = this.trackedClusters.clusters[i].statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE ?
							DrawingUtility.DrawSimCars :
							DrawingUtility.DrawSimCars && DrawingUtility.DrawSimCarDeleted;

						if (draw)
						{
							if (trackedClusters.clusters[i].relativePoints.Length > 2)
							{
								tCoord = trackedClusters.clusters[i].relativePoints[0];
								if (Math.Abs(tCoord.X) < t.ScreenSize.Width / 1.5 && Math.Abs(tCoord.Y) < t.ScreenSize.Height / 1.5)
								{
									p = Polygon.GrahamScan(new List<Coordinates>(trackedClusters.clusters[i].relativePoints));
									DrawingUtility.DrawControlPolygon(p, c, DashStyle.Solid, g, t);
								}
							}
						}
					}
				}
			}

			g.Transform = origTrans;		
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
			return DrawingUtility.DrawSimObstacles;
		}

		#endregion
	}
}
