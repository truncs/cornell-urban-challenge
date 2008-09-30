using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common.Shapes;

namespace Simulator.Engine
{
	/// <summary>
	/// An obstacle in the sim
	/// </summary>
	[Serializable]
	public class SimObstacle : IDisplayObject
	{
		#region Private Members

		private double width;
		private double length;		
		private bool movable = true;
		private SelectionType selected = SelectionType.NotSelected;
		private float nomPixelWidth = 2;
		private bool isBlockage = false;

		#endregion

		#region Public Members

		/// <summary>
		/// Heading of obstacle
		/// </summary>
		public Coordinates Heading;

		/// <summary>
		/// Position of Obstacle
		/// </summary>
		public Coordinates Position;

		/// <summary>
		/// Id of the obstacle
		/// </summary>
		public SimObstacleId ObstacleId;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="soi"></param>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		public SimObstacle(SimObstacleId soi, Coordinates position, Coordinates heading)
		{
			this.ObstacleId = soi;
			this.Position = position;
			this.Heading = heading;
			this.length = 4;
			this.width = 2;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="soi"></param>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		/// <param name="length"></param>
		/// <param name="width"></param>
		public SimObstacle(SimObstacleId soi, Coordinates position, Coordinates heading, double length, double width)
		{
			this.ObstacleId = soi;
			this.Position = position;
			this.Heading = heading;
			this.width = width;
			this.length = length;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Width of hte obstacle along heading
		/// </summary>
		[CategoryAttribute("Obstacle Settings"), DescriptionAttribute("Width of Obstacle")]
		public double Width
		{
			get { return width; }
			set { width = value; }
		}

		/// <summary>
		/// Length of obstacle around heading
		/// </summary>
		[CategoryAttribute("Obstacle Settings"), DescriptionAttribute("Length of Obstacle")]
		public double Length
		{
			get { return length; }
			set { length = value; }
		}

		/// <summary>
		/// Whether or not obstacle is a blockage
		/// </summary>
		[CategoryAttribute("Obstacle Type"), DescriptionAttribute("Blockage")]
		public bool Blockage
		{
			get { return isBlockage; }
			set { isBlockage = value; }
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			// Determine size of bounding box
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = DrawingUtility.cp_large_size / wt.Scale;

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
			// default color
			Color c = DrawingUtility.ColorSimObstacles;

			// check selection
			if (this.selected == SelectionType.SingleSelected)
				c = DrawingUtility.ColorSimSelectedObstacle;
			else if (this.isBlockage)
				c = DrawingUtility.ColorSimObstacleBlockage;

			// draw box
			// width of drawing
			float penWidth = nomPixelWidth / t.Scale;

			// body rectangle
			RectangleF bodyRect = new RectangleF((float)(-(float)this.Width / 2), (-(float)this.Length / 2), (float)Width, (float)Length);

			// body transformation matrix
			Matrix bodyTrans = new Matrix();
			bodyTrans.Rotate((float)(this.Heading.ToDegrees() - 90));
			bodyTrans.Translate((float)Position.X, (float)Position.Y, MatrixOrder.Append);

			// save original world transformation matrix
			Matrix origTrans = g.Transform.Clone();
			bodyTrans.Multiply(g.Transform, MatrixOrder.Append);

			// set the new transform
			g.Transform = bodyTrans;

			// make a new pen and draw obstacle body
			using (Pen p = new Pen(c, penWidth))
			{	
				g.DrawRectangle(p, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);
			}

			// return to normal transformation
			g.Transform = origTrans;

			// draw id
			if (DrawingUtility.DrawSimObstacleIds)
			{
				DrawingUtility.DrawControlLabel(this.Position, c, this.ObstacleId.ToString(), ContentAlignment.MiddleCenter, ControlPointStyle.SmallX, g, t);
			}
			else
			{
				DrawingUtility.DrawControlPoint(this.Position, c, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallX, g, t);
			}
		}

		[CategoryAttribute("Drag Settings"), DescriptionAttribute("Movable")]
		public bool MoveAllowed
		{
			get { return movable; }
			set { movable = value; }
		}

		public void BeginMove(Coordinates orig, WorldTransform t)
		{
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			this.Position = orig + offset;
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			this.Position = orig + offset;
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
		{
			this.Position = orig;
		}

		[Browsable(false)]
		public SelectionType Selected
		{
			get
			{
				return this.selected;
			}
			set
			{
				this.selected = value;
			}
		}

		[Browsable(false)]
		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		[Browsable(false)]
		public bool CanDelete
		{
			get { return true; }
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

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is SimObstacle)
			{
				return ((SimObstacle)obj).ObstacleId.Equals(this.ObstacleId);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return this.ObstacleId.GetHashCode();
		}

		public override string ToString()
		{
			return this.ObstacleId.ToString();
		}

		#endregion
	}
}
