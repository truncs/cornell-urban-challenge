using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Tao.Platform.Windows;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map.Tools;
using UrbanChallenge.Common;

namespace UrbanChallenge.OperationalUI.Controls {
	public partial class DrawingSurface : SimpleOpenGlControl, IMap {
		// world transformation
		private WorldTransform worldTransform;
		// currently selected object
		private ISelectable selectedObject;

		// currently selected tool
		private ITool currentTool;

		// the graphics wrapper object
		private GLGraphics graphics;

		public DrawingSurface() {
			InitializeComponent();

			worldTransform = new WorldTransform();

			CurrentTool = new NullTool();

			// attach to the run control service if this is runtime
			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				Services.RunControlService.DrawCycle += RunControlService_DrawCycle;
			}
		}

		#region IDrawingSurface Members

		[Browsable(false)]
		public ISelectable SelectedObject {
			get { return selectedObject; }
			set {
				// don't do anything if they're the same object
				if (object.Equals(selectedObject, value))
					return;

				if (selectedObject != null) {
					selectedObject.OnDeselect();
				}

				selectedObject = value;

				if (selectedObject != null) {
					selectedObject.OnSelect();
				}
			}
		}

		private bool ShouldSerializeSelectedObject() {
			return false;
		}

		[Browsable(false)]
		public ITool CurrentTool {
			get {
				return currentTool;
			}
			set {
				if (currentTool == value)
					return;

				if (currentTool != null) {
					currentTool.OnDeactivate(this);
				}

				currentTool = value;

				if (currentTool != null) {
					currentTool.OnActivate(this);
				}
			}
		}

		private bool ShouldSerializeCurrentTool() {
			return false;
		}

		[Browsable(false)]
		public WorldTransform Transform {
			get { return worldTransform; }
		}

		private bool ShouldSerializeTransform() {
			return false;
		}

		public Control GetControl() {
			return this;
		}

		public IGraphics GetGraphics() {
			return graphics;
		}

		public void ZoomDelta(int steps) {
			worldTransform.Scale *= (float)Math.Pow(1.07, steps);
		}

		public new void Draw() {
			// check if we've constructed the graphics class yet
			if (graphics == null)
				return;

			Form parentForm = this.FindForm();
			if (parentForm != null && (parentForm.WindowState == FormWindowState.Minimized || !parentForm.Visible)) {
				return;
			}

			// fill in the screen size
			SizeF clientSize = this.ClientSize;
			if (clientSize != worldTransform.ScreenSize) {
				worldTransform.ScreenSize = clientSize;
			}

			// call the setup transform on the current control
			if (currentTool != null) {
				currentTool.SetupTransform(worldTransform);
			}

			// initialize the GL utility
			graphics.InitScene(worldTransform, Color.White);

			// call pre-paint
			if (currentTool != null) {
				currentTool.OnPreRender(graphics, worldTransform);
			}

			// render each of the display objects
			foreach (IRenderable obj in Services.DisplayObjectService.GetVisibleEnumerator()) {
				if (!object.Equals(obj, selectedObject)) {
					obj.Render(graphics, worldTransform);
				}
			}

			// render the selected object last
			if (selectedObject != null) {
				selectedObject.Render(graphics, worldTransform);
			}

			// call post-paint
			if (currentTool != null) {
				currentTool.OnPostRender(graphics, worldTransform);
			}

			// invoke the drawing on the base gl control
			base.Draw();
		}

		public HitTestResult HitTest(Coordinates point, double tolerance, HitTestFilter filter) {
			// first check the selected object and it's hierarchy
			if (selectedObject != null) {
				object obj = selectedObject;
				while (obj != null) {
					if (obj is IHittable) {
						// run the hit test on the object
						HitTestResult hitResult = HitTest((IHittable)obj, point, tolerance, filter);
						// if it's a hit, we're done
						if (hitResult.Hit)
							return hitResult;
					}

					// move up the next step in the hierarchy
					obj = ((IHittable)obj).Parent;
				}
			}

			// iterate backwards through the objects (last object is drawn on top)
			foreach (object obj in Services.DisplayObjectService.GetReverseVisibleEnumerator()) {
				if (obj is IHittable) {
					// run the hit test on the object
					HitTestResult hitResult = HitTest((IHittable)obj, point, tolerance, filter);
					// if it's a hit, we're done
					if (hitResult.Hit)
						return hitResult;
				}
			}

			// return a no hit
			return HitTestResult.NoHit;
		}

		public void Clear() {
			foreach (IRenderable obj in Services.DisplayObjectService) {
				if (obj is IClearable) {
					((IClearable)obj).Clear();
				}
			}
		}

		#endregion

		#region Event Handling

		void RunControlService_DrawCycle(object sender, EventArgs e) {
			Draw();
		}

		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);

			base.InitializeContexts();
			graphics = new GLGraphics(this, Color.White);
			graphics.InitScene(worldTransform, Color.White);
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			base.OnMouseDown(e);

			if (currentTool != null) {
				currentTool.OnMouseDown(e);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove(e);

			if (currentTool != null) {
				currentTool.OnMouseMove(e);
			}

		}

		protected override void OnMouseUp(MouseEventArgs e) {
			base.OnMouseUp(e);

			if (currentTool != null) {
				currentTool.OnMouseUp(e);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e) {
			base.OnMouseWheel(e);

			ZoomDelta(e.Delta/SystemInformation.MouseWheelScrollDelta);
		}

		protected override void OnKeyDown(KeyEventArgs e) {
			base.OnKeyDown(e);

			if (currentTool != null) {
				currentTool.OnKeyDown(e);
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e) {
			base.OnKeyPress(e);

			if (currentTool != null) {
				currentTool.OnKeyPress(e);
			}
		}

		protected override void OnKeyUp(KeyEventArgs e) {
			base.OnKeyUp(e);

			if (currentTool != null) {
				currentTool.OnKeyUp(e);
			}
		}

		protected override void OnHandleDestroyed(EventArgs e) {
			base.OnHandleDestroyed(e);

			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				Services.RunControlService.DrawCycle -= RunControlService_DrawCycle;
			}
		}

		#endregion

		#region Utilities

		private HitTestResult HitTest(IHittable obj, Coordinates point, double tolerance, HitTestFilter filter) {
			// check if we're in the bounding box
			RectangleF boundingBox = obj.GetBoundingBox();
			// inflate by the tolerance
			if (!boundingBox.IsEmpty) {
				boundingBox.Inflate((float)tolerance, (float)tolerance);
			}
			// check if the bounding box is empty or we're inside it
			if (boundingBox.IsEmpty || boundingBox.Contains(Utility.ToPointF(point))) {
				// run the hit test
				HitTestResult result = obj.HitTest(point, (float)tolerance);
				// apply the filter
				if (filter(result)) {
					return result;
				}
			}

			// return a no-hit
			return HitTestResult.NoHit;
		}

		#endregion
	}
}