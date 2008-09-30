using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;

namespace UrbanChallenge.OperationalUIService.Parameters {
	public class TunableParamTable {
		private SortedDictionary<string, TunableParam> paramTable = new SortedDictionary<string, TunableParam>();

		public TunableParamTable() {
		}

		public TunableParamTable(Stream xmlConfig) {
			if (xmlConfig == null || xmlConfig.Length == 0)
				return;

			// load from config
			Assembly thisAssm = typeof(TunableParamTable).Assembly;
			Stream xsdStream = thisAssm.GetManifestResourceStream(this.GetType(), "TunableParams.xsd");
			XmlSchema mappingSchema = XmlSchema.Read(xsdStream, ValidationEvent);

			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.Schemas.Add(mappingSchema);
			readerSettings.IgnoreComments = true;
			readerSettings.IgnoreWhitespace = true;

			XmlReader reader = XmlReader.Create(xmlConfig, readerSettings);

			// read through the Parameters
			bool skipRead = false;
			while (skipRead || reader.Read()) {
				skipRead = false;
				if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tunableParams") {
					break;
				}
				else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "param") {
					string name = reader.GetAttribute("name");
					string group = reader.GetAttribute("group");
					double value = reader.ReadElementContentAsDouble();

					TunableParam param = GetParam(name, group, value);
					param.Value = value;

					skipRead = true;
				}
			}
		}

		private void ValidationEvent(object sender, ValidationEventArgs e) {
			// do something with this
		}

		public void SaveConfig(Stream stream) {
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "  ";
			settings.CheckCharacters = true;

			XmlWriter writer = XmlWriter.Create(stream, settings);

			writer.WriteStartElement("tunableParams", "http://www.cornellracing.com/TunableParams.xsd");
			writer.WriteAttributeString("xmlns", "http://www.cornellracing.com/TunableParams.xsd");

			foreach (KeyValuePair<string, TunableParam> ke in paramTable) {
				writer.WriteStartElement("param", "http://www.cornellracing.com/TunableParams.xsd");
				writer.WriteAttributeString("name", ke.Value.Name);
				if (!string.IsNullOrEmpty(ke.Value.Group))
				{
					writer.WriteAttributeString("group", ke.Value.Group);
				}
				writer.WriteValue(ke.Value.Value);
				writer.WriteEndElement();
			}

			writer.WriteEndElement();

			writer.Flush();
		}

		public TunableParam GetParam(string name, double defaultValue) {
			TunableParam param;
			if (paramTable.TryGetValue(name, out param)) {
				return param;
			}
			else {
				param = new TunableParam(name, defaultValue);
				paramTable.Add(name, param);
				return param;
			}
		}

		public TunableParam GetParam(string name, string group, double defaultValue)
		{
			TunableParam param;
			if (paramTable.TryGetValue(name, out param))
			{
				return param;
			}
			else
			{
				param = new TunableParam(name, group, defaultValue);
				paramTable.Add(name, param);
				return param;
			}
		}

		public TunableParam GetParam(string name) {
			TunableParam param;
			if (paramTable.TryGetValue(name, out param)) {
				return param;
			}
			else {
				return null;
			}
		}

		public ICollection<TunableParam> Parameters {
			get { return paramTable.Values; }
		}
	}
}
