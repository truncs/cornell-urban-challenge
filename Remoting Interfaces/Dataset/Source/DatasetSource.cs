using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UrbanChallenge.Common;
using System.Threading;
using System.IO.Compression;
using Dataset.Utility;
using Dataset.Config;

namespace Dataset.Source {
	public class DatasetSource : IDictionary<string, IDataItemSource> {
		private DatasetSender sender;

		private Dictionary<string, IDataItemSource> items;

		public DatasetSource(string group) {
			sender = new DatasetSender();
			InitItems(group);
		}

		public DatasetSource(string group, string configFile) {
			sender = new DatasetSender(configFile);
			InitItems(group);
		}

		public DatasetSource(string group, IPEndPoint endpoint) {
			sender = new DatasetSender(endpoint);
			InitItems(group);
		}

		private void InitItems(string group) {
			items = new Dictionary<string, IDataItemSource>(new CaseInsensitiveStringComparer());

			Type diGenType = typeof(DataItemSource<>);
			DatasetXmlParser.ParseConfig(delegate(DataItemDescriptor ds, string specialType, List<KeyValuePair<string, string>> attributes) {
				if (specialType == "runrate") {
					RunRateDataItemSource runRate = new RunRateDataItemSource(ds, attributes);
					Add(ds.Name, runRate);
				}
				else {
					Type diType = diGenType.MakeGenericType(ds.DataType);
					object dataItem = Activator.CreateInstance(diType, ds);
					Add(ds.Name, (IDataItemSource)dataItem);
				}
			}, group);
		}

		public DatasetSender Sender {
			get { return sender; }
		}

		internal void OnDataItemValueAdded(SourceDataValueAddedEventArgs e) {
			sender.OnDataItemValueAdded(e);
		}

		#region IDictionary<string,IDataItemSource> Members

		public void Add(string key, IDataItemSource value) {
			items.Add(key, value);
			value.Parent = this;
		}

		public bool ContainsKey(string key) {
			return items.ContainsKey(key);
		}

		public ICollection<string> Keys {
			get { return items.Keys; }
		}

		public bool Remove(string key) {
			IDataItemSource di;
			if (TryGetValue(key, out di)) {
				di.Parent = null;
			}
			return items.Remove(key);
		}

		public bool TryGetValue(string key, out IDataItemSource value) {
			return items.TryGetValue(key, out value);
		}

		public ICollection<IDataItemSource> Values {
			get { return items.Values; }
		}

		public IDataItemSource this[string key] {
			get { return items[key]; }
			set { items[key] = value; }
		}

		public DataItemSource<T> ItemAs<T>(string key) {
			return items[key] as DataItemSource<T>;
		}

		public void MarkOperation(string key, CarTimestamp ts) {
			RunRateDataItemSource item = items[key] as RunRateDataItemSource;
			if (item == null) {
				throw new KeyNotFoundException();
			}

			item.Mark(ts);
		}

		#endregion

		#region ICollection<KeyValuePair<string,IDataItemSource>> Members

		void ICollection<KeyValuePair<string,IDataItemSource>>.Add(KeyValuePair<string, IDataItemSource> item) {
			((ICollection<KeyValuePair<string, IDataItemSource>>)items).Add(item);
		}

		public void Clear() {
			items.Clear();
		}

		bool ICollection<KeyValuePair<string, IDataItemSource>>.Contains(KeyValuePair<string, IDataItemSource> item) {
			return ((ICollection<KeyValuePair<string, IDataItemSource>>)items).Contains(item);
		}

		void ICollection<KeyValuePair<string, IDataItemSource>>.CopyTo(KeyValuePair<string, IDataItemSource>[] array, int arrayIndex) {
			((ICollection<KeyValuePair<string, IDataItemSource>>)items).CopyTo(array, arrayIndex);
		}

		public int Count {
			get { return items.Count; }
		}

		bool ICollection<KeyValuePair<string, IDataItemSource>>.IsReadOnly {
			get { return false; }
		}

		bool ICollection<KeyValuePair<string, IDataItemSource>>.Remove(KeyValuePair<string, IDataItemSource> item) {
			return ((ICollection<KeyValuePair<string, IDataItemSource>>)items).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,IDataItemSource>> Members

		public IEnumerator<KeyValuePair<string, IDataItemSource>> GetEnumerator() {
			return items.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion
	}
}
