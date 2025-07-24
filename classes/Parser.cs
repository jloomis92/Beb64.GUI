using System;
using System.Text.RegularExpressions;

namespace beb64
{
    public class Parser
    {
        // Parses menu choice ("1" or "2") and blocks common injection patterns
        public string? ParseMenuChoice(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            string parsed = input.Trim().ToLower();

            // Block common injection patterns
            if (Regex.IsMatch(parsed, @"(;|--|\bexec\b|\bdrop\b|\bselect\b|\binsert\b|\bdelete\b|<script>|</script>)", RegexOptions.IgnoreCase))
            {
                return null;
            }

            if (!Regex.IsMatch(parsed, @"^[a-zA-Z0-9]+$"))
            {
                return null;
            }

            if (parsed == "1" || parsed == "2" || parsed == "q")
            {
                return parsed;
            }

            return null;
        }

        // Parses user input for encoding/decoding
        public string? ParseGeneralInput(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            string parsed = input.Trim();

            // Block common injection patterns
            if (Regex.IsMatch(parsed, @"(;|--|\bexec\b|\bdrop\b|\bselect\b|\binsert\b|\bdelete\b|<script>|</script>)", RegexOptions.IgnoreCase))
            {
                return null;
            }

            // Allow letters, digits, spaces, dashes, and underscores
            if (!Regex.IsMatch(parsed, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                return null;
            }

            return parsed;
        }
    }
}