using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace CarBrowser {
	interface IColumnSorter : IComparer {
		int ColumnIndex { get; set; }
		SortOrder SortOrder { get; set; }
	}
}
