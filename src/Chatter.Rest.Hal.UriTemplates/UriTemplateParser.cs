namespace Chatter.Rest.Hal.UriTemplates;

internal static class UriTemplateParser
{
    private static readonly char[] OperatorChars = { '+', '#', '.', '/', ';', '?', '&' };

    internal static IReadOnlyList<object> Parse(string template)
    {
        var tokens = new List<object>();
        var pos = 0;

        while (pos < template.Length)
        {
            var openIndex = template.IndexOf('{', pos);

            if (openIndex < 0)
            {
                // No more expressions; rest is literal
                tokens.Add(template.Substring(pos));
                break;
            }

            // Emit literal text before the '{'
            if (openIndex > pos)
            {
                tokens.Add(template.Substring(pos, openIndex - pos));
            }

            // Find matching '}'
            var closeIndex = template.IndexOf('}', openIndex + 1);

            if (closeIndex < 0)
            {
                throw new FormatException("Unclosed '{' in URI template.");
            }

            // Check for nested '{' between openIndex+1 and closeIndex
            var nestedOpen = template.IndexOf('{', openIndex + 1);
            if (nestedOpen >= 0 && nestedOpen < closeIndex)
            {
                throw new FormatException("Nested '{' are not allowed in URI templates.");
            }

            var content = template.Substring(openIndex + 1, closeIndex - openIndex - 1);

            if (content.Length == 0)
            {
                throw new FormatException("Empty expression '{}' is not valid in a URI template.");
            }

            // Determine operator
            var op = UriTemplateOperator.None;
            var varsPart = content;

            if (content.Length > 0 && Array.IndexOf(OperatorChars, content[0]) >= 0)
            {
                op = MapOperator(content[0]);
                varsPart = content.Substring(1);
            }

            // Split variable names on ','
            var rawNames = varsPart.Split(',');
            var variables = new List<string>(rawNames.Length);

            for (var i = 0; i < rawNames.Length; i++)
            {
                var name = rawNames[i].Trim();

                // Detect Level 4 modifiers
                if (name.IndexOf(':') >= 0 || name.IndexOf('*') >= 0)
                {
                    throw new NotSupportedException(
                        "RFC 6570 Level 4 modifiers (':N' and '*') are not supported. See the backlog for Level 4 implementation status.");
                }

                variables.Add(name);
            }

            tokens.Add(new UriTemplateExpression(op, variables));

            pos = closeIndex + 1;
        }

        return tokens;
    }

    private static UriTemplateOperator MapOperator(char c)
    {
        switch (c)
        {
            case '+': return UriTemplateOperator.Plus;
            case '#': return UriTemplateOperator.Hash;
            case '.': return UriTemplateOperator.Dot;
            case '/': return UriTemplateOperator.Slash;
            case ';': return UriTemplateOperator.Semicolon;
            case '?': return UriTemplateOperator.Query;
            case '&': return UriTemplateOperator.Ampersand;
            default: return UriTemplateOperator.None;
        }
    }
}
