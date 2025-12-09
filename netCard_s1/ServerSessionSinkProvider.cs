using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;

namespace netCard_s1
{
    public class ServerSessionSinkProvider : IServerChannelSinkProvider
    {
        private IServerChannelSinkProvider nextProvider;
        private byte[] sessionKey;
        private Hashtable properties;
        private object value;

        public ServerSessionSinkProvider(Hashtable properties, object value)
        {
            this.properties = properties;
            this.value = value;
        }

        public IServerChannelSinkProvider Next { get => nextProvider; set => nextProvider = value; }

        public IServerChannelSink CreateSink(IChannelReceiver channel)
        {
            // create other sinks in the chain
            IServerChannelSink next = nextProvider.CreateSink(channel);
            // put our sink on top of the chain and return it
            return new ServerSessionSink(next, sessionKey);
        }

        public void GetChannelData(IChannelDataStore channelData)
        {
            throw new NotImplementedException();
        }
    }
}
