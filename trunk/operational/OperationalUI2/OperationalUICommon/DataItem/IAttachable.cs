using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public interface IAttachable<T> {
		void SetCurrentValue(T value, string label);
	}
}
