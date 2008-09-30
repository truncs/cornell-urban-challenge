using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Actuation {
	public class EnumField : FeedbackField {
		private Dictionary<int, object> map;
		private object defaultValue;

		public EnumField(string name, InType intype, Type outtype, string dsitem, string units, Dictionary<int, object> map, object defaultValue)
			: base(name, intype, outtype, dsitem, units) {
			this.map = map;
			this.defaultValue = defaultValue;
		}

		public Dictionary<int, object> EnumMap {
			get { return map; }
		}

		public object DefaultValue {
			get { return defaultValue; }
		}

		protected override object MapData(int data) {
			object value;
			if (map.TryGetValue(data, out value)) {
				return value;
			}
			else if (defaultValue == null) {
				throw new InvalidOperationException("Unknown enum value and no default assigned");
			}
			else {
				return defaultValue;
			}
		}
	}
}
