
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using SmartCard.Runtime.Remoting.Channels.APDU;

namespace Cipher.OnCardApp
{
    /// <summary>
    /// Summary description for MyServer.
    /// </summary>
    public class Server
    {
        /// <summary>
        /// specify the exposed remote object URI.
        /// </summary>
        private const string REMOTE_OBJECT_URI = "gemalto_dotnet_cipher.uri";

        /// <summary>
        /// Register the server onto the card.
        /// </summary>
        /// <returns></returns>
        public static int Main()
        {
            // Register the channel the server will be listening to.
            ChannelServices.RegisterChannel(new APDUServerChannel());

            // Register this application as a server            
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(Service), REMOTE_OBJECT_URI, WellKnownObjectMode.Singleton);

            return 0;
        }
    }
}

