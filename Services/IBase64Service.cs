namespace Beb64.GUI.Services
{
    public interface IBase64Service
    {
        string Encode(string input);
        string Decode(string base64Input);
        bool TryDecode(string base64Input, out string result, out string error);
    }
}
