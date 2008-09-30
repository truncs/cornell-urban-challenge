using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.DataItem;
using Dataset.Client;

namespace UrbanChallenge.OperationalUI.Controls {
	public class BasicLabelAttacher<T> : IAttachable<T>, IDisposable {
		private Label target;
		private DataItemAttacher<T> attacher;

		public BasicLabelAttacher(Label target, DataItemClient<T> dataItem) {
			this.target = target;
			this.attacher = new DataItemAttacher<T>(this, dataItem);
		}

		#region IAttachable<T> Members

		public void SetCurrentValue(T value, string label) {
			if (value != null) {
				target.Text = value.ToString();
			}
			else {
				target.Text = "<null>";
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			attacher.Dispose();
		}

		#endregion
	}

	public static class BasicLabelAttacher {
		public static BasicLabelAttacher<T> Attach<T>(Label target, DataItemClient<T> source) {
			return new BasicLabelAttacher<T>(target, source);
		}
	}
}
