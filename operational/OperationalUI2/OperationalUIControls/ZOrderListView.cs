using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;

namespace UrbanChallenge.OperationalUI.Controls {
	public partial class ZOrderListView : UserControl {
		private Font boldFont;

		public ZOrderListView() {
			InitializeComponent();

			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				Services.DisplayObjectService.DisplayObjectAdded += DisplayObjectService_DisplayObjectAdded;
				Services.DisplayObjectService.DisplayObjectRemoved += DisplayObjectService_DisplayObjectRemoved;
				Services.DisplayObjectService.DisplayObjectVisibleChanged += DisplayObjectService_DisplayObjectVisibleChanged;
				Services.DisplayObjectService.DisplayObjectZOrderChanged += DisplayObjectService_DisplayObjectZOrderChanged;
				this.Disposed += ZOrderListView_Disposed;
			}

			listViewItems.InsertionMark.Color = Color.DarkBlue;

			boldFont = new Font(this.Font, FontStyle.Bold);
		}

		void ZOrderListView_Disposed(object sender, EventArgs e) {
			Services.DisplayObjectService.DisplayObjectAdded -= DisplayObjectService_DisplayObjectAdded;
			Services.DisplayObjectService.DisplayObjectRemoved -= DisplayObjectService_DisplayObjectRemoved;
			Services.DisplayObjectService.DisplayObjectVisibleChanged -= DisplayObjectService_DisplayObjectVisibleChanged;
			Services.DisplayObjectService.DisplayObjectZOrderChanged -= DisplayObjectService_DisplayObjectZOrderChanged;
		}

		protected override void OnFontChanged(EventArgs e) {
			base.OnFontChanged(e);

			this.boldFont = new Font(this.Font, FontStyle.Bold);
		}

		private void listViewItems_SelectedIndexChanged(object sender, EventArgs e) {
			bool enabled = listViewItems.SelectedItems.Count != 0;
			foreach (ToolStripItem item in toolStripZOrder.Items) {
				if (item is ToolStripButton) {
					((ToolStripButton)item).Enabled = enabled;
				}
			}
		}

		bool doingResize;
		private void listViewItems_Resize(object sender, EventArgs e) {
			doingResize = true;
			columnName.Width = listViewItems.ClientSize.Width;
			doingResize = false;
		}

