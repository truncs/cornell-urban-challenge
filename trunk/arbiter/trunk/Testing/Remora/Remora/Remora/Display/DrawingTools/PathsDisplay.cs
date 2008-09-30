using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Mapack;
using System.Drawing;
using UrbanChallenge.Common.Splines;

namespace Remora.Display
{
	/// <summary>
	/// Display paths
	/// </summary>
	public class PathsDisplay : IDisplayObject
	{
		VehicleState updatedState;
		VehicleState arbiterState;
		IPath path;

		public PathsDisplay(VehicleState updatedState, VehicleState arbiterState, IPath path)
		{
			this.updatedState = updatedState;
			this.arbiterState = arbiterState;
			this.path = path;
		}

		#region IDisplayObject Members

		public void Render(Graphics g, WorldTransform t)
		{
			if (DrawingUtility.DrawArbiterLanePath && arbiterState != null && this.path is Path)
			{
				this.RenderRelativePath(arbiterState, (Path)this.path, g, t, true);
			}

			if (DrawingUtility.DrawOperationalLanePath && updatedState != null && this.path is Path)
			{
				this.RenderRelativePath(updatedState, (Path)this.path, g, t, false);
			}
		}

		#endregion

		private void RenderRelativePath(VehicleState vs, Path transPath, Graphics g, WorldTransform t, bool isArbiterPath)
		{
			if((isArbiterPath && DrawingUtility.DrawArbiterLanePath) ||
				(!isArbiterPath && DrawingUtility.DrawOperationalLanePath))
			{
				// compute the rotation matrix to add in our vehicles rotation
				/*Matrix3 rotMatrix = new Matrix3(
					Math.Cos(vs.heading.ArcTan), -Math.Sin(vs.heading.ArcTan), 0,
					Math.Sin(vs.heading.ArcTan), Math.Cos(vs.heading.ArcTan), 0,
					0, 0, 1);

				// compute the translation matrix to move our vehicle's location
				Matrix3 transMatrix = new Matrix3(
					1, 0, vs.xyPosition.X,
					0, 1, vs.xyPosition.Y,
					0, 0, 1);

				// compute the combined transformation matrix
				Matrix3 m = rotMatrix * transMatrix;

				// clone, transform and add each segment to our path
				transPath.Transform(m);*/

				float nomPixelWidth = 2.0f;
				float penWidth = nomPixelWidth / t.Scale;
				Pen arbiterPen = new Pen(Color.FromArgb(100, DrawingUtility.ArbiterLanePath), penWidth);
				Pen operationalPen = new Pen(Color.FromArgb(100, DrawingUtility.OperationalLanePath), penWidth);

				// display path
				foreach (IPathSegment ps in transPath)
				{
					if (ps is BezierPathSegment)
					{
						BezierPathSegment seg = (BezierPathSegment)ps;
						CubicBezier cb = seg.cb; 

						if (isArbiterPath)
						{													
							g.DrawBezier(arbiterPen, DrawingUtility.ToPointF(cb.P0), DrawingUtility.ToPointF(cb.P1), DrawingUtility.ToPointF(cb.P2), DrawingUtility.ToPointF(cb.P3));
						}
						else
						{
							g.DrawBezier(operationalPen, DrawingUtility.ToPointF(cb.P0), DrawingUtility.ToPointF(cb.P1), DrawingUtility.ToPointF(cb.P2), DrawingUtility.ToPointF(cb.P3));
						}
					}
				}
			}
		}
	}
}
