using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using RndfEditor.Display.Utilities;
using System.Runtime.Serialization.Formatters.Binary;
using Simulator.Display.DisplayObjects;
using Simulator.Tools;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using Simulator.Files;
using System.Runtime.Serialization;
using Simulator.Engine;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace Simulator
{
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
		/// Serializer for the display object graph
		/// </summary>
		private BinaryFormatter serializer = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));

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
		/// Temp coordinate for general purpose
		/// </summary>
		private Coordinates? temporaryCoordinate;

		/// <summary>
		/// Tracked vehicle
		/// </summary>
		public SimVehicle tracked;

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
		public Simulation Simulation;

		/// <summary>
		/// The display grid
		/// </summary>
		public GridDisplay DisplayGrid;

		/// <summary>
		/// Tool currently being used
		/// </summary>
		public ISimulatorTool CurrentEditorTool;

		/// <summary>
		/// A secondary level tool being used
		/// </summary>
		/// <remarks>These can be used in conjunction with other tools</remarks>
		public ISimulatorTool SecondaryEditorTool;

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

				// set new display objects
				displayObjects = new List<IDisplayObject>();

				// display grid
				DisplayGrid = new GridDisplay();
				displayObjects.Add(DisplayGrid);

				// context menu
				this.vehicleContextMenuStrip1.Closing += new ToolStripDropDownClosingEventHandler(vehicleContextMenuStrip1_Closing);
				
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

			// render current tool
			if (this.CurrentEditorTool != null && this.CurrentEditorTool.ShouldDraw())
			{
				this.CurrentEditorTool.Render(e.Graphics, transform);
			}

			// render secondary tool
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

		#region Mouse

		/// <summary>
		/// What to do when user clicks display
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			#region Hit Test

			// filter for vehicles or obstacles
			DisplayObjectFilter dof = delegate(IDisplayObject target)
			{
				// check if target is network object
				if (target is CarDisplayObject || target is SimObstacle)
					return true;
				else
					return false;
			};

			// perform the hit test over the filter
			HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);

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
				this.Simulation.simEngine.SetPropertyGridDefault();
			}

			#endregion

			#region Left

			if (e.Button == MouseButtons.Left)
			{
				// check if we hit a vehicle
				if (htr.Hit &&  htr.DisplayObject is SimVehicle)
				{
					// display obj
					CarDisplayObject cdo = (CarDisplayObject)htr.DisplayObject;

					// set the vehicles as selected
					cdo.Selected = SelectionType.SingleSelected;
					this.selected = cdo;
					this.Simulation.simEngine.propertyGrid.SelectedObject = cdo;
					
					// check if we can move the vehicle
					if (!((SimVehicle)cdo).VehicleState.IsBound)
					{
						// set dragging
						isDragging = true;
						Cursor.Current = Cursors.Hand;

						// set temp
						this.temporaryCoordinate = cdo.Position;
					}

					// redraw
					this.Invalidate();
				}
				// check if hit obstacle
				else if (htr.Hit && htr.DisplayObject is SimObstacle)
				{
					// set selected
					this.selected = htr.DisplayObject;
					this.selected.Selected = SelectionType.SingleSelected;
					this.Simulation.simEngine.propertyGrid.SelectedObject = this.selected;

					// check if can move
					if (((SimObstacle)htr.DisplayObject).MoveAllowed)
					{
						// set dragging
						isDragging = true;
						Cursor.Current = Cursors.Hand;

						// set temp
						this.temporaryCoordinate = ((SimObstacle)this.selected).Position;
					}

					// redraw
					this.Invalidate();
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
					this.selected = htr.DisplayObject;
					this.Simulation.simEngine.propertyGrid.SelectedObject = this.selected;
					this.vehicleContextMenuStrip1.Show(this, e.X, e.Y);
					((CarDisplayObject)this.selected).Selected = SelectionType.SingleSelected;
				}
				else if (htr.Hit && htr.DisplayObject is SimObstacle)
				{
					// set selected
					this.selected = htr.DisplayObject;					
					this.selected.Selected = SelectionType.SingleSelected;
					this.Simulation.simEngine.propertyGrid.SelectedObject = this.selected;

					// check if we can move the obstacle
					if (((SimObstacle)htr.DisplayObject).MoveAllowed)
					{
						// set dragging
						isDragging = true;
						Cursor.Current = Cursors.Hand;

						// set temp
						this.temporaryCoordinate = transform.GetWorldPoint(new PointF(e.X, e.Y)) - ((SimObstacle)this.selected).Position;
					}

					// redraw
					this.Invalidate();
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
				// dragging vehicle
				if (this.selected != null && this.selected is CarDisplayObject && isDragging && this.temporaryCoordinate.HasValue)
				{	
					// Get the offset.
					Point point = e.Location;

					// new coord
					Coordinates newCoord = this.transform.GetWorldPoint(new PointF(point.X, point.Y));

					// calc offse
					Coordinates offset = newCoord - this.temporaryCoordinate.Value;

					// moving object
					CarDisplayObject cdo = (CarDisplayObject)this.selected;

					// check we are not tracking
					if (this.tracked != null && cdo.Equals(this.tracked))
					{
						this.tracked = null;
					}

					// move
					cdo.InMove(this.temporaryCoordinate.Value, offset, this.transform);

					// check snap pos or heading
					if (cdo.SnapHeading || cdo.SnapPosition)
					{
						// filter for vehicles
						DisplayObjectFilter partitionDof = delegate(IDisplayObject target)
						{
							// check if target is network object
							if (target is ArbiterLanePartition)
								return true;
							else
								return false;
						};

						// check to see if selected a partition
						HitTestResult vhcHtr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), partitionDof);

						// check hit
						if (vhcHtr.Hit)
						{
							// get partition
							ArbiterLanePartition alp = (ArbiterLanePartition)vhcHtr.DisplayObject;

							// heading
							Coordinates heading = alp.Vector();

							// position
							Coordinates closest = alp.PartitionPath.GetPoint(alp.PartitionPath.GetClosestPoint(transform.GetWorldPoint(new PointF(e.X, e.Y))));

							if (cdo.SnapPosition)
								cdo.Position = closest;

							if (cdo.SnapHeading)
								cdo.Heading = heading;
						}
					}
				}
				else if (this.selected != null && this.selected is SimObstacle && isDragging && this.temporaryCoordinate.HasValue)
				{
					// Get the offset.
					Point point = e.Location;

					// new coord
					Coordinates newCoord = this.transform.GetWorldPoint(new PointF(point.X, point.Y));

					// calc offse
					Coordinates offset = newCoord - this.temporaryCoordinate.Value;

					// moving object
					SimObstacle so = (SimObstacle)this.selected;

					// move
					so.InMove(this.temporaryCoordinate.Value, offset, this.transform);
				}
				// check if user is dragging
				else if (isDragging)
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
				if (this.selected != null && this.selected is SimObstacle && isDragging && this.temporaryCoordinate.HasValue)
				{
					// Get the offset.
					Point point = e.Location;

					// new coord
					Coordinates newCoord = this.transform.GetWorldPoint(new PointF(point.X, point.Y));

					// moving object
					SimObstacle so = (SimObstacle)this.selected;

					// calc new rel heading
					Coordinates offset = newCoord - so.Position;

					// calc degree diff
					//double rotDiff = offset.ToDegrees() - this.temporaryCoordinate.Value.ToDegrees();

					// new head
					//Coordinates newHead = so.Heading.Rotate(rotDiff);

					// set
					so.Heading = offset;

					this.Invalidate();
				}
			}

			#endregion

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
				if (this.selected != null && this.selected is CarDisplayObject && this.isDragging)
				{
					this.isDragging = false;
					this.temporaryCoordinate = null;
					Cursor.Current = Cursors.Default;
				}
				else if (this.selected != null && this.selected is SimObstacle && this.isDragging)
				{
					this.isDragging = false;
					this.temporaryCoordinate = null;
					Cursor.Current = Cursors.Default;
				}
				// if the user is dragging
				else if (isDragging)
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
				if (this.selected != null && this.selected is SimObstacle && this.isDragging && this.temporaryCoordinate.HasValue)
				{
					this.isDragging = false;
					this.temporaryCoordinate = null;
					Cursor.Current = Cursors.Default;
				}
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
				// filter for vehicles
				DisplayObjectFilter vhcDof = delegate(IDisplayObject target)
				{
					// check if target is network object
					if (target is SimObstacle)
						return true;
					else
						return false;
				};

				// check to see if selected a car or obstacle
				HitTestResult vhcHtr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), vhcDof);

				// check for obstacle
				if (vhcHtr.Hit && vhcHtr.DisplayObject is SimObstacle)
				{
					this.selected = vhcHtr.DisplayObject;
					this.Simulation.simEngine.propertyGrid.SelectedObject = this.selected;
					this.obstacleContextMenuStrip.Show(this, e.X, e.Y);
					((SimObstacle)this.selected).Selected = SelectionType.SingleSelected;
				}
			}

			// redraw
			Invalidate();
		}

		#endregion

		#region Handling

		/// <summary>
		/// Save the state of the display
		/// </summary>
		/// <returns></returns>
		public DisplaySave Save()
		{
			// make new save object
			DisplaySave save = new DisplaySave();

			// set fields
			save.displayObjects = displayObjects;
			save.center = transform.CenterPoint;
			save.scale = transform.Scale;
			save.DisplayGrid = DisplayGrid;

			// return save
			return save;
		}

		/// <summary>
		/// Loads the display from a save point
		/// </summary>
		/// <param name="save"></param>
		public void LoadSave(DisplaySave save)
		{
			// set fields
			displayObjects = save.displayObjects;
			transform.CenterPoint = save.center;
			transform.Scale = save.scale;
			DisplayGrid = save.DisplayGrid;
		}

		#endregion

		#region Keys

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				
			}
		}

		#endregion

		#region Context Menus

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

		#endregion

		#region Vehicle Context Menu Item

		private void vehicleContextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (this.selected is SimVehicle)
			{
				// vehicle
				SimVehicle isv = (SimVehicle)this.selected;

				if(isv.SimVehicleState.IsBound)
				{
					this.bindToolStripMenuItem.Enabled = false;
					this.trackToolStripMenuItem.Enabled = true;
					this.unbindToolStripMenuItem.Enabled = true;
					this.stopTrackToolStripMenuItem.Enabled = true;
					this.deleteToolStripMenuItem.Enabled = true;
				}
				else
				{
					this.bindToolStripMenuItem.Enabled = true;
					this.trackToolStripMenuItem.Enabled = true;
					this.unbindToolStripMenuItem.Enabled = false;
					this.stopTrackToolStripMenuItem.Enabled = true;
					this.deleteToolStripMenuItem.Enabled = true;
				}
			}
			else
			{
				this.bindToolStripMenuItem.Enabled = false;
				this.trackToolStripMenuItem.Enabled = false;
				this.unbindToolStripMenuItem.Enabled = false;
				this.stopTrackToolStripMenuItem.Enabled = false;
				this.deleteToolStripMenuItem.Enabled = false;
			}
		}

		private void bindToolStripMenuItem_Click(object sender, EventArgs e)
		{
			((SimVehicle)this.selected).SimVehicleState.IsBound = true;
			this.Invalidate();
		}

		private void trackToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.tracked = (SimVehicle)this.selected;
			this.Invalidate();
		}

		private void unbindToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SimVehicle isv = (SimVehicle)this.selected;
			if (this.tracked != null && this.tracked.VehicleId.Equals(isv.VehicleId))
				this.tracked = null;

			((SimVehicle)this.selected).SimVehicleState.IsBound = false;
			this.Invalidate();
		}

		private void stopTrackToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.tracked = null;
			this.Invalidate();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is SimVehicle)
			{
				// vehicle
				SimVehicle isv = (SimVehicle)this.selected;
				
				// notfy
				SimulatorOutput.WriteLine("Removed Vehicle: " + isv.SimVehicleState.VehicleID.ToString());

				// remove
				this.displayObjects.Remove(this.selected);
				this.Simulation.simEngine.Vehicles.Remove(isv.VehicleId);
				this.Simulation.clientHandler.Remove(isv.VehicleId);
				this.Simulation.OnClientsChanged();

				if (this.tracked != null && this.tracked.VehicleId.Equals(isv.VehicleId))
					this.tracked = null;

				// remove selecation
				this.selected = null;

				// properties
				this.Simulation.simEngine.SetPropertyGridDefault();

				// redraw
				this.Invalidate();
			}
		}

		/// <summary>
		/// Connect the ai to the selected vehicle
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void connectAiToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// What to do when closing
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void vehicleContextMenuStrip1_Closing(object sender, ToolStripDropDownClosingEventArgs e)
		{
		}

		#endregion

		#region Obstacle Context Menu Item

		/// <summary>
		/// Delete obstacle
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void deleteObstacleContextToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (this.selected is SimObstacle)
			{
				SimObstacle so = (SimObstacle)this.selected;
				this.displayObjects.Remove(so);
				this.Simulation.simEngine.WorldService.Obstacles.Remove(so.ObstacleId);
				this.Simulation.simEngine.SetPropertyGridDefault();
				this.selected = null;

				SimulatorOutput.WriteLine("Removed Obstacle: " + so.ObstacleId.ToString());

				this.Invalidate();
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

	}
}
