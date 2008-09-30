using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public delegate bool HitTestFilter(HitTestResult result);

	public static class HitTestFilters {
		public static readonly HitTestFilter All = delegate(HitTestResult result) {
			return result.Hit;
		};

		public static readonly HitTestFilter SelectableOnly = delegate(HitTestResult result) {
			return result.Hit && result.Target is ISelectable;
		};

		public static readonly HitTestFilter HasSnap = delegate(HitTestResult result) {
			return result.Hit && !result.SnapPoint.IsNaN;
		};
	}
}
