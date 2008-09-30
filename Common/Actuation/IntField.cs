using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Actuation {
	public class IntField : FeedbackField {
		private Dictionary<int, int> map;
		private object defaultValue;

		public IntField(string name, InType intype, Type outtype, string dsitem, string units, Dictionary<int, int> map, int defaultValue)
			: base(name, intype, outtype, dsitem, units) {
			this.map = map;
			this.defaultValue = Convert.ChangeType(defaultValue, outtype);
		}

		public Dictionary<int, int> Map {
			get { return map; }
		}

		public object DefaultValue {
			get { return defaultValue; }
		}

		protected override object MapData(int data) {
			int result;
			if (map.TryGetValue(data, out result)) {
				return Convert.ChangeType(result, outtype);
			}
			else {
				return defaultValue;
			}
		}
	}
}
