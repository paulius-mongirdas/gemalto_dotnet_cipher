
using System;
using System.Runtime.Remoting.Channels;
using SmartCard.Runtime.Remoting.Channels.APDU;
// make sure you add the reference to your server stub dll or interface
// The stub file is automatically generated for you, under [Server Project Output]\Stub).
using System.IO;
using Cipher.OnCardApp;

namespace Cipher.ClientApp
{
    /// <summary>
    /// Client program to access the remote object on the card.
    /// </summary>
    public class Client1
    {
        private const string URL = "apdu://selfdiscover/gemalto_dotnet_cipher.uri";

        public static void Main()
        {
            // Sukurti ir užregistruoti komunikavimo kanalą
            APDUClientChannel channel = new APDUClientChannel();
            ChannelServices.RegisterChannel(channel);

            // gauti nuorodą į kortelėje veikianti objekta
            Service service = (Service)Activator.GetObject(typeof(Service), URL);

            InputLoop(service);

            // uždaryti komunikavimo kanalą
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
    }
}

