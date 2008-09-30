using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Schema;

namespace Dataset.Config {
	internal static class DatasetXmlParser {
		internal delegate void DataItemCallback(DataItemDescriptor ds, string specialType, List<KeyValuePair<string, string>> attributes);

		internal static void ParseConfig(DataItemCallback dataItemCallback, string group) {
			Assembly thisAssm = typeof(DatasetXmlParser).Assembly;

			XmlReaderSettings settings = new XmlReaderSettings();

			XmlSchemaSet schemas = new XmlSchemaSet();
			Stream schemaFile = thisAssm.GetManifestResourceStream(typeof(DatasetXmlParser), "datasetConfig.xsd");
			XmlSchema schema = XmlSchema.Read(schemaFile, ValidationEvent);
			schemas.Add(schema);
			schemaFile = thisAssm.GetManifestResourceStream(typeof(DatasetXmlParser), "SpecialTypeConfig.xsd");
			schema = XmlSchema.Read(schemaFile, ValidationEvent);
			schemas.Add(schema);

			settings.Schemas = schemas;
			settings.IgnoreComments = true;
			settings.IgnoreWhitespace = true;
			Stream configFile = thisAssm.GetManifestResourceStream(typeof(DatasetXmlParser), "datasetConfig.xml");
			XmlReader reader = XmlReader.Create(configFile, settings);

			ParseConfig(reader, dataItemCallback, group);
		}

		internal static void ValidationEvent(object sender, ValidationEventArgs e) {
			if (e.Severity == XmlSeverityType.Error) {
				Console.WriteLine("dataset config validation error: " + e.Message);
			}
			else if (e.Severity == XmlSeverityType.Warning) {
				Console.WriteLine("dataset config validation warning: " + e.Message);
			}
		}

		internal static void ParseConfig(XmlReader reader, DataItemCallback dataItemCallback, string group) {
			reader.ReadToDescendant("dataItems", "http://www.cornellracing.com/datasetConfig.xsd");

			bool doRead = true;
			while (true) {
				if (doRead) {
					if (!reader.Read())
						return;
				}
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (reader.LocalName == "dataItem" && reader.NamespaceURI == "http://www.cornellracing.com/datasetConfig.xsd") {
							HandleItem(reader, dataItemCallback);
							doRead = false;
						}
						else if (reader.LocalName == "dataItemGroup" && reader.NamespaceURI == "http://www.cornellracing.com/datasetConfig.xsd") {
							string name = reader.GetAttribute("name", "http://www.cornellracing.com/datasetConfig.xsd");
							if (!string.Equals(group, name, StringComparison.InvariantCultureIgnoreCase)) {
								// skip this group
								reader.Skip();
								doRead = false;
							}
						}
						else {
							throw new InvalidOperationException();
						}
						break;
					case XmlNodeType.EndElement:
						if (reader.LocalName == "dataItems" && reader.NamespaceURI == "http://www.cornellracing.com/datasetConfig.xsd")
							return;
						else
							doRead = true;
						break;
				}
			}
		}

		private static void HandleItem(XmlReader reader, DataItemCallback cbk) {
			// should be positioned on the dataItem element
			Debug.Assert(reader.NodeType == XmlNodeType.Element && reader.LocalName == "dataItem");

			//reader.ReadAttributeValue();
			string name = reader.GetAttribute("name", "http://www.cornellracing.com/datasetConfig.xsd");
			string typename = reader.GetAttribute("type", "http://www.cornellracing.com/datasetConfig.xsd");
			string units = reader.GetAttribute("units", "http://www.cornellracing.com/datasetConfig.xsd");
			string maxEntriesStr = reader.GetAttribute("maxEntries", "http://www.cornellracing.com/datasetConfig.xsd");
			string specialType = reader.GetAttribute("specialType", "http://www.cornellracing.com/datasetConfig.xsd");

			// get the list of attributes
			List<KeyValuePair<string, string>> attribs = new List<KeyValuePair<string, string>>();
			if (reader.MoveToFirstAttribute()) {
				do {
					if (reader.NamespaceURI != "http://www.cornellracing.com/datasetConfig.xsd") {
						attribs.Add(new KeyValuePair<string, string>(reader.LocalName, reader.Value));
					}
				} while (reader.MoveToNextAttribute());

				reader.MoveToElement();
			}

			// try to parse 
			Type type = GetType(typename);

			// read the next thing in the document
			string description = null;
			if (!reader.Read())
				throw new InvalidOperationException();
			if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "description" && reader.NamespaceURI == "http://www.cornellracing.com/datasetConfig.xsd") {
				// there is a description field
				description = reader.ReadElementContentAsString("description", "http://www.cornellracing.com/datasetConfig.xsd");
			}

			int capacity = 250;
			if (maxEntriesStr != null) {
				capacity = int.Parse(maxEntriesStr);
			}

			DataItemDescriptor desc = new DataItemDescriptor(name, type, description, units, capacity);
			cbk(desc, specialType, attribs);
		}

		private static Type GetType(string typeref) {
			string lowerTyperef = typeref.ToLower();
			if (lowerTyperef == "double") {
				return typeof(double);
			}
			else if (lowerTyperef == "float") {
				return typeof(float);
			}
			else if (lowerTyperef == "int") {
				return typeof(int);
			}
			else if (lowerTyperef == "bool") {
				return typeof(bool);
			}
			else if (lowerTyperef == "datetime") {
				return typeof(DateTime);
			}
			else if (lowerTyperef == "timespan") {
				return typeof(TimeSpan);
			}
			else if (lowerTyperef == "string") {
				return typeof(string);
			}
			else if (lowerTyperef == "coordinates") {
				return typeof(UrbanChallenge.Common.Coordinates);
			}
			else if (lowerTyperef == "coordinates[]") {
				return typeof(UrbanChallenge.Common.Coordinates[]);
			}
			else if (lowerTyperef == "circle") {
				return typeof(UrbanChallenge.Common.Shapes.Circle);
			}
			else if (lowerTyperef == "circlesegment") {
				return typeof(UrbanChallenge.Common.Shapes.CircleSegment);
			}
			else if (lowerTyperef == "polygon") {
				return typeof(UrbanChallenge.Common.Shapes.Polygon);
			}
			else if (lowerTyperef == "polygon[]") {
				return typeof(UrbanChallenge.Common.Shapes.Polygon[]);
			}
			else if (lowerTyperef == "line") {
				return typeof(UrbanChallenge.Common.Shapes.Line);
			}
			else if (lowerTyperef == "linesegment") {
				return typeof(UrbanChallenge.Common.Shapes.LineSegment);
			}
			else if (lowerTyperef == "path") {
				return typeof(UrbanChallenge.Common.Path.Path);
			}
			else if (lowerTyperef == "bezier") {
				return typeof(UrbanChallenge.Common.Splines.CubicBezier);
			}
			else if (lowerTyperef == "linelist") {
				return typeof(UrbanChallenge.Common.Shapes.LineList);
			}
			else if (lowerTyperef == "linelist[]") {
				return typeof(UrbanChallenge.Common.Shapes.LineList[]);
			}
			else if (lowerTyperef == "obstacle[]") {
				return typeof(UrbanChallenge.Common.Sensors.OperationalObstacle[]);
			}
			else {
				return Type.GetType(typeref, true, true);
			}
		}
	}
}
