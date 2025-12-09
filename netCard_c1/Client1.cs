
using System;
using System.Runtime.Remoting.Channels;
using SmartCard.Runtime.Remoting.Channels.APDU;
// make sure you add the reference to your server stub dll or interface
// The stub file is automatically generated for you, under [Server Project Output]\Stub).
using System.IO;
using Cipher.OnCardApp;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using netCard_c1;
using System.Collections;
using static System.Collections.Specialized.BitVector32;

namespace Cipher.ClientApp
{
    /// <summary>
    /// Client program to access the remote object on the card.
    /// </summary>
    public class Client1
    {
        private const string URL = "apdu://selfdiscover/gemalto_dotnet_cipher.uri";
        private const string URL2 = "apdu://selfdiscover/gemalto_dotnet_cipher_2.uri";

        public static void Main()
        {
            // 1. Sukurti ir užregistruoti komunikavimo kanalą
            APDUClientChannel channel = new APDUClientChannel();
            ChannelServices.RegisterChannel(channel);

            // gauti nuorodą į kortelėje veikianti objekta
            Service service = (Service)Activator.GetObject(typeof(Service), URL);

            //InputLoop(service);
            EstablishSecureChannel(service);

            ChannelServices.UnregisterChannel(channel);
        }
        public static void InputLoop(Service service)
        {
            int input = -1;
            while (input != 0)
            {
                Console.WriteLine("--Please type in a command--\n" +
                    "[1]: View directories and files\n" +
                    "[2]: Create a directory\n" +
                    "[3]: Delete file/directory\n" +
                    "[4]: Copy a file from client to server\n" +
                    "[5]: Read file contents\n" +
                    "[6]: View system usage data\n" +
                    "[7]: View running services\n" +
                    "[8]: Generate crypto key and cipher/decipher a file\n" +
                    "[0]: Exit");

                int.TryParse(Console.ReadLine(), out input);
                Console.Clear();
                switch (input)
                {
                    case 1:
                        Console.WriteLine("Please specify path (eg. C:)");
                        string dirPath = @"" + Console.ReadLine();
                        string[] dirList;
                        string[] files;
                        try
                        {
                            dirList = service.GetDirs(dirPath);
                            files = service.GetFiles(dirPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to get directories and files.\n");
                            break;
                        }

                        Console.WriteLine("--Directories--");
                        foreach (string dir in dirList)
                        {
                            Console.WriteLine(dir);
                        }
                        if (dirList.Length == 0)
                            Console.WriteLine("No directories in specified path.");

                        Console.WriteLine("\n--Files--");
                        foreach (string file in files)
                        {
                            Console.WriteLine(file);
                        }
                        if (files.Length == 0)
                            Console.WriteLine("No files in specified path.");
                        Console.WriteLine();
                        break;
                    case 2: // create directory (eg. "C:\Temp")
                        Console.WriteLine("Please specify directory name with full path:");
                        string path = @"" + Console.ReadLine();
                        try
                        {
                            int ret = service.CreateDir(path);
                            if (ret == 0)
                                Console.WriteLine("Directory created successfully.\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to create directory.\n");
                            //Console.WriteLine(ex.ToString());
                        }
                        break;
                    case 4: // copy file (eg. output: C:\Temp\test.txt input: C:\Users\Paulius\Desktop\text.txt
                        Console.WriteLine("Please type in input file:");
                        string inputPath = Console.ReadLine();
                        Console.WriteLine("Please type in output file:");
                        string outputPath = Console.ReadLine();
                        try
                        {
                            int ret = service.PutFile(outputPath, File.ReadAllBytes(inputPath));
                            if (ret == 0)
                                Console.WriteLine("File copied successfully.\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to copy file.\n");
                            //Console.WriteLine(ex.ToString());
                        }
                        break;
                    case 6: // memory usage
                        Console.WriteLine($"Free persistent memory: {service.GetMemoryUsage()} bytes\n");
                        break;
                    case 7: // services
                        string[] serviceList = service.GetServices();
                        foreach (string s in serviceList)
                        {
                            Console.WriteLine(s);
                        }
                        Console.WriteLine();
                        break;
                    case 3: // delete file/dir
                        Console.WriteLine("Please type in file path:");
                        string deletePath = @"" + Console.ReadLine();
                        try
                        {
                            int ret = service.DeleteFile(deletePath);
                            if (ret == 0)
                                Console.WriteLine("File/Directory deleted successfully.\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to delete file/directory.\n");
                            //Console.WriteLine(ex.ToString());
                        }
                        break;
                    case 5: // read file contents
                        Console.WriteLine("Please type in file path:");
                        string readPath = @"" + Console.ReadLine();
                        try
                        {
                            string arr = service.ReadFile(readPath);
                            Console.WriteLine(arr);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to read file.\n");
                            //Console.WriteLine(ex.ToString());
                        }
                        break;
                    case 8:
                        Console.WriteLine("Generating keys..");
                        service.GenerateAndSaveKeyIv("C:/Texts/key.txt", 256);
                        Console.WriteLine("Encrypting file..");
                        service.EncryptFileStreamed("C:/Texts/text.txt", "C:/Texts/text.txt.enc", "C:/Texts/key.txt");
                        Console.WriteLine("Decrypting file..");
                        service.DecryptFileStreamed("C:/Texts/text.txt.enc", "C:/Texts/text_decrypted.txt", "C:/Texts/key.txt");
                        Console.WriteLine("Done");
                        break;
                }
            }
        }
        public static void EstablishSecureChannel(Service service)
        {
            service.GenerateRsa();
            // 2. Get the public key from the card (Which is the RSA Modulus and the Exponent)
            byte[] cardPKmod = service.GetPublicKey2();
            byte[] cardPKexp = service.GetExponent();

            Console.WriteLine($"IsOnEstablisherChannel: {service.IsOnEstablisherChannel()}");
            Console.WriteLine("Getting public key from card...");

            // 3. Generate a 128 bit session key (can be generated from the card as well using the same service).
            byte[] sessionKey = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(sessionKey);

            Console.WriteLine("Generating session key...");

            // Put the public key from the card into an RSACryptoServiceProvider
            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
            RSAParameters rsaParam = new RSAParameters();
            rsaParam.Modulus = cardPKmod;
            rsaParam.Exponent = cardPKexp;
            rsaProvider.ImportParameters(rsaParam);
            // This is the pin that we share with the card
            byte[] pin = Encoding.UTF8.GetBytes("0000");

            // 4. Encrypt the pin and session key using the public key of the card
            byte[] encryptedPin = rsaProvider.Encrypt(pin, false);
            byte[] encryptedSessionKey = rsaProvider.Encrypt(sessionKey, false);

            Console.WriteLine($"Encrypted PIN: {Convert.ToBase64String(encryptedPin)}");

            Console.WriteLine("Encrypting PIN and session key...");

            // 5. Now call the EstablishSecureChannel method of the card using the encrypted PIN and session key. The
            // card will set up an encrypted channel using the provided session key.
            try
            {
                Console.WriteLine("Establishing secure channel...");
                service.EstablishSecureChannel(7655, encryptedPin, encryptedSessionKey);
            }
            catch (Exception ex)
            {
                throw new AuthenticationException(ex.Message);
            }

            Console.WriteLine("Secure channel established.");
            // 6. Set up a Sink Provider with a SessionSink attached to it using the sessionKey as a parameter for
            // creating the SessionSink.
            Hashtable properties = new Hashtable();
            properties["key"] = sessionKey;
            IClientChannelSinkProvider provider = new APDUClientFormatterSinkProvider();
            //provider.Next = new ServerSessionSinkProvider(properties);
            // Create and register a new channel using the sink provider that we've just created.
            string channelName = "SecureChannel_" + DateTime.Now.Ticks;
            // 7.
            APDUClientChannel channel = new APDUClientChannel(channelName, provider);
            ChannelServices.RegisterChannel(channel);

            Service secure_service = (Service)Activator.GetObject(typeof(Service), URL2);
            //EstablishSecureChannel(secure_service);

            Console.WriteLine("Communicating over secure channel now.\n");
            // All communication from here onwards are on the encrypted channel
            Console.WriteLine($"IsOnEstablisherChannel: {secure_service.IsOnEstablisherChannel()}");
            InputLoop(secure_service);

            // Close encrypted channel
            ChannelServices.UnregisterChannel(channel);
        }
    }
}

