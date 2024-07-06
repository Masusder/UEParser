using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UEParser.Services;
using Sprache;

namespace UEParser.CDNDecoder;

public class DbdDecryption
{
    private static readonly string ASSET_ENCRYPTION_PREFIX = "DbdDAwAC";
    private static readonly string PROFILE_ENCRYPTION_PREFIX = "DbdDAgAC";
    private static readonly byte[] PROFILE_ENCRYPTION_AES_KEY = Encoding.ASCII.GetBytes("5BCC2D6A95D4DF04A005504E59A9B36E");
    private static readonly string ZLIB_COMPRESSION_PREFIX = "DbdDAQEB";

    public static string DecryptCDN(string inputText, string branch)
    {
        if (inputText.StartsWith(ASSET_ENCRYPTION_PREFIX))
        {
            return DecryptDbdAsset(inputText, branch);
        }

        if (inputText.StartsWith(PROFILE_ENCRYPTION_PREFIX))
        {
            return DecryptDbdProfile(inputText, branch);
        }

        if (inputText.StartsWith(ZLIB_COMPRESSION_PREFIX))
        {
            return DecompressDbdZlib(inputText, branch);
        }

        // In case of encryption key not being valid
        // inputText will still go past all above methods without any errors
        // but result will be meaningless string
        // check for that here
        if (!string.IsNullOrEmpty(inputText) && !IsValidJson(inputText))
        {
            throw new Exception("Decrypted data is not a valid JSON. Most likely encryption key is invalid.");
        }

        return inputText;
    }

    private static bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) return true;
        try
        {
            var obj = JToken.Parse(strInput);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    private static string DecryptDbdAsset(string inputText, string branch)
    {
        if (!inputText.StartsWith(ASSET_ENCRYPTION_PREFIX))
        {
            throw new Exception("Input text does not start with " + ASSET_ENCRYPTION_PREFIX);
        }

        var inputTextNoPrefix = inputText[ASSET_ENCRYPTION_PREFIX.Length..];
        var decodedBufferAndKeyId = Convert.FromBase64String(inputTextNoPrefix);

        int branchLength = branch.Length;
        int sliceLength = 7 + branchLength;
        var keyIdBuffer = new byte[sliceLength];
        Array.Copy(decodedBufferAndKeyId, keyIdBuffer, sliceLength);
        for (int i = 0; i < keyIdBuffer.Length; i++)
        {
            keyIdBuffer[i] += 1;
        }

        var resultKeyId = Encoding.ASCII.GetString(keyIdBuffer).Replace("\u0001", "");

        var config = ConfigurationService.Config;

        string ENCRYPTED_KEY = config?.Core.ApiConfig.S3AccessKeys[resultKeyId] ?? throw new Exception("Not found matching keys inside Config file, key: " + resultKeyId);

        byte[] DECRYPTED_KEY = Convert.FromBase64String(ENCRYPTED_KEY ?? "");

        byte[] foundKeyBuffer = DECRYPTED_KEY ?? throw new Exception("Input text is encrypted with the unknown AES key: " + resultKeyId);

        var decodedBuffer = new byte[decodedBufferAndKeyId.Length - sliceLength];
        Array.Copy(decodedBufferAndKeyId, sliceLength, decodedBuffer, 0, decodedBuffer.Length);

        return DecryptDbdSymmetricalInternal(decodedBuffer, foundKeyBuffer, branch);
    }

    private static string DecryptDbdProfile(string inputText, string branch)
    {
        if (!inputText.StartsWith(PROFILE_ENCRYPTION_PREFIX))
        {
            throw new Exception("Input text does not start with " + PROFILE_ENCRYPTION_PREFIX);
        }

        var inputTextNoPrefix = inputText[PROFILE_ENCRYPTION_PREFIX.Length..];
        var decodedBuffer = Convert.FromBase64String(inputTextNoPrefix);

        return DecryptDbdSymmetricalInternal(decodedBuffer, PROFILE_ENCRYPTION_AES_KEY, branch);
    }

    private static string DecryptDbdSymmetricalInternal(byte[] buffer, byte[] encryptionKey, string branch)
    {
        var aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var decryptor = aes.CreateDecryptor();

        var decipheredBuffer = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);

        int validNonPaddingBytes = 0;
        for (int i = 0; i < decipheredBuffer.Length; i++)
        {
            var rawByteValue = decipheredBuffer[i];
            if (rawByteValue != 0)
            {
                var offsetByteValue = (byte)((rawByteValue + 1) % 256);
                decipheredBuffer[i] = offsetByteValue;
                validNonPaddingBytes++;
            }
            else
            {
                break;
            }
        }
        var resultText = Encoding.ASCII.GetString(decipheredBuffer, 0, validNonPaddingBytes);
        return DecryptCDN(resultText, branch);
    }

    private static string DecompressDbdZlib(string inputText, string branch)
    {
        if (!inputText.StartsWith(ZLIB_COMPRESSION_PREFIX))
        {
            throw new Exception("Input does not start with " + ZLIB_COMPRESSION_PREFIX);
        }

        string inputTextNoPrefix = inputText[ZLIB_COMPRESSION_PREFIX.Length..];
        byte[] decodedBufferAndDeflatedLength = Convert.FromBase64String(inputTextNoPrefix);
        int expectedDeflatedDataLength = BitConverter.ToInt32(decodedBufferAndDeflatedLength, 0);

        byte[] inflatedBuffer = ZlibStream.UncompressBuffer(decodedBufferAndDeflatedLength.Skip(4).ToArray());

        if (inflatedBuffer.Length != expectedDeflatedDataLength)
        {
            throw new Exception("Inflated Data Length Mismatch: Expected " + expectedDeflatedDataLength + ", Received " + inflatedBuffer.Length);
        }

        string resultText = Encoding.Unicode.GetString(inflatedBuffer);
        return DecryptCDN(resultText, branch);
    }
}
