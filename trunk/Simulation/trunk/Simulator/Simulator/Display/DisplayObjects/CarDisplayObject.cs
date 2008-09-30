using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using RndfEditor.Display.Utilities;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace Simulator.Display.DisplayObjects
{
	/// <summary>
	/// Type of rear axle the car has
	/// </summary>
	public enum RearAxleType
	{
		Center,
		Rear
	}

	/// <summary>
	/// Draws a vehicle
	/// </summary>
	[Serializable]
	public abstract class CarDisplayObject : IDisplayObject
	{
		#region Car Display

		#region Private Members

		private float wheelbase = 2.9464f;
		private float tireDiameter = 0.7874f;
		private float tireWidth = 0.28f;
		private float nomPixelWidth = 2;
		private bool snapHeadingOnDrag = true;		
		private bool snapPositionOnDrag = true;

		#endregion

		#region Properties

		/// <summary>
		/// snap heading to heading on drag
		/// </summary>
		[CategoryAttribute("Drag Settings"), DescriptionAttribute("Snap heading to closest partition heading on drag")]
		public bool SnapHeading
		{
			get { return snapHeadingOnDrag; }
			set { snapHeadingOnDrag = value; }
		}

		/// <summary>
		/// snap position to partitions on drap
		/// </summary>
		[CategoryAttribute("Drag Settings"), DescriptionAttribute("Snap position to closest point on any partition on drag")]
		public bool SnapPosition
		{
			get { return snapPositionOnDrag; }
			set { snapPositionOnDrag = value; }
		}

		/// <summary>
		/// Type of rear axle
		/// </summary>
		public abstract RearAxleType RearAxleType
		{
			get;
		}

		/// <summary>
		/// Position of vehicle's rear axle
		/// </summary>
		public abstract Coordinates Position
		{
			get;
			set;
		}

		/// <summary>
		/// Heading of vehicle
		/// </summary>
		public abstract Coordinates Heading
		{
			get;
			set;
		}

		/// <summary>
		/// Width of vehicle in meters
		/// </summary>
		public abstract double Width
		{
			get;
			set;
		}

		/// <summary>
		/// Length of the vehicle in meters
		/// </summary>
		public abstract double Length
		{
			get;
			set;
		}

		/// <summary>
		/// Color of the vehicle
		/// </summary>
		protected abstract Color color
		{
			get;
		}

		/// <summary>
		/// Selection type
		/// </summary>
		protected abstract SelectionType selectionType
		{
			get;
		}

		/// <summary>
		/// steering angle
		/// </summary>
		protected abstract float steeringAngle
		{
			get;			
		}

		/// <summary>
		/// Id of vehicle
		/// </summary>
		protected abstract string Id
		{
			get;
		}

		#endregion

		#region Display Objects

		/// <summary>
		/// Gets rear offset of body
		/// </summary>
		private float rearOffset
		{
			get
			{
				if (this.RearAxleType == RearAxleType.Center)
				{
					return 0;
				}
				else
				{
					return 1.2192f;
				}
			}
		}

		/// <summary>
		/// determines wheel offset
		/// </summary>
		private float wheelOffset
		{
			get {	return (float)(this.Width) / 2 - tireWidth; }
		}

		/// <summary>
		/// right wheel rectangle
		/// </summary>
		private RectangleF wheelRectR
		{
			get { return RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2); }
		}

		/// <summary>
		/// left wheel rectangle
		/// </summary>
		private RectangleF wheelRectL
		{
			get
			{ return RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2); }
		}

		#endregion

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			// Determine size of bounding box
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = DrawingUtility.cp_large_size;

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)this.Position.X - scaled_size / 2, (float)this.Position.Y - scaled_size / 2, scaled_size, scaled_size);

			// return
			return rect;
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			// check filter
			if (filter(this))
			{
				// get bounding box dependent on tolerance
				RectangleF bounding = this.GetBoundingBox(wt);
				bounding.Inflate(tol, tol);

				// check if contains point
				if (bounding.Contains(DrawingUtility.ToPointF(loc)))
				{
					return new HitTestResult(this, true, (float)loc.DistanceTo(this.Position));
				}
			}

			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			// width of drawing
			float penWidth = nomPixelWidth / t.Scale;

			// body rectangle
			RectangleF bodyRect = new RectangleF((float)(-this.Width / 2), -rearOffset, (float)Width, (float)Length);

			// body transformation matrix
			Matrix bodyTrans = new Matrix();
			bodyTrans.Rotate((float)(this.Heading.ToDegrees() - 90));
			bodyTrans.Translate((float)Position.X, (float)Position.Y, MatrixOrder.Append);

			// save original world transformation matrix
			Matrix origTrans = g.Transform.Clone();
			bodyTrans.Multiply(g.Transform, MatrixOrder.Append);

			// set the new transform
			g.Transform = bodyTrans;

			// make a new pen and draw wheels
			using (Pen p = new Pen(color, penWidth))
			{
				DrawRectangle(g, p, bodyRect);

				// build the transform for the rear wheels
				// do the left wheel
				Matrix wheelTransform = bodyTrans.Clone();
				wheelTransform.Translate(-wheelOffset, 0, MatrixOrder.Prepend);
				g.Transform = wheelTransform;
				g.FillRectangle(Brushes.White, wheelRectL);
				DrawRectangle(g, p, wheelRectL);

				// do the right wheel
				wheelTransform = bodyTrans.Clone();
				wheelTransform.Translate(wheelOffset, 0, MatrixOrder.Prepend);
				g.Transform = wheelTransform;
				g.FillRectangle(Brushes.White, wheelRectR);
				DrawRectangle(g, p, wheelRectR);
				
				// do the front wheels
				// do the left wheel
				wheelTransform = bodyTrans.Clone();
				wheelTransform.Translate(-wheelOffset, wheelbase, MatrixOrder.Prepend);
				wheelTransform.Rotate(steeringAngle * 180 / (float)Math.PI, MatrixOrder.Prepend);
				g.Transform = wheelTransform;
				g.FillRectangle(Brushes.White, wheelRectL);
				DrawRectangle(g, p, wheelRectL);
			
				// do the right wheel
				wheelTransform = bodyTrans.Clone();
				wheelTransform.Translate(wheelOffset, wheelbase, MatrixOrder.Prepend);
				wheelTransform.Rotate(steeringAngle * 180 / (float)Math.PI, MatrixOrder.Prepend);
				g.Transform = wheelTransform;
				g.FillRectangle(Brushes.White, wheelRectR);
				DrawRectangle(g, p, wheelRectR);
			}

			// return to normal transformation
			g.Transform = origTrans;

			// draw car center point
			DrawingUtility.DrawControlPoint(this.Position, this.color, null, ContentAlignment.MiddleCenter, ControlPointStyle.LargeX, g, t);

			// check if should draw id
			if (DrawingUtility.DrawSimCarId)
			{
				// get length
				double idOffset = this.Length / 2;

				if (this.RearAxleType == RearAxleType.Center)
				{
					idOffset = this.Length / 3;
				}

				// get label position
				Coordinates labelPosition = this.Position + this.Heading.Normalize(idOffset);

				// draw label					
				DrawingUtility.DrawControlLabel(labelPosition, this.color, this.Id, ContentAlignment.MiddleCenter, ControlPointStyle.None, g, t);
			}
		}

		private void DrawRectangle(Graphics g, Pen p, RectangleF r)
		{
			g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
		}

		public abstract bool MoveAllowed
		{
			get;
		}

		public abstract void BeginMove(Coordinates orig, WorldTransform t);

		public abstract void InMove(Coordinates orig, Coordinates offset, WorldTransform t);

		public abstract void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t);

		public abstract void CancelMove(Coordinates orig, WorldTransform t);

		public abstract SelectionType Selected
		{
			get;
			set;
		}

		[Browsable(false)]
		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public abstract bool CanDelete
		{
			get;
		}

		public abstract List<IDisplayObject> Delete();

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			return true;
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawSimCars;
		}

		#endregion
	}
}
