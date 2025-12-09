using SmartCard;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Remoting.Channels;
using SmartCard.Runtime.Remoting.Channels.APDU;
using System.Collections;
using netCard_s1;
using System.Runtime.Remoting;

namespace Cipher.OnCardApp
{
    /// <summary>
    /// Summary description for MyService.
    /// </summary>
    public class Service : MarshalByRefObject
    {
        ContentManager cm = (ContentManager)Activator.GetObject(typeof(ContentManager),
        "ContentManager");
        private int _myPIN = 0000;
        public static bool _onEstablisherChannel = true;
        private RSAParameters rsaParams;
        public bool IsOnEstablisherChannel()
        {
            return _onEstablisherChannel;
        }

        public void GenerateRsa()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            rsaParams = rsa.ExportParameters(true);
        }
        public int GenerateAndSaveKeyIv(string keyFile, int keySizeBits)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            // Validate key size (128,192,256)
            if (keySizeBits != 128 && keySizeBits != 192 && keySizeBits != 256)
                throw new ArgumentException("Key size must be 128, 192 or 256.");

            using (Rijndael rij = Rijndael.Create())
            {
                rij.KeySize = keySizeBits;
                rij.BlockSize = 128; // AES block size (Rijndael variant compatible with AES)
                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;

                // Generate random key and IV
                rij.GenerateKey();
                rij.GenerateIV();

                byte[] key = (byte[])rij.Key.Clone();
                byte[] iv = (byte[])rij.IV.Clone();

                // Save to file - Base64 (DO NOT store this unprotected in production)
                string keyBase64 = Convert.ToBase64String(key);
                string ivBase64 = Convert.ToBase64String(iv);

                // Simple storage: one line with key, next line with IV.
                // In production, protect with DPAPI / certificate / secure store.
                using (StreamWriter sw = new StreamWriter(keyFile, false, Encoding.UTF8))
                {
                    sw.WriteLine(keyBase64);
                    sw.WriteLine(ivBase64);
                }
            }
            return 0;
        }
        public int LoadKeyIvFromFile(string keyFile, out byte[] key, out byte[] iv)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            string[] lines = ReadKeyFile(keyFile);
            if (lines.Length < 2)
                throw new InvalidOperationException("Key file must contain key and iv in Base64, one per line.");

            key = Convert.FromBase64String(lines[0].Trim());
            iv = Convert.FromBase64String(lines[1].Trim());

            return 0;
        }

        public int EncryptFileStreamed(string inputFile, string outputFile, string keyFile)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            byte[] key, iv;
            LoadKeyIvFromFile(keyFile, out key, out iv);

            const int bufferSize = 1024 * 16;

            using (FileStream inFs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Rijndael rij = Rijndael.Create())
            {
                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = key;
                rij.IV = iv;

                ICryptoTransform encryptor = rij.CreateEncryptor();

                // OPTIONAL: store IV at start (still allowed even if key file has IV)
                outFs.Write(iv, 0, iv.Length);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = inFs.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (inFs.Position == inFs.Length);

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
            return 0;
        }

        public int DecryptFileStreamed(string inputFile, string outputFile, string keyFile)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            byte[] key, iv;
            LoadKeyIvFromFile(keyFile, out key, out iv);

            const int bufferSize = 1024 * 16;

            using (FileStream inFs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Rijndael rij = Rijndael.Create())
            {
                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = key;
                rij.IV = iv;

                ICryptoTransform decryptor = rij.CreateDecryptor();

                // If you wrote IV into the file, skip it.
                // If you do NOT store IV in encrypted file, comment this block out.

                int blockSizeBytes = rij.BlockSize / 8;
                inFs.Seek(blockSizeBytes, SeekOrigin.Begin);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = inFs.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (inFs.Position == inFs.Length);

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
            return 0;
        }
        public byte[] GetPublicKey2()
        {
            // TEST
            return rsaParams.Modulus;

        }
        /*public static string GetPublicKey()
        {
            // TEST
            return puk;

        }*/
        public byte[] GetExponent()
        {
            // TEST
            return rsaParams.Exponent;
        }
        public void EstablishSecureChannel(int port, byte[] encryptedPin, byte[] encryptedSessionKey)
        {
            if (!_onEstablisherChannel) throw new UnauthorizedAccessException("This method is to be called with channel having SessionEstablisherSink");

            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
            rsaProvider.ImportParameters(rsaParams);

            // Decrypt the pin and sessionKey first
            byte[] pin = rsaProvider.Decrypt(encryptedPin, false);
            byte[] sessionKey = rsaProvider.Decrypt(encryptedSessionKey, false);
            // Verify the PIN
            //_myPIN.Verify(new String(cpin)); CHANGE THIS
            // Create and register the channel at the specified port and using SessionSink,
            // set up the SecureSink properties.
            Hashtable properties = new Hashtable();
            properties["key"] = sessionKey;
            IServerChannelSinkProvider newProvider = new ServerSessionSinkProvider(properties, null);
            newProvider.Next = new APDUServerFormatterSinkProvider();
            APDUServerChannel channel = new APDUServerChannel(newProvider, port);
            ChannelServices.RegisterChannel(channel);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(Service), "gemalto_dotnet_cipher_2.uri", WellKnownObjectMode.Singleton);
        }

        // c1 ---------------------------
        public string[] GetDirs(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            return cm.GetDirectories(path);
        }
        public string[] GetFiles(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            return cm.GetFiles(path);
        }
        public int CreateDir(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            cm.CreateDirectory(path);
            return 0;
        }
        public string ReadFile(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            string readContents;
            using (StreamReader streamReader = new StreamReader(path))
            {
                readContents = streamReader.ReadToEnd();
            }
            return readContents;
        }
        private string[] ReadKeyFile(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            string[] readContents = new string[2];
            using (StreamReader streamReader = new StreamReader(path))
            {
                readContents[0] = streamReader.ReadLine();
                readContents[1] = streamReader.ReadLine();
            }
            return readContents;
        }
        public int PutFile(string path, byte[] file)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            cm.LoadFile(path, file);
            return 0;
        }
        public int DeleteFile(string path)
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            cm.Delete(path);
            return 0;
        }
        public int GetMemoryUsage()
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            return cm.FreePersistentMemory;
        }
        public string[] GetServices()
        {
            if (_onEstablisherChannel)
            {
                throw new UnauthorizedAccessException("Unauthorized: Cannot use this method without ServerSessionSink");
            }
            return cm.GetServices(false);
        }
        const int bufferSize = 1024 * 16;
        static byte[] iv = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
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
}

