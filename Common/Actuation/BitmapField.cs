using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;

namespace UrbanChallenge.Actuation {
	public class BitmapField : FeedbackField {
		private bool boolArray;
		private bool bitArray;
		private string[] fields;
		private FieldInfo[] fieldHandles;
		private ConstructorInfo ctor;

		public BitmapField(string name, InType intype, Type outtype, string dsitem, string units, string[] fields)
			: base(name, intype, outtype, dsitem, units) {

			this.fields = fields;

			// get the type size
			int typeSize = 0;
			switch (intype) {
				case InType.U8:
				case InType.S8:
					typeSize = 8;
					break;

				case InType.U16:
				case InType.S16:
					typeSize = 16;
					break;

				case InType.U32:
				case InType.S32:
					typeSize = 32;
					break;

				default:
					typeSize = -1;
					break;
			}

			if (fields.Length > typeSize) {
				throw new InvalidOperationException("more fields specified than the number of bits for " + name);
			}

			if (outtype == typeof(bool[])) {
				boolArray = true;
			}
			else if (outtype == typeof(BitArray)) {
				bitArray = true;
			}
			else {
				// find a default constructor
				ctor = outtype.GetConstructor(Type.EmptyTypes);

				if (ctor == null) {
					throw new InvalidOperationException("no default constructor for type " + outtype.Name);
				}

				fieldHandles = new FieldInfo[fields.Length];
				for (int i = 0; i < fields.Length; i++) {
					if (!string.IsNullOrEmpty(fields[i])) {
						fieldHandles[i] = outtype.GetField(fields[i]);

						if (fieldHandles[i] == null) {
							throw new InvalidOperationException("could not find field " + fields[i] + " on type " + outtype.Name);
						}
						else if (fieldHandles[i].FieldType != typeof(bool)) {
							throw new InvalidOperationException("field " + fields[i] + " is not a bool");
						}
						else if (fieldHandles[i].IsInitOnly || fieldHandles[i].IsLiteral || fieldHandles[i].IsStatic) {
							throw new InvalidOperationException("field " + fields[i] + " is read only or static");
						}
					}
				}
			}
		}

		protected override object MapData(int data) {
			if (boolArray) {
				bool[] ret = new bool[fields.Length];
				for (int i = 0; i < fields.Length; i++) {
					ret[i] = ((data & 0x1) == 1);
					data >>= 1;
				}

				return ret;
			}
			else if (bitArray) {
				BitArray array = new BitArray(fields.Length);
				for (int i = 0; i < fields.Length; i++) {
					array[i] = ((data & 0x1) == 1);
					data >>= 1;
				}

				return array;
			}
			else {
				object obj = ctor.Invoke(null);

				for (int i = 0; i < fieldHandles.Length; i++) {
					if (fieldHandles[i] != null) {
						bool set = ((data & 0x1) == 1);
						fieldHandles[i].SetValue(obj, set);
					}

					// shift the data over
					data >>= 1;
				}

				return obj;
			}
		}

		public string[] Fields {
			get {
				return fields;
			}
		}
	}
}
