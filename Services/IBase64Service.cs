namespace Beb64.GUI.Services
{
    public interface IBase64Service
    {
        string Encode(string input);
        string Decode(string base64Input);
        bool TryDecode(string base64Input, out string result, out string error);
        Task<string> EncodeAsync(string input, IProgress<double> progress = null);
        Task EncodeFileToBase64Async(string inputFile, string outputFile, IProgress<double> progress = null, int bufferSize = 1024 * 1024);
        Task DecodeBase64FileAsync(string inputFile, string outputFile, IProgress<double>? progress = null, int bufferSize = 1024 * 1024);
    }
}
