using System;
using System.IO;
using System.Text;

namespace BeB64GUI.Services
{
    public static class Encoder
    {
        public static string EncodeToBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes);
        }

        public static string DecodeFromBase64(string base64Input)
        {
            if (string.IsNullOrEmpty(base64Input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(base64Input));

            byte[] bytes = Convert.FromBase64String(base64Input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static string DecodeFromBase64StrictUtf8(string base64Input)
        {
            if (string.IsNullOrEmpty(base64Input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(base64Input));

            byte[] bytes = Convert.FromBase64String(base64Input);

            // strict UTF-8: throw if bytes can't be decoded
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                return utf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                throw new InvalidDataException("Decoded bytes are not valid UTF-8 text.");
            }
        }
    }
}
