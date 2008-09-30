using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Resources;
using System.IO;
using System.Xml;
using System.Xml.Schema;

using Dataset.Source;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Utility;

namespace UrbanChallenge.Actuation {
	public class FeedbackMapper {
		private Dictionary<int, FeedbackType> types;
		private Type targetType;
		private TextWriter logWriter;

		public FeedbackMapper(Stream configStream, Type targetType, TextWriter logWriter) {
			this.targetType = targetType;
			types = new Dictionary<int, FeedbackType>();

			this.logWriter = logWriter;

			LoadMapping(configStream);
		}

		public ICollection<FeedbackType> FeedbackTypes {
			get { return types.Values; }
		}

		public void HandleMessage(byte[] message, object target, DatasetSource ds) {
			BigEndianBinaryReader reader = new BigEndianBinaryReader(message);
			// read in the current time
			CarTimestamp time = new CarTimestamp(reader.ReadUInt16(), reader.ReadInt32());
			// read the feedback type
			byte feedbackType = reader.ReadByte();

			// read the length
			ushort len = reader.ReadUInt16();

			// dispatch if we can
			FeedbackType type;
			if (types.TryGetValue((int)feedbackType, out type)) {
				object[] vals = type.MapMessage(target, ds, time, reader);

				if (logWriter != null) {
					logWriter.Write(time);
					logWriter.Write(",");
					logWriter.Write(feedbackType);
					logWriter.Write(",");

					for (int i = 0; i < vals.Length - 1; i++) {
						logWriter.Write(vals[i]);
						logWriter.Write(",");
					}

					logWriter.WriteLine(vals[vals.Length - 1]);
				}
			}
		}

		#region Mapping configuration

		private void LoadMapping(Stream configStream) {
			Assembly thisAssm = typeof(FeedbackMapper).Assembly;
			Stream xsdStream = thisAssm.GetManifestResourceStream(this.GetType(), "FeedbackMapping.xsd");
			XmlSchema mappingSchema = XmlSchema.Read(xsdStream, ValidationEvent);

			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.Schemas.Add(mappingSchema);
			readerSettings.IgnoreComments = true;
			readerSettings.IgnoreWhitespace = true;

			XmlReader reader = XmlReader.Create(configStream, readerSettings);

			HandleMapping(reader);
		}

		private void ValidationEvent(object sender, ValidationEventArgs e) {
			// do something with this
		}

