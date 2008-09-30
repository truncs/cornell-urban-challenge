using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;
using Remora.Vehicles;

namespace Remora.Display
{
	public class CarDisplay : IDisplayObject
	{
		// Constants
		private const float length = 5.1308f;
		private const float width = 2.0066f;
		private const float wheelbase = 2.9464f;
		private const float rearOffset = 1.2192f;
		private const float tireDiameter = 0.7874f;
		private const float tireWidth = 0.28f;
		private const float wheelOffset = width / 2 - tireWidth;
		private const float nomPixelWidth = 2;

		private Coordinates position;
		private Coordinates heading;

		private double Heading
		{
			get
			{
				return this.heading.ToDegrees() * Math.PI / 180.0;
			}
		}

		// Private Members
		private float steeringAngle;
		private RectangleF bodyRect;
		private RectangleF wheelRectR, wheelRectL; // left and right wheel rectangle
		private Color color = DrawingUtility.OurVehicleColor;

		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		public CarDisplay()
		{
			bodyRect = new RectangleF(-width / 2, -rearOffset, width, length);
			wheelRectL = RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2);
			wheelRectR = RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2);
		}

		/// <summary>
		/// Full Constructor
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="velocity"></param>
		public CarDisplay(Coordinates position, Coordinates velocity, bool aiVehicle)
		{
			bodyRect = new RectangleF(-width / 2, -rearOffset, width, length);
			wheelRectL = RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2);
			wheelRectR = RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2);

			this.position = position;
			this.heading = velocity;

			if (aiVehicle)
				color = DrawingUtility.OurVehicleColor;
			else
				color = DrawingUtility.NormalVehicleColor;
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
			Coordinates wll = t.WorldLowerLeft;
			Coordinates wup = t.WorldUpperRight;

			if ((position.X < wll.X || position.X > wup.X || position.Y < wll.Y || position.Y > wup.Y))
			{
				return;
			}

			Matrix bodyTrans = new Matrix();
			bodyTrans.Rotate((float)(this.Heading) * 180 / (float)Math.PI - 90);
			bodyTrans.Translate((float)position.X, (float)position.Y, MatrixOrder.Append);

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
				wheelTransform.Translate(-wheelOffset, 0, MatrixOrder.Prepend);
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
				wheelTransform.Translate(wheelOffset, 0, MatrixOrder.Prepend);
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
				wheelTransform.Translate(-wheelOffset, wheelbase, MatrixOrder.Prepend);
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
				wheelTransform.Translate(wheelOffset, wheelbase, MatrixOrder.Prepend);
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
			DrawingUtility.DrawControlPoint(this.position, color, null, ContentAlignment.MiddleCenter, ControlPointStyle.LargeX, g, t);

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
