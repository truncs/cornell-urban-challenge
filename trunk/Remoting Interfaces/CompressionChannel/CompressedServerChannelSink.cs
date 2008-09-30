using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Diagnostics;

namespace CompressionChannel {
	public class CompressedServerChannelSink : BaseChannelSinkWithProperties, IServerChannelSink {
		private class ClientState {
			public bool clientCompressed;

			public ClientState(bool comp) {
				this.clientCompressed = comp;
			}
		}

		private IServerChannelSink _next;

		public CompressedServerChannelSink(IServerChannelSink next) {
			_next = next;
		}

		#region IServerChannelSink Members

		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream) {
			ClientState cs = state as ClientState;
			if (cs != null && cs.clientCompressed) {
				// compress the shits
				stream = CompressionHelper.CompressStream(stream);
				headers[CompressionHelper.CompressKey] = CompressionHelper.CompressedFlag;
			}

			sinkStack.AsyncProcessResponse(msg, headers, stream);
		}

		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers) {
			return null;
		}

		public IServerChannelSink NextChannelSink {
			get { return _next; }
		}

		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream) {
			bool clientCompressed = false;

			// decompress the shits
			Stream decompressedStream;
			if (object.Equals(requestHeaders[CompressionHelper.CompressKey], CompressionHelper.CompressedFlag)) {
				//Debug.WriteLine("client compressed");
				clientCompressed = true;
				decompressedStream = CompressionHelper.DecompressStream(requestStream);
				// close the request stream
				requestStream.Close();
			}
			else {
				if (object.Equals(requestHeaders[CompressionHelper.CompressKey], CompressionHelper.CompressRequest)) {
					//Debug.WriteLine("client requesting compress");
					clientCompressed = true;
				}

				decompressedStream = requestStream;
			}

			sinkStack.Push(this, new ClientState(clientCompressed));

			// send the decompressed message on through the sink chain
			ServerProcessing processingResult = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, decompressedStream, out responseMsg, out responseHeaders, out responseStream);

			// get the compressed stream
			if (clientCompressed && processingResult == ServerProcessing.Complete) {
				Stream compressedStream = CompressionHelper.CompressStream(responseStream);
				responseStream.Close();
				responseStream = compressedStream;
				responseHeaders[CompressionHelper.CompressKey] = CompressionHelper.CompressedFlag;
			}

			// Take us off the stack and return the result.
			if (processingResult == ServerProcessing.Async) {
				sinkStack.Store(this, new ClientState(clientCompressed));
			}
			else {
				sinkStack.Pop(this);
			}
			return processingResult;
		}

		#endregion
	}
}
