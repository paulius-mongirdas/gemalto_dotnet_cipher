using Cipher.OnCardApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace netCard_c1
{
    class ClientSessionSink : BaseChannelSinkWithProperties, IClientChannelSink
    {
        /// <summary>
        /// Next Sink in Chain
        /// </summary>
        private IClientChannelSink nextSink;
        /// <summary>
        /// The session key to encrypt the communication
        /// </summary>
        private byte[] sessionKey;
        public ClientSessionSink(IClientChannelSink next, byte[] sessionKey)
        {
            nextSink = next;
            this.sessionKey = sessionKey;
        }
        [SecurityPermission(SecurityAction.LinkDemand, Infrastructure = true)]
        public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream,
out ITransportHeaders responseHeaders, out Stream responseStream)
        {
            requestStream = ProcessOutboundStream(requestStream, "Rijndael", sessionKey);
            // forward the call to the next sink
            nextSink.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
            responseStream = ProcessInboundStream(responseStream, "Rijndael", sessionKey);
        }
        public IClientChannelSink NextChannelSink => nextSink;

        public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
        {
            throw new NotImplementedException();
        }
        public static Stream ProcessInboundStream(Stream requestStream, string v, byte[] sessionKey)
        {
            const int bufferSize = 1024 * 16;
            Stream outFs = new MemoryStream(bufferSize);

            using (Rijndael rij = Rijndael.Create())
            {
                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = sessionKey;
                rij.IV = iv;

                ICryptoTransform decryptor = rij.CreateDecryptor();

                // If you wrote IV into the file, skip it.
                // If you do NOT store IV in encrypted file, comment this block out.

                int blockSizeBytes = rij.BlockSize / 8;
                requestStream.Seek(blockSizeBytes, SeekOrigin.Begin);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = requestStream.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (requestStream.Position == requestStream.Length);

                    if (!isLast)
                    {
                        int transformed = decryptor.TransformBlock(inBuffer, 0, read, outBuffer, 0);
                        outFs.Write(outBuffer, 0, transformed);
                    }
                    else
                    {
                        byte[] finalBytes = decryptor.TransformFinalBlock(inBuffer, 0, read);
                        outFs.Write(finalBytes, 0, finalBytes.Length);
                        Array.Clear(finalBytes, 0, finalBytes.Length);
                        break;
                    }
                }
            }
            return outFs;
        }
        const int bufferSize = 1024 * 16;
        static byte[] iv = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        public static Stream ProcessOutboundStream(Stream responseStream, string v, byte[] sessionKey)
        {
            const int bufferSize = 1024 * 16;
            Stream outFs = new MemoryStream(bufferSize);

            using (Rijndael rij = Rijndael.Create())
            {
                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = sessionKey;
                rij.IV = iv;

                ICryptoTransform encryptor = rij.CreateEncryptor();

                // OPTIONAL: store IV at start (still allowed even if key file has IV)
                //outFs.Write(iv, 0, iv.Length);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = responseStream.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (responseStream.Position == responseStream.Length);

                    if (!isLast)
                    {
                        int transformed = encryptor.TransformBlock(inBuffer, 0, read, outBuffer, 0);
                        outFs.Write(outBuffer, 0, transformed);
                    }
                    else
                    {
                        byte[] finalBytes = encryptor.TransformFinalBlock(inBuffer, 0, read);
                        outFs.Write(finalBytes, 0, finalBytes.Length);
                        Array.Clear(finalBytes, 0, finalBytes.Length);
                        break;
                    }
                }
            }
            return outFs;
        }
    }
    public class SessionSinkProvider : IClientChannelSinkProvider
    {
        /// Next Sink in Chain
        /// </summary>
        private IClientChannelSinkProvider nextProvider;
        /// <summary>
        /// The symmetric key to encrypt the sink
        /// </summary>
        private byte[] sessionKey;

        public IClientChannelSinkProvider Next { get => nextProvider; set => nextProvider = value; }

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
