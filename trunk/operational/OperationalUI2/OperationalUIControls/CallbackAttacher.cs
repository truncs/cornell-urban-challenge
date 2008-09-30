using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.DataItem;
using Dataset.Client;

namespace UrbanChallenge.OperationalUI.Controls {
	public class CallbackAttacher<T> : IAttachable<T>, IDisposable {
		public delegate void SetValueHandler(T value, string label);

		private DataItemAttacher<T> attacher;
		public SetValueHandler callback;

		public CallbackAttacher(DataItemClient<T> source, SetValueHandler callback) {
			this.callback = callback;
			this.attacher = new DataItemAttacher<T>(this, source);
		}

		#region IAttachable<T> Members

		public void SetCurrentValue(T value, string label) {
			callback(value, label);
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			attacher.Dispose();
		}

		#endregion
	}

	public static class CallbackAttacher {
		public static CallbackAttacher<T> Attach<T>(DataItemClient<T> source, CallbackAttacher<T>.SetValueHandler callback) {
			return new CallbackAttacher<T>(source, callback);
		}
	}
}
