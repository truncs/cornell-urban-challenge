using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Dataset.Client {
	public class MapFieldsDataItemClient<T> : DataItemClient<T> {
		private delegate object GetValueHandler(T target);

		private class SubItemEntry {
			public GetValueHandler getValueHandler;
			public IDataItemClient subitem;

			public SubItemEntry(GetValueHandler handler, IDataItemClient subitem) {
				this.getValueHandler = handler;
				this.subitem = subitem;
			}
		}

		private List<SubItemEntry> subitems;

		public MapFieldsDataItemClient(DataItemDescriptor desc, DatasetClient parent)
			: base(desc) {
			Init(desc.Name, parent);
		}

		public MapFieldsDataItemClient(string name, DatasetClient parent)
			: base(name) {
			Init(name, parent);
		}

		private void Init(string name, DatasetClient parent) {
			subitems = new List<SubItemEntry>();

			// iterate over public fields
			FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo fi in fields) {
				if (CanMapType(fi.FieldType)) {
					// want to map this stuff
					SubItemEntry entry = new SubItemEntry(BuildFieldGetter(typeof(T), fi), GetDataItemClient(fi.FieldType, name + "." + fi.Name));
					subitems.Add(entry);
					parent.Add(entry.subitem.Name, entry.subitem);
				}
			}

			PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo prop in props) {
				if (CanMapType(prop.PropertyType)) {
					// want to map this stuff
					SubItemEntry entry = new SubItemEntry(BuildPropertyGetter(typeof(T), prop), GetDataItemClient(prop.PropertyType, name + "." + prop.Name));
					subitems.Add(entry);
					parent.Add(entry.subitem.Name, entry.subitem);
				}
			}
		}

		private IDataItemClient GetDataItemClient(Type destType, string name) {
			Type dataItemGenericType = typeof(DataItemClient<>);
			Type dataItemType = dataItemGenericType.MakeGenericType(destType);
			return (IDataItemClient)Activator.CreateInstance(dataItemType, name);
		}

		private bool CanMapType(Type type) {
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;

				default:
					return false;
			}
		}

		private GetValueHandler BuildFieldGetter(Type t, FieldInfo field) {
			DynamicMethod method = new DynamicMethod("GetField" + field.Name, typeof(object), new Type[] { t }, typeof(MapFieldsDataItemClient<T>));
			ILGenerator ilgen = method.GetILGenerator();

			// load the parameter value on the stack
			if (t.IsValueType) {
				ilgen.Emit(OpCodes.Ldarga_S, 0);
			}
			else {
				ilgen.Emit(OpCodes.Ldarg_0);
			}

			// load the field on the stack
			ilgen.Emit(OpCodes.Ldfld, field);

			// box that stuff
			ilgen.Emit(OpCodes.Box, field.FieldType);

			// return
			ilgen.Emit(OpCodes.Ret);

			return (GetValueHandler)method.CreateDelegate(typeof(GetValueHandler));
		}

		private GetValueHandler BuildPropertyGetter(Type t, PropertyInfo prop) {
			DynamicMethod method = new DynamicMethod("GetProperty" + prop.Name, typeof(object), new Type[] { t }, typeof(MapFieldsDataItemClient<T>));
			ILGenerator ilgen = method.GetILGenerator();

			// load the parameter value on the stack
			if (t.IsValueType) {
				ilgen.Emit(OpCodes.Ldarga_S, 0);
			}
			else {
				ilgen.Emit(OpCodes.Ldarg_0);
			}

			// call the get method
			ilgen.Emit(OpCodes.Callvirt, prop.GetGetMethod());

			// box that stuff
			ilgen.Emit(OpCodes.Box, prop.PropertyType);

			// return
			ilgen.Emit(OpCodes.Ret);

			return (GetValueHandler)method.CreateDelegate(typeof(GetValueHandler));
		}

		protected override void OnDataValueAdded(object val, UrbanChallenge.Common.CarTimestamp t) {
			base.OnDataValueAdded(val, t);

			// call on all the children
			foreach (SubItemEntry entry in subitems) {
				entry.subitem.AddDataItem(entry.getValueHandler(currentValue), t);
			}
		}
	}
}
