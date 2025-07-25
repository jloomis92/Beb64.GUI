using System;
using System.Text;

namespace Beb64.GUI.Services
{
    public class Base64Service : IBase64Service
    {
        public string Encode(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public string Decode(string base64Input)
        {
            var bytes = Convert.FromBase64String(base64Input);
            return Encoding.UTF8.GetString(bytes);
        }

        public bool TryDecode(string base64Input, out string result, out string error)
        {
            result = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(base64Input))
            {
                error = "Empty input";
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64Input);
                // strict UTF8 to surface invalid sequences
                result = Encoding.GetEncoding("UTF-8", new EncoderExceptionFallback(), new DecoderExceptionFallback())
                                 .GetString(bytes);
                return true;
            }
            catch (FormatException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (DecoderFallbackException ex)
            {
                error = $"Invalid UTF-8: {ex.Message}";
                return false;
            }
        }
    }
}
