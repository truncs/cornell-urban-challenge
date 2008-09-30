using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;

namespace Remora.Display
{
	public partial class RoadDisplay : UserControl
	{
		#region Member Variables

		/// <summary>
		/// Current world to screen coordinate transform
		/// </summary>
		public WorldTransform transform;

		/// <summary>
		/// List of display objects
		/// </summary>
		public List<IDisplayObject> displayObjects;

		/// <summary>
		/// Event raised when the zoom changes. 
		/// </summary>
		public event EventHandler ZoomChanged;

		// are we moving the display
		private bool isDragging;
		Point controlTag;

		public RndfNetwork rndf;
		public Mdf mdf;

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		public RoadDisplay()
		{
			// initialize the tranform before calling InitializeComponent so the OnResize method works properly
			transform = new WorldTransform();

            // initialize the form
            InitializeComponent();

            // set our style
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			base.SetStyle(ControlStyles.Opaque, true);
			base.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			base.SetStyle(ControlStyles.ResizeRedraw, true);
			base.SetStyle(ControlStyles.Selectable, true);

			// list of object to display
			displayObjects = new List<IDisplayObject>();

            // don't draw things in design mode
            if (!this.DesignMode)
            {
                // initialize the grid			
                this.AddDisplayObject(new DisplayGrid());
            }
		}

		#region Drawing

		/// <summary>
		/// What happens when we paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			// clear the background
			e.Graphics.Clear(BackColor);

			// save the graphics state
			GraphicsState gs = e.Graphics.Save();

			// set the drawing modes
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.Bicubic;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			// set the transform
			e.Graphics.Transform = transform.GetTransform();

			// paint the display objects
			foreach (IDisplayObject obj in displayObjects)
			{
				obj.Render(e.Graphics, transform);
			}

			// restore the graphics state
			e.Graphics.Restore(gs);
		}

		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				base.BackColor = value;
			}
		}

		#endregion

		#region General user control events

		protected override void OnResize(EventArgs e)
		{
			transform.ScreenSize = this.ClientSize;

			base.OnResize(e);
		}

		#endregion

		#region Utility Functions

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldSerializeBackColor()
		{
			return BackColor != Color.Black;
		}

		/// <summary>
		/// Adds a range of new display objects to the screen and forces a redraw of the drawing surface
		/// </summary>
		/// <param name="obj">Object to add</param>
		/// <remarks>
		/// Note that if you add the same object twice, nothing complains. However, only one will be removed once if calling RemoveDisplayObject.
		/// </remarks>
		public void AddDisplayObjectRange(List<IDisplayObject> objects)
		{
			displayObjects.AddRange(objects);
			Invalidate();
		}

		/// <summary>
		/// Sets the display objects to something new
		/// </summary>
		/// <param name="objects"></param>
		public void SetDisplayObjects(List<IDisplayObject> objects)
		{
			displayObjects = objects;
			Invalidate();
		}

		/// <summary>
		/// Adds a new display object to the screen and forces a redraw of the drawing surface
		/// </summary>
		/// <param name="obj">Object to add</param>
		/// <remarks>
		/// Note that if you add the same object twice, nothing complains. However, only one will be removed once if calling RemoveDisplayObject.
		/// </remarks>
		public void AddDisplayObject(IDisplayObject obj)
		{
			displayObjects.Add(obj);
			Invalidate();
		}

		#endregion

		#region Fields

		/// <summary>
		/// Sets the rndf, removing older ones
		/// </summary>
		/// <param name="rndf"></param>
		public void SetRndf(RndfNetwork rndf)
		{
			this.rndf = rndf;

			for (int i = 0; i < displayObjects.Count; i++)
			{
				if (displayObjects[i] is RndfDisplay)
				{
					displayObjects.RemoveAt(i);
				}
			}

			displayObjects.Add(new RndfDisplay(rndf));
			this.Invalidate();
		}

		public void SetMdf(Mdf mdf)
		{
			this.mdf = mdf;
		}

		/// <summary>
		/// Gets the world transform object.
		/// </summary>
		/// <remarks>
		/// Changing the values of this instance does not get immediately reflected in the display. Invalidate must be called
		/// to force redrawing the screen. Also, if the scale is changed, the ZoomChanged event will not be raised.
		/// </remarks>
		[Browsable(false)]
		public WorldTransform WorldTransform
		{
			get { return transform; }
		}

		/// <summary>
		/// Gets or sets the zoom in units of pixels/meter. 
		/// </summary>
		[DefaultValue(1.0f)]
		public float Zoom
		{
			get { return transform.Scale; }
			set
			{
				if (value == transform.Scale)
					return;

				// update the transform
				transform.Scale = value;

				// raise the event
				OnZoomChanged();

				// force a redraw
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets the center point of the transformation.
		/// </summary>
		public Coordinates CenterPoint
		{
			get { return transform.CenterPoint; }
			set
			{
				if (transform.CenterPoint == value)
					return;

				transform.CenterPoint = value;
				Invalidate();
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldSerializeCenterPoint()
		{
			return transform.CenterPoint != new Coordinates(0, 0);
		}

		#endregion

		#region Mouse Handling

		protected override void OnMouseDown(MouseEventArgs e)
		{
			
			if (e.Button == MouseButtons.Left)
			{
				controlTag = new Point(e.X, e.Y);
				isDragging = true;
				Cursor.Current = Cursors.Hand;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (isDragging)
			{
				// Get the offset.
				Point point = (Point)controlTag;

				// Calculate change in Position
				double deltaX = e.X - point.X;
				double deltaY = e.Y - point.Y;

				// Update the world	
				Coordinates tempCenter = transform.CenterPoint;
				tempCenter.X -= deltaX / transform.Scale;
				tempCenter.Y += deltaY / transform.Scale;
				transform.CenterPoint = tempCenter;

				// update control
				controlTag = new Point(e.X, e.Y);
			}
			Invalidate();

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{			
			isDragging = false;
			Cursor.Current = Cursors.Default;
			
			base.OnMouseUp(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			// update the zoom
			Zoom = Zoom * (float)Math.Pow(1.07, e.Delta / (double)SystemInformation.MouseWheelScrollDelta);

			base.OnMouseWheel(e);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			// Get the offset.
			Point point = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);

			// Calculate change in Position
			double deltaX = e.X - point.X;
			double deltaY = e.Y - point.Y;

			// Update the world	
			Coordinates tempCenter = WorldTransform.CenterPoint;
			tempCenter.X += deltaX / WorldTransform.Scale;
			tempCenter.Y -= deltaY / WorldTransform.Scale;
			WorldTransform.CenterPoint = tempCenter;

			// update control
			controlTag = new Point(e.X, e.Y);

			// redraw
			Invalidate();
		}

		public void Center(int x, int y)
		{
			// Get the offset.
			Point point = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);

			// Calculate change in Position
			double deltaX = x - point.X;
			double deltaY = y - point.Y;

			// Update the world	
			Coordinates tempCenter = WorldTransform.CenterPoint;
			tempCenter.X += deltaX / WorldTransform.Scale;
			tempCenter.Y -= deltaY / WorldTransform.Scale;
			WorldTransform.CenterPoint = tempCenter;

			// redraw
			//Invalidate();
		}

		#endregion

		#region Event Raisers

		/// <summary>
		/// Invokes the ZoomChanged event
		/// </summary>
		protected virtual void OnZoomChanged()
		{
			if (ZoomChanged != null)
			{
				ZoomChanged(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}
