using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Reflection;

namespace Dataset.Units {
	[Serializable]
	public static class UnitConverter {
		private static List<Unit> units;
		private static List<UnitConversion> conversions;

		static UnitConverter() {
			units = new List<Unit>();
			conversions = new List<UnitConversion>();
			LoadFromConfig();
			Process();
		}

		private static void LoadFromConfig() {
			try {
				Assembly thisAssm = typeof(UnitConverter).Assembly;
				XmlReaderSettings settings = new XmlReaderSettings();
				XmlSchemaSet schemas = new XmlSchemaSet();
				Stream schemaFile = thisAssm.GetManifestResourceStream(typeof(UnitConverter), "Units.xsd");
				XmlSchema schema = XmlSchema.Read(schemaFile, ValidationEvent);
				schemas.Add(schema);
				settings.Schemas = schemas;
				settings.IgnoreComments = true;
				settings.IgnoreWhitespace = true;
				Stream configFile = thisAssm.GetManifestResourceStream(typeof(UnitConverter), "Units.xml");
				XmlReader reader = XmlReader.Create(configFile, settings);

				reader.ReadToDescendant("unitsConfig", "http://www.cornellracing.com/Units.xsd");

				while (reader.Read()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "units" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							HandleUnits(reader);
						}
						else if (reader.LocalName == "conversions" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							HandleConversions(reader);
						}
					}
					else if (reader.NodeType == XmlNodeType.EndElement) {
						if (reader.LocalName == "unitsConfig")
							return;
					}
				}
			}
			catch (Exception ex) {
				Console.WriteLine("homo stuff: " + ex.Message);
			}
		}

		private static void ValidationEvent(object sender, ValidationEventArgs e) {
			if (e.Severity == XmlSeverityType.Error) {
				Console.WriteLine("dataset config validation error: " + e.Message);
			}
			else if (e.Severity == XmlSeverityType.Warning) {
				Console.WriteLine("dataset config validation warning: " + e.Message);
			}
		}

		private static void HandleUnits(XmlReader reader) {
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (reader.LocalName == "unit" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							HandleUnit(reader);
						}
						else {
							throw new InvalidOperationException();
						}
						break;

					case XmlNodeType.EndElement:
						if (reader.LocalName == "units" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							return;
						}
						else {
							throw new InvalidOperationException();
						}
				}
			}
		}

		private static void HandleUnit(XmlReader reader) {
			string name = reader.GetAttribute("name", "http://www.cornellracing.com/Units.xsd");
			string abbrev = reader.GetAttribute("abbrev", "http://www.cornellracing.com/Units.xsd");
			string category = reader.GetAttribute("category", "http://www.cornellracing.com/Units.xsd");

			Unit u = new Unit(name, abbrev, category);
			AddUnit(u);
		}

		private static void HandleConversions(XmlReader reader) {
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (reader.LocalName == "conversion" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							HandleConversion(reader);
						}
						else {
							throw new InvalidOperationException();
						}
						break;

					case XmlNodeType.EndElement:
						if (reader.LocalName == "conversions" && reader.NamespaceURI == "http://www.cornellracing.com/Units.xsd") {
							return;
						}
						else {
							throw new InvalidOperationException();
						}
				}
			}
		}

		private static void HandleConversion(XmlReader reader) {
			string fromUnit = reader.GetAttribute("fromUnit", "http://www.cornellracing.com/Units.xsd");
			string toUnit = reader.GetAttribute("toUnit", "http://www.cornellracing.com/Units.xsd");

			double preAdj = 0;
			double postAdj = 0;
			double scale = 1;

			bool doRead = true;

			while (!doRead || reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement) {
					UnitConversion uc = new UnitConversion(GetUnit(fromUnit), GetUnit(toUnit), preAdj, scale, postAdj);
					AddConversion(uc);
					return;
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "scale") {
						doRead = false;
						scale = reader.ReadElementContentAsDouble();
					}
					else if (reader.LocalName == "offset") {
						string prePost = reader.GetAttribute("order", "http://www.cornellracing.com/Units.xsd");
						if (prePost == "pre") {
							doRead = false;
							preAdj = reader.ReadElementContentAsDouble();
						}
						else if (prePost == "post") {
							doRead = false;
							postAdj = reader.ReadElementContentAsDouble();
						}
					}
				}
			}
		}

		public static void AddUnit(Unit unit) {
			units.Add(unit);
		}

		public static void AddConversion(UnitConversion conv) {
			conversions.Add(conv);
		}

		private static void Process() {
			// do the linking and stuff
			while (Link()) { }
		}

		public static Unit GetUnit(string unit) {
				// first check the names
				foreach (Unit u in units) {
					if (u.Name.Equals(unit, StringComparison.InvariantCultureIgnoreCase))
						return u;
				}

				// next check the abbreviation
				foreach (Unit u in units) {
					if (string.Equals(u.Abbreviation, unit, StringComparison.InvariantCultureIgnoreCase))
						return u;
				}

				return null;
		}

		public static UnitConversion GetConversion(string from, string to) {
			Unit fromUnit = GetUnit(from);
			Unit toUnit = GetUnit(to);

			return GetConversion(fromUnit, toUnit);
		}

		public static UnitConversion GetConversion(Unit from, Unit to) {
			foreach (UnitConversion conv in conversions) {
				if (object.Equals(from, conv.From) && object.Equals(to, conv.To))
					return conv;
			}

			return null;
		}

		public static double Convert(double val, string from, string to) {
			UnitConversion conv = GetConversion(from, to);
			if (conv == null)
				throw new ArgumentException("Unknown unit");

			return conv.Convert(val);
		}

		public static double Convert(double val, Unit from, Unit to) {
			UnitConversion conv = GetConversion(from, to);
			if (conv == null)
				throw new ArgumentException("Unknown unit");

			return conv.Convert(val);
		}

		public static IEnumerable<Unit> GetCategory(string cat) {
			foreach (Unit u in units) {
				if (string.Equals(cat, u.Category, StringComparison.InvariantCultureIgnoreCase))
					yield return u;
			}

			yield break;
		}

		public static IEnumerable<UnitConversion> GetConversions(string unit) {
			return GetConversions(GetUnit(unit));
		}

		public static IEnumerable<UnitConversion> GetConversions(Unit uo) {
			if (uo == null)
				yield break;

			foreach (UnitConversion u in conversions) {
				if (object.Equals(u.From, uo))
					yield return u;
			}

			yield break;
		}

		public static IEnumerable<Unit> GetUnitsEnumerator() {
			foreach (Unit unit in units){
				yield return unit;
			}
		}

		private static bool Link() {
			bool mod = false;
			for (int i = 0; i < conversions.Count; i++) {
				UnitConversion t = conversions[i];
				// add the inverse conversions
				UnitConversion inv = t.Inverse();
				if (!conversions.Contains(inv)) {
					conversions.Add(inv);
					mod = true;
				}

				// iterate through and see if there is any we can link
				for (int j = 0; j < conversions.Count; j++) {
					UnitConversion o = conversions[j];
					if (t.To == o.From) {
						UnitConversion link = UnitConversion.Chain(t, o);
						if (!conversions.Contains(link)) {
							conversions.Add(link);
							mod = true;
						}
					}
				}
			}

			return mod;
		}
	}
}
