using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PLCRegistersParsing.Publisher.Services
{
    public static class EncryptionService
    {
        public static byte[] Encrypt(string toEncrypt, string key)
        {
            byte[] Key = StringToByteArray(key);
            byte[] cipherText = Encoding.UTF8.GetBytes(toEncrypt);
            cipherText = AddControlBlock(cipherText);
            PadToMultipleOf(ref cipherText, 16);

            byte[] resultArray;

            //cipherText[8] = 67;
            //cipherText[9] = 77;
            //cipherText[10] = 68;
            //cipherText[11] = 61;
            //cipherText[12] = 20;
            //cipherText[15] = 80;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Padding = PaddingMode.Zeros;
                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, null);

                // Create the streams used for decryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {

                        resultArray = encryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    }
                }
            }

            return resultArray;

        }

        public static void PadToMultipleOf(ref byte[] src, int pad)
        {
            int len = (src.Length + pad - 1) / pad * pad;
            Array.Resize(ref src, len);
        }

        // Adds control blocks to the package
        private static byte[] AddControlBlock(byte[] nonEncryptedBytes)
        {
            string nonEncryptedHex = Convert.ToHexString(nonEncryptedBytes);
            string finalString = "";
            bool finish = false;

            // Iterates through the 15 sequence numbers (0 to F)
            for (int i = 0; i <= 15; i++)
            {
                int validPairsQuantity = 15; 

                if (nonEncryptedHex.Length < 30)
                {
                    validPairsQuantity = nonEncryptedHex.Length / 2;
                    finish = true;
                    
                    // Fills the rest of the block with zeros. The block must have 15 digits
                    nonEncryptedHex += new string('0', 30 - nonEncryptedHex.Length);
                }

                // Gets 30 digits for the first block
                string currentString = nonEncryptedHex.Substring(0, 30);
                int currentStringLength = currentString.Length;

                // Removes the current string from the inital hex string
                nonEncryptedHex = nonEncryptedHex.Substring(currentStringLength);

                // On the last pair the first nibble indicates the number of valid data pairs within that block 
                string validPairs = validPairsQuantity.ToString("X");

                // Converts the second nibble to hexadecimal
                string sequence = i.ToString("X");

                // Appends the current string, the number of valid blocks in it and its sequence
                finalString += currentString + validPairs + sequence;

                if (finish == true)
                {
                    break;
                }
                else if (i == 15)
                {
                    i = 0;
                }
            }

            byte[] nonEncryptedAdjustedBytes = StringToByteArray(finalString);
            string result = Encoding.ASCII.GetString(nonEncryptedAdjustedBytes);


            return nonEncryptedAdjustedBytes;

        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] GenerateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return hashBytes;
            }
        }
        public static string GenerateMD5String(string input)
        {
            byte[] hashBytes = GenerateMD5(input);

            string result = Convert.ToHexString(hashBytes);

            return result;
        }
    }
}
