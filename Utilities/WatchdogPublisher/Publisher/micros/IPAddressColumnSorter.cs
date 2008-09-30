using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;

namespace CarBrowser {
	class IPAddressColumnSorter : IColumnSorter {
		public int columnIndex;
		public SortOrder sortOrder;

		public IPAddressColumnSorter(int columnIndex, SortOrder sortOrder) {
			this.columnIndex = columnIndex;
			this.sortOrder = sortOrder;
		}

		#region IComparer Members

		public int Compare(object x, object y) {
			ListViewItem lix = (ListViewItem)x;
			ListViewItem liy = (ListViewItem)y;

			// get the numeric parts of the first/seconds
			string[] xParts = lix.SubItems[columnIndex].Text.Split('.', ':');
			string[] yParts = liy.SubItems[columnIndex].Text.Split('.', ':');

			// parse the parts
			for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++) {
				// try to parse each part into an int
				int xInt, yInt;
				if (!int.TryParse(xParts[i], out xInt) || !int.TryParse(yParts[i], out yInt)) {
					// one of the two couldn't be converted to an int
					return xParts[i].CompareTo(yParts[i]) * (sortOrder == SortOrder.Descending ? -1 : 1);
				}
				else {
					if (xInt != yInt) {
						return xInt.CompareTo(yInt) * (sortOrder == SortOrder.Descending ? -1 : 1);
					}
				}
			}

			return xParts.Length.CompareTo(yParts.Length) * (sortOrder == SortOrder.Descending ? -1 : 1);
		}

		#endregion

		#region IColumnSorter Members

		public int ColumnIndex {
			get {
				return columnIndex;
			}
			set {
				columnIndex = value;
			}
		}

		public SortOrder SortOrder {
			get {
				return sortOrder;
			}
			set {
				sortOrder = value;
			}
		}

		#endregion
	}
}
