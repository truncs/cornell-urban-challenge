using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset.Utility {
	[Serializable]
	internal class CaseInsensitiveStringComparer : IComparer<string>, IEqualityComparer<string> {
		#region IComparer<string> Members

		public int Compare(string x, string y) {
			return string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
		}

		#endregion

		#region IEqualityComparer<string> Members

		public bool Equals(string x, string y) {
			return string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);
		}

		public int GetHashCode(string obj) {
			return obj.ToLowerInvariant().GetHashCode();
		}

		#endregion
	}
}
