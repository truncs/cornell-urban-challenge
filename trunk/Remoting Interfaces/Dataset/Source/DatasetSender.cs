#define COMP_BIN_SER

using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Utility;
using UrbanChallenge.Common;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Dataset.Config;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Splines;
using System.IO.Compression;
using UrbanChallenge.Common.Sensors;

namespace Dataset.Source {
	public class DatasetSender {
		private class PooledMemoryStream : MemoryStream {
			public PooledMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
				: base(buffer, index, count, writable, publiclyVisible) {
			}
		}

		// buffer size for non-binary serialized objects
		private const int NomSendBufferSize = ushort.MaxValue-10;
		// buffer size for binary serialized objects
		private const int BinSerSendBufferSize = ushort.MaxValue;

		struct SendQueueEntry {
			public byte[] buffer;
			public int length;

			public SendQueueEntry(byte[] buffer, int length) {
				this.buffer = buffer;
				this.length = length;
			}
		}

		private const int target_packet_size = 250;

		private Socket sender;

		private List<IPEndPoint> listenerEndPoints = new List<IPEndPoint>();
		private ReaderWriterLock listenerRWLock = new ReaderWriterLock();

		private RollingQueue<SendQueueEntry> sendQueue = new RollingQueue<SendQueueEntry>(5000);
		private Stack<byte[]> nomBuffers = new Stack<byte[]>();
		private Stack<byte[]> binSerBuffers = new Stack<byte[]>();
		private BinaryFormatter formatter = new BinaryFormatter();

		private Thread senderThread;

		private bool doSendDataValues;

		private int seqNo = 0;
		private int packetCount = 0;
		private long byteCount = 0;
		private DateTime startTime = DateTime.Now;

		public DatasetSender() {
			InitializeDataSender(new IPEndPoint(IPAddress.Any, 0));
		}

		public DatasetSender(string configFile) {
			DatasetConfigurationSection sect = DatasetConfigurationSection.Deserialize(configFile);
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			if (sect.SourceConfig != null) {
				ep.Address = IPAddress.Parse(sect.SourceConfig.BindTo);
				ep.Port = sect.SourceConfig.Port;
			}
			InitializeDataSender(ep);
		}

		public DatasetSender(IPEndPoint endpoint) {
			InitializeDataSender(endpoint);
		}

		public EndPoint LocalEndpoint {
			get { return sender.LocalEndPoint; }
		}

		private void InitializeDataSender(IPEndPoint endpoint) {
			// create a udp socket
			sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			// bind to a local address with any available port
			sender.Bind(endpoint);

			// indicated that we're not sending data values
			doSendDataValues = false;

			senderThread = new Thread(SenderThread);
			senderThread.Start();
		}

