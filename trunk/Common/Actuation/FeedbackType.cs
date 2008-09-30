using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Dataset.Source;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Utility;


namespace UrbanChallenge.Actuation {
	[Flags]
	public enum MethodType {
		None = 0,
		AllFields = 1,
		Timestamp = 2,
		MessageType = 4,
		ObjectArray = 8
	}

	public class FeedbackType {
		private List<FeedbackField> fields;
		private int id;
		private string name;
		private MethodInfo method;
		private bool methodHasTimestamp;
		private bool methodHasMsgType;
		private bool methodIsObjectArray;

		public FeedbackType(string name, int id, MethodInfo callback, MethodType methodType, List<FeedbackField> fields) {
			this.name = name;
			this.id = id;
			this.method = callback;
			this.fields = fields;

			methodHasTimestamp = (methodType & MethodType.Timestamp) != MethodType.None;
			methodHasMsgType = (methodType & MethodType.MessageType) != MethodType.None;
			methodIsObjectArray = (methodType & MethodType.ObjectArray) != MethodType.None;
		}

		public object[] MapMessage(object target, DatasetSource ds, CarTimestamp ts, BigEndianBinaryReader reader) {
			// reader will be positioned at start of payload
			int nextra = 0;
			if (methodHasTimestamp)
				nextra++;
			if (methodHasMsgType)
				nextra++;

			object[] vals = new object[fields.Count];
			object[] param = null;
			if (method != null) {
				if (methodIsObjectArray) {
					param = new object[1+nextra];
				}
				else {
					param = new object[fields.Count + nextra];
				}

				if (methodHasTimestamp) {
					param[0] = ts;
					if (methodHasMsgType)
						param[1] = id;
				}
				else if (methodHasMsgType) {
					param[0] = id;
				}
			}

			for (int i = 0; i < fields.Count; i++) {
				object val = fields[i].MapField(reader, ds, ts);
				if (param != null && !methodIsObjectArray)
					param[i + nextra] = val;

				vals[i] = val;
			}

			if (methodIsObjectArray && param != null) {
				param[nextra] = vals;
			}

			if (method != null) {
				method.Invoke(target, param);
			}

			return vals;
		}

		public string Name {
			get { return name; }
		}

		public int MessageID {
			get { return id; }
		}

		public List<FeedbackField> Fields {
			get { return fields; }
		}

		public override string ToString() {
			return name;
		}
	}
}
