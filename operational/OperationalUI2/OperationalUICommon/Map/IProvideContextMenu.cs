using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface IProvideContextMenu {
		ICollection<ToolStripMenuItem> GetMenuItems();
		void OnMenuOpening();
	}
}
