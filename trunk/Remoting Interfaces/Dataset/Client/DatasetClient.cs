using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Threading;
using Dataset.Utility;
using Dataset.Config;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Splines;
using UrbanChallenge.Common.Sensors;

namespace Dataset.Client {
	public class DatasetClient : IDictionary<string, IDataItemClient> {
		public event EventHandler<ClientDataItemAddedEventArgs> DataItemAdded;

		private SortedDictionary<string, IDataItemClient> items;

		private Socket listener;
		private IPEndPoint localEndPoint;

		private IPAddress multicastAddr;
		private bool didJoinMulticast = false;

		public DatasetClient(string group) {
			multicastAddr = null;

			Initialize(new IPEndPoint(IPAddress.Any, 0), group);
		}

		public DatasetClient(string group, string configFile) {
			// load the client config
			DatasetConfigurationSection configSection = DatasetConfigurationSection.Deserialize(configFile);
			if (configSection.ClientConfig != null) {
				localEndPoint = new IPEndPoint(IPAddress.Parse(configSection.ClientConfig.BindTo), configSection.ClientConfig.Port);
				multicastAddr = IPAddress.Parse(configSection.ClientConfig.MulticastAddress);
				if (!configSection.ClientConfig.Multicast || multicastAddr.Equals(IPAddress.Any))
					multicastAddr = null;
			}

			Initialize(localEndPoint, group);
		}

		public DatasetClient(string group, IPEndPoint localEndpoint, IPAddress multicastAddr) {
			this.multicastAddr = multicastAddr;
			Initialize(localEndpoint, group);
		}

		private void Initialize(IPEndPoint endpoint, string group) {
			items = new SortedDictionary<string, IDataItemClient>(new CaseInsensitiveStringComparer());

			// load the config 
			Type diGenType = typeof(DataItemClient<>);
			Type mapFieldsGenericType = typeof(MapFieldsDataItemClient<>);
			DatasetXmlParser.ParseConfig(delegate(DataItemDescriptor desc, string specialType, List<KeyValuePair<string, string>> attributes) {
				if (specialType == "mapfields") {
					Type mapFieldsType = mapFieldsGenericType.MakeGenericType(desc.DataType);
					Add(desc.Name, (IDataItemClient)Activator.CreateInstance(mapFieldsType, desc, this));
				}
				else {
					// general case
					Type diType = diGenType.MakeGenericType(desc.DataType);
					Add(desc.Name, (IDataItemClient)Activator.CreateInstance(diType, desc));
				}
			}, group);

			// initialize the listener object
			InitializeListener(endpoint);
		}

		#region IDictionary<string,IDataItemClient> Members

		public void Add(DataItemDescriptor desc) {
			Type diGenType = typeof(DataItemClient<>);
			Type diType = diGenType.MakeGenericType(desc.DataType);
			Add(desc.Name, (IDataItemClient)Activator.CreateInstance(diType, desc));
		}

		public void Add(string key, IDataItemClient value) {
			lock (items) {
				items.Add(key, value);
			}

			if (DataItemAdded != null) {
				DataItemAdded(this, new ClientDataItemAddedEventArgs(value));
			}
		}

		public bool ContainsKey(string key) {
			lock (items) {
				return items.ContainsKey(key);
			}
		}

		public ICollection<string> Keys {
			get { return items.Keys; }
		}

		public bool Remove(string key) {
			lock (items) {
				return items.Remove(key);
			}
		}

		public bool TryGetValue(string key, out IDataItemClient value) {
			lock (items) {
				return items.TryGetValue(key, out value);
			}
		}

		public ICollection<IDataItemClient> Values {
			get { return items.Values; }
		}

		public List<IDataItemClient> GetDataItems() {
			lock (items) {
				return new List<IDataItemClient>(items.Values);
			}
		}

		public DataItemClient<T> ItemAs<T>(string key) {
			return items[key] as DataItemClient<T>;
		}