		private void HandleMapping(XmlReader reader) {
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "feedbackType") {
					HandleFeedbackType(reader);
				}
			}
		}

		private void HandleFeedbackType(XmlReader reader) {
			string name = reader.GetAttribute("name");
			string id_str = reader.GetAttribute("id");
			string methodname = reader.GetAttribute("method");

			int id = int.Parse(id_str);
			List<FeedbackField> fields = new List<FeedbackField>();

			// parse all the fields
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "feedbackType") {
					break;
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "field") {
						fields.Add(HandleField(reader));
					}
				}
			}

			MethodInfo method = null;
			MethodType methodType = MethodType.None;
			if (methodname != null) {
				// try to find the method on the target type
				methodType = MethodType.AllFields | MethodType.Timestamp | MethodType.MessageType;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.AllFields | MethodType.Timestamp;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.AllFields | MethodType.MessageType;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.AllFields;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.ObjectArray | MethodType.Timestamp | MethodType.MessageType;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.ObjectArray | MethodType.Timestamp;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.ObjectArray | MethodType.MessageType;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

				methodType = MethodType.ObjectArray;
				method = TryGetMethodInfo(methodname, methodType, fields);
				if (method != null) goto FoundMethod;

			FoundMethod:
				if (method == null) {
					throw new InvalidOperationException("Could not find a matching method for " + methodname);
				}
			}

			types.Add(id, new FeedbackType(name, id, method, methodType, fields));
		}

		private MethodInfo TryGetMethodInfo(string methodName, MethodType methodType, IList<FeedbackField> fields) {
			List<Type> methodSig = new List<Type>(fields.Count+2);
			if ((methodType & MethodType.Timestamp) != MethodType.None) {
				methodSig.Add(typeof(CarTimestamp));
			}

			if ((methodType & MethodType.MessageType) != MethodType.None) {
				methodSig.Add(typeof(int));
			}

			if ((methodType & MethodType.AllFields) != MethodType.None) {
				for (int i = 0; i < fields.Count; i++) {
					methodSig.Add(fields[i].OutType);
				}
			}

			if ((methodType & MethodType.ObjectArray) != MethodType.None) {
				methodSig.Add(typeof(object[]));
			}

			try {
				return targetType.GetMethod(methodName, methodSig.ToArray());
			}
			catch (Exception) {
			}

			return null;
		}

		private FeedbackField HandleField(XmlReader reader) {
			string name = reader.GetAttribute("name");
			string intype_str = reader.GetAttribute("intype");
			string outtype_str = reader.GetAttribute("outtype");
			string dsitem = reader.GetAttribute("dsitem");
			string units = reader.GetAttribute("units");

			// figure out the in type
			InType intype = (InType)Enum.Parse(typeof(InType), intype_str.ToUpper(), false);
			Type outtype = MapType(outtype_str);

			if (!reader.Read())
				throw new InvalidOperationException();

			if (reader.NodeType != XmlNodeType.Element)
				throw new InvalidOperationException();

			FeedbackField field = null;

			string elementName = reader.LocalName;
			if (elementName == "raw") {
				field = new RawField(name, intype, outtype, dsitem, units);
			}
			else if (elementName == "bool") {
				field = new BoolField(name, intype, outtype, dsitem, units);
			}
			else if (elementName == "conversion") {
				double scale = 0, preOffset = 0, postOffset = 0;
				HandleConversion(reader, ref scale, ref preOffset, ref postOffset);
				field = new ConversionField(name, intype, outtype, dsitem, units, scale, preOffset, postOffset);
			}
			else if (elementName == "enum") {
				Dictionary<int, object> map = new Dictionary<int, object>();
				object defVal = null;
				if (!outtype.IsEnum)
					throw new InvalidOperationException("Error in field " + name + ": outtype is not an enum");
				HandleEnumMap(reader, outtype, ref map, ref defVal);
				field = new EnumField(name, intype, outtype, dsitem, units, map, defVal);
			}
			else if (elementName == "intMap") {
				Dictionary<int, int> map = new Dictionary<int, int>();
				int defVal = 0;
				// should check if out type is valid
				HandleIntMap(reader, ref map, ref defVal);
				field = new IntField(name, intype, outtype, dsitem, units, map, defVal);
			}
			else if (elementName == "bitmap") {
				string[] fields;
				HandleBitmap(reader, out fields);
				field = new BitmapField(name, intype, outtype, dsitem, units, fields);
			}
			else {
				throw new InvalidOperationException();
			}

			reader.Read();
			if (reader.NodeType != XmlNodeType.EndElement || reader.LocalName != "field")
				throw new InvalidOperationException();

			return field;
		}

		private void HandleConversion(XmlReader reader, ref double scale, ref double preOffset, ref double postOffset) {
			bool noRead = false;
			while (noRead || reader.Read()) {
				noRead = false;
				if (reader.NodeType == XmlNodeType.EndElement) {
					if (reader.LocalName == "conversion")
						return;
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "scale") {
						scale = reader.ReadElementContentAsDouble();
						noRead = true;
					}
					else if (reader.LocalName == "offset") {
						string prePost = reader.GetAttribute("order");
						if (prePost == "pre")
							preOffset = reader.ReadElementContentAsDouble();
						else if (prePost == "post")
							postOffset = reader.ReadElementContentAsDouble();
						else
							throw new InvalidOperationException();

						noRead = true;
					}
				}
			}
		}

		private void HandleEnumMap(XmlReader reader, Type enumType, ref Dictionary<int, object> map, ref object defaultVal) {
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement) {
					if (reader.LocalName == "enum")
						return;
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "value") {
						string inval = reader.GetAttribute("in");
						string outval = reader.GetAttribute("outenum");

						object enumVal = Enum.Parse(enumType, outval);

						int int_inval;
						if (inval == "default") {
							defaultVal = enumVal;
						}
						else if (int.TryParse(inval, out int_inval)) {
							map.Add(int_inval, enumVal);
						}
						else {
							throw new InvalidOperationException();
						}
					}
				}
			}
		}

		private void HandleIntMap(XmlReader reader, ref Dictionary<int, int> map, ref int defVal) {
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement) {
					if (reader.LocalName == "intMap")
						return;
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "value") {
						string inval = reader.GetAttribute("in");
						string outval = reader.GetAttribute("outint");

						int intVal = int.Parse(outval);

						int int_inval;
						if (inval == "default") {
							defVal = intVal;
						}
						else if (int.TryParse(inval, out int_inval)) {
							map.Add(int_inval, intVal);
						}
						else {
							throw new InvalidOperationException();
						}
					}
				}
			}
		}

		private void HandleBitmap(XmlReader reader, out string[] fields) {
			fields = null;
			List<string> fieldList = new List<string>();
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement) {
					if (reader.LocalName == "bitmap") {
						fields = fieldList.ToArray();
						return;
					}
				}
				else if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "bit") {
						string inval = reader.GetAttribute("fieldname");
						if (string.IsNullOrEmpty(inval)) {
							// this is an empty position
							fieldList.Add(null);
						}
						else {
							fieldList.Add(inval);
						}
					}
				}
			}
		}

		private Type MapType(string typename) {
			string lowername = typename.ToLower().Trim();
			bool isArray = false;
			if (lowername.EndsWith("[]")) {
				isArray = true;
				lowername = lowername.Substring(0, lowername.Length-2);
			}
			Type type = null;
			if (lowername == "double")
				type = typeof(double);
			else if (lowername == "float" || lowername == "single")
				type = typeof(float);
			else if (lowername == "int" || lowername == "int32" || lowername == "s32")
				type = typeof(int);
			else if (lowername == "uint" || lowername == "uint32" || lowername == "u32")
				type = typeof(uint);
			else if (lowername == "short" || lowername == "int16" || lowername == "s16")
				type = typeof(short);
			else if (lowername == "ushort" || lowername == "uint16" || lowername == "u16")
				type = typeof(ushort);
			else if (lowername == "sbyte" || lowername == "int8" || lowername == "s8")
				type = typeof(sbyte);
			else if (lowername == "byte" || lowername == "uint8" || lowername == "u8")
				type = typeof(byte);
			else if (lowername == "bool")
				type = typeof(bool);
			else
				return Type.GetType(typename, true, true);

			if (isArray) {
				return type.MakeArrayType();
			}
			else {
				return type;
			}
		}
		
		#endregion
	}
}
