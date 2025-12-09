using SmartCard;
using SmartCard.Services;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Cipher.OnCardApp
{
    /// <summary>
    /// Summary description for MyService.
    /// </summary>
    public class Service : MarshalByRefObject
    {
        ContentManager cm = (ContentManager)Activator.GetObject(typeof(ContentManager),
        "ContentManager");
        int balance = 0;
        int pinCode = 0000;
        bool locked = false;
        [OutOfTransaction]
        int attempts = 0;
        int maxAttempts = 5;

        private Rijndael currentRijndael;
        private ICryptoTransform currentEncryptor;
        private MemoryStream encryptedStream;
        private ICryptoTransform currentDecryptor;
        private MemoryStream decryptedStream;

        public int GenerateAndSaveKeyIv(string keyFile, int keySizeBits)
        {
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

        public int GenerateAndSaveKeyIvByFileName(string keyFileName, int keySizeBits)
        {
            string keyFile = "C:/Keys/" + keyFileName;

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
            string[] lines = ReadKeyFile(keyFile);
            if (lines.Length < 2)
                throw new InvalidOperationException("Key file must contain key and iv in Base64, one per line.");

            key = Convert.FromBase64String(lines[0].Trim());
            iv = Convert.FromBase64String(lines[1].Trim());

            return 0;
        }

        public int LoadKeyIvFromFileByName(string keyFileName, out byte[] key, out byte[] iv)
        {
            string keyFile = "C:/Keys/" + keyFileName;

            string[] lines = ReadKeyFile(keyFile);
            if (lines.Length < 2)
                throw new InvalidOperationException("Key file must contain key and iv in Base64, one per line.");

            key = Convert.FromBase64String(lines[0].Trim());
            iv = Convert.FromBase64String(lines[1].Trim());

            return 0;
        }

        public int EncryptFileStreamed(string inputFile, string outputFile, string keyFile)
        {
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

        public byte[] EncryptFileStreamedByKeyName(byte[] inputFileData, string keyFileName)
        {

            byte[] key, iv;
            LoadKeyIvFromFileByName(keyFileName, out key, out iv);

            const int bufferSize = 1024 * 16;

            using (MemoryStream inMs = new MemoryStream(inputFileData.Length))
            using (MemoryStream outMs = new MemoryStream(0))
            using (Rijndael rij = Rijndael.Create())
            {
                inMs.Write(inputFileData, 0, inputFileData.Length);
                inMs.Position = 0;

                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = key;
                rij.IV = iv;

                ICryptoTransform encryptor = rij.CreateEncryptor();

                // OPTIONAL: store IV at start (still allowed even if key file has IV)
                int blockSizeBytes = rij.BlockSize / 8;
                inMs.Seek(blockSizeBytes, SeekOrigin.Begin);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = inMs.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (inMs.Position == inMs.Length);

                    if (!isLast)
                    {
                        int transformed = encryptor.TransformBlock(inBuffer, 0, read, outBuffer, 0);
                        outMs.Write(outBuffer, 0, transformed);
                    }
                    else
                    {
                        byte[] finalBytes = encryptor.TransformFinalBlock(inBuffer, 0, read);
                        outMs.Write(finalBytes, 0, finalBytes.Length);
                        Array.Clear(finalBytes, 0, finalBytes.Length);
                        break;
                    }
                }
                return outMs.ToArray();
            }
        }

        public void StartEncryption(string keyFileName)
        {
            byte[] key, iv;
            LoadKeyIvFromFileByName(keyFileName, out key, out iv);

            // Clear old session if exists
            if (currentEncryptor != null)
            {
                try { currentEncryptor.Dispose(); } catch { }
            }
            if (currentRijndael != null)
            {
                try
                {
                    if (currentRijndael.Key != null)
                        Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                    if (currentRijndael.IV != null)
                        Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);
                }
                catch { }
            }
            if (encryptedStream != null)
            {
                try { encryptedStream.Close(); } catch { }
            }

            currentRijndael = Rijndael.Create();
            currentRijndael.Mode = CipherMode.CBC;
            currentRijndael.Padding = PaddingMode.PKCS7;
            currentRijndael.Key = key;
            currentRijndael.IV = iv;
            currentEncryptor = currentRijndael.CreateEncryptor();
            encryptedStream = new MemoryStream(0);
        }

        public byte[] ProcessEncryptionChunk(byte[] chunk, bool isFinal)
        {
            if (currentEncryptor == null)
                throw new InvalidOperationException("Encryption not started. Call StartEncryption first.");

            if (isFinal)
            {
                // Final chunk
                byte[] finalBytes = currentEncryptor.TransformFinalBlock(chunk, 0, chunk.Length);
                encryptedStream.Write(finalBytes, 0, finalBytes.Length);

                // Get result
                byte[] result = encryptedStream.ToArray();

                // Cleanup
                currentEncryptor.Dispose();
                encryptedStream.Close();

                // Clear sensitive data
                if (currentRijndael.Key != null)
                    Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                if (currentRijndael.IV != null)
                    Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);

                currentEncryptor = null;
                currentRijndael = null;
                encryptedStream = null;

                return result;
            }
            else
            {
                // Regular chunk
                byte[] outBuffer = new byte[chunk.Length + currentRijndael.BlockSize / 8];
                int transformed = currentEncryptor.TransformBlock(chunk, 0, chunk.Length, outBuffer, 0);
                encryptedStream.Write(outBuffer, 0, transformed);

                // Return empty array for non-final chunks (or you could return progress info)
                return new byte[0];
            }
        }

        public void CancelEncryption()
        {
            try
            {
                currentEncryptor?.Dispose();
                encryptedStream?.Close();

                if (currentRijndael != null)
                {
                    if (currentRijndael.Key != null)
                        Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                    if (currentRijndael.IV != null)
                        Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);
                }
            }
            finally
            {
                currentEncryptor = null;
                currentRijndael = null;
                encryptedStream = null;
            }
        }

        public int DecryptFileStreamed(string inputFile, string outputFile, string keyFile)
        {
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

        public byte[] DecryptFileStreamedByKeyName(byte[] inputFileData, string keyFileName)
        {
            byte[] key, iv;
            LoadKeyIvFromFileByName(keyFileName, out key, out iv);

            const int bufferSize = 1024 * 16;

            using (MemoryStream inMs = new MemoryStream(inputFileData.Length))
            using (MemoryStream outMs = new MemoryStream(0))
            using (Rijndael rij = Rijndael.Create())
            {
                inMs.Write(inputFileData, 0, inputFileData.Length);
                inMs.Position = 0;

                rij.Mode = CipherMode.CBC;
                rij.Padding = PaddingMode.PKCS7;
                rij.Key = key;
                rij.IV = iv;

                ICryptoTransform decryptor = rij.CreateDecryptor();

                // If you wrote IV into the file, skip it.
                // If you do NOT store IV in encrypted file, comment this block out.

                int blockSizeBytes = rij.BlockSize / 8;
                inMs.Seek(blockSizeBytes, SeekOrigin.Begin);

                byte[] inBuffer = new byte[bufferSize];
                byte[] outBuffer = new byte[bufferSize + rij.BlockSize / 8];

                while (true)
                {
                    int read = inMs.Read(inBuffer, 0, inBuffer.Length);
                    if (read <= 0) break;

                    bool isLast = (inMs.Position == inMs.Length);

                    if (!isLast)
                    {
                        int transformed = decryptor.TransformBlock(inBuffer, 0, read, outBuffer, 0);
                        outMs.Write(outBuffer, 0, transformed);
                    }
                    else
                    {
                        byte[] finalBytes = decryptor.TransformFinalBlock(inBuffer, 0, read);
                        outMs.Write(finalBytes, 0, finalBytes.Length);
                        Array.Clear(finalBytes, 0, finalBytes.Length);
                        break;
                    }
                }

                return outMs.ToArray();
            }
        }

        public void StartDecryption(string keyFileName)
        {
            byte[] key, iv;
            LoadKeyIvFromFileByName(keyFileName, out key, out iv);

            // Clear old session if exists
            if (currentDecryptor != null)
            {
                try { currentDecryptor.Dispose(); } catch { }
            }
            if (currentRijndael != null)
            {
                try
                {
                    if (currentRijndael.Key != null)
                        Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                    if (currentRijndael.IV != null)
                        Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);
                }
                catch { }
            }
            if (decryptedStream != null)
            {
                try { decryptedStream.Close(); } catch { }
            }

            currentRijndael = Rijndael.Create();
            currentRijndael.Mode = CipherMode.CBC;
            currentRijndael.Padding = PaddingMode.PKCS7;
            currentRijndael.Key = key;
            currentRijndael.IV = iv;
            currentDecryptor = currentRijndael.CreateDecryptor();
            decryptedStream = new MemoryStream(0);
        }

        public byte[] ProcessDecryptionChunk(byte[] chunk, bool isFinal)
        {
            if (currentDecryptor == null)
                throw new InvalidOperationException("Decryption not started. Call StartDecryption first.");

            if (isFinal)
            {
                // Final chunk
                byte[] finalBytes = currentDecryptor.TransformFinalBlock(chunk, 0, chunk.Length);
                decryptedStream.Write(finalBytes, 0, finalBytes.Length);

                // Get result
                byte[] result = decryptedStream.ToArray();

                // Cleanup
                currentDecryptor.Dispose();
                decryptedStream.Close();

                // Clear sensitive data
                if (currentRijndael.Key != null)
                    Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                if (currentRijndael.IV != null)
                    Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);

                currentDecryptor = null;
                currentRijndael = null;
                decryptedStream = null;

                return result;
            }
            else
            {
                // Regular chunk
                byte[] outBuffer = new byte[chunk.Length + currentRijndael.BlockSize / 8];
                int transformed = currentDecryptor.TransformBlock(chunk, 0, chunk.Length, outBuffer, 0);
                decryptedStream.Write(outBuffer, 0, transformed);

                // Return empty array for non-final chunks
                return new byte[0];
            }
        }

        public void CancelDecryption()
        {
            try
            {
                currentDecryptor?.Dispose();
                decryptedStream?.Close();

                if (currentRijndael != null)
                {
                    if (currentRijndael.Key != null)
                        Array.Clear(currentRijndael.Key, 0, currentRijndael.Key.Length);
                    if (currentRijndael.IV != null)
                        Array.Clear(currentRijndael.IV, 0, currentRijndael.IV.Length);
                }
            }
            finally
            {
                currentDecryptor = null;
                currentRijndael = null;
                decryptedStream = null;
            }
        }

        // c1 ---------------------------
        public string[] GetDirs(string path)
        {
            return cm.GetDirectories(path);
        }

        public string[] GetKeys()
        {
            return cm.GetFiles("C:\\Keys");
        }

        public string[] GetFiles(string path)
        {
            return cm.GetFiles(path);
        }
        public int CreateDir(string path)
        {
            cm.CreateDirectory(path);
            return 0;
        }
        public string ReadFile(string path)
        {
            string readContents;
            using (StreamReader streamReader = new StreamReader(path))
            {
                readContents = streamReader.ReadToEnd();
            }
            return readContents;
        }
        private string[] ReadKeyFile(string path)
        {
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
            cm.LoadFile(path, file);
            return 0;
        }
        public int DeleteFile(string path)
        {
            cm.Delete(path);
            return 0;
        }
        public int GetMemoryUsage()
        {
            return cm.FreePersistentMemory;
        }
        public string[] GetServices()
        {
            return cm.GetServices(false);
        }
        // c2 ---------------------------
        [Transaction]
        public int VerifyPIN(int pin)
        {
            if (pin != pinCode)
            {
                attempts++;
                if (attempts >= maxAttempts) locked = true;
                return -1;
            }
            if (locked) return -2;
            return 0;
        }
        [Transaction]
        public int ChangePIN(int oldPin, int newPin)
        {
            if (oldPin != pinCode) return -1;
            pinCode = newPin;
            return 0;
        }
        public int ViewBalance()
        {
            return balance;
        }
        [Transaction]
        public int AddToWallet(int amount)
        {
            if (amount < 0) return -1;
            balance += amount;
            return 0;
        }
        [Transaction]
        public int SubtractFromWallet(int amount)
        {
            if (amount < 0 || amount > balance) return -1;
            balance -= amount;
            return 0;
        }
        public int GetAttempts()
        {
            return maxAttempts - attempts;
        }
    }
}

