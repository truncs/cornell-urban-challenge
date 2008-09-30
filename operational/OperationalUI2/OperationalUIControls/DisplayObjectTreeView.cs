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
	public partial class DisplayObjectTreeView : TreeView {
		private Font boldFont;

		public DisplayObjectTreeView() {
			InitializeComponent();

			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				Services.DisplayObjectService.DisplayObjectAdded += DisplayObjectService_DisplayObjectAdded;
				Services.DisplayObjectService.DisplayObjectRemoved += DisplayObjectService_DisplayObjectRemoved;
				Services.DisplayObjectService.DisplayObjectVisibleChanged += DisplayObjectService_DisplayObjectVisibleChanged;
			}

			boldFont = new Font(this.Font, FontStyle.Bold);
		}

		protected override void OnHandleDestroyed(EventArgs e) {
			base.OnHandleDestroyed(e);

			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				Services.DisplayObjectService.DisplayObjectAdded -= DisplayObjectService_DisplayObjectAdded;
				Services.DisplayObjectService.DisplayObjectRemoved -= DisplayObjectService_DisplayObjectRemoved;
				Services.DisplayObjectService.DisplayObjectVisibleChanged -= DisplayObjectService_DisplayObjectVisibleChanged;
			}
		}

		protected override void OnFontChanged(EventArgs e) {
			base.OnFontChanged(e);

			this.boldFont = new Font(this.Font, FontStyle.Bold);
		}

		void DisplayObjectService_DisplayObjectAdded(object sender, DisplayObjectEventArgs e) {
			// add to the nodes
			string[] groups = DisplayObjectService.GetGroups(e.DisplayObject);
			string name = DisplayObjectService.GetDisplayName(e.DisplayObject);

			TreeNodeCollection parentCollection = this.Nodes;
			for (int i = 0; i < groups.Length; i++) {
				TreeNode node = null;
				if (parentCollection.ContainsKey(groups[i])) {
					node = parentCollection[groups[i]];
				}
				else {
					node = new TreeNode(groups[i]);
					node.SelectedImageKey = node.ImageKey = "folder closed";
					node.Name = groups[i];
					parentCollection.Add(node);
				}

				node.Expand();

				parentCollection = node.Nodes;
			}

			// we have the collection for the immediate parent
			// add the node if it doesn't exist
			if (!parentCollection.ContainsKey(name)) {
				TreeNode node = new TreeNode(name);
				node.Name = name;
				node.Tag = e.DisplayObject;
				node.SelectedImageKey = node.ImageKey = "display object";

				if (Services.DisplayObjectService.IsVisible(e.DisplayObject)) {
					node.NodeFont = boldFont;
				}

				parentCollection.Add(node);

				// walk to the root and expand all
				while (node.Parent != null) {
					node = node.Parent;
					node.Expand();
				}
			} 
		}

		void DisplayObjectService_DisplayObjectRemoved(object sender, DisplayObjectEventArgs e) {
			string[] groups = DisplayObjectService.GetGroups(e.DisplayObject);
			string name = DisplayObjectService.GetDisplayName(e.DisplayObject);

			TreeNode parentNode = null;
			TreeNodeCollection parentCollection = this.Nodes;
			for (int i = 0; i < groups.Length; i++) {
				// if the group node doesn't exist, get out
				if (!parentCollection.ContainsKey(groups[i]))
					return;

				parentNode = parentCollection[groups[i]];
				parentCollection = parentNode.Nodes;
			}

			// we have the collection for the immediate parent
			if (parentCollection.ContainsKey(name)) {
				parentCollection.RemoveByKey(name);
			}

			// walk up the parents and see if the folder is empty
			while (parentNode != null) {
				if (parentCollection.Count == 0) {
					// get the parent node and it's collection
					TreeNode removeNode = parentNode;
					parentNode = parentNode.Parent;
					if (parentNode == null) {
						parentCollection = this.Nodes;
					}
					else {
						parentCollection = parentNode.Nodes;
					}

					parentCollection.Remove(removeNode);
				}
				else {
					break;
				}
			}
		}

		void DisplayObjectService_DisplayObjectVisibleChanged(object sender, DisplayObjectEventArgs e) {
			string[] groups = DisplayObjectService.GetGroups(e.DisplayObject);
			string name = DisplayObjectService.GetDisplayName(e.DisplayObject);

			TreeNodeCollection parentCollection = this.Nodes;
			for (int i = 0; i < groups.Length; i++) {
				// if the group node doesn't exist, get out
				if (!parentCollection.ContainsKey(groups[i]))
					return;

				parentCollection = parentCollection[groups[i]].Nodes;
			}

			// we have the collection for the immediate parent
			if (parentCollection.ContainsKey(name)) {
				if (Services.DisplayObjectService.IsVisible(e.DisplayObject)) {
					parentCollection[name].NodeFont = boldFont;
				}
				else {
					parentCollection[name].NodeFont = this.Font;
				}
			}
		}

		private void DisplayObjectTreeView_AfterExpand(object sender, TreeViewEventArgs e) {
			if (e.Node.Tag == null) {
				e.Node.SelectedImageKey = e.Node.ImageKey = "folder open";
			}
		}

		private void DisplayObjectTreeView_AfterCollapse(object sender, TreeViewEventArgs e) {
			if (e.Node.Tag == null) {
				e.Node.SelectedImageKey = e.Node.ImageKey = "folder closed";
			}
		}

		private void DisplayObjectTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
			if (e.Button == MouseButtons.Right) {
				this.SelectedNode = e.Node;
				IRenderable target = e.Node.Tag as IRenderable;
				if (target != null) {
					contextMenuDisplayObject.Items.Clear();
					contextMenuDisplayObject.Items.Add(menuRenderDisplayObject);

					// determine if this object is currently rendered
					menuRenderDisplayObject.Checked = Services.DisplayObjectService.IsVisible(target);

					if (target is ISimpleColored) {
						contextMenuDisplayObject.Items.Add(menuSelectColor);
					}

					// this is a display object node
					IProvideContextMenu contextMenuProvider = target as IProvideContextMenu;
					if (contextMenuProvider != null) {
						// get the context menu items
						contextMenuDisplayObject.Items.Add(toolStripSeparator);

						ICollection<ToolStripMenuItem> menuItems = contextMenuProvider.GetMenuItems();
						foreach (ToolStripMenuItem item in menuItems) {
							contextMenuDisplayObject.Items.Add(item);
						}

						contextMenuProvider.OnMenuOpening();
					}

					contextMenuDisplayObject.Tag = target;

					contextMenuDisplayObject.Show(this, e.X, e.Y);
				}
			}
		}

		private void DisplayObjectTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) {
			IRenderable target = e.Node.Tag as IRenderable;
			if (target != null) {
				// toggle whether the target is displayed or not
				if (Services.DisplayObjectService.IsVisible(target)) {
					Services.DisplayObjectService.SetVisible(target, false);
				}
				else {
					Services.DisplayObjectService.SetVisible(target, true);
				}
			}
		}

		private void menuRenderDisplayObject_Click(object sender, EventArgs e) {
			IRenderable target = contextMenuDisplayObject.Tag as IRenderable;
			if (target != null) {
				bool currentlyDisplayed = menuRenderDisplayObject.Checked;
				if (currentlyDisplayed) {
					Services.DisplayObjectService.SetVisible(target, false);
				}
				else {
					Services.DisplayObjectService.SetVisible(target, true);
				}
			}
		}

		private void menuSelectColor_Click(object sender, EventArgs e) {
			ISimpleColored target = contextMenuDisplayObject.Tag as ISimpleColored;
			if (target != null) {
				colorDialog1.Color = target.Color;
				if (colorDialog1.ShowDialog(this) == DialogResult.OK) {
					target.Color = colorDialog1.Color;
				}
			}
		}

	}
}
