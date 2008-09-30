using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;

namespace CompressionChannel {
	public class CompressedClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink {
		private class RequestState {
			public Stream origStream;
			public long origPos;
			public bool didCompress;
			public IMessage msg;
			public ITransportHeaders headers;

			public RequestState(Stream origStream, long origPosition, bool didCompress, IMessage msg, ITransportHeaders headers) {
				this.origStream = origStream;
				this.origPos = origPosition;
				this.didCompress = didCompress;
				this.msg = msg;
				this.headers = headers;
			}
		};

		private IClientChannelSink _next;

		private bool serverCanCompress = true;
		private bool checkedCompress = false;
		private bool assumeCompress = true;

		public CompressedClientChannelSink(IClientChannelSink next) {
			_next = next;
			//Debug.WriteLine("Creating new client sink");
		}

		#region IClientChannelSink Members

		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream) {
			Stream compStream;

			Stream origStream = stream;
			long origPos = stream.Position;

			if ((checkedCompress && serverCanCompress) || (!checkedCompress && assumeCompress)) {
				headers[CompressionHelper.CompressKey] = CompressionHelper.CompressedFlag;
				compStream = CompressionHelper.CompressStream(stream);
				sinkStack.Push(this, new RequestState(origStream, origPos, true, msg, headers));
			}
			else {
				headers[CompressionHelper.CompressKey] = CompressionHelper.CompressRequest;
				compStream = stream;
				sinkStack.Push(this, null);
			}

			try {
				_next.AsyncProcessRequest(sinkStack, msg, headers, compStream);
			}
			catch (Exception) {
			}
		}

		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream) {
			RequestState rs = state as RequestState;
			if (rs != null && rs.didCompress) {
				bool compressOK = object.Equals(headers[CompressionHelper.CompressKey], CompressionHelper.CompressedFlag);
				if (!compressOK) {
					serverCanCompress = false;
					checkedCompress = true;

					// synchronously send the message now
					rs.origStream.Position = rs.origPos;
					ProcessMessage(rs.msg, rs.headers, rs.origStream, out headers, out stream);
				}
				else {
					// decompress stuff
					stream = CompressionHelper.DecompressStream(stream);

					serverCanCompress = true;
					checkedCompress = true;
				}
				rs.origStream.Close();
			}

			// process through the rest of the stacks
			sinkStack.AsyncProcessResponse(headers, stream);
		}

		public Stream GetRequestStream(IMessage msg, ITransportHeaders headers) {
			return null;
		}

		public IClientChannelSink NextChannelSink {
			get { return _next; }
		}

		public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream) {
			Stream origStream = requestStream;
			long origPos = requestStream.Position;
			
			bool didCompress = true;
			if ((checkedCompress && serverCanCompress) || (!checkedCompress && assumeCompress)) {
				requestHeaders[CompressionHelper.CompressKey] = CompressionHelper.CompressedFlag;
				requestStream = CompressionHelper.CompressStream(requestStream);
			}
			else {
				didCompress = false;
				requestHeaders[CompressionHelper.CompressKey] = CompressionHelper.CompressRequest;
			}

			responseStream = null;
			responseHeaders = null;

			// Send the compressed request to the server
			try {
				_next.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);

				serverCanCompress = object.Equals(responseHeaders[CompressionHelper.CompressKey], CompressionHelper.CompressedFlag);
				checkedCompress = true;
				if (!serverCanCompress && didCompress) {
					origStream.Position = origPos;
					_next.ProcessMessage(msg, requestHeaders, origStream, out responseHeaders, out responseStream);
				}
			}
			catch (Exception) {
				serverCanCompress = false;
				throw;
			}

			if (serverCanCompress) {
				//Debug.WriteLine("Server can compress");

				Stream decompressedStream = CompressionHelper.DecompressStream(responseStream);
				// close that ish as nobody wants it any more
				responseStream.Close();
				responseStream = decompressedStream;
			}
		}

		#endregion
	}
}
