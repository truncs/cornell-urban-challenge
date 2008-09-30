using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Common.DataItem;
using System.Windows.Forms;
using Dataset.Units;
using Dataset.Client;

namespace UrbanChallenge.OperationalUI.Controls {
	public class NumericLabelAttacher<T> : IAttachable<T>, IDisposable {
		private Label target;
		private UnitConversion[] targetUnits;
		private DataItemAttacher<T> attacher;
		private string formatString;

		public NumericLabelAttacher(Label target, string formatString, IDataItemClient clientDataItem) {
			this.target = target;
			this.formatString = formatString;
			this.attacher = new DataItemAttacher<T>(this, clientDataItem);
			this.targetUnits = null;
		}

		public NumericLabelAttacher(Label target, Unit sourceUnit, Unit[] targetUnits, string formatString, IDataItemClient clientDataItem) {
			this.target = target;
			this.formatString = formatString;

			this.targetUnits = new UnitConversion[targetUnits.Length];
			for (int i = 0; i < targetUnits.Length; i++) {
				Unit targetUnit = targetUnits[i];
				this.targetUnits[i] = UnitConverter.GetConversion(sourceUnit, targetUnit);
			}

			this.attacher = new DataItemAttacher<T>(this, clientDataItem);
		}

		#region IAttachable<T> Members

		public void SetCurrentValue(T rawValue, string label) {
			// get the double value 
			double value = Convert.ToDouble(rawValue);

			// determine if we have unit conversions or not
			if (targetUnits == null) {
				target.Text = value.ToString(formatString);
			}
			else {
				StringBuilder sb = new StringBuilder();
				bool first = true;
				for (int i = 0; i < targetUnits.Length; i++) {
					double convVal = targetUnits[i].Convert(value);

					if (!first) {
						sb.Append(", ");
					}
					first = false;

					sb.Append(convVal.ToString(formatString));
					sb.Append(" ");
					sb.Append(targetUnits[i].To.Abbreviation ?? targetUnits[i].To.Name);
				}

				target.Text = sb.ToString();
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			attacher.Dispose();
		}

		#endregion
	}

	public static class NumericLabelAttacher {
		public static NumericLabelAttacher<T> Attach<T>(Label target, string formatString, DataItemClient<T> source) {
			return new NumericLabelAttacher<T>(target, formatString, source);
		}

		public static NumericLabelAttacher<T> Attach<T>(Label target, string formatString, DataItemClient<T> source, string sourceUnit, params string[] destUnits) {
			Unit sourceUnitObj = UnitConverter.GetUnit(sourceUnit);
			if (sourceUnitObj == null) {
				throw new ArgumentException("Invalid source unit (" + sourceUnit + ")");
			}
			Unit[] destUnitObj = new Unit[destUnits.Length];

			for (int i = 0; i < destUnits.Length; i++) {
				destUnitObj[i] = UnitConverter.GetUnit(destUnits[i]);
				if (destUnitObj[i] == null) {
					throw new ArgumentException("Invalid dest unit (" + destUnits[i] + ")");
				}
			}

			return new NumericLabelAttacher<T>(target, sourceUnitObj, destUnitObj, formatString, source);
		}
	}
}