		private void listViewItems_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e) {
			if (!doingResize) {
				e.Cancel = true;
			}
		}

		void DisplayObjectService_DisplayObjectZOrderChanged(object sender, EventArgs e) {
			// refresh the entire shits
			ListViewItem[] items = new ListViewItem[Services.DisplayObjectService.Count];
			for (int i = 0; i < items.Length; i++) {
				ListViewItem item = new ListViewItem(Services.DisplayObjectService[i].Name, "obj");
				if (Services.DisplayObjectService.IsVisible(i)) {
					item.Font = boldFont;
					item.ForeColor = this.ForeColor;
				}
				else {
					item.Font = this.Font;
					item.ForeColor = Color.Gray;
				}
				item.Tag = Services.DisplayObjectService[i];
				items[i] = item;
			}

			Array.Reverse(items);

			listViewItems.BeginUpdate();
			listViewItems.Items.Clear();
			listViewItems.Items.AddRange(items);
			listViewItems.EndUpdate();
		}

		void DisplayObjectService_DisplayObjectVisibleChanged(object sender, DisplayObjectEventArgs e) {
			// find the item
			foreach (ListViewItem item in listViewItems.Items) {
				if (object.Equals(item.Tag, e.DisplayObject)) {
					if (Services.DisplayObjectService.IsVisible(e.DisplayObject)) {
						item.Font = boldFont;
						item.ForeColor = this.ForeColor;
					}
					else {
						item.Font = this.Font;
						item.ForeColor = Color.Gray;
					}
					break;
				}
			}
		}

		void DisplayObjectService_DisplayObjectRemoved(object sender, DisplayObjectEventArgs e) {
			// find the item and remove it
			for (int i = 0; i < listViewItems.Items.Count; i++) {
				if (object.Equals(listViewItems.Items[i].Tag, e.DisplayObject)) {
					listViewItems.Items.RemoveAt(i);
					break;
				}
			}
		}

		void DisplayObjectService_DisplayObjectAdded(object sender, DisplayObjectEventArgs e) {
			ListViewItem item = new ListViewItem(e.DisplayObject.Name, "obj");
			if (Services.DisplayObjectService.IsVisible(e.DisplayObject)) {
				item.Font = boldFont;
				item.ForeColor = this.ForeColor;
			}
			else {
				item.Font = this.Font;
				item.ForeColor = Color.Gray;
			}
			item.Tag = e.DisplayObject;
			listViewItems.Items.Insert(0, item);
		}

		private void listViewItems_DragDrop(object sender, DragEventArgs e) {
			listViewItems.InsertionMark.Index = -1;

			if (e.Effect != DragDropEffects.Move)
				return;

			if (!e.Data.GetDataPresent(typeof(ListViewItem))) {
				e.Effect = DragDropEffects.None;
				return;
			}

			Point clientPoint = listViewItems.PointToClient(new Point(e.X, e.Y));
			ListViewHitTestInfo hitTest = listViewItems.HitTest(clientPoint);
			if (hitTest.Item != null) {
				// determine if it's on the top or bottom half of the item
				Rectangle itemRect = hitTest.Item.Bounds;
				bool bottom = clientPoint.Y > itemRect.Top + itemRect.Height/2;

				ListViewItem source = (ListViewItem)e.Data.GetData(typeof(ListViewItem));

				ListViewItem target;
				if (!bottom) {
					if (hitTest.Item.Index == 0) {
						target = null;
					}
					else {
						target = listViewItems.Items[hitTest.Item.Index-1];
					}
				}
				else {
					target = hitTest.Item;
				}

				IRenderable sourceObj = source.Tag as IRenderable;
				IRenderable targetObj = null;
				if (target != null) {
					targetObj = target.Tag as IRenderable;
				}

				Services.DisplayObjectService.MoveToBefore(sourceObj, targetObj);
			}
		}

		private void listViewItems_DragOver(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent(typeof(ListViewItem))) {
				e.Effect = DragDropEffects.None;
				return;
			}

			// do a hit test on shits
			Point clientPoint = listViewItems.PointToClient(new Point(e.X, e.Y));
			ListViewHitTestInfo hitTest = listViewItems.HitTest(clientPoint);
			if (hitTest.Item != null) {
				// determine if it's on the top or bottom half of the item
				Rectangle itemRect = hitTest.Item.Bounds;
				bool bottom = clientPoint.Y > itemRect.Top + itemRect.Height/2;
				listViewItems.InsertionMark.Index = hitTest.Item.Index;
				listViewItems.InsertionMark.AppearsAfterItem = bottom;

				e.Effect = DragDropEffects.Move;
			}
			else {
				listViewItems.InsertionMark.Index = -1;
				e.Effect = DragDropEffects.None;
			}
		}

		private void listViewItems_DragEnter(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent(typeof(ListViewItem))) {
				e.Effect = DragDropEffects.None;
			}
		}

		private void listViewItems_ItemDrag(object sender, ItemDragEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				// start the draw operation
				this.DoDragDrop(listViewItems.SelectedItems[0], DragDropEffects.Move);
			}
		}

		private void listViewItems_ItemActivate(object sender, EventArgs e) {
			IRenderable obj = listViewItems.SelectedItems[0].Tag as IRenderable;
			if (obj != null) {
				// swap the visibility
				Services.DisplayObjectService.SetVisible(obj, !Services.DisplayObjectService.IsVisible(obj));
			}
		}

		private void listViewItems_ItemChecked(object sender, ItemCheckedEventArgs e) {
			IRenderable obj = e.Item.Tag as IRenderable;
			if (obj != null) {
				Services.DisplayObjectService.SetVisible(obj, e.Item.Checked);
			}
		}

		private void buttonMoveUp_Click(object sender, EventArgs e) {
			if (listViewItems.SelectedItems.Count == 1) {
				IRenderable target = listViewItems.SelectedItems[0].Tag as IRenderable;
				if (target != null) {
					Services.DisplayObjectService.MoveUp(target);
					Select(target);
				}
			}
		}

		private void buttonMoveDown_Click(object sender, EventArgs e) {
			if (listViewItems.SelectedItems.Count == 1) {
				IRenderable target = listViewItems.SelectedItems[0].Tag as IRenderable;
				if (target != null) {
					Services.DisplayObjectService.MoveDown(target);
					Select(target);
				}
			}
		}

		private void buttonMoveToTop_Click(object sender, EventArgs e) {
			if (listViewItems.SelectedItems.Count == 1) {
				IRenderable target = listViewItems.SelectedItems[0].Tag as IRenderable;
				if (target != null) {
					Services.DisplayObjectService.MoveToTop(target);
					Select(target);
				}
			}
		}

		private void buttonMoveToBottom_Click(object sender, EventArgs e) {
			if (listViewItems.SelectedItems.Count == 1) {
				IRenderable target = listViewItems.SelectedItems[0].Tag as IRenderable;
				if (target != null) {
					Services.DisplayObjectService.MoveToBottom(target);
					Select(target);
				}
			}
		}

		private void Select(IRenderable obj) {
			for (int i = 0; i < listViewItems.Items.Count; i++) {
				if (object.Equals(listViewItems.Items[i].Tag, obj)) {
					listViewItems.Items[i].Selected = true;
					break;
				}
			}
		}
	}
}
