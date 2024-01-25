using System;
using System.IO;
using System.Security.Cryptography;

class Encryption
{
    public static string DecryptData(string key, string encryptedString)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedString);

        using Aes aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);

        byte[] iv = new byte[16];
        Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);

        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream memoryStream = new(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
        using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
        using StreamReader streamReader = new(cryptoStream);

        return streamReader.ReadToEnd();
    }

    static string EncryptData(string key, string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();

        byte[] iv = aes.IV;

        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
        using (StreamWriter streamWriter = new(cryptoStream))
        {
            memoryStream.Write(iv, 0, iv.Length);
            streamWriter.Write(plainText);
        }

        string encryptedString = Convert.ToBase64String(memoryStream.ToArray());
        return encryptedString;
    }
}