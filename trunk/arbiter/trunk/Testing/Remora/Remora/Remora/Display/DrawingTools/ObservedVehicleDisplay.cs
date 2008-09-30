using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;
using Remora.Vehicles;
using UrbanChallenge.Common.Sensors.Vehicle;

namespace Remora.Display
{
	/// <summary>
	/// Displays an observed vehicle
	/// </summary>
	public class ObservedVehicleDisplay : IDisplayObject
	{
		// members
		private ObservedVehicle observedVehicle;

		// Constants
		private const float tireDiameter = 0.7874f;
		private const float tireWidth = 0.28f;
		private const float nomPixelWidth = 2;

		private float WheelOffset
		{
			get { return Width / 2 - tireWidth; }
		}

		private float RearOffset
		{
			get { return 1.2192f / 5.1308f * this.Length; }
		}

		private float WheelBase
		{
			get { return this.RearOffset; }//(2.9464f / 5.1308f) * this.Length; }
		}

		private float Width
		{
			get { return (float)this.observedVehicle.Width; }
		}

		private float Length
		{
			get { return (float)this.observedVehicle.Length; }
		}

		private Coordinates Position
		{
			get { return this.observedVehicle.AbsolutePosition; }
		}

		private double Heading
		{
			get
			{
				return this.observedVehicle.Heading.ToDegrees() * Math.PI / 180.0;
			}
		}

		// Private Members
		private float steeringAngle;
		private RectangleF bodyRect;
		private RectangleF wheelRectR, wheelRectL; // left and right wheel rectangle
		private Color color = DrawingUtility.OurVehicleColor;

		#region Constructors

		/// <summary>
		/// Full Constructor
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="velocity"></param>
		public ObservedVehicleDisplay(ObservedVehicle observedVehicle)
		{
			// set the vehicle
			this.observedVehicle = observedVehicle;

			// set the vehicle's color
			if (observedVehicle.ObservationState == ObservedVehicleState.Normal)
			{
				color = Color.Green;
			}
			else if (observedVehicle.ObservationState == ObservedVehicleState.Occluded)
			{
				color = Color.Red;
			}
			else
			{
				color = Color.Black;
			}

			bodyRect = new RectangleF(-Width / 2, -RearOffset, Width, Length);
			wheelRectL = RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2);
			wheelRectR = RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2);
		}

		public ObservedVehicleDisplay(ObservedVehicle observedVehicle, Color c)
		{
			// set the vehicle
			this.observedVehicle = observedVehicle;

			color = c;

			bodyRect = new RectangleF(-Width / 2, -RearOffset, Width, Length);
			wheelRectL = RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2);
			wheelRectR = RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2);
		}

		#endregion

		#region Fields

		public float SteeringAngle
		{
			get { return -steeringAngle / 0.027222084f; }
			set { steeringAngle = -value * 0.027222084f; }
		}

		#endregion Fields

		#region IDisplayObject Members

		public RectangleF BoundingBox
		{
			get { return RectangleF.Empty; }
		}

		public HitTestResult HitTest(Coordinates loc, float tol)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		private void DrawRectangle(Graphics g, Pen p, RectangleF r)
		{
			try
			{
				g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
			}
			catch (Exception)
			{
			}
		}

		public void Render(Graphics g, WorldTransform t)
		{
			if (this.observedVehicle.ObservationState != ObservedVehicleState.Deleted ||
				(this.observedVehicle.ObservationState == ObservedVehicleState.Deleted &&
				DrawingUtility.DisplayDeletedVehicles))
			{
				Coordinates wll = t.WorldLowerLeft;
				Coordinates wup = t.WorldUpperRight;

				if ((Position.X < wll.X || Position.X > wup.X || Position.Y < wll.Y || Position.Y > wup.Y))
				{
					return;
				}

				Matrix bodyTrans = new Matrix();
				bodyTrans.Rotate((float)(this.Heading) * 180 / (float)Math.PI - 90);
				bodyTrans.Translate((float)Position.X, (float)Position.Y, MatrixOrder.Append);

				Matrix origTrans = g.Transform.Clone();
				bodyTrans.Multiply(g.Transform, MatrixOrder.Append);
				g.Transform = bodyTrans;

				float penWidth = nomPixelWidth / t.Scale;
				using (Pen p = new Pen(color, penWidth))
				{
					DrawRectangle(g, p, bodyRect);

					// build the transform for the rear wheels
					// do the left wheel
					Matrix wheelTransform = bodyTrans.Clone();
					wheelTransform.Translate(-WheelOffset, 0, MatrixOrder.Prepend);
					try
					{
						g.Transform = wheelTransform;
						g.FillRectangle(Brushes.White, wheelRectL);
						DrawRectangle(g, p, wheelRectL);
					}
					catch (Exception)
					{
					}

					// do the right wheel
					wheelTransform = bodyTrans.Clone();
					wheelTransform.Translate(WheelOffset, 0, MatrixOrder.Prepend);
					try
					{
						g.Transform = wheelTransform;
						g.FillRectangle(Brushes.White, wheelRectR);
						DrawRectangle(g, p, wheelRectR);
					}
					catch (Exception)
					{
					}

					// do the front wheels
					// do the left wheel
					wheelTransform = bodyTrans.Clone();
					wheelTransform.Translate(-WheelOffset, WheelBase, MatrixOrder.Prepend);
					wheelTransform.Rotate(steeringAngle * 180 / (float)Math.PI, MatrixOrder.Prepend);
					try
					{
						g.Transform = wheelTransform;
						g.FillRectangle(Brushes.White, wheelRectL);
						DrawRectangle(g, p, wheelRectL);
					}
					catch (Exception)
					{
					}

					// do the right wheel
					wheelTransform = bodyTrans.Clone();
					wheelTransform.Translate(WheelOffset, WheelBase, MatrixOrder.Prepend);
					wheelTransform.Rotate(steeringAngle * 180 / (float)Math.PI, MatrixOrder.Prepend);
					try
					{
						g.Transform = wheelTransform;
						g.FillRectangle(Brushes.White, wheelRectR);
						DrawRectangle(g, p, wheelRectR);
					}
					catch (Exception)
					{
					}
				}

				g.Transform = origTrans;

				// draw Position					
				DrawingUtility.DrawControlPoint(this.Position, color, this.observedVehicle.Id.ToString(), ContentAlignment.MiddleCenter, ControlPointStyle.LargeBox, g, t);

				Coordinates head = this.Position + this.observedVehicle.Heading.Normalize(this.observedVehicle.Length / 2.0);
				DrawingUtility.DrawControlLine(this.Position, head, color, g, t);
			}
		}

		public bool MoveAllowed
		{
			get { return false; }
		}

		public void BeingMove(Coordinates orig, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
		{
			throw new NotSupportedException();
		}

		public SelectionMode Selected
		{
			get
			{
				return SelectionMode.NotSelected;
			}
			set
			{

			}
		}

		public IDisplayObject Parent
		{
			get { return null; }
		}

		#endregion
	}
}

