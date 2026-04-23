using System.Text;

namespace Chatter.Rest.UriTemplates;

internal static class UriTemplateExpander
{
    internal static string Expand(UriTemplateExpression expression, IDictionary<string, string> variables)
    {
        var op = expression.Operator;
        var parts = new List<string>();

        foreach (var varName in expression.Variables)
        {
            if (!variables.TryGetValue(varName, out var value))
            {
                // Undefined: omit
                continue;
            }

            if (value.Length == 0)
            {
                // Empty value: apply operator-specific empty-value rule
                parts.Add(FormatEmpty(op, varName));
            }
            else
            {
                // Non-empty value: encode and format
                var encoded = Encode(op, value);
                parts.Add(FormatValue(op, varName, encoded));
            }
        }

        if (parts.Count == 0)
        {
            return "";
        }

        var separator = GetSeparator(op);
        var joined = JoinParts(parts, separator);
        var prefix = GetPrefix(op);

        if (prefix.Length > 0)
        {
            return prefix + joined;
        }

        return joined;
    }

    private static string FormatEmpty(UriTemplateOperator op, string varName)
    {
        switch (op)
        {
            case UriTemplateOperator.None:
            case UriTemplateOperator.Plus:
                return "";
            case UriTemplateOperator.Hash:
                return "";
            case UriTemplateOperator.Dot:
                return "";
            case UriTemplateOperator.Slash:
                return "";
            case UriTemplateOperator.Semicolon:
                return varName;
            case UriTemplateOperator.Query:
            case UriTemplateOperator.Ampersand:
                return varName + "=";
            default:
                return "";
        }
    }

    private static string FormatValue(UriTemplateOperator op, string varName, string encodedValue)
    {
        switch (op)
        {
            case UriTemplateOperator.None:
            case UriTemplateOperator.Plus:
            case UriTemplateOperator.Hash:
            case UriTemplateOperator.Dot:
            case UriTemplateOperator.Slash:
                return encodedValue;
            case UriTemplateOperator.Semicolon:
            case UriTemplateOperator.Query:
            case UriTemplateOperator.Ampersand:
                return varName + "=" + encodedValue;
            default:
                return encodedValue;
        }
    }

    private static string Encode(UriTemplateOperator op, string value)
    {
        switch (op)
        {
            case UriTemplateOperator.Plus:
            case UriTemplateOperator.Hash:
                return EncodeReserved(value);
            default:
                return EncodeUnreserved(value);
        }
    }

    private static string GetPrefix(UriTemplateOperator op)
    {
        switch (op)
        {
            case UriTemplateOperator.Hash: return "#";
            case UriTemplateOperator.Dot: return ".";
            case UriTemplateOperator.Slash: return "/";
            case UriTemplateOperator.Semicolon: return ";";
            case UriTemplateOperator.Query: return "?";
            case UriTemplateOperator.Ampersand: return "&";
            default: return "";
        }
    }

    private static string GetSeparator(UriTemplateOperator op)
    {
        switch (op)
        {
            case UriTemplateOperator.None:
            case UriTemplateOperator.Plus:
            case UriTemplateOperator.Hash:
                return ",";
            case UriTemplateOperator.Dot:
                return ".";
            case UriTemplateOperator.Slash:
                return "/";
            case UriTemplateOperator.Semicolon:
                return ";";
            case UriTemplateOperator.Query:
            case UriTemplateOperator.Ampersand:
                return "&";
            default:
                return ",";
        }
    }

    private static string JoinParts(List<string> parts, string separator)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }
            sb.Append(parts[i]);
        }
        return sb.ToString();
    }

    private static string EncodeUnreserved(string value)
    {
        var sb = new StringBuilder();
        var bytes = Encoding.UTF8.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            var c = (char)b;
            if (IsUnreservedChar(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%');
                sb.Append(b.ToString("X2"));
            }
        }
        return sb.ToString();
    }

    private static string EncodeReserved(string value)
    {
        var sb = new StringBuilder();
        var bytes = Encoding.UTF8.GetBytes(value);
        var i = 0;
        while (i < bytes.Length)
        {
            var c = (char)bytes[i];

            // Check for existing pct-encoded sequence: %XX
            if (c == '%' && i + 2 < bytes.Length && IsHexDigit((char)bytes[i + 1]) && IsHexDigit((char)bytes[i + 2]))
            {
                sb.Append((char)bytes[i]);
                sb.Append((char)bytes[i + 1]);
                sb.Append((char)bytes[i + 2]);
                i += 3;
                continue;
            }

            if (IsUnreservedChar(c) || IsReservedChar(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%');
                sb.Append(bytes[i].ToString("X2"));
            }

            i++;
        }
        return sb.ToString();
    }

    private static bool IsUnreservedChar(char c)
    {
        return (c >= 'A' && c <= 'Z') ||
               (c >= 'a' && c <= 'z') ||
               (c >= '0' && c <= '9') ||
               c == '-' || c == '.' || c == '_' || c == '~';
    }

    private static bool IsReservedChar(char c)
    {
        // gen-delims: : / ? # [ ] @
        // sub-delims: ! $ & ' ( ) * + , ; =
        // Also % for pct-encoded passthrough
        switch (c)
        {
            case ':':
            case '/':
            case '?':
            case '#':
            case '[':
            case ']':
            case '@':
            case '!':
            case '$':
            case '&':
            case '\'':
            case '(':
            case ')':
            case '*':
            case '+':
            case ',':
            case ';':
            case '=':
            case '%':
                return true;
            default:
                return false;
        }
    }

    private static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
    }
}