		public IDataItemClient this[string key] {
			get {
				return items[key];
			}
			set {
				items[key] = value;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<string,IDataItemClient>> Members

		void ICollection<KeyValuePair<string,IDataItemClient>>.Add(KeyValuePair<string, IDataItemClient> item) {
			lock (items) {
				((ICollection<KeyValuePair<string, IDataItemClient>>)items).Add(item);
			}
		}

		public void Clear() {
			lock (items) {
				items.Clear();
			}
		}

		bool ICollection<KeyValuePair<string,IDataItemClient>>.Contains(KeyValuePair<string, IDataItemClient> item) {
			lock (items) {
				return ((ICollection<KeyValuePair<string, IDataItemClient>>)items).Contains(item);
			}
		}

		void ICollection<KeyValuePair<string,IDataItemClient>>.CopyTo(KeyValuePair<string, IDataItemClient>[] array, int arrayIndex) {
			lock (items) {
				((ICollection<KeyValuePair<string, IDataItemClient>>)items).CopyTo(array, arrayIndex);
			}
		}

		public int Count {
			get { return items.Count; }
		}

		bool ICollection<KeyValuePair<string,IDataItemClient>>.IsReadOnly {
			get { return false; }
		}

		bool ICollection<KeyValuePair<string,IDataItemClient>>.Remove(KeyValuePair<string, IDataItemClient> item) {
			return ((ICollection<KeyValuePair<string, IDataItemClient>>)items).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,IDataItemClient>> Members

		public IEnumerator<KeyValuePair<string, IDataItemClient>> GetEnumerator() {
			return items.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region Listener

		private int lastSeqNo = -1;
		private int receivedCount = 0;
		private int missedCount = 0;
		private long bytesCount = 0;
		private DateTime start;

		private Thread listenerThread;

		private BinaryFormatter formatter = new BinaryFormatter();

		private void InitializeListener(IPEndPoint endpoint) {
			this.localEndPoint = endpoint;

			if (localEndPoint.Address.Equals(IPAddress.Any)) {
				// lookup an ip address of the local computer
				string hostName = Dns.GetHostName();
				IPAddress[] addrs = Dns.GetHostAddresses(hostName);
				// take the first one
				for (int i = 0; i < addrs.Length; i++) {
					if (addrs[i].AddressFamily == AddressFamily.InterNetwork) {
						localEndPoint.Address = addrs[i];
						break;
					}
				}
			}

			// open the socket
			listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			// bind the endpoint
			listener.Bind(localEndPoint);

			if (localEndPoint.Port == 0) {
				// get the local port
				localEndPoint.Port = ((IPEndPoint)listener.LocalEndPoint).Port;
			}

			// start the listener thread
			listenerThread = new Thread(ListenerThread);
			listenerThread.Start();
		}

		public void AttachToSource(Dataset.Source.DatasetSourceFacade source) {
			IPEndPoint localEP = new IPEndPoint(localEndPoint.Address, localEndPoint.Port);
			if (multicastAddr != null) {
				localEP.Address = multicastAddr;
				if (!didJoinMulticast) {
					MulticastOption opt = new MulticastOption(multicastAddr, localEndPoint.Address);
					listener.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, opt);
					didJoinMulticast = true;
				}
			}

			// register as a listener on the source
			source.RegisterListener(localEP);
		}

		public IPEndPoint EndPoint {
			get { return localEndPoint; }
		}

		private void ListenerThread() {
			Thread.CurrentThread.IsBackground = true;
			Thread.CurrentThread.Name = "DatasetClient Listener Thread";

			start = DateTime.Now;

			byte[] buffer = new byte[ushort.MaxValue];
			while (true) {
				// receive the next udp message
				int bytesRead = listener.Receive(buffer);

				bytesCount += bytesRead;

				MemoryStream ms = new MemoryStream(buffer, 0, bytesRead, false);
				while (ms.Position < bytesRead) {
					int messageType = ms.ReadByte();
					if (messageType == DataItemComm.DataValueMsgID)
						HandleDataValueMessage(ms);
				}
			}
		}

		private void HandleDataValueMessage(Stream s) {
			// construct a binary reader
			BinaryReader reader = new BinaryReader(s);

			// get the sequence number
			int seqNo = reader.ReadInt32();

			// calculate how many packet we've missed
			// we are expecting a difference of 1 in the sequence numbers
			int missed = seqNo - lastSeqNo - 1;
			// if the difference is less than zero, something stuff is going on 
			if (missed > 0 && lastSeqNo != -1) {
				missedCount += missed;
			}

			// set the last sequence number
			lastSeqNo = seqNo;

			// increment the received count
			receivedCount++;

			// get the data item name
			string diName = reader.ReadString();
			// get the time
			CarTimestamp t = reader.ReadDouble();

			// read the data type code
			DataTypeCode dtc = (DataTypeCode)reader.ReadInt32();

			// handle the type appropriately
			object value = null;
			switch (dtc) {
				case DataTypeCode.Double:
					value = reader.ReadDouble();
					break;

				case DataTypeCode.Single:
					value = reader.ReadSingle();
					break;

				case DataTypeCode.Int8:
					value = reader.ReadSByte();
					break;

				case DataTypeCode.Int16:
					value = reader.ReadInt16();
					break;

				case DataTypeCode.Int32:
					value = reader.ReadInt32();
					break;

				case DataTypeCode.Int64:
					value = reader.ReadInt64();
					break;

				case DataTypeCode.UInt8:
					value = reader.ReadByte();
					break;

				case DataTypeCode.UInt16:
					value = reader.ReadUInt16();
					break;

				case DataTypeCode.UInt32:
					value = reader.ReadUInt32();
					break;

				case DataTypeCode.UInt64:
					value = reader.ReadUInt64();
					break;

				case DataTypeCode.Boolean:
					value = reader.ReadBoolean();
					break;

				case DataTypeCode.DateTime:
					value = DateTime.FromBinary(reader.ReadInt64());
					break;

				case DataTypeCode.TimeSpan:
					value = new TimeSpan(reader.ReadInt64());
					break;

				case DataTypeCode.Coordinates:
					value = new Coordinates(reader.ReadDouble(), reader.ReadDouble());
					break;

				case DataTypeCode.Circle:
					value = new Circle(reader.ReadDouble(), new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
					break;

				case DataTypeCode.Line:
					value = new Line(new Coordinates(reader.ReadDouble(), reader.ReadDouble()), new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
					break;

				case DataTypeCode.LineSegment:
					value = new LineSegment(new Coordinates(reader.ReadDouble(), reader.ReadDouble()), new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
					break;

				case DataTypeCode.Polygon: {
						CoordinateMode coordMode = (CoordinateMode)reader.ReadInt32();
						int count = reader.ReadInt32();
						Polygon pg = new Polygon(coordMode);
						for (int i = 0; i < count; i++) {
							pg.Add(new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
						}
						value = pg;
					}
					break;

				case DataTypeCode.Bezier: {
						value = new CubicBezier(
							new Coordinates(reader.ReadDouble(), reader.ReadDouble()),
							new Coordinates(reader.ReadDouble(), reader.ReadDouble()),
							new Coordinates(reader.ReadDouble(), reader.ReadDouble()),
							new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
					}
					break;

				case DataTypeCode.LineList: {
						LineList linelist = new LineList();
						int ll_count = reader.ReadInt32();
						linelist.Capacity = ll_count;
						for (int i = 0; i < ll_count; i++) {
							linelist.Add(new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
						}
						value = linelist;
					}
					break;

				case DataTypeCode.CoordinatesArray: {
						int numPts = reader.ReadInt32();
						Coordinates[] pts = new Coordinates[numPts];
						for (int i = 0; i < numPts; i++) {
							pts[i] = new Coordinates(reader.ReadDouble(), reader.ReadDouble());
						}
						value = pts;
					}
					break;

				case DataTypeCode.LineListArray: {
						int numLineList = reader.ReadInt32();
						LineList[] lineLists = new LineList[numLineList];
						for (int i = 0; i < numLineList; i++) {
							int numPoints = reader.ReadInt32();
							lineLists[i] = new LineList(numPoints);
							for (int j = 0; j < numPoints; j++) {
								lineLists[i].Add(new Coordinates(reader.ReadDouble(), reader.ReadDouble()));
							}
						}
						value = lineLists;
					}
					break;

				case DataTypeCode.PolygonArray: {
						int numPolys = reader.ReadInt32();
						Polygon[] polys = new Polygon[numPolys];
						for (int i = 0; i < numPolys; i++) {
							int numPoints = reader.ReadInt32();
							polys[i] = new Polygon(numPoints);
							for (int j = 0; j < numPoints; j++) {
								polys[i].Add(ReadReducedCoord(reader));
							}
						}
						value = polys;
					}
					break;

				case DataTypeCode.ObstacleArray: {
						int numObs = reader.ReadInt32();
						OperationalObstacle[] obstacles = new OperationalObstacle[numObs];
						for (int i = 0; i < numObs; i++) {
							OperationalObstacle obs = new OperationalObstacle();
							obs.age = reader.ReadInt32();
							obs.obstacleClass = (ObstacleClass)reader.ReadInt32();
							obs.ignored = reader.ReadBoolean();
							obs.headingValid = reader.ReadBoolean();
							obs.heading = reader.ReadDouble();

							int numPoints = reader.ReadInt32();
							obs.poly = new Polygon(numPoints);
							for (int j = 0; j < numPoints; j++) {
								obs.poly.Add(ReadReducedCoord(reader));
							}

							obstacles[i] = obs;
						}

						value = obstacles;
					}
					break;

				case DataTypeCode.BinarySerialized:
					if (reader.ReadByte() == 0) {
						value = formatter.Deserialize(s);
					}
					else {
						DeflateStream ds = new DeflateStream(s, CompressionMode.Decompress, true);
						value = formatter.Deserialize(ds);
					}
					break;

				default:
					// unknown type
					throw new InvalidOperationException();
			}

			// check if there is no value
			if (value == null)
				return;

			// check if we have the data item
			IDataItemClient diObj;
			if (!items.TryGetValue(diName, out diObj)) {	
				// build the data item
				Type diGenType = typeof(DataItemClient<>);
				Type diType = diGenType.MakeGenericType(value.GetType());
				diObj = (IDataItemClient)Activator.CreateInstance(diType, diName);

				Add(diName, diObj);
			}

			diObj.AddDataItem(value, t);
		}

		private static Coordinates ReadReducedCoord(BinaryReader reader) {
			return new Coordinates(reader.ReadSingle(), reader.ReadSingle());
		}

		public int ReceivedCount {
			get { return receivedCount; }
		}

		public int MissedCount {
			get { return missedCount; }
		}

		public double MissedPerctange {
			get { return missedCount / (missedCount + receivedCount); }
		}

		public double ReceivedRate {
			get { return receivedCount / (DateTime.Now - start).TotalSeconds; }
		}

		public double ByteRate {
			get { return bytesCount / (DateTime.Now - start).TotalSeconds; }
		}

		public void ResetCounters() {
			receivedCount = 0;
			missedCount = 0;
			bytesCount = 0;
			start = DateTime.Now;
		}

		#endregion
	}
}
