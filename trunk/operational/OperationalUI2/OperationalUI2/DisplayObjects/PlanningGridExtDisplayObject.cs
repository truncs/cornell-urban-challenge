using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Controls.DisplayObjects;
using System.Drawing;
using UrbanChallenge.OperationalUIService.Debugging;
using UrbanChallenge.OperationalUI.Common.Map;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.DisplayObjects {
	class PlanningGridExtDisplayObject : PlanningGridDisplayObject, IProvideContextMenu {
		private ToolStripMenuItem[] menuItems;
		private ColorDialog colorDialog;

		public PlanningGridExtDisplayObject(string name, Color maxColor, Color minColor) 
			: base(name, maxColor, minColor) {

			string[] gridTypes = Enum.GetNames(typeof(PlanningGrids));
			PlanningGrids[] values = (PlanningGrids[])Enum.GetValues(typeof(PlanningGrids));

			ToolStripMenuItem menuGetGrid = new ToolStripMenuItem("Get Grid");

			for (int i = 0; i < gridTypes.Length; i++) {
				ToolStripMenuItem gridItem = new ToolStripMenuItem();
				gridItem.Text = gridTypes[i];
				gridItem.Tag = values[i];
				gridItem.Click += new EventHandler(gridItem_Click);

				menuGetGrid.DropDownItems.Add(gridItem);
			}

			ToolStripMenuItem menuSetMaxColor = new ToolStripMenuItem("Set Max Color", null, menuSetMaxColor_Click);
			ToolStripMenuItem menuSetMinColor = new ToolStripMenuItem("Set Min Color", null, menuSetMinColor_Click);

			menuItems = new ToolStripMenuItem[] { menuGetGrid, menuSetMaxColor, menuSetMinColor };

			colorDialog = new ColorDialog();
			colorDialog.AnyColor = true;
			colorDialog.AllowFullOpen = true;
			colorDialog.FullOpen = true;
		}

		private Form FindParent(ToolStripMenuItem item) {
			Form parentForm = null;
			if (item.GetCurrentParent() is ContextMenuStrip) {
				ContextMenuStrip contextMenu = item.GetCurrentParent() as ContextMenuStrip;
				if (contextMenu.SourceControl != null) {
					parentForm = contextMenu.SourceControl.FindForm();
				}
				else {
					parentForm = contextMenu.FindForm();
				}
			}
			else if (item.GetCurrentParent() is ToolStrip) {
				parentForm = item.GetCurrentParent().FindForm();
			}

			return parentForm;
		}

		void menuSetMaxColor_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
			Form parentForm = FindParent(menuItem);
			colorDialog.Color = maxColor;
			if (colorDialog.ShowDialog(parentForm) == DialogResult.OK) {
				maxColor = colorDialog.Color;
			}
		}

		void menuSetMinColor_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
			Form parentForm = FindParent(menuItem);
			colorDialog.Color = minColor;
			if (colorDialog.ShowDialog(parentForm) == DialogResult.OK) {
				minColor = colorDialog.Color;
			}
		}

		void gridItem_Click(object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			PlanningGrids selectedGrid = (PlanningGrids)item.Tag;

			try {
				this.grid = OperationalInterface.OperationalUIFacade.DebuggingFacade.GetPlanningGrid(selectedGrid);
			}
			catch (Exception ex) {
				MessageBox.Show("Error retrieving grid:\n" + ex.Message);
			}
		}

		#region IProvideContextMenu Members

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			return menuItems;
		}

		#endregion

		#region IProvideContextMenu Members


		public void OnMenuOpening() {
			
		}

		#endregion
	}
}
