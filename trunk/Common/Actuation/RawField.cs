using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Actuation {
	public class RawField : FeedbackField {
		public RawField(string name, InType intype, Type outtype, string dsitem, string units)
			: base(name, intype, outtype, dsitem, units) {
		}

		protected override object MapData(int data) {
			return Convert.ChangeType(data, outtype);
		}
	}
}
