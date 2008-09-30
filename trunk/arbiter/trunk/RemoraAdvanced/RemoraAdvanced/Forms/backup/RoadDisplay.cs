using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using RemoraAdvanced.Display;
using RemoraAdvanced.Tools;
using System.Drawing.Drawing2D;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RemoraAdvanced.Display.DisplayObjects;
using RemoraAdvanced.Common;
using System.Runtime.InteropServices;

namespace RemoraAdvanced.Forms
{
	/// <summary>
	/// Main Display
	/// </summary>
	public partial class RoadDisplay : UserControl
	{
		#region Member variables

		/// <summary>
		/// Current world to screen coordinate transform
		/// </summary>
		private WorldTransform transform;

		/// <summary>
		/// List of display objects
		/// </summary>
		private List<IDisplayObject> displayObjects;

		/// <summary>
		/// A saved point in the graph
		/// </summary>
		private Point controlTag;

		/// <summary>
		/// Whether we are dragging the display
		/// </summary>
		private bool isDragging;

		/// <summary>
		/// selected object
		/// </summary>
		public IDisplayObject selected;

		/// <summary>
		/// Tracked vehicle
		/// </summary>
		public CarDisplayObject tracked;

		/// <summary>
		/// Ai vehicle
		/// </summary>
		public AiVehicle aiVehicle;

		#endregion

		#region Constants

		/// <summary>
		/// Tolerance, in pixels, to allow for a hit during a hit test
		/// </summary>
		private const float hitPixelTol = 5;

		#endregion

		#region Public Members

		/// <summary>
		/// shortcut to upper level
		/// </summary>
		public Remora remora;

		/// <summary>
		/// The display grid
		/// </summary>
		public GridDisplay DisplayGrid;

		/// <summary>
		/// Tool currently being used
		/// </summary>
		public IRemoraTool CurrentEditorTool;

		/// <summary>
		/// Tool currently being used in a secondary sense
		/// </summary>
		public IRemoraTool SecondaryEditorTool;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public RoadDisplay()
		{
			// create the display
			InitializeComponent();

			// make sure we're not in design mode
			if (!this.DesignMode)
			{
				// initialize the tranform before calling InitializeComponent so the OnResize method works properly
				transform = new WorldTransform();

				// set our style
				base.SetStyle(ControlStyles.UserPaint, true);
				base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				base.SetStyle(ControlStyles.Opaque, true);
				base.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
				base.SetStyle(ControlStyles.ResizeRedraw, true);
				base.SetStyle(ControlStyles.Selectable, true);

				// new ai vehicle
				this.aiVehicle = new AiVehicle();

				// set new display objects
				this.Reset();
			}
		}

		/// <summary>
		/// Resets teh display
		/// </summary>
		public void Reset()
		{
			// set new display objects
			displayObjects = new List<IDisplayObject>();

			// display grid
			DisplayGrid = new GridDisplay();
			displayObjects.Add(DisplayGrid);

			// center transform
			transform.CenterPoint = new Coordinates(0, 0);

			// default zoom
			transform.Scale = 6.0f;

			// redraw
			this.Invalidate();
		}

		#endregion

		#region Painting

		/// <summary>
		/// What happens when we paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			// clear the background
			e.Graphics.Clear(BackColor);

			// center on tracked vehicle if exists
			if (this.tracked != null)
			{
				// Get the offset.
				Point point = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);

				// get screen po of vehicle
				PointF screenCarPos = this.transform.GetScreenPoint(this.tracked.Position);

				// Calculate change in Position
				double deltaX = ((double)screenCarPos.X) - point.X;
				double deltaY = ((double)screenCarPos.Y) - point.Y;

