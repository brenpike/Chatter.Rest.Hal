namespace Chatter.Rest.UriTemplates;

public sealed class UriTemplate
{
    private readonly IReadOnlyList<object> _tokens;

    public UriTemplate(string template)
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        _tokens = UriTemplateParser.Parse(template);
    }

    public string Expand(IDictionary<string, string> variables)
    {
        if (variables is null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        var sb = new System.Text.StringBuilder();

        foreach (var token in _tokens)
        {
            if (token is string literal)
            {
                sb.Append(literal);
            }
            else if (token is UriTemplateExpression expression)
            {
                sb.Append(UriTemplateExpander.Expand(expression, variables));
            }
        }

        return sb.ToString();
    }

    public string Expand(params (string Key, string Value)[] variables)
    {
        if (variables is null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        var dict = new Dictionary<string, string>();

        foreach (var (key, value) in variables)
        {
            // First-wins for duplicates
            if (!dict.ContainsKey(key))
            {
                dict[key] = value;
            }
        }

        return Expand(dict);
    }

    public IReadOnlyList<string> GetVariables()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var token in _tokens)
        {
            if (token is UriTemplateExpression expression)
            {
                foreach (var varName in expression.Variables)
                {
                    if (seen.Add(varName))
                    {
                        result.Add(varName);
                    }
                }
            }
        }

        return result;
    }
}