		internal void OnDataItemValueAdded(SourceDataValueAddedEventArgs e) {
			if (!doSendDataValues)
				return;

			MemoryStream ms = null;
			try {
				// construct the message
				ms = GetSendBuffer(e.AbstractDataItem.TypeCode);
				BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8);

				// write the message type -- for now we only send data value messages
				writer.Write(DataItemComm.DataValueMsgID);

				// write the sequence number and increment
				writer.Write(seqNo);
				Interlocked.Increment(ref seqNo);

				// write the data item name
				writer.Write(e.AbstractDataItem.Name);
				// write the time as a double (seconds in car time)
				writer.Write(e.Time.ts);

				// figure out the type
				DataTypeCode dtc = e.AbstractDataItem.TypeCode;
				// write out the type code
				writer.Write((int)dtc);

				// write the data depending on the type
				switch (dtc) {
					case DataTypeCode.Double:
						writer.Write(((SourceDataValueAddedEventArgs<double>)e).Value);
						break;

					case DataTypeCode.Single:
						writer.Write(((SourceDataValueAddedEventArgs<float>)e).Value);
						break;

					case DataTypeCode.Int8:
						writer.Write(((SourceDataValueAddedEventArgs<sbyte>)e).Value);
						break;

					case DataTypeCode.Int16:
						writer.Write(((SourceDataValueAddedEventArgs<Int16>)e).Value);
						break;

					case DataTypeCode.Int32:
						writer.Write(((SourceDataValueAddedEventArgs<Int32>)e).Value);
						break;

					case DataTypeCode.Int64:
						writer.Write(((SourceDataValueAddedEventArgs<Int64>)e).Value);
						break;

					case DataTypeCode.UInt8:
						writer.Write(((SourceDataValueAddedEventArgs<byte>)e).Value);
						break;

					case DataTypeCode.UInt16:
						writer.Write(((SourceDataValueAddedEventArgs<UInt16>)e).Value);
						break;

					case DataTypeCode.UInt32:
						writer.Write(((SourceDataValueAddedEventArgs<UInt32>)e).Value);
						break;

					case DataTypeCode.UInt64:
						writer.Write(((SourceDataValueAddedEventArgs<UInt64>)e).Value);
						break;

					case DataTypeCode.Boolean:
						writer.Write(((SourceDataValueAddedEventArgs<bool>)e).Value);
						break;

					case DataTypeCode.DateTime:
						writer.Write(((SourceDataValueAddedEventArgs<DateTime>)e).Value.ToBinary());
						break;

					case DataTypeCode.TimeSpan:
						writer.Write(((SourceDataValueAddedEventArgs<TimeSpan>)e).Value.Ticks);
						break;

					case DataTypeCode.Coordinates: {
							Coordinates v = ((SourceDataValueAddedEventArgs<Coordinates>)e).Value;
							WriteCoordinate(v, writer);
						}
						break;

					case DataTypeCode.Circle: {
							Circle c = ((SourceDataValueAddedEventArgs<Circle>)e).Value;
							writer.Write(c.r);
							WriteCoordinate(c.center, writer);
						}
						break;

					case DataTypeCode.Line: {
							Line l = ((SourceDataValueAddedEventArgs<Line>)e).Value;
							WriteCoordinate(l.P0, writer);
							WriteCoordinate(l.P1, writer);
						}
						break;

					case DataTypeCode.LineSegment: {
							LineSegment ls = ((SourceDataValueAddedEventArgs<LineSegment>)e).Value;
							WriteCoordinate(ls.P0, writer);
							WriteCoordinate(ls.P1, writer);
						}
						break;

					case DataTypeCode.Polygon: {
							Polygon pg = ((SourceDataValueAddedEventArgs<Polygon>)e).Value;
							writer.Write((int)pg.CoordinateMode);
							writer.Write(pg.Count);
							foreach (Coordinates pg_pt in pg) {
								WriteCoordinate(pg_pt, writer);
							}
						}
						break;

					case DataTypeCode.LineList: {
							LineList ll = ((SourceDataValueAddedEventArgs<LineList>)e).Value;
							writer.Write(ll.Count);
							foreach (Coordinates ll_pt in ll) {
								WriteCoordinate(ll_pt, writer);
							}
						}
						break;

					case DataTypeCode.Bezier: {
							CubicBezier cb = ((SourceDataValueAddedEventArgs<CubicBezier>)e).Value;
							WriteCoordinate(cb.P0, writer);
							WriteCoordinate(cb.P1, writer);
							WriteCoordinate(cb.P2, writer);
							WriteCoordinate(cb.P3, writer);
						}
						break;

					case DataTypeCode.CoordinatesArray: {
							Coordinates[] pts = ((SourceDataValueAddedEventArgs<Coordinates[]>)e).Value;
							writer.Write(pts.Length);
							for (int i = 0; i < pts.Length; i++) {
								WriteCoordinate(pts[i], writer);
							}
						}
						break;

					case DataTypeCode.LineListArray: {
							LineList[] lineLists = ((SourceDataValueAddedEventArgs<LineList[]>)e).Value;
							writer.Write(lineLists.Length);
							for (int i = 0; i < lineLists.Length; i++) {
								LineList list = lineLists[i];
								writer.Write(list.Count);
								for (int j = 0; j < list.Count; j++) {
									WriteCoordinate(list[i], writer);
								}
							}
						}
						break;

					case DataTypeCode.PolygonArray: {
							Polygon[] polys = ((SourceDataValueAddedEventArgs<Polygon[]>)e).Value;
							writer.Write(polys.Length);
							for (int i = 0; i < polys.Length; i++) {
								Polygon poly = polys[i];
								writer.Write(poly.Count);
								for (int j = 0; j < poly.Count; j++) {
									WriteReducedCoord(poly[j], writer);
								}
							}
						}
						break;

					case DataTypeCode.ObstacleArray: {
							OperationalObstacle[] obs = ((SourceDataValueAddedEventArgs<OperationalObstacle[]>)e).Value;
							writer.Write(obs.Length);
							for (int i = 0; i < obs.Length; i++) {
								writer.Write(obs[i].age);
								writer.Write((int)obs[i].obstacleClass);
								writer.Write(obs[i].ignored);
								writer.Write(obs[i].headingValid);
								writer.Write(obs[i].heading);
								writer.Write(obs[i].poly.Count);

								for (int j = 0; j < obs[i].poly.Count; j++) {
									WriteReducedCoord(obs[i].poly[j], writer);
								}
							}
						}
						break;

					case DataTypeCode.BinarySerialized:
						BinarySerializeData(ms, e.ObjectValue);
						break;
				}

				// we've constructed the message, queue it to send to the listeners
				lock (sendQueue) {
					sendQueue.Enqueue(new SendQueueEntry(ms.GetBuffer(), (int)ms.Position));
					// pulse the send event to indicate that there's stuff to send
					Monitor.Pulse(sendQueue);
				}
			}
			catch (Exception) {
				// don't try to recover
				if (ms != null) {
					FreeBuffer(ms);
				}
			}
		}

