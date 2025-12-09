using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace netCard_c1
{
    public class ClientSessionSinkProvider : IClientChannelSinkProvider
    {
        private IClientChannelSinkProvider nextProvider;
        /// <summary>
        /// The symmetric key to encrypt the sink
        /// </summary>
        private byte[] sessionKey;
        private Hashtable properties;

        public IClientChannelSinkProvider Next { get => nextProvider; set => nextProvider = this; }

        public ClientSessionSinkProvider(Hashtable properties)
        {
            this.properties = properties;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Infrastructure = true)]
        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
        {
            // create other sinks in the chain
            IClientChannelSink next = nextProvider.CreateSink(channel, url, remoteChannelData);
            // put our sink on top of the chain and return it
            return new ClientSessionSink(next, sessionKey);
        }
    }
}
