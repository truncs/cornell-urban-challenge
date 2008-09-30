using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset.Units {
	[Serializable]
	public class UnitConversion {
		public static readonly UnitConversion Null = new UnitConversion(null, null, 0, 1, 0);
		private Unit from;
		private Unit to;

		private double preAdjust;
		private double postAdjust;
		private double scale;

		public UnitConversion(Unit from, Unit to, double preAdjust, double scale, double postAdjust) {
			this.from = from;
			this.to = to;
			this.preAdjust = preAdjust;
			this.postAdjust = postAdjust;
			this.scale = scale;
		}

		public Unit From {
			get { return from; }
		}

		public Unit To {
			get { return to; }
		}

		public double Convert(double orig) {
			return (orig + preAdjust) * scale + postAdjust;
		}

		public UnitConversion Inverse() {
			return new UnitConversion(to, from, -postAdjust, 1 / scale, -preAdjust);
		}

		public static UnitConversion Chain(UnitConversion left, UnitConversion right) {
			return new UnitConversion(left.from, right.to,
				left.preAdjust + left.postAdjust / left.scale + right.preAdjust / left.scale,
				left.scale * right.scale, right.postAdjust);
		}

		public override int GetHashCode() {
			return from.GetHashCode() ^ to.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is UnitConversion) {
				UnitConversion uc = obj as UnitConversion;
				return uc.from.Equals(from) && uc.to.Equals(to);
			}
			else {
				return false;
			}
		}

		public override string ToString() {
			return from.Name + "->" + to.Name;
		}
	}
}
