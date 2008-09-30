using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset.Units {
	[Serializable]
	public class Unit {
		private string name;
		private string abbrev;
		private string category;

		public Unit(string name, string abbrev, string category) {
			this.name = name;
			this.abbrev = abbrev;
			this.category = category;
		}

		public string Name {
			get { return name; }
		}

		public string Abbreviation {
			get { return abbrev; }
		}

		public string Category {
			get { return category; }
		}

		public override int GetHashCode() {
			return name.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is Unit) {
				return ((Unit)obj).name.Equals(name);
			}
			else {
				return false;
			}
		}

		public override string ToString() {
			return name;
		}

		public static bool operator ==(Unit left, Unit right) {
			return object.ReferenceEquals(left, right);
		}

		public static bool operator !=(Unit left, Unit right) {
			return !object.ReferenceEquals(left, right);
		}
	}
}
