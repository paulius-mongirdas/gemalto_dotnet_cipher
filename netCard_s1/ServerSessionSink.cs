using Cipher.OnCardApp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace netCard_s1
{
    public class ServerSessionSink : IServerChannelSink
    {
        /// <summary>
        /// Next Sink in Chain
        /// </summary>
        private IServerChannelSink nextSink;
        /// <summary>
        /// The symmetric key to encrypt the sink
        /// </summary>
        private byte[] sessionKey;

        public ServerSessionSink(IServerChannelSink next, byte[] sessionKey)
        {
            nextSink = next;
            this.sessionKey = sessionKey;
        }

        public IDictionary Properties => new Hashtable();

        public IServerChannelSink NextChannelSink => nextSink;

        public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
        {
            throw new NotImplementedException();
        }

        public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg,
ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg,
out ITransportHeaders responseHeaders, out Stream responseStream)
        {
            //Logger.Log(Logger.LogLevel.Info, "Process Message Entry");
            // decrypt the inbound messsage
            requestStream = Service.ProcessInboundStream(requestStream, "Rijndael", sessionKey);
            // mark that we are on coming from sessionestablishersink
            Service.onEstablisherChannel = false;
            ServerProcessing srvProc = nextSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
            out responseMsg, out responseHeaders, out responseStream);
            // encrypt the outbound message
            responseStream = Service.ProcessOutboundStream(responseStream, "Rijndael", sessionKey);
            //Logger.Log(Logger.LogLevel.Info, "Process Message Exit");
            // returning status information
            return srvProc;
        }
    }
}