		private void WriteCoordinate(Coordinates c, BinaryWriter w) {
			w.Write(c.X);
			w.Write(c.Y);
		}

		private void WriteReducedCoord(Coordinates c, BinaryWriter w) {
			// write as a fixed-point short
			w.Write((float)c.X); w.Write((float)c.Y);
		}

		private void BinarySerializeData(Stream target, object value) {
#if COMP_BIN_SER
			// write a 1 indicating that we're compressing
			target.WriteByte(1);
			DeflateStream ds = new DeflateStream(target, CompressionMode.Compress, true);
			formatter.Serialize(ds, value);
			ds.Flush();
			ds.Close();
#else
			// write a 0 indicating that we're not compressing
			target.WriteByte(0);
			formatter.Serialize(target, value);
#endif
		}

		private MemoryStream GetSendBuffer(DataTypeCode dtc) {
			if (dtc == DataTypeCode.BinarySerialized) {
				return new MemoryStream();
			}
			else {
				byte[] data = null;
				lock (nomBuffers) {
					if (nomBuffers.Count == 0) {
						data = new byte[NomSendBufferSize];
					}
					else {
						data = nomBuffers.Pop();
					}
				}

				return new PooledMemoryStream(data, 0, data.Length, true, true);
			}
		}

		private void FreeBuffer(MemoryStream ms) {
			if (ms is PooledMemoryStream) {
				lock (nomBuffers) {
					if (nomBuffers.Count < 200) {
						nomBuffers.Push(ms.GetBuffer());
					}
				}
			}
		}

		private void FreeBuffer(byte[] buffer) {
			if (buffer.Length == NomSendBufferSize) {
				lock (nomBuffers) {
					if (nomBuffers.Count < 200) {
						nomBuffers.Push(buffer);
					}
				}
			}
		}

