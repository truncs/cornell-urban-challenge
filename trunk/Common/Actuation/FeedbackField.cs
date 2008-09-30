using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Source;
using Dataset;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Common;


namespace UrbanChallenge.Actuation {
	public abstract class FeedbackField {
		protected string dsitem;
		protected InType intype;
		protected Type outtype;
		protected string name;
		private string unitString;

		protected FeedbackField(string name, InType intype, Type outtype, string dsitem, string unitString) {
			this.name = name;
			this.intype = intype;
			this.outtype = outtype;
			this.dsitem = dsitem;
			this.unitString = unitString;
		}

		public string Name {
			get { return name; }
		}

		public InType InTypeEnum {
			get { return intype; }
		}

		public Type GetInType() {
			switch (intype) {
				case InType.U8:
					return typeof(byte);
				case InType.S8:
					return typeof(sbyte);

				case InType.U16:
					return typeof(ushort);
				case InType.S16:
					return typeof(short);

				case InType.U32:
					return typeof(uint);
				case InType.S32:
					return typeof(int);

				default:
					throw new InvalidOperationException();
			}
		}

		public Type OutType {
			get { return outtype; }
		}

		public string DataSetItem {
			get { return dsitem; }
		}

		public string UnitString {
			get { return unitString; }
		}

		public object MapField(BigEndianBinaryReader reader, DatasetSource ds, CarTimestamp ct) {
			object val = MapData(ReadData(reader));

			if (!string.IsNullOrEmpty(dsitem) && ds != null) {
				IDataItemSource di;
				if (!ds.TryGetValue(dsitem, out di)) {
					Type digType = typeof(DataItemSource<>);
					Type diType = digType.MakeGenericType(outtype);
					DataItemDescriptor desc = new DataItemDescriptor(dsitem, outtype, null, null, 250);
					di = (IDataItemSource)Activator.CreateInstance(diType, desc);
					ds.Add(dsitem, di);
				}
				di.Add(val, ct);
			}

			return val;
		}
		
		protected abstract object MapData(int data);

		protected int ReadData(BigEndianBinaryReader reader) {
			switch (intype) {
				case InType.U8:
					return reader.ReadByte();
				case InType.S8:
					return reader.ReadSByte();
				case InType.U16:
					return reader.ReadUInt16();
				case InType.S16:
					return reader.ReadInt16();
				case InType.U32:
					return (int)reader.ReadUInt32();
				case InType.S32:
					return reader.ReadInt32();
				default:
					throw new InvalidOperationException();
			}
		}

		public override string ToString() {
			return name + "{" + this.GetType().Name + "}";
		}
	}
}
