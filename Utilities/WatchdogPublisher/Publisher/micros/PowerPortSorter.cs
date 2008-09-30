using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CarBrowser.Micros {
	class PowerPortSorter : IColumnSorter {
		private int columnIndex;
		private SortOrder sortOrder;

		public PowerPortSorter(int columnIndex, SortOrder sortOrder) {
			this.columnIndex = columnIndex;
			this.sortOrder = sortOrder;
		}
		
		#region IColumnSorter Members

		public int ColumnIndex {
			get { return columnIndex; }
			set { columnIndex = value; }
		}

		public System.Windows.Forms.SortOrder SortOrder {
			get { return sortOrder; }
			set { sortOrder = value; }
		}

		#endregion

		#region IComparer Members

		public int Compare(object x, object y) {
			ListViewItem lix = (ListViewItem)x;
			ListViewItem liy = (ListViewItem)y;

			// get the numeric parts of the first/seconds
			string[] xParts = lix.SubItems[columnIndex].Text.Split('/');
			string[] yParts = liy.SubItems[columnIndex].Text.Split('/');

			int xval = -1, yval = -1;
			if (xParts != null && xParts.Length > 0) {
				if (!int.TryParse(xParts[0], out xval))
					xval = -1;
			}

			if (yParts != null && yParts.Length > 0){
				if (!int.TryParse(yParts[0], out yval))
					yval = -1;
			}

			return xval.CompareTo(yval) * (sortOrder == SortOrder.Descending ? -1 : 1);
		}

		#endregion
	}
}