		private void SenderThread() {
			// mark this thread as a background thread
			Thread.CurrentThread.IsBackground = true;
			Thread.CurrentThread.Name = "Dataset sender thread";

			// keep track of client to remove
			List<IPEndPoint> removeList = new List<IPEndPoint>();

			// enter the send queue lock
			Monitor.Enter(sendQueue);
			while (true) {
				if (sendQueue.Count == 0) {
					Monitor.Wait(sendQueue);
				}

				// assume that now there is an entry in the queue
				if (sendQueue.Count > 0) {
					// dequeue the entry
					SendQueueEntry entry = sendQueue.Dequeue();

					// fill up the buffer until we're at our target size
					while (sendQueue.Count > 0) {
						SendQueueEntry nextEntry = sendQueue.Peek();
						if (entry.length + nextEntry.length <= target_packet_size) {
							// add in this entry
							// actually dequeue the entry
							sendQueue.Dequeue();
							// copy the next entry into our working buffer
							Buffer.BlockCopy(nextEntry.buffer, 0, entry.buffer, entry.length, nextEntry.length);
							// update the entry length
							entry.length += nextEntry.length;

							// free the next entry buffer
							FreeBuffer(nextEntry.buffer);
						}
						else {
							// leave the loop, adding the next packet would go over the target size
							break;
						}
					}

					// increment the packet count and byte count
					packetCount++;

					// release the lock
					Monitor.Exit(sendQueue);
					try {
						// acquire a reader lock
						using (ReaderLock rl = new ReaderLock(listenerRWLock)) {
							// clear the remove list
							removeList.Clear();

							// iterate through the listeners and send to each of them
							foreach (IPEndPoint listener in listenerEndPoints) {
								try {
									sender.SendTo(entry.buffer, entry.length, SocketFlags.None, listener);
									byteCount += entry.length;
								}
								catch (Exception) {
									// for any client error, add to the remove list
									removeList.Add(listener);
								}
							}

							// if there are listeners to remove, acquire a writer lock and remove them
							// only wait for 20 milliseconds to get the writer lock to avoid blocking 
							using (WriterLock wl = rl.UpgradeToWriter(20)) {
								foreach (IPEndPoint ep in removeList) {
									listenerEndPoints.Remove(ep);
								}
							}

							doSendDataValues = listenerEndPoints.Count > 0;
						}
					}
					catch (Exception) {
						// ignore any exceptions
					}
					finally {
						// re-enter the send queue lock
						Monitor.Enter(sendQueue);
					}

					// free the buffer entry
					FreeBuffer(entry.buffer);
				}
			}
		}

		public void RegisterListener(IPEndPoint ep) {
			// acquire a write lock
			using (WriterLock wl = new WriterLock(listenerRWLock)) {
				// add the end point if it doesn't exists already
				if (!listenerEndPoints.Contains(ep)) {
					listenerEndPoints.Add(ep);
				}

				// indicate that we're now sending value
				doSendDataValues = true;
			}
		}

		public void RemoveListener(IPEndPoint ep) {
			// acquire a write lock
			using (WriterLock wl = new WriterLock(listenerRWLock)) {
				// add the end point if it doesn't exists already
				listenerEndPoints.Remove(ep);

				doSendDataValues = listenerEndPoints.Count > 0;
			}
		}

		public IPEndPoint[] GetListeners() {
			using (ReaderLock rl = new ReaderLock(listenerRWLock)) {
				// return a list of endpoints
				return listenerEndPoints.ToArray();
			}
		}

		public void ClearListeners() {
			using (WriterLock wl = new WriterLock(listenerRWLock)) {
				listenerEndPoints.Clear();
				doSendDataValues = false;
			}
		}

		public int QueueLength {
			get { return sendQueue.Count; }
		}

		public int PacketCount {
			get { return packetCount; }
		}

		public double PacketRate {
			get { return packetCount / (DateTime.Now - startTime).TotalSeconds; }
		}

		public long ByteCount {
			get { return byteCount; }
		}

		public double ByteRate {
			get { return byteCount / (DateTime.Now - startTime).TotalSeconds; }
		}

		public void ResetCounters() {
			packetCount = 0;
			byteCount = 0;
			startTime = DateTime.Now;
		}
	}
}