				// Update the world	
				Coordinates tempCenter = WorldTransform.CenterPoint;
				tempCenter.X += deltaX / WorldTransform.Scale;
				tempCenter.Y -= deltaY / WorldTransform.Scale;
				WorldTransform.CenterPoint = tempCenter;
			}

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
				// check if we should display the object
				if (obj.ShouldDraw())
				{
					// render each object
					obj.Render(e.Graphics, transform);
				}
			}

			// render ai vehicle
			if (!this.DesignMode && this.aiVehicle != null && this.aiVehicle.State != null && this.aiVehicle.ShouldDraw())
			{
				this.aiVehicle.Render(e.Graphics, transform);
			}

			// render ai information
			if(!this.DesignMode && RemoraCommon.aiInformation.ShouldDraw())
			{
				RemoraCommon.aiInformation.Render(e.Graphics, transform);
			}

			// render current tool
			if (this.CurrentEditorTool != null && this.CurrentEditorTool.ShouldDraw())
			{
				this.CurrentEditorTool.Render(e.Graphics, transform);
			}

			// render current tool
			if (this.SecondaryEditorTool != null && this.SecondaryEditorTool.ShouldDraw())
			{
				this.SecondaryEditorTool.Render(e.Graphics, transform);
			}

			// restore the graphics state
			e.Graphics.Restore(gs);
		}

		/// <summary>
		/// Get the back color
		/// </summary>
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

		/// <summary>
		/// Don't serialize the back color
		/// </summary>
		/// <returns></returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldSerializeBackColor()
		{
			return BackColor != Color.Black;
		}

		#endregion

		#region General user control events

		protected override void OnResize(EventArgs e)
		{
			transform.ScreenSize = this.ClientSize;

			base.OnResize(e);
		}

		#endregion

		#region Accessors

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
		[DefaultValue(6.0f)]
		public float Zoom
		{
			get { return transform.Scale; }
			set
			{
				if (value == transform.Scale)
					return;

				// update the transform
				transform.Scale = value;

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

		/// <summary>
		/// add a display object
		/// </summary>
		/// <param name="ido"></param>
		public void AddDisplayObject(IDisplayObject ido)
		{
			this.displayObjects.Add(ido);
		}

		/// <summary>
		/// Add a range of display object
		/// </summary>
		/// <param name="displayObjects"></param>
		public void AddDisplayObjectRange(List<IDisplayObject> displayObjects)
		{
			this.displayObjects.AddRange(displayObjects);
		}

		#endregion

		#region Mouse

		/// <summary>
		/// What to do when user clicks display
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			#region Hit Test

			// perform the hit test over the filter
			HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), this.CarFilter);

			// check current selection if need to set as not selected
			if (this.selected != null && (htr.Hit && !this.selected.Equals(htr.DisplayObject))
				|| (!htr.Hit))
			{
				if (this.selected != null)
				{
					// remove current selection
					this.selected.Selected = SelectionType.NotSelected;
				}

				this.selected = null;
			}

			#endregion

			#region Left

			if (e.Button == MouseButtons.Left)
			{
				// check if we hit a vehicle
				if (htr.Hit && htr.DisplayObject is CarDisplayObject)
				{
				}
				else
				{
					controlTag = new Point(e.X, e.Y);
					isDragging = true;
					Cursor.Current = Cursors.Hand;
				}
			}

			#endregion

			#region Right

			else if (e.Button == MouseButtons.Right)
			{
				if (htr.Hit && htr.DisplayObject is CarDisplayObject)
				{
				}
			}

			#endregion

			base.OnMouseDown(e);
			this.Invalidate();
		}

		/// <summary>
		/// What to do when mouse moves
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			#region Left

			if (e.Button == MouseButtons.Left)
			{
				if (isDragging)
				{
					// Get the offset.
					Point point = (Point)controlTag;

					// Calculate change in position
					double deltaX = e.X - point.X;
					double deltaY = e.Y - point.Y;

					// Update the world	
					Coordinates tempCenter = WorldTransform.CenterPoint;
					tempCenter.X -= deltaX / WorldTransform.Scale;
					tempCenter.Y += deltaY / WorldTransform.Scale;
					WorldTransform.CenterPoint = tempCenter;

					// update control
					controlTag = new Point(e.X, e.Y);
				}

				// redraw
				this.Invalidate();
			}

			#endregion

			#region Right

			else if (e.Button == MouseButtons.Right)
			{
			}

			#endregion

			// check if this is a ruler tool
			if (this.CurrentEditorTool is RulerTool)
			{
				RulerTool ruler = (RulerTool)this.CurrentEditorTool;

				if (ruler.Initial != null)
				{
					KeyStateInfo shiftKey = KeyboardInfo.GetKeyState(Keys.ShiftKey);
					Coordinates current = transform.GetWorldPoint(new PointF(e.X, e.Y));

					if (!shiftKey.IsPressed)
						ruler.Current = current;
					else
					{
						Coordinates rulerOrig = ruler.Initial.Value;
						Coordinates xVec = new Coordinates(current.X - rulerOrig.X, 0.0);
						Coordinates yVec = new Coordinates(0.0, current.Y - rulerOrig.Y);

						if (xVec.Length > yVec.Length)
							ruler.Current = rulerOrig + xVec;
						else
							ruler.Current = rulerOrig + yVec;
					}
				}

				// redraw
				this.Invalidate();
			}

			// check if point analysis tool
			if (this.SecondaryEditorTool is PointAnalysisTool)
			{
				// update
				((PointAnalysisTool)this.SecondaryEditorTool).Current = transform.GetWorldPoint(new PointF(e.X, e.Y));

				// redraw
				this.Invalidate();
			}

			base.OnMouseMove(e);
		}

		/// <summary>
		/// What to do when mouse button is lifted up
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			#region Left

			if (e.Button == MouseButtons.Left)
			{
				if (isDragging)
				{
					isDragging = false;
					Cursor.Current = Cursors.Default;
				}

				// redraw
				this.Invalidate();
			}

			#endregion

			#region Right

			if (e.Button == MouseButtons.Left)
			{
			}

			#endregion

			base.OnMouseUp(e);
		}

		/// <summary>
		/// Zoom with scroll wheel
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			// update the zoom
			Zoom = Zoom * (float)Math.Pow(1.07, e.Delta / (double)SystemInformation.MouseWheelScrollDelta);

			// do what else is needed
			base.OnMouseWheel(e);
		}

		/// <summary>
		/// Center upon double click
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// Get the offset.
				Point point = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);

				// Calculate change in position
				double deltaX = e.X - point.X;
				double deltaY = e.Y - point.Y;

				// Update the world	
				Coordinates tempCenter = WorldTransform.CenterPoint;
				tempCenter.X += deltaX / WorldTransform.Scale;
				tempCenter.Y -= deltaY / WorldTransform.Scale;
				WorldTransform.CenterPoint = tempCenter;
			}
			else if (e.Button == MouseButtons.Right)
			{
			}

			// redraw
			Invalidate();
		}

		/// <summary>
		/// What to do when click mouse button
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (this.CurrentEditorTool is RulerTool)
				{
					RulerTool ruler = (RulerTool)this.CurrentEditorTool;

					if (ruler.Initial == null)
					{
						ruler.Initial = transform.GetWorldPoint(new PointF(e.X, e.Y));

						if (this.SecondaryEditorTool != null && this.SecondaryEditorTool is PointAnalysisTool)
						{
							PointAnalysisTool pat = ((PointAnalysisTool)this.SecondaryEditorTool);

							pat.Save = new List<Coordinates>();
							pat.Save.Add(ruler.Initial.Value);
						}
					}
					else if (ruler.Current != null)
					{
						ruler.Initial = null;
						ruler.Current = null;

						if (this.SecondaryEditorTool != null && this.SecondaryEditorTool is PointAnalysisTool)
						{
							PointAnalysisTool pat = ((PointAnalysisTool)this.SecondaryEditorTool);

							pat.Save = null;
						}
					}
				}
			}
		}

		#endregion

		#region Filters

		/// <summary>
		/// Filters for cars
		/// </summary>
		public DisplayObjectFilter CarFilter
		{
			get
			{
				// filter for vehicles or obstacles
				DisplayObjectFilter dof = delegate(IDisplayObject target)
				{
					// check if target is network object
					if (target is CarDisplayObject)
						return true;
					else
						return false;
				};

				// return filter
				return dof;
			}
		}

		/// <summary>
		/// Filters for network objects
		/// </summary>
		public DisplayObjectFilter RoadNetworkFilter
		{
			get
			{
				// filter for vehicles or obstacles
				DisplayObjectFilter dof = delegate(IDisplayObject target)
				{
					// check if target is network object
					if (target is INetworkObject)
						return true;
					else
						return false;
				};

				// return filter
				return dof;
			}
		}

		#endregion

		#region Tools

		/// <summary>
		/// Centers the world transform on a coordinate
		/// </summary>
		/// <param name="center"></param>
		public void Center(Coordinates center)
		{
			WorldTransform.CenterPoint = center;
		}

		/// <summary>
		/// Centers the world transform to a point on the screen
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Center(int x, int y)
		{
			// Get the offset.
			Point point = new Point(this.ClientRectangle.Width / 2, this.ClientRectangle.Height / 2);

			// Calculate change in position
			double deltaX = x - point.X;
			double deltaY = y - point.Y;

			// Update the world	
			Coordinates tempCenter = WorldTransform.CenterPoint;
			tempCenter.X += deltaX / WorldTransform.Scale;
			tempCenter.Y -= deltaY / WorldTransform.Scale;
			WorldTransform.CenterPoint = tempCenter;
		}

		/// <summary>
		/// Performs a hit test over all display objects, starting with the currently selected object (and its parents).
		/// </summary>
		/// <param name="worldCoords">World coordinates (i.e. in meters) of the location to test.</param>
		/// <param name="filter">Delegate used for filtering the hit test results.</param>
		/// <returns>A valid hit test object with the Hit property set to true if a hit occurred. Hit is false otherwise.</returns>
		/// <remarks>This does not set the selected object. It is up to the caller to decide what to do with the results.</remarks>
		public HitTestResult HitTest(Coordinates worldCoords, DisplayObjectFilter filter)
		{
			// calculate the hit tolerance (make it scale independent)
			float tol = hitPixelTol / transform.Scale;

			// current min
			HitTestResult min = new HitTestResult(null, false, float.MaxValue);

			// loop through the display objects and check each and return one if found
			for (int i = displayObjects.Count - 1; i >= 0; --i)
			{
				if (filter(displayObjects[i]))
				{
					HitTestResult tmp = displayObjects[i].HitTest(worldCoords, tol, this.transform, filter);

					if (tmp.Hit == true && (min.DisplayObject == null || min.Dist > tmp.Dist))
					{
						min = tmp;
					}
				}
			}

			// return the minimum
			return min;
		}

		/// <summary>
		/// Removes display objects matching a certain filter
		/// </summary>
		/// <param name="filter"></param>
		public void RemoveDisplayObjectType(DisplayObjectFilter filter)
		{
			// recreation of object list
			List<IDisplayObject> newDisplayObjects = new List<IDisplayObject>();

			// loop over display objects
			foreach (IDisplayObject ido in this.displayObjects)
			{
				// check type
				if (!(filter(ido)))
				{
					// add to new list
					newDisplayObjects.Add(ido);
				}
			}

			// set display object
			this.displayObjects = newDisplayObjects;
		}

		#endregion
	}

	/// <summary>
	/// keyboard info
	/// </summary>
	public class KeyboardInfo
	{
		private KeyboardInfo() { }
		[DllImport("user32")]
		private static extern short GetKeyState(int vKey);
		public static KeyStateInfo GetKeyState(Keys key)
		{
			short keyState = GetKeyState((int)key);
			int low = Low(keyState),
					high = High(keyState);
			bool toggled = low == 1 ? true : false,
					 pressed = high == 1;
			return new KeyStateInfo(key, pressed, toggled);
		}
		private static int High(int keyState)
		{
			return keyState > 0 ? keyState >> 0x10
							: (keyState >> 0x10) & 0x1;
		}
		private static int Low(int keyState)
		{
			return keyState & 0xffff;
		}
	}

	public struct KeyStateInfo
	{
		Keys _key;
		bool _isPressed,
				_isToggled;
		public KeyStateInfo(Keys key,
										bool ispressed,
										bool istoggled)
		{
			_key = key;
			_isPressed = ispressed;
			_isToggled = istoggled;
		}
		public static KeyStateInfo Default
		{
			get
			{
				return new KeyStateInfo(Keys.None,
																		false,
																		false);
			}
		}
		public Keys Key
		{
			get { return _key; }
		}
		public bool IsPressed
		{
			get { return _isPressed; }
		}
		public bool IsToggled
		{
			get { return _isToggled; }
		}
	}
}
