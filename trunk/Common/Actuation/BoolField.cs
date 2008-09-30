using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Actuation {
	public class BoolField : FeedbackField {
		public BoolField(string name, InType intype, Type outtype, string dsitem, string units)
			: base(name, intype, outtype, dsitem, units) {
			if (outtype != typeof(bool))
				throw new ArgumentException("BoolField where out type is not a bool");
		}

		protected override object MapData(int data) {
			bool result = (data != 0);
			return result;
		}
	}
}
