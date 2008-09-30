using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUIService.Parameters {
	[Serializable]
	public class TunableParam {
		private string name;
		private double value;
		private string group;

		public TunableParam(string name, double value) {
			this.name = name;
			this.value = value;
		}

		public TunableParam(string name, string group, double value)
		{
			this.name = name;
			this.value = value;
			this.group = group;
		}

		public string Group
		{
			get { return group; }
		}

		public string Name {
			get { return name; }
		}

		public double Value {
			get { return value; }
			set { this.value = value; }
		}
	}
}
