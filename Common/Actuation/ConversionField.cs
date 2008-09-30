using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Actuation {
	public class ConversionField : FeedbackField {
		private double scale;
		private double preOffset;
		private double postOffset;

		public ConversionField(string name, InType intype, Type outtype, string dsitem, string units, double scale, double preOffset, double postOffset)
			: base(name, intype, outtype, dsitem, units) {
			this.scale = scale;
			this.preOffset = preOffset;
			this.postOffset = postOffset;
		}

		protected override object MapData(int data) {
			double result = (data + preOffset) * scale + postOffset;

			if (outtype == typeof(double))
				return result;
			else {
				try {
					return Convert.ChangeType(result, outtype);
				}
				catch (Exception) {
				}

				// could try the type converter next, for now just throw an exception
				throw new InvalidOperationException();
			}
		}
	}
}
