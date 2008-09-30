using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using RndfEditor.Display.Utilities;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RndfEditor.Display.DisplayObjects;
using System.Runtime.Serialization;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using RndfEditor.Tools;
using RndfEditor.Files;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using System.Runtime.InteropServices;
using RndfEditor.Common;

namespace RndfEditor.Forms
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
		public List<IDisplayObject> displayObjects;

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
		private IDisplayObject selected;

		#endregion

		#region Constants

		/// <summary>
		/// Tolerance, in pixels, to allow for a hit during a hit test
		/// </summary>
		private const float hitPixelTol = 5;

		#endregion

		#region Public Members

		/// <summary>
		/// The display grid
		/// </summary>
		public GridDisplay DisplayGrid;

		/// <summary>
		/// Tool currently being used
		/// </summary>
		public IEditorTool CurrentEditorTool;

		/// <summary>
		/// A secondary level tool being used
		/// </summary>
		/// <remarks>These can be used in conjunction with other tools</remarks>
		public IEditorTool SecondaryEditorTool;

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

		#region Mouse

		/// <summary>
		/// What to do when user clicks display
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				controlTag = new Point(e.X, e.Y);
				isDragging = true;
				Cursor.Current = Cursors.Hand;
			}

			base.OnMouseDown(e);
			this.Invalidate();
		}

		/// <summary>
		/// What to do when mouse moves
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{	
				// check if user is dragging
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

			// check if point analysis tool
			if (this.SecondaryEditorTool is PointAnalysisTool)
			{
				// update
				((PointAnalysisTool)this.SecondaryEditorTool).Current = transform.GetWorldPoint(new PointF(e.X, e.Y));

				// redraw
				this.Invalidate();
			}

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
			else if (this.CurrentEditorTool is AngleMeasureTool)
			{
				AngleMeasureTool amt = (AngleMeasureTool)this.CurrentEditorTool;

				if (amt.SetP1 == false)
				{
					amt.P1 = transform.GetWorldPoint(new PointF(e.X, e.Y));
				}
				else if (amt.SetP2 == false)
				{
					amt.P2 = transform.GetWorldPoint(new PointF(e.X, e.Y));
				}
				else if (amt.SetP3 == false)
				{
					amt.P3 = transform.GetWorldPoint(new PointF(e.X, e.Y));
				}

				// redraw
				this.Invalidate();
			}
			else if (this.CurrentEditorTool is WaypointAdjustmentTool)
			{
				WaypointAdjustmentTool wat = (WaypointAdjustmentTool)this.CurrentEditorTool;

				if (wat.CheckInMove)
				{
					// move point
					wat.Move(transform.GetWorldPoint(new PointF(e.X, e.Y)));

					// redraw
					this.Invalidate();
				}
			}
			else if (this.CurrentEditorTool is IntersectionPulloutTool)
			{
				IntersectionPulloutTool tool = (IntersectionPulloutTool)this.CurrentEditorTool;

				if (tool.Mode == InterToolboxMode.Box && tool.WrapInitial != null)
				{
					// clicked point
					Coordinates c = transform.GetWorldPoint(new PointF(e.X, e.Y));

					if(!tool.WrapInitial.Equals(c))
						tool.WrapFinal = c;

					this.Invalidate();
				}
			}
			else if (this.CurrentEditorTool is ZoneTool)
			{
				ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
				zt.CurrentMouse = transform.GetWorldPoint(new PointF(e.X, e.Y));

				if (zt.moveNode != null)
				{
					zt.MoveMouse(zt.CurrentMouse);
				}
				
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
			if (e.Button == MouseButtons.Left)
			{
				// if the user is dragging
				if (isDragging)
				{
					isDragging = false;
					Cursor.Current = Cursors.Default;
				}

				// redraw
				this.Invalidate();
			}

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

			// redraw
			Invalidate();
		}

		/// <summary>
		/// When mouse clicked
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseClick(MouseEventArgs e)
		{
			#region Left

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
				else if (this.CurrentEditorTool is SparseTool)
				{
					((SparseTool)this.CurrentEditorTool).Click(transform.GetWorldPoint(new PointF(e.X, e.Y)));
				}
				else if (this.CurrentEditorTool is AngleMeasureTool)
				{
					AngleMeasureTool amt = (AngleMeasureTool)this.CurrentEditorTool;

					if (amt.SetP1 == false)
					{
						amt.VSetP1(transform.GetWorldPoint(new PointF(e.X, e.Y)), this.SecondaryEditorTool);
					}
					else if (amt.SetP2 == false)
					{
						amt.VSetP2(transform.GetWorldPoint(new PointF(e.X, e.Y)), this.SecondaryEditorTool);
					}
					else if (amt.SetP3 == false)
					{
						amt.VSetP3(this.SecondaryEditorTool);
					}
				}
				else if (this.CurrentEditorTool is WaypointAdjustmentTool)
				{
					if (((WaypointAdjustmentTool)this.CurrentEditorTool).CheckInMove)
					{
						this.CurrentEditorTool = new WaypointAdjustmentTool(this.transform);
					}
					else
					{
						// create display object filter for waypoints
						DisplayObjectFilter dof = delegate(IDisplayObject target)
						{
							// check if target is network object
							if (target is IGenericWaypoint && target is IDisplayObject)
								return true;
							else
								return false;
						};

						// perform hit test
						HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);

						// check for validity
						if (htr.Hit)
						{
							// set adjust
							((WaypointAdjustmentTool)this.CurrentEditorTool).SetWaypoint(htr.DisplayObject, ((IGenericWaypoint)htr.DisplayObject).Position);
						}
					}
				}
				else if (this.CurrentEditorTool is IntersectionPulloutTool)
				{
					IntersectionPulloutTool tool = (IntersectionPulloutTool)this.CurrentEditorTool;

					// get key current
					KeyStateInfo shiftKey = KeyboardInfo.GetKeyState(Keys.ShiftKey);

					if (!shiftKey.IsPressed)
					{
						if (tool.Mode == InterToolboxMode.SafetyZone)
						{
							// create display object filter for waypoints
							DisplayObjectFilter dof = delegate(IDisplayObject target)
							{
								// check if target is network object
								if (target is ArbiterLane)
									return true;
								else
									return false;
							};

							// perform hit test
							HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);

							// check for validity
							if (htr.Hit)
							{
								// set undo
								tool.ed.SaveUndoPoint();

								// get lane
								ArbiterLane al = (ArbiterLane)htr.DisplayObject;

								// get point on lane
								LinePath.PointOnPath end = al.GetClosestPoint(transform.GetWorldPoint(new PointF(e.X, e.Y)));

								ArbiterWaypoint aw = al.GetClosestWaypoint(al.LanePath().GetPoint(end), 5);

								if (aw != null && aw.IsExit == true)
								{
									end = al.GetClosestPoint(aw.Position);
								}

								double dist = -30;
								LinePath.PointOnPath begin = al.LanePath().AdvancePoint(end, ref dist);
								if (dist != 0)
								{
									EditorOutput.WriteLine("safety zone too close to start of lane, setting start to start of lane");
									begin = al.LanePath().StartPoint;
								}
								ArbiterSafetyZone asz = new ArbiterSafetyZone(al, end, begin);
								al.SafetyZones.Add(asz);
								al.Way.Segment.RoadNetwork.DisplayObjects.Add(asz);
								al.Way.Segment.RoadNetwork.ArbiterSafetyZones.Add(asz);

								if (aw != null && aw.IsExit == true)
								{
									asz.isExit = true;
									asz.Exit = aw;
								}

								// add to display
								this.displayObjects.Add(asz);
							}
						}
						else if (tool.Mode == InterToolboxMode.Helpers)
						{
							// create display object filter for waypoints
							DisplayObjectFilter dof = delegate(IDisplayObject target)
							{
								// check if target is network object
								if (target is ArbiterLane)
									return true;
								else
									return false;
							};

							// perform hit test
							HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);

							// check for validity
							if (htr.Hit)
							{
								// get lane
								ArbiterLane al = (ArbiterLane)htr.DisplayObject;

								// get point on lane
								tool.WrappingHelpers.Add(al.GetClosest(transform.GetWorldPoint(new PointF(e.X, e.Y))));
							}
						}
						else if (tool.Mode == InterToolboxMode.Box)
						{
							// clicked point
							Coordinates c = transform.GetWorldPoint(new PointF(e.X, e.Y));

							// add points
							if (tool.WrapInitial == null)
							{
								tool.WrapInitial = c;
							}
							else if (tool.WrapFinal != null && !tool.WrapInitial.Equals(c))
							{
								tool.FinalizeIntersection();
							}
						}
					}
				}
				else if (this.CurrentEditorTool is ZoneTool)
				{
					// get key current
					KeyStateInfo shiftKey = KeyboardInfo.GetKeyState(Keys.ShiftKey);

					ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
					if (zt.moveNode != null)
					{
						zt.moveNode = null;
						zt.Reset(false);
					}
					else
					{
						if (!shiftKey.IsPressed)
						{
							// get key current
							KeyStateInfo wKey = KeyboardInfo.GetKeyState(Keys.W);

							if (!wKey.IsPressed)
							{
								// clicked point
								Coordinates c = transform.GetWorldPoint(new PointF(e.X, e.Y));
								zt.Click(c);
							}
							else
							{
								// clicked point
								Coordinates c = transform.GetWorldPoint(new PointF(e.X, e.Y));
								if (zt.zt.current != null && zt.zt.current.Perimeter.PerimeterPolygon.IsInside(c))
								{
									zt.ed.SaveUndoPoint();
									zt.BeginMove(c);
								}
							}
						}
					}
				}

				this.Invalidate();
			}

			#endregion

			#region Right

			else if (e.Button == MouseButtons.Right)
			{
				if (this.CurrentEditorTool is ZoneTool)
				{
					// clicked point
					Coordinates c = transform.GetWorldPoint(new PointF(e.X, e.Y));

					ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
					if (zt.zt.current != null && zt.zt.current.Perimeter.PerimeterPolygon.IsInside(c))
					{
						zt.RightClick(c);
						this.Invalidate();

						if (zt.rightClickNode != null || zt.rightClickEdge != null)
						{
							this.removeToolStripMenuItem.Enabled = true;
							this.zoneContextMenuStrip.Show(this, e.X, e.Y);
						}
						else
						{
							this.removeToolStripMenuItem.Enabled = false;
							this.zoneContextMenuStrip.Show(this, e.X, e.Y);
						}
					}
				}
				else if (this.CurrentEditorTool != null && this.CurrentEditorTool is PartitionTools)
				{
					PartitionTools pt = (PartitionTools)this.CurrentEditorTool;

					if (!pt.RemoveUserWaypointMode)
					{
						// create display object filter for waypoints
						DisplayObjectFilter dof = delegate(IDisplayObject target)
						{
							// check if target is network object
							if (target is ArbiterLanePartition)
								return true;
							else
								return false;
						};

						HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);
						if (htr.DisplayObject != null && htr.DisplayObject is ArbiterLanePartition && htr.Hit)
						{
							this.selected = htr.DisplayObject;
							this.selected.Selected = SelectionType.SingleSelected;
							this.partitionContextMenuStrip.Show(this, e.X, e.Y);

							pt.HitPoint = transform.GetWorldPoint(new PointF(e.X, e.Y));
						}
						else if (this.selected != null)
						{
							this.selected.Selected = SelectionType.NotSelected;
						}
					}
					else
					{
						// create display object filter for waypoints
						DisplayObjectFilter dof = delegate(IDisplayObject target)
						{
							// check if target is network object
							if (target is ArbiterUserWaypoint)
								return true;
							else
								return false;
						};

						HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);
						if (htr.DisplayObject != null && htr.DisplayObject is ArbiterUserWaypoint && htr.Hit)
						{
							this.selected = htr.DisplayObject;
							this.selected.Selected = SelectionType.SingleSelected;
							this.userWaypointContextMenuStrip.Show(this, e.X, e.Y);
						}
						else if (this.selected != null)
						{
							this.selected.Selected = SelectionType.NotSelected;
						}
					}

					this.Invalidate();
				}
				else
				{
					// perform hit test
					HitTestResult htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), DrawingUtility.DefaultFilter);

					if (htr.DisplayObject == null)
					{
					}
					else if (htr.DisplayObject is ArbiterIntersection)
					{
						this.intersectionContextMenuStrip.Show(this, e.X, e.Y);
						this.selected = htr.DisplayObject;
					}
					else if (this.CurrentEditorTool is IntersectionPulloutTool && ((IntersectionPulloutTool)this.CurrentEditorTool).Mode == InterToolboxMode.SafetyZone)
					{
						// create display object filter for waypoints
						DisplayObjectFilter dof = delegate(IDisplayObject target)
						{
							// check if target is network object
							if (target is ArbiterSafetyZone)
								return true;
							else
								return false;
						};

						htr = this.HitTest(transform.GetWorldPoint(new PointF(e.X, e.Y)), dof);
						if (htr.DisplayObject is ArbiterSafetyZone)
						{
							this.safetyZoneContextMenu.Show(this, e.X, e.Y);
							this.selected = htr.DisplayObject;
						}
					}
				}
			}

			#endregion
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
				if (this.CurrentEditorTool is WaypointAdjustmentTool)
				{
					WaypointAdjustmentTool wat = (WaypointAdjustmentTool)this.CurrentEditorTool;

					if (wat.CheckInMove)
					{
						wat.CancelMove();
						this.Invalidate();
					}
				}
				else if (this.CurrentEditorTool is IntersectionPulloutTool)
				{
					IntersectionPulloutTool ipt = (IntersectionPulloutTool)this.CurrentEditorTool;
					ipt.WrapFinal = null;
					ipt.WrapInitial = null;
					this.Invalidate();
				}
				else if (this.CurrentEditorTool is ZoneTool)
				{
					ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
					zt.Reset(false);
					this.Invalidate();
				}
			}
			else if (e.KeyCode == Keys.T && this.CurrentEditorTool is IntersectionPulloutTool)
			{
				IntersectionPulloutTool ipt = (IntersectionPulloutTool)this.CurrentEditorTool;
				if (ipt.Mode == InterToolboxMode.None)
				{
					ipt.Toolbox.boxIntersectionToolkitButton_Click(new Object(), e);
				}
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.Z)
			{
				if (this.CurrentEditorTool is ZoneTool)
				{	
					ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
					zt.ed.Undo();
				}
			}
		}

		#endregion

		#region Context Menus

		#region Intersection Context Menu

		/// <summary>
		/// Remove the intersection
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void removeIntersectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (selected is ArbiterIntersection)
			{
				ArbiterIntersection ai = (ArbiterIntersection)this.selected;

				this.displayObjects.Remove(ai);
				ai.RoadNetwork.DisplayObjects.Remove(ai);
				ai.RoadNetwork.ArbiterIntersections.Remove(ai.IntersectionId);

				foreach (ITraversableWaypoint aw in ai.AllExits.Values)
					ai.RoadNetwork.IntersectionLookup.Remove(aw.AreaSubtypeWaypointId);

				this.Invalidate();
			}
		}

		private void printToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (selected is ArbiterIntersection)
			{
				EditorOutput.WriteLine("");
				ArbiterIntersection ai = (ArbiterIntersection)this.selected;
				if (ai.PriorityLanes != null)
				{					
					EditorOutput.WriteLine("Intersection Priority Lanes: " + ai.ToString());
					foreach (KeyValuePair<ArbiterInterconnect, List<IntersectionInvolved>> pls in ai.PriorityLanes)
					{
						EditorOutput.WriteLine("  IC: " + pls.Key.ToString() + " turn direction: " + pls.Key.TurnDirection.ToString());
						foreach (IntersectionInvolved ii in pls.Value)
						{
							string exitString = ii.Exit != null ? ii.Exit.ToString() : "";
							EditorOutput.WriteLine("    Priority Area: " + ii.Area.ToString() + ", exit: " + exitString);
						}
					}
				}

				EditorOutput.WriteLine("Intersection Exits: " + ai.ToString());
				foreach (ITraversableWaypoint itw in ai.AllExits.Values)
				{
					EditorOutput.WriteLine("  " + itw.ToString());
				}

				// print entries
				EditorOutput.WriteLine("Intersection Entries: " + ai.ToString());
				foreach (ITraversableWaypoint itw in ai.AllEntries.Values)
				{
					EditorOutput.WriteLine("  " + itw.ToString());
				}

				// stopped exits
				EditorOutput.WriteLine("Stopped Exits: " + ai.ToString());
				foreach (ArbiterStoppedExit ase in ai.StoppedExits)
				{
					EditorOutput.WriteLine("  " + ase.Waypoint.ToString());
				}	
			}
		}

		#endregion

		private void intersectionContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{

		}

		#endregion

		#region Zone Context Menu

		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoneTool zt = (ZoneTool)this.CurrentEditorTool;

			if (zt.zt.current != null)
			{
				if (zt.rightClickNode != null || zt.rightClickEdge != null)
				{
					zt.DeleteSelected();
				}
			}

			zt.Reset(false);
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
			zt.ed.Undo();
			zt.Reset(false);
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoneTool zt = (ZoneTool)this.CurrentEditorTool;
			zt.ed.Redo();
			zt.Reset(false);
		}

		#endregion

		#region Safety Zone context Menu

		private void safetyZoneRemoveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterSafetyZone)
			{
				ArbiterSafetyZone asz = (ArbiterSafetyZone)this.selected;
				asz.lane.SafetyZones.Remove(asz);
				this.displayObjects.Remove(asz);
				asz.lane.Way.Segment.RoadNetwork.DisplayObjects.Remove(asz);
				asz.lane.Way.Segment.RoadNetwork.ArbiterSafetyZones.Remove(asz);
				this.Invalidate();
			}
		}

		#endregion

		#region Partition context menu

		private void normalToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.selected;
				alp.Type = PartitionType.Normal;
				alp.selected = SelectionType.NotSelected;
				this.selected = null;
				this.Invalidate();
			}
		}

		private void sparsePartitionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.selected;
				alp.Type = PartitionType.Sparse;
				alp.selected = SelectionType.NotSelected;
				this.selected = null;
				this.Invalidate();
			}
		}

		private void startupPartitionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.selected;
				alp.Type = PartitionType.Startup;
				alp.selected = SelectionType.NotSelected;
				this.selected = null;
				this.Invalidate();
			}
		}

		private void insertUserWaypointToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.selected;
				Coordinates c = ((PartitionTools)this.CurrentEditorTool).HitPoint;

				foreach (ArbiterUserPartition aup in alp.UserPartitions)
				{
					if (aup.IsInsideClose(c))
					{						
						aup.InsertUserWaypoint(c);
						alp.selected = SelectionType.NotSelected;
						this.selected = null;
						this.displayObjects = new List<IDisplayObject>(alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.ToArray());
						this.displayObjects.Insert(0, this.DisplayGrid);
						this.Invalidate();
						return;
					}
				}

				alp.selected = SelectionType.NotSelected;
				this.selected = null;
				this.Invalidate();
			}
		}

		private void partitionContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (this.selected is ArbiterLanePartition)
			{
				ArbiterLanePartition alp = (ArbiterLanePartition)this.selected;
				if (alp.Type == PartitionType.Normal)
				{
					this.normalToolStripMenuItem.Enabled = false;
					this.sparsePartitionToolStripMenuItem.Enabled = true;
					this.startupPartitionToolStripMenuItem.Enabled = true;
					this.normalToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Plus;
					this.sparsePartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
					this.startupPartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
				}
				else if (alp.Type == PartitionType.Sparse)
				{
					this.normalToolStripMenuItem.Enabled = true;
					this.sparsePartitionToolStripMenuItem.Enabled = false;
					this.startupPartitionToolStripMenuItem.Enabled = true;
					this.normalToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
					this.sparsePartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Plus;
					this.startupPartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
				}
				else if (alp.Type == PartitionType.Startup)
				{
					this.normalToolStripMenuItem.Enabled = true;
					this.sparsePartitionToolStripMenuItem.Enabled = true;
					this.startupPartitionToolStripMenuItem.Enabled = false;
					this.normalToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
					this.sparsePartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Minus;
					this.startupPartitionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Plus;
				}
			}
		}

		#endregion

		#region user waypoint context menu

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.selected is ArbiterUserWaypoint)
			{
				ArbiterUserWaypoint auw = (ArbiterUserWaypoint)this.selected;

				if(auw.Partition is ArbiterLanePartition)
				{
					ArbiterLanePartition alp = (ArbiterLanePartition)auw.Partition;
					ArbiterUserPartition init = auw.Previous;				
					ArbiterUserPartition fin = auw.Next;

					// remove the waypouints
					alp.UserWaypoints.Remove(auw);
					alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Remove(auw);

					// remove partition					
					alp.UserPartitions.Remove(fin);
					alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Remove(fin);
					alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Remove(init);

					// setup the initial partition
					init.FinalGeneric = fin.FinalGeneric;
					init.ReformPath();
					init.PartitionId = new ArbiterUserPartitionId(alp.ConnectionId, init.InitialGeneric.GenericId, init.FinalGeneric.GenericId);
					if (init.FinalGeneric is ArbiterUserWaypoint)
						((ArbiterUserWaypoint)init.FinalGeneric).Previous = init;

					// redo the ids
					foreach (ArbiterUserWaypoint tmp in alp.UserWaypoints)
					{
						if (tmp.WaypointId.Number > auw.WaypointId.Number)
							tmp.WaypointId.Number--;
					}

					// refresh paths
					alp.UserPartitions.Sort();
					alp.ReformPath();
					alp.Lane.ReformPath();

					// display
					alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.Add(init);

					// display 
					if (this.selected != null)
						this.selected.Selected = SelectionType.NotSelected;
					this.selected = null;
					this.displayObjects = new List<IDisplayObject>(alp.Lane.Way.Segment.RoadNetwork.DisplayObjects.ToArray());
					this.displayObjects.Insert(0, this.DisplayGrid);
					this.Invalidate();
					return;
				}
			}

			if (this.selected != null)
				this.selected.Selected = SelectionType.NotSelected;
			this.selected = null;
			this.Invalidate();
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
