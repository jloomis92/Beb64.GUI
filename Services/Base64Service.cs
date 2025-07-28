using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

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

        // Streaming encode for string input (memory efficient)
        public async Task<string> EncodeAsync(string input, IProgress<double> progress = null, int bufferSize = 1024 * 1024)
        {
            if (string.IsNullOrEmpty(input))
            {
                progress?.Report(100);
                return string.Empty;
            }

            using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            using var outputStream = new MemoryStream();
            await EncodeStreamToBase64Async(inputStream, outputStream, progress, bufferSize);
            outputStream.Position = 0;
            using var reader = new StreamReader(outputStream);
            return await reader.ReadToEndAsync();
        }

        // Add this overload to implement the interface method
        public Task<string> EncodeAsync(string input, IProgress<double> progress)
        {
            return EncodeAsync(input, progress, 1024 * 1024);
        }

        // Streaming encode for file input/output
        public async Task EncodeFileToBase64Async(string inputFile, string outputFile, IProgress<double> progress = null, int bufferSize = 1024 * 1024)
        {
            using var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            await EncodeStreamToBase64Async(input, output, progress, bufferSize, input.Length);
        }

        // To encode a file to Base64 (binary-safe)
        public async Task<string> EncodeFileToBase64StringAsync(string inputFile, IProgress<double>? progress = null)
        {
            const int bufferSize = 81920;
            using var input = File.OpenRead(inputFile);
            using var ms = new MemoryStream();
            var buffer = new byte[bufferSize];
            int read;
            long totalRead = 0;
            long length = input.Length;

            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
                totalRead += read;
                progress?.Report(length > 0 ? (double)totalRead / length * 100 : 0);
            }

            var base64 = Convert.ToBase64String(ms.ToArray());
            return base64;
        }

        // Core streaming logic
        private async Task EncodeStreamToBase64Async(Stream input, Stream output, IProgress<double> progress, int bufferSize, long totalLength = -1)
        {
            totalLength = totalLength < 0 ? input.Length : totalLength;
            long processed = 0;

            using var base64Transform = new System.Security.Cryptography.ToBase64Transform();
            using var cryptoStream = new CryptoStream(output, base64Transform, CryptoStreamMode.Write, leaveOpen: true);

            var buffer = new byte[bufferSize];
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, read);
                processed += read;
                progress?.Report(totalLength > 0 ? (double)processed / totalLength * 100 : 100);
            }
            await cryptoStream.FlushAsync();
            progress?.Report(100);
        }

        // To decode Base64 back to a file (binary-safe)
        public async Task DecodeBase64FileAsync(string inputFile, string outputFile, IProgress<double>? progress = null, int bufferSize = 1024 * 1024)
        {
            long totalRead = 0;
            long totalLength = new FileInfo(inputFile).Length;

            using var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

            // Check for UTF-8 BOM and skip if present
            byte[] bom = new byte[3];
            int bomRead = await input.ReadAsync(bom, 0, 3);
            if (bomRead == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                totalRead += 3;
                // BOM skipped, continue as normal
            }
            else
            {
                // No BOM, rewind to start
                input.Seek(0, SeekOrigin.Begin);
            }

            // Buffer for valid Base64 characters
            var base64Buffer = new StringBuilder(bufferSize * 2);
            var byteBuffer = new byte[bufferSize];
            int bytesRead;
            int invalidCount = 0;
            const int maxInvalidToLog = 20; // Only log the first 20 invalids for brevity

            while ((bytesRead = await input.ReadAsync(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                // Filter only valid Base64 characters
                for (int i = 0; i < bytesRead; i++)
                {
                    char c = (char)byteBuffer[i];
                    if ((c >= 'A' && c <= 'Z') ||
                        (c >= 'a' && c <= 'z') ||
                        (c >= '0' && c <= '9') ||
                        c == '+' || c == '/' || c == '=')
                    {
                        base64Buffer.Append(c);
                    }
                    else
                    {
                        if (invalidCount < maxInvalidToLog)
                        {
                            Debug.WriteLine($"Invalid Base64 char: 0x{(int)c:X2} ('{(char.IsControl(c) ? '?' : c)}') at byte offset {totalRead + i}");
                        }
                        invalidCount++;
                    }
                }
                if (invalidCount > 0)
                    Debug.WriteLine($"Total invalid Base64 characters found: {invalidCount}");
                
                // Decode only complete 4-char blocks
                int toDecodeLen = (base64Buffer.Length / 4) * 4;
                if (toDecodeLen > 0)
                {
                    string toDecode = base64Buffer.ToString(0, toDecodeLen);
                    try
                    {
                        byte[] decoded = Convert.FromBase64String(toDecode);
                        await output.WriteAsync(decoded, 0, decoded.Length);
                    }
                    catch (FormatException ex)
                    {
                        throw new InvalidOperationException($"Invalid Base64 chunk: {ex.Message}");
                    }
                    base64Buffer.Remove(0, toDecodeLen);
                }

                totalRead += bytesRead;
                progress?.Report(totalLength > 0 ? (double)totalRead / totalLength * 100 : 100);
            }

            // Decode any remaining Base64 (pad if needed)
            if (base64Buffer.Length > 0)
            {
                string toDecode = base64Buffer.ToString();
                int mod4 = toDecode.Length % 4;
                if (mod4 != 0)
                    toDecode = toDecode.PadRight(toDecode.Length + (4 - mod4), '=');

                try
                {
                    byte[] decoded = Convert.FromBase64String(toDecode);
                    await output.WriteAsync(decoded, 0, decoded.Length);
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException($"Invalid Base64 at end of file: {ex.Message}");
                }
            }

            progress?.Report(100);
        }
    }
}
