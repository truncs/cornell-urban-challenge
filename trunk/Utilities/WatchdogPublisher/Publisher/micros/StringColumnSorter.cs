using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Windows.Forms;

namespace CarBrowser {
	class StringColumnSorter : IColumnSorter {
		private int columnIndex;
		private SortOrder sortOrder;

		public StringColumnSorter(int columnIndex, SortOrder sortOrder) {
			this.columnIndex = columnIndex;
			this.sortOrder = sortOrder;
		}

		#region IComparer Members

		public int Compare(object x, object y) {
			ListViewItem lix = (ListViewItem)x;
			ListViewItem liy = (ListViewItem)y;

			return lix.SubItems[columnIndex].Text.CompareTo(liy.SubItems[columnIndex].Text) * (sortOrder == SortOrder.Descending ? -1 : 1);
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
