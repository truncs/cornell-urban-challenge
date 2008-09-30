using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public class ColorSet {
		protected Dictionary<string, Color> colorMap;

		public ColorSet() {
			this.colorMap = new Dictionary<string, Color>();
		}

		public virtual Color this[string name] {
			get { return colorMap[name]; }
			set { colorMap[name] = value; }
		}

		public virtual bool TryGetColor(string name, out Color color) {
			return colorMap.TryGetValue(name, out color);
		}

		public Color GetColorOrNew(string name) {
			return GetColorOrNew(name, Color.Empty);
		}

		public virtual Color GetColorOrNew(string name, Color suggested) {
			return suggested;
		}

		public virtual void Save(XmlWriter writer) {
			// do some stuff
		}

		public virtual void Load(XmlReader reader) {
			// do some stuff
		}
	}
}
